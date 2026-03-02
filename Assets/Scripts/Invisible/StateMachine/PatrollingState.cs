using UnityEngine;
using System.Collections.Generic;

public class PatrollingState : IState
{
    private StateController controller;

    List<Room> rooms;
    int recount = 0;
    private int currentRoomIndex = -1;

    private Dictionary<int, int> visitCount = new Dictionary<int, int>();
    private int lastVisitedRoom = -1;

    private List<Transform> currentInspectionPoints;
    private int currentPointIndex;
    private int pointsToCheck;

    private bool inspectingRoom;

    private int minPoints = 2;
    private int maxPoints = 5;

    public PatrollingState(StateController controller)
    {
        this.controller = controller;
    }
    public void Enter()
    {
        rooms = controller.Rooms;
        recount = rooms.Count;
        for (int i = 0; i < recount; i++)
        {
            if (!visitCount.ContainsKey(i))
                visitCount[i] = 0;
        }

        ChooseNextRoom();
    }
    public void Exit()
    {
        controller.Agent.ResetPath();
    }
    public void Update()
    {
        if (controller.Behaviour.CanSeeTarget())
        {
            controller.ChangeState(new ChasingState(controller));
            return;
        }

        if (controller.Agent.pathPending)
            return;

        if (controller.Agent.remainingDistance > controller.Agent.stoppingDistance)
            return;

        if (!inspectingRoom)
        {
            StartInspection();
        }
        else
        {
            GoToNextInspectionPoint();
        }
    }

    private void ChooseNextRoom()
    {
        if (recount == 0)
            return;

        float totalWeight = 0f;
        List<float> weights = new List<float>();

        for (int i = 0; i < recount; i++)
        {
            float weight = 1f / (1f + visitCount[i]);
            weights.Add(weight);
            totalWeight += weight;
        }

        float randomPoint = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < weights.Count; i++)
        {
            cumulative += weights[i];

            if (randomPoint <= cumulative)
            {
                visitCount[i]++;
                controller.Agent.SetDestination(rooms[i].entrance.position);
                if (visitCount[i] > recount + Random.Range(-3, 3))
                    visitCount[i] = Mathf.Max(0, visitCount[i] - 1);
                return;
            }
        }
    }
    private void StartInspection()
    {
        currentInspectionPoints = new List<Transform>(rooms[currentRoomIndex].inspectionPoints);

        if (currentInspectionPoints.Count == 0)
        {
            ChooseNextRoom();
            return;
        }

        pointsToCheck = Random.Range(minPoints, maxPoints + 1);
        currentPointIndex = 0;
        inspectingRoom = true;

        Shuffle(currentInspectionPoints);

        controller.Agent.SetDestination(currentInspectionPoints[currentPointIndex].position);
    }

    private void GoToNextInspectionPoint()
    {
        currentPointIndex++;

        if (currentPointIndex >= pointsToCheck || currentPointIndex >= currentInspectionPoints.Count)
        {
            ChooseNextRoom();
            return;
        }

        controller.Agent.SetDestination(currentInspectionPoints[currentPointIndex].position);
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}