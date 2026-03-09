using System.Collections.Generic;
using Game.Interaction;
using UnityEngine;
using UnityEngine.AI;

public class StateController : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform target;
    [SerializeField] private InvisibleBehaviour behaviour;
    [SerializeField] private List<Room> rooms;
    [SerializeField] private float catchDistance = 1f;

    private IState currentState;

    public NavMeshAgent Agent => agent;
    public Transform Target => target;
    public InvisibleBehaviour Behaviour => behaviour;
    public List<Room> Rooms => rooms;
    public float CatchDistance => catchDistance;

    private void Awake()
    {
        IgnoreTargetCollisions();
    }

    private void Start()
    {
        ChangeState(new PatrollingState(this));
    }

    private void Update()
    {
        currentState?.Update();
    }

    public void ChangeState(IState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    public void StartInvestigation(bool shouldInspectHidingSpots, HidingSpotInteractable guaranteedSpot = null)
    {
        ChangeState(new InvestigationState(this, shouldInspectHidingSpots, guaranteedSpot));
    }

    public void StartInvestigationFromNoise()
    {
        StartInvestigation(true);
    }

    private void IgnoreTargetCollisions()
    {
        if (target == null)
            return;

        Collider[] selfColliders = GetComponentsInChildren<Collider>(true);
        Collider[] targetColliders = target.GetComponentsInChildren<Collider>(true);

        if (selfColliders.Length == 0 || targetColliders.Length == 0)
            return;

        for (int i = 0; i < selfColliders.Length; i++)
        {
            Collider selfCollider = selfColliders[i];
            if (selfCollider == null)
                continue;

            for (int j = 0; j < targetColliders.Length; j++)
            {
                Collider targetCollider = targetColliders[j];
                if (targetCollider == null)
                    continue;

                Physics.IgnoreCollision(selfCollider, targetCollider, true);
            }
        }
    }
}

public interface IState
{
    void Enter();
    void Update();
    void Exit();
}

[System.Serializable]
public class Room
{
    public Transform entrance;
    public List<Transform> inspectionPoints;
    public List<Transform> hidingPoints;
}
