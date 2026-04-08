using Game.Interaction;
using UnityEngine;

public class ChasingState : IState
{
    private readonly StateController controller;

    private Vector3 lastSeenPosition;
    private float lostTimer;
    private bool waitingAfterLost;
    private bool hadVisualContact;
    private bool hasLoggedCaughtPlayer;
    private float originalAcceleration;
    private float originalAngularSpeed;
    private float originalStoppingDistance;
    private bool originalAutoBraking;

    public ChasingState(StateController controller)
    {
        this.controller = controller;
    }

    public void Enter()
    {
        originalAcceleration = controller.Agent.acceleration;
        originalAngularSpeed = controller.Agent.angularSpeed;
        originalStoppingDistance = controller.Agent.stoppingDistance;
        originalAutoBraking = controller.Agent.autoBraking;

        controller.Agent.speed = InvisibleParameters.Instance.ChaseSpeed;
        controller.Agent.acceleration = controller.ChaseAcceleration;
        controller.Agent.angularSpeed = controller.ChaseAngularSpeed;
        controller.Agent.stoppingDistance = controller.ChaseStoppingDistance;
        controller.Agent.autoBraking = controller.ChaseAutoBraking;
        controller.Agent.isStopped = false;
        controller.Agent.updateRotation = false;
        waitingAfterLost = false;
        lostTimer = 0f;
        hadVisualContact = false;
        hasLoggedCaughtPlayer = false;

        if (controller.Target != null)
            lastSeenPosition = controller.Target.position;

        Debug.Log("Chase");
    }

    public void Update()
    {
        if (TryCatchPlayer())
            return;

        if (controller.Behaviour.CanSeeTarget())
        {
            lastSeenPosition = controller.Target.position;
            controller.Agent.SetDestination(lastSeenPosition);
            hadVisualContact = true;

            RotateTowardsMovement();

            waitingAfterLost = false;
            lostTimer = 0f;
            return;
        }

        HidingSpotInteractable guaranteedSpot = hadVisualContact ? GetGuaranteedHidingSpot() : null;
        if (guaranteedSpot != null)
        {
            controller.SetPrioritySearchArea(lastSeenPosition);
            controller.StartInvestigation(true, guaranteedSpot);
            return;
        }

        controller.Agent.SetDestination(lastSeenPosition);
        RotateTowardsMovement();

        if (!waitingAfterLost && !controller.Agent.pathPending &&
            controller.Agent.remainingDistance <= controller.Agent.stoppingDistance)
        {
            waitingAfterLost = true;
        }

        if (waitingAfterLost)
        {
            lostTimer += Time.deltaTime;

            if (lostTimer >= controller.ChaseLoseDelay)
            {
                controller.SetPrioritySearchArea(lastSeenPosition);
                controller.StartInvestigation(true);
            }
        }
    }

    public void Exit()
    {
        controller.Agent.acceleration = originalAcceleration;
        controller.Agent.angularSpeed = originalAngularSpeed;
        controller.Agent.stoppingDistance = originalStoppingDistance;
        controller.Agent.autoBraking = originalAutoBraking;
        controller.Agent.updateRotation = true;
        controller.Agent.ResetPath();
    }

    private bool TryCatchPlayer()
    {
        if (!controller.CatchEnabled || hasLoggedCaughtPlayer || controller.Target == null)
            return false;

        float distanceToPlayer = Vector3.Distance(controller.transform.position, controller.Target.position);
        if (distanceToPlayer > controller.CatchDistance)
            return false;

        hasLoggedCaughtPlayer = true;
        controller.Agent.isStopped = true;
        Debug.Log("Player caught");
        return true;
    }

    private void RotateTowardsMovement()
    {
        Vector3 direction = Vector3.zero;

        if (controller.Target != null && controller.Behaviour.CanSeeTarget())
            direction = controller.Target.position - controller.transform.position;
        else if (controller.Agent.hasPath)
            direction = controller.Agent.steeringTarget - controller.transform.position;
        else
            direction = controller.Agent.desiredVelocity;

        direction.y = 0f;
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            controller.transform.rotation = Quaternion.RotateTowards(
                controller.transform.rotation,
                targetRotation,
                controller.ChaseAngularSpeed * Time.deltaTime);
        }
    }

    private HidingSpotInteractable GetGuaranteedHidingSpot()
    {
        if (controller.Target == null)
            return null;

        PlayerHiding playerHiding = controller.Target.GetComponent<PlayerHiding>();
        if (playerHiding == null)
            playerHiding = controller.Target.GetComponentInParent<PlayerHiding>();

        if (playerHiding == null || !playerHiding.IsHidden)
            return null;

        return playerHiding.CurrentSpot;
    }
}
