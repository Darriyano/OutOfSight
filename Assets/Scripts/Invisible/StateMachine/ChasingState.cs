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

    private const float LoseDelay = 1f;

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

            if (lostTimer >= LoseDelay)
                controller.StartInvestigation(true);
        }
    }

    public void Exit()
    {
        controller.Agent.updateRotation = true;
        controller.Agent.ResetPath();
    }

    private bool TryCatchPlayer()
    {
        if (hasLoggedCaughtPlayer || controller.Target == null)
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
        Vector3 direction = controller.Agent.desiredVelocity;
        if (direction.sqrMagnitude > 0.01f)
            controller.transform.rotation = Quaternion.LookRotation(direction);
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
