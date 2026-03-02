using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class ChasingState : IState
{
    private StateController controller;

    private Vector3 lastSeenPosition;
    private float lostTimer;
    private bool waitingAfterLost;

    private const float loseDelay = 1f;

    public ChasingState(StateController controller)
    {
        this.controller = controller;
    }

    public void Enter()
    {
        controller.Agent.isStopped = false;
        controller.Agent.updateRotation = false;
        waitingAfterLost = false;
        lostTimer = 0f;
        Debug.Log("Chase");
    }

    public void Update()
    {
        if (controller.Behaviour.CanSeeTarget())
        {
            lastSeenPosition = controller.Target.position;

            waitingAfterLost = false;
            lostTimer = 0f;
            return;
        }

        controller.Agent.SetDestination(lastSeenPosition);
        Vector3 direction = controller.Agent.desiredVelocity;

        if (direction.sqrMagnitude > 0.01f)
        {
            controller.transform.rotation = Quaternion.LookRotation(direction);
        }

        if (!waitingAfterLost && !controller.Agent.pathPending &&
            controller.Agent.remainingDistance <= controller.Agent.stoppingDistance)
        {
            waitingAfterLost = true;
        }

        if (waitingAfterLost)
        {
            lostTimer += Time.deltaTime;

            if (lostTimer >= loseDelay)
            {
                controller.ChangeState(new InvestigationState(controller));
            }
        }
    }

    public void Exit()
    {
        controller.Agent.updateRotation = true;
        controller.Agent.ResetPath();
    }
}
