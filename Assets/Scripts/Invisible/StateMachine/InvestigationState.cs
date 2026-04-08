using System.Collections.Generic;
using Game.Interaction;
using UnityEngine;

public class InvestigationState : IState
{
    private class InvestigationPoint
    {
        public Vector3 PointPosition;
        public HidingSpotInteractable HidingSpot;
    }

    private readonly StateController controller;
    private readonly bool shouldInspectHidingSpots;
    private readonly HidingSpotInteractable guaranteedSpot;
    private readonly HashSet<HidingSpotInteractable> addedHidingSpots = new HashSet<HidingSpotInteractable>();

    private readonly List<InvestigationPoint> checkPoints = new List<InvestigationPoint>();
    private int currentPointIndex;
    private float waitTimer;
    private float investigationTimer;
    private float maxInvestigationTime;
    private int fixedPointCount;
    private bool hasPrioritySearchOrigin;
    private Vector3 prioritySearchOrigin;

    private const float PointCheckPause = 0.75f;
    private const float HiddenPlayerCheckDistance = 1.25f;

    public InvestigationState(
        StateController controller,
        bool shouldInspectHidingSpots,
        HidingSpotInteractable guaranteedSpot = null)
    {
        this.controller = controller;
        this.shouldInspectHidingSpots = shouldInspectHidingSpots;
        this.guaranteedSpot = guaranteedSpot;
    }

    public void Enter()
    {
        controller.Agent.speed = InvisibleParameters.Instance.InvestigationSpeed;
        controller.Agent.isStopped = false;

        currentPointIndex = 0;
        waitTimer = 0f;
        investigationTimer = 0f;
        maxInvestigationTime = Random.Range(8f, 15f);
        hasPrioritySearchOrigin = controller.TryConsumePrioritySearchArea(out prioritySearchOrigin);

        if (hasPrioritySearchOrigin)
            maxInvestigationTime += controller.InvestigationTimeBonusAfterSighting;

        BuildCheckRoute();

        if (checkPoints.Count == 0)
        {
            controller.ChangeState(new PatrollingState(controller));
            return;
        }

        controller.Agent.SetDestination(checkPoints[currentPointIndex].PointPosition);
        Debug.Log("Investigation");
    }

    public void Update()
    {
        if (controller.Behaviour.CanSeeTarget())
        {
            controller.ChangeState(new ChasingState(controller));
            return;
        }

        investigationTimer += Time.deltaTime;
        if (investigationTimer >= maxInvestigationTime)
        {
            controller.ChangeState(new PatrollingState(controller));
            return;
        }

        if (controller.Agent.pathPending)
            return;

        if (controller.Agent.remainingDistance > controller.Agent.stoppingDistance)
            return;

        waitTimer += Time.deltaTime;
        if (waitTimer < PointCheckPause)
            return;

        waitTimer = 0f;

        if (TryFindHiddenPlayerAtCurrentPoint())
            return;

        currentPointIndex++;
        if (currentPointIndex >= checkPoints.Count)
        {
            controller.ChangeState(new PatrollingState(controller));
            return;
        }

        controller.Agent.SetDestination(checkPoints[currentPointIndex].PointPosition);
    }

    public void Exit()
    {
        controller.Agent.ResetPath();
    }

    private void BuildCheckRoute()
    {
        checkPoints.Clear();
        addedHidingSpots.Clear();
        fixedPointCount = 0;

        AddGuaranteedSpot();
        AddNoisePoint();
        AddPrioritySearchOriginPoint();
        AddPriorityNearbyHidingSpots();

        if (shouldInspectHidingSpots)
            BuildHidingRoute();

        if (checkPoints.Count == 0)
            BuildInspectionRoute();

        ShuffleFromIndex(checkPoints, fixedPointCount);
    }

    private void AddGuaranteedSpot()
    {
        if (guaranteedSpot == null)
            return;

        if (TryAddHidingSpot(guaranteedSpot, guaranteedSpot.transform.position))
            fixedPointCount++;
    }

    private void AddNoisePoint()
    {
        if (!controller.TryConsumePendingNoise(out Vector3 noisePosition))
            return;

        checkPoints.Add(new InvestigationPoint
        {
            PointPosition = noisePosition
        });

        fixedPointCount++;
    }

    private void AddPrioritySearchOriginPoint()
    {
        if (!hasPrioritySearchOrigin)
            return;

        checkPoints.Add(new InvestigationPoint
        {
            PointPosition = prioritySearchOrigin
        });
        fixedPointCount++;
    }

