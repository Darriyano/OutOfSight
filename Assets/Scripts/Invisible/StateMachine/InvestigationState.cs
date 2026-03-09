using System.Collections.Generic;
using Game.Interaction;
using UnityEngine;

public class InvestigationState : IState
{
    private class InvestigationPoint
    {
        public Transform PointTransform;
        public HidingSpotInteractable HidingSpot;
    }

    private readonly StateController controller;
    private readonly bool shouldInspectHidingSpots;
    private readonly HidingSpotInteractable guaranteedSpot;

    private readonly List<InvestigationPoint> checkPoints = new List<InvestigationPoint>();
    private int currentPointIndex;
    private float waitTimer;
    private float investigationTimer;
    private float maxInvestigationTime;

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
        controller.Agent.speed = InvisibleParameters.Instance.investigationSpeed;
        controller.Agent.isStopped = false;

        currentPointIndex = 0;
        waitTimer = 0f;
        investigationTimer = 0f;
        maxInvestigationTime = Random.Range(8f, 15f);

        BuildCheckRoute();

        if (checkPoints.Count == 0)
        {
            controller.ChangeState(new PatrollingState(controller));
            return;
        }

        controller.Agent.SetDestination(checkPoints[currentPointIndex].PointTransform.position);
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

        controller.Agent.SetDestination(checkPoints[currentPointIndex].PointTransform.position);
    }

    public void Exit()
    {
        controller.Agent.ResetPath();
    }

    private void BuildCheckRoute()
    {
        checkPoints.Clear();

        AddGuaranteedSpot();

        if (shouldInspectHidingSpots)
            BuildHidingRoute();

        if (checkPoints.Count == 0)
            BuildInspectionRoute();

        ShuffleFromIndex(checkPoints, guaranteedSpot != null ? 1 : 0);
    }

    private void AddGuaranteedSpot()
    {
        if (guaranteedSpot == null)
            return;

        checkPoints.Add(new InvestigationPoint
        {
            PointTransform = guaranteedSpot.transform,
            HidingSpot = guaranteedSpot
        });
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
            float distanceToCheckPoint = Vector3.Distance(point.PointTransform.position, controller.Target.position);
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
                if (hidingSpot == null || hidingSpot == guaranteedSpot || !hidingSpot.ShouldBeInspected())
                    continue;

                checkPoints.Add(new InvestigationPoint
                {
                    PointTransform = hidingPoint,
                    HidingSpot = hidingSpot
                });
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
                    PointTransform = inspectionPoint
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
}
