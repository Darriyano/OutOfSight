using UnityEngine;

[CreateAssetMenu(fileName = "InvisibleParameters", menuName = "Scriptable Objects/InvisibleParameters")]
public class InvisibleParameters : ScriptableObject
{
    private static InvisibleParameters instance;

    public static InvisibleParameters Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<InvisibleParameters>("InvisibleParameters");
                if (instance == null)
                    Debug.LogError("InvisibleParameters asset not found in Resources!");
            }

            return instance;
        }
    }

    [Header("Vision")]
    [SerializeField] private float viewDistance = 15f;
    [SerializeField] private float baseViewAngle = 180f;
    [SerializeField] private float focusedViewAngle = 60f;

    [Header("Movement Speeds")]
    [SerializeField] private float slowSpeed = 3f;
    [SerializeField] private float usualSpeed = 7f;
    [SerializeField] private float investigationSpeed = 10f;
    [SerializeField] private float chaseSpeed = 30f;

    [Header("Chase Tuning")]
    [SerializeField] private float catchDistance = 1f;
    [SerializeField] private bool catchEnabled = true;
    [SerializeField] private float chaseStoppingDistance = 0.2f;
    [SerializeField] private float chaseAcceleration = 80f;
    [SerializeField] private float chaseAngularSpeed = 1080f;
    [SerializeField] private bool chaseAutoBraking = true;
    [SerializeField] private float chaseLoseDelay = 2.5f;

    [Header("Post-Sighting Pressure")]
    [SerializeField] private float forcedHidingInspectionRadiusAfterSighting = 5f;
    [SerializeField] private float investigationTimeBonusAfterSighting = 4f;

    [Header("Audio - Ambient")]
    [SerializeField] private float nearMonsterDistance = 12f;
    [SerializeField] private float maxNearMonsterAmbientVolume = 1f;
    [SerializeField] private float ambientVolumeLerpSpeed = 5f;

    [Header("Audio - Footsteps")]
    [SerializeField] private float footstepStartSpeed = 0.15f;
    [SerializeField] private float footstepMaxVolume = 1f;
    [SerializeField] private float footstepPitchAtUsualSpeed = 1f;

    [Header("Audio - Occlusion")]
    [SerializeField] private bool blockAmbientThroughWalls = true;
    [SerializeField] private float audioRaycastHeight = 1.4f;
    [SerializeField] [Range(0f, 1f)] private float footstepTransmissionPerObstacle = 0.55f;

    [Header("Screen FX")]
    [SerializeField] private float nearMonsterScreenFxDistance = 10f;
    [SerializeField] private float maxVignetteIntensity = 0.45f;
    [SerializeField] private float maxDarkeningExposure = -1.2f;
    [SerializeField] private float screenFxLerpSpeed = 5f;
    [SerializeField] private bool pulseScreenFx = true;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float pulseAmount = 0.15f;

    [Header("Noise Hearing")]
    [SerializeField] private float noiseListenerCaptureRadius = 1.5f;
    [SerializeField] private float minHeardNoiseStrength = 0.1f;

    [Header("Patrol Wander")]
    [SerializeField] private float patrolWanderRadius = 10f;
    [SerializeField] private float patrolMinMoveDistance = 3f;
    [SerializeField] private float patrolWaitMin = 0.4f;
    [SerializeField] private float patrolWaitMax = 1.15f;
    [SerializeField] private int patrolSampleAttempts = 10;

    public float ViewDistance => viewDistance;
    public float BaseViewAngle => baseViewAngle;
    public float FocusedViewAngle => focusedViewAngle;
    public float SlowSpeed => slowSpeed;
    public float UsualSpeed => usualSpeed;
    public float InvestigationSpeed => investigationSpeed;
    public float ChaseSpeed => chaseSpeed;
    public float CatchDistance => catchDistance;
    public bool CatchEnabled => catchEnabled;
    public float ChaseStoppingDistance => chaseStoppingDistance;
    public float ChaseAcceleration => chaseAcceleration;
    public float ChaseAngularSpeed => chaseAngularSpeed;
    public bool ChaseAutoBraking => chaseAutoBraking;
    public float ChaseLoseDelay => chaseLoseDelay;
    public float ForcedHidingInspectionRadiusAfterSighting => forcedHidingInspectionRadiusAfterSighting;
    public float InvestigationTimeBonusAfterSighting => investigationTimeBonusAfterSighting;
    public float NearMonsterDistance => nearMonsterDistance;
    public float MaxNearMonsterAmbientVolume => maxNearMonsterAmbientVolume;
    public float AmbientVolumeLerpSpeed => ambientVolumeLerpSpeed;
    public float FootstepStartSpeed => footstepStartSpeed;
    public float FootstepMaxVolume => footstepMaxVolume;
    public float FootstepPitchAtUsualSpeed => footstepPitchAtUsualSpeed;
    public bool BlockAmbientThroughWalls => blockAmbientThroughWalls;
    public float AudioRaycastHeight => audioRaycastHeight;
    public float FootstepTransmissionPerObstacle => footstepTransmissionPerObstacle;
    public float NearMonsterScreenFxDistance => nearMonsterScreenFxDistance;
    public float MaxVignetteIntensity => maxVignetteIntensity;
    public float MaxDarkeningExposure => maxDarkeningExposure;
    public float ScreenFxLerpSpeed => screenFxLerpSpeed;
    public bool PulseScreenFx => pulseScreenFx;
    public float PulseSpeed => pulseSpeed;
    public float PulseAmount => pulseAmount;
    public float NoiseListenerCaptureRadius => noiseListenerCaptureRadius;
    public float MinHeardNoiseStrength => minHeardNoiseStrength;
    public float PatrolWanderRadius => patrolWanderRadius;
    public float PatrolMinMoveDistance => patrolMinMoveDistance;
    public float PatrolWaitMin => patrolWaitMin;
    public float PatrolWaitMax => patrolWaitMax;
    public int PatrolSampleAttempts => patrolSampleAttempts;
}