    private void AddPriorityNearbyHidingSpots()
    {
        if (!hasPrioritySearchOrigin || !shouldInspectHidingSpots)
            return;

        List<Room> rooms = controller.Rooms;
        if (rooms == null)
            return;

        float forcedRadius = controller.ForcedHidingInspectionRadiusAfterSighting;
        float forcedRadiusSqr = forcedRadius * forcedRadius;

        for (int i = 0; i < rooms.Count; i++)
        {
            Room room = rooms[i];
            if (room == null || room.hidingPoints == null)
                continue;

            for (int j = 0; j < room.hidingPoints.Count; j++)
            {
                Transform hidingPoint = room.hidingPoints[j];
                if (hidingPoint == null)
                    continue;

                if ((hidingPoint.position - prioritySearchOrigin).sqrMagnitude > forcedRadiusSqr)
                    continue;

                HidingSpotInteractable hidingSpot = ResolveHidingSpot(hidingPoint);
                if (hidingSpot == null)
                    continue;

                if (TryAddHidingSpot(hidingSpot, hidingPoint.position))
                    fixedPointCount++;
            }
        }
    }

    private bool TryFindHiddenPlayerAtCurrentPoint()
    {
        if (controller.Target == null || currentPointIndex < 0 || currentPointIndex >= checkPoints.Count)
            return false;

        PlayerHiding playerHiding = controller.Target.GetComponent<PlayerHiding>();
        if (playerHiding == null)
            playerHiding = controller.Target.GetComponentInParent<PlayerHiding>();

        if (playerHiding == null || !playerHiding.IsHidden)
            return false;

        InvestigationPoint point = checkPoints[currentPointIndex];
        if (point.HidingSpot != null)
        {
            if (playerHiding.CurrentSpot != point.HidingSpot)
                return false;
        }
        else
        {
            float distanceToCheckPoint = Vector3.Distance(point.PointPosition, controller.Target.position);
            if (distanceToCheckPoint > HiddenPlayerCheckDistance)
                return false;
        }

        playerHiding.ForceExitFromHiding();
        controller.ChangeState(new ChasingState(controller));
        return true;
    }

    private void BuildHidingRoute()
    {
        List<Room> rooms = controller.Rooms;
        if (rooms == null)
            return;

        for (int i = 0; i < rooms.Count; i++)
        {
            Room room = rooms[i];
            if (room == null || room.hidingPoints == null)
                continue;

            for (int j = 0; j < room.hidingPoints.Count; j++)
            {
                Transform hidingPoint = room.hidingPoints[j];
                if (hidingPoint == null)
                    continue;

                HidingSpotInteractable hidingSpot = ResolveHidingSpot(hidingPoint);
                if (hidingSpot == null || addedHidingSpots.Contains(hidingSpot) || !hidingSpot.ShouldBeInspected())
                    continue;

                TryAddHidingSpot(hidingSpot, hidingPoint.position);
            }
        }
    }

    private void BuildInspectionRoute()
    {
        List<Room> rooms = controller.Rooms;
        if (rooms == null)
            return;

        for (int i = 0; i < rooms.Count; i++)
        {
            Room room = rooms[i];
            if (room == null || room.inspectionPoints == null)
                continue;

            for (int j = 0; j < room.inspectionPoints.Count; j++)
            {
                Transform inspectionPoint = room.inspectionPoints[j];
                if (inspectionPoint == null)
                    continue;

                checkPoints.Add(new InvestigationPoint
                {
                    PointPosition = inspectionPoint.position
                });
            }
        }
    }

    private HidingSpotInteractable ResolveHidingSpot(Transform hidingPoint)
    {
        HidingSpotInteractable hidingSpot = hidingPoint.GetComponent<HidingSpotInteractable>();
        if (hidingSpot != null)
            return hidingSpot;

        return hidingPoint.GetComponentInParent<HidingSpotInteractable>();
    }

    private void ShuffleFromIndex<T>(List<T> list, int startIndex)
    {
        for (int i = startIndex; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private bool TryAddHidingSpot(HidingSpotInteractable hidingSpot, Vector3 pointPosition)
    {
        if (hidingSpot == null || addedHidingSpots.Contains(hidingSpot))
            return false;

        checkPoints.Add(new InvestigationPoint
        {
            PointPosition = pointPosition,
            HidingSpot = hidingSpot
        });

        addedHidingSpots.Add(hidingSpot);
        return true;
    }
}
