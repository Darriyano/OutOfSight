using System.Collections.Generic;
using Game.Interaction;
using UnityEngine;
using UnityEngine.AI;

public class StateController : MonoBehaviour, INoiseListener
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform target;
    [SerializeField] private InvisibleBehaviour behaviour;
    [SerializeField] private List<Room> rooms;

    private IState currentState;
    private bool hasPendingNoise;
    private Vector3 pendingNoisePosition;
    private float pendingNoiseStrength;

    public NavMeshAgent Agent => agent;
    public Transform Target => target;
    public InvisibleBehaviour Behaviour => behaviour;
    public List<Room> Rooms => rooms;
    public IState CurrentState => currentState;
    public float CatchDistance => InvisibleParameters.Instance.CatchDistance;
    public bool CatchEnabled => InvisibleParameters.Instance.CatchEnabled;
    public float ChaseStoppingDistance => InvisibleParameters.Instance.ChaseStoppingDistance;
    public float ChaseAcceleration => InvisibleParameters.Instance.ChaseAcceleration;
    public float ChaseAngularSpeed => InvisibleParameters.Instance.ChaseAngularSpeed;
    public bool ChaseAutoBraking => InvisibleParameters.Instance.ChaseAutoBraking;
    public Component NoiseListenerComponent => this;
    public Vector3 HearingPosition => transform.position;
    public float HearingCaptureRadius => InvisibleParameters.Instance.NoiseListenerCaptureRadius;
    public float MinimumHeardNoiseStrength => InvisibleParameters.Instance.MinHeardNoiseStrength;
    public bool CanReceiveNoise => isActiveAndEnabled && agent != null;

    private void Awake()
    {
        IgnoreTargetCollisions();
    }

    private void OnEnable()
    {
        NoiseSystem.RegisterListener(this);
    }

    private void OnDisable()
    {
        NoiseSystem.UnregisterListener(this);
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

    public void StartInvestigationFromNoise(Vector3 noisePosition, bool shouldInspectHidingSpots = true)
    {
        SetPendingNoise(noisePosition, 1f);
        StartInvestigation(shouldInspectHidingSpots);
    }

    public bool TryConsumePendingNoise(out Vector3 noisePosition)
    {
        if (!hasPendingNoise)
        {
            noisePosition = Vector3.zero;
            return false;
        }

        hasPendingNoise = false;
        pendingNoiseStrength = 0f;
        noisePosition = pendingNoisePosition;
        return true;
    }

    public void OnNoiseHeard(NoiseHeardInfo heardNoise)
    {
        if (!CanReceiveNoise)
            return;

        if (behaviour != null && behaviour.CanSeeTarget())
            return;

        if (currentState is ChasingState)
            return;

        SetPendingNoise(heardNoise.SourcePosition, heardNoise.Strength);
        StartInvestigation(shouldInspectHidingSpots: true);
    }

    private void SetPendingNoise(Vector3 noisePosition, float noiseStrength)
    {
        if (hasPendingNoise && noiseStrength < pendingNoiseStrength)
            return;

        hasPendingNoise = true;
        pendingNoisePosition = noisePosition;
        pendingNoiseStrength = noiseStrength;
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
