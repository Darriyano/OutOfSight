using UnityEngine;
using UnityEngine.AI;

public class PatrollingState : IState
{
    private readonly StateController controller;
    private float waitTimer;
    private float currentWaitDuration;
    private bool waitingAtPoint;
    private bool hasDestination;

    public PatrollingState(StateController controller)
    {
        this.controller = controller;
    }

    public void Enter()
    {
        controller.Agent.speed = InvisibleParameters.Instance.UsualSpeed;
        controller.Agent.isStopped = false;
        waitTimer = 0f;
        waitingAtPoint = false;
        hasDestination = false;
        ChooseNextPoint(forceAnyDistance: true);
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

        if (!hasDestination)
        {
            ChooseNextPoint(forceAnyDistance: true);
            return;
        }

        if (controller.Agent.remainingDistance > controller.Agent.stoppingDistance + 0.05f)
            return;

        if (!waitingAtPoint)
        {
            waitingAtPoint = true;
            waitTimer = 0f;
            currentWaitDuration = Random.Range(
                InvisibleParameters.Instance.PatrolWaitMin,
                InvisibleParameters.Instance.PatrolWaitMax);
        }

        waitTimer += Time.deltaTime;
        if (waitTimer < currentWaitDuration)
            return;

        ChooseNextPoint(forceAnyDistance: false);
    }

    private void ChooseNextPoint(bool forceAnyDistance)
    {
        waitingAtPoint = false;
        waitTimer = 0f;

        Vector3 origin = controller.transform.position;
        float radius = Mathf.Max(1f, InvisibleParameters.Instance.PatrolWanderRadius);
        float minDistance = forceAnyDistance ? 0f : Mathf.Max(0f, InvisibleParameters.Instance.PatrolMinMoveDistance);
        int attempts = Mathf.Max(3, InvisibleParameters.Instance.PatrolSampleAttempts);

        for (int i = 0; i < attempts; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 candidate = origin + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, radius * 0.75f, NavMesh.AllAreas))
                continue;

            if (!forceAnyDistance && Vector3.Distance(origin, navHit.position) < minDistance)
                continue;

            controller.Agent.SetDestination(navHit.position);
            hasDestination = true;
            return;
        }

        hasDestination = false;
        controller.Agent.ResetPath();
    }
}
