using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(AudioSource))]
public class InvisibleBehaviour : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private AudioClip nearMonsterAmbientClip;
    [SerializeField] private AudioClip footstepsClip;

    private InvisibleParameters parameters;
    private AudioSource footstepsAudioSource;
    private AudioSource nearMonsterAmbientAudioSource;
    private Volume nearMonsterScreenFxVolume;
    private VolumeProfile nearMonsterScreenFxProfile;
    private Vignette nearMonsterVignette;
    private ColorAdjustments nearMonsterColorAdjustments;
    private readonly HashSet<Collider> ignoredAudioColliders = new HashSet<Collider>();

    private void Awake()
    {
        parameters = InvisibleParameters.Instance;

        if (parameters == null)
        {
            Debug.LogError("InvisibleParameters is missing. Disabling InvisibleBehaviour.");
            enabled = false;
            return;
        }

        footstepsAudioSource = GetComponent<AudioSource>();
        ConfigureFootstepAudio();
        EnsureAmbientAudioSource();
        ConfigureAmbientAudio();
        EnsureScreenFxVolume();
        ConfigureScreenFx();
        EnablePlayerCameraPostProcessing();
        CacheIgnoredAudioColliders();
    }

    private void Update()
    {
        UpdateNearMonsterAmbient();
        UpdateFootstepAudio();
        UpdateNearMonsterScreenFx();
    }

    public bool CanSeeTarget()
    {
        if (parameters == null || target == null)
            return false;

        PlayerHiding playerHiding = target.GetComponent<PlayerHiding>();
        if (playerHiding == null)
            playerHiding = target.GetComponentInParent<PlayerHiding>();

        if (playerHiding != null && playerHiding.IsHidden)
            return false;

        float viewDistance = parameters.ViewDistance;
        float viewAngle = parameters.BaseViewAngle;

        Vector3 direction = (target.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, target.position);

        if (distance > viewDistance)
            return false;

        float angle = Vector3.Angle(transform.forward, direction);
        if (angle > viewAngle * 0.5f)
            return false;

        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, viewDistance))
            return hit.transform == target;

        return false;
    }

    private void ConfigureFootstepAudio()
    {
        if (footstepsAudioSource == null)
            return;

        footstepsAudioSource.playOnAwake = false;
        footstepsAudioSource.loop = true;
        footstepsAudioSource.spatialBlend = 1f;
        footstepsAudioSource.volume = 0f;
        footstepsAudioSource.pitch = parameters.FootstepPitchAtUsualSpeed;
        footstepsAudioSource.clip = footstepsClip;
    }

    private void EnsureAmbientAudioSource()
    {
        if (target == null)
            return;

        Transform ambientRoot = target.Find("MonsterNearAmbientAudio");
        if (ambientRoot == null)
        {
            GameObject ambientObject = new GameObject("MonsterNearAmbientAudio");
            ambientObject.transform.SetParent(target, false);
            ambientRoot = ambientObject.transform;
        }

        nearMonsterAmbientAudioSource = ambientRoot.GetComponent<AudioSource>();
        if (nearMonsterAmbientAudioSource == null)
            nearMonsterAmbientAudioSource = ambientRoot.gameObject.AddComponent<AudioSource>();
    }

    private void ConfigureAmbientAudio()
    {
        if (nearMonsterAmbientAudioSource == null)
            return;

        nearMonsterAmbientAudioSource.playOnAwake = false;
        nearMonsterAmbientAudioSource.loop = true;
        nearMonsterAmbientAudioSource.spatialBlend = 0f;
        nearMonsterAmbientAudioSource.volume = 0f;
        nearMonsterAmbientAudioSource.clip = nearMonsterAmbientClip;
    }

    private void EnsureScreenFxVolume()
    {
        if (target == null)
            return;

        Transform screenFxRoot = target.Find("MonsterNearScreenFx");
        if (screenFxRoot == null)
        {
            GameObject screenFxObject = new GameObject("MonsterNearScreenFx");
            screenFxObject.transform.SetParent(target, false);
            screenFxRoot = screenFxObject.transform;
        }

        nearMonsterScreenFxVolume = screenFxRoot.GetComponent<Volume>();
        if (nearMonsterScreenFxVolume == null)
            nearMonsterScreenFxVolume = screenFxRoot.gameObject.AddComponent<Volume>();

        if (nearMonsterScreenFxProfile == null)
            nearMonsterScreenFxProfile = ScriptableObject.CreateInstance<VolumeProfile>();

        nearMonsterScreenFxVolume.isGlobal = true;
        nearMonsterScreenFxVolume.priority = 100f;
        nearMonsterScreenFxVolume.weight = 1f;
        nearMonsterScreenFxVolume.sharedProfile = nearMonsterScreenFxProfile;
    }

    private void ConfigureScreenFx()
    {
        if (nearMonsterScreenFxProfile == null)
            return;

        if (!nearMonsterScreenFxProfile.TryGet(out nearMonsterVignette))
            nearMonsterVignette = nearMonsterScreenFxProfile.Add<Vignette>(true);

        if (!nearMonsterScreenFxProfile.TryGet(out nearMonsterColorAdjustments))
            nearMonsterColorAdjustments = nearMonsterScreenFxProfile.Add<ColorAdjustments>(true);

        nearMonsterVignette.active = true;
        nearMonsterVignette.intensity.overrideState = true;
        nearMonsterVignette.smoothness.overrideState = true;
        nearMonsterVignette.color.overrideState = true;
        nearMonsterVignette.intensity.value = 0f;
        nearMonsterVignette.smoothness.value = 0.85f;
        nearMonsterVignette.color.value = Color.black;

        nearMonsterColorAdjustments.active = true;
        nearMonsterColorAdjustments.postExposure.overrideState = true;
        nearMonsterColorAdjustments.postExposure.value = 0f;
    }

    private void EnablePlayerCameraPostProcessing()
    {
        if (target == null)
            return;

        Camera playerCamera = target.GetComponentInChildren<Camera>(true);
        if (playerCamera == null)
            return;

        UniversalAdditionalCameraData cameraData = playerCamera.GetComponent<UniversalAdditionalCameraData>();
        if (cameraData != null)
            cameraData.renderPostProcessing = true;
    }

    private void UpdateNearMonsterAmbient()
    {
        if (nearMonsterAmbientAudioSource == null || nearMonsterAmbientClip == null || target == null)
            return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        float targetVolume = 0f;
        int obstacleCount = CountAudioObstaclesToTarget();
        bool hasDirectAudioPath = obstacleCount == 0;

        if (distanceToTarget <= parameters.NearMonsterDistance)
        {
            float normalizedDistance = Mathf.Clamp01(distanceToTarget / parameters.NearMonsterDistance);
            targetVolume = (1f - normalizedDistance) * parameters.MaxNearMonsterAmbientVolume;

            if (parameters.BlockAmbientThroughWalls && !hasDirectAudioPath)
                targetVolume = 0f;
        }

        nearMonsterAmbientAudioSource.volume = Mathf.MoveTowards(
            nearMonsterAmbientAudioSource.volume,
            targetVolume,
            parameters.AmbientVolumeLerpSpeed * Time.deltaTime);

        if (nearMonsterAmbientAudioSource.volume > 0.001f)
        {
            if (!nearMonsterAmbientAudioSource.isPlaying)
                nearMonsterAmbientAudioSource.Play();
        }
        else if (nearMonsterAmbientAudioSource.isPlaying)
        {
            nearMonsterAmbientAudioSource.Stop();
        }
    }

    private void UpdateFootstepAudio()
    {
        if (footstepsAudioSource == null || footstepsClip == null)
            return;

        float movementSpeed = 0f;

        StateController stateController = GetComponent<StateController>();
        if (stateController != null && stateController.Agent != null)
            movementSpeed = stateController.Agent.velocity.magnitude;

        bool shouldPlay = movementSpeed > parameters.FootstepStartSpeed;
        float occlusionFactor = 1f;

        if (shouldPlay)
        {
            int obstacleCount = CountAudioObstaclesToTarget();
            occlusionFactor = Mathf.Pow(parameters.FootstepTransmissionPerObstacle, obstacleCount);
        }

        footstepsAudioSource.volume = shouldPlay ? parameters.FootstepMaxVolume * occlusionFactor : 0f;

        float usualSpeed = parameters.UsualSpeed;
        if (usualSpeed > 0.01f)
        {
            float speedRatio = movementSpeed / usualSpeed;
            footstepsAudioSource.pitch = Mathf.Max(0.01f, parameters.FootstepPitchAtUsualSpeed * speedRatio);
        }
        else
        {
            footstepsAudioSource.pitch = parameters.FootstepPitchAtUsualSpeed;
        }

        if (shouldPlay)
        {
            if (!footstepsAudioSource.isPlaying)
                footstepsAudioSource.Play();
        }
        else if (footstepsAudioSource.isPlaying)
        {
            footstepsAudioSource.Stop();
        }
    }

    private void UpdateNearMonsterScreenFx()
    {
        if (nearMonsterVignette == null || nearMonsterColorAdjustments == null || target == null)
            return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        float proximity = 0f;

        if (distanceToTarget <= parameters.NearMonsterScreenFxDistance)
        {
            float normalizedDistance = Mathf.Clamp01(distanceToTarget / parameters.NearMonsterScreenFxDistance);
            proximity = 1f - normalizedDistance;
        }

        if (proximity > 0f && parameters.PulseScreenFx)
        {
            float pulse = (Mathf.Sin(Time.time * parameters.PulseSpeed) + 1f) * 0.5f;
            proximity *= Mathf.Lerp(1f - parameters.PulseAmount, 1f, pulse);
        }

        float targetVignetteIntensity = proximity * parameters.MaxVignetteIntensity;
        float targetExposure = proximity * parameters.MaxDarkeningExposure;

        nearMonsterVignette.intensity.value = Mathf.MoveTowards(
            nearMonsterVignette.intensity.value,
            targetVignetteIntensity,
            parameters.ScreenFxLerpSpeed * Time.deltaTime);

        nearMonsterColorAdjustments.postExposure.value = Mathf.MoveTowards(
            nearMonsterColorAdjustments.postExposure.value,
            targetExposure,
            parameters.ScreenFxLerpSpeed * Time.deltaTime);
    }

    private void CacheIgnoredAudioColliders()
    {
        ignoredAudioColliders.Clear();

        AddIgnoredCollidersFromTransform(transform);
        AddIgnoredCollidersFromTransform(target);
    }

    private void AddIgnoredCollidersFromTransform(Transform root)
    {
        if (root == null)
            return;

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                ignoredAudioColliders.Add(colliders[i]);
        }
    }

    private int CountAudioObstaclesToTarget()
    {
        if (target == null)
            return 0;

        Vector3 origin = transform.position + Vector3.up * parameters.AudioRaycastHeight;
        Vector3 targetPosition = target.position + Vector3.up * parameters.AudioRaycastHeight;
        Vector3 direction = targetPosition - origin;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
            return 0;

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction.normalized,
            distance,
            obstacleMask,
            QueryTriggerInteraction.Ignore);

        if (hits == null || hits.Length == 0)
            return 0;

        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        int obstacleCount = 0;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null || ignoredAudioColliders.Contains(hitCollider))
                continue;

            obstacleCount++;
        }

        return obstacleCount;
    }

    private void OnDrawGizmos()
    {
        if (parameters == null)
            parameters = InvisibleParameters.Instance;

        if (parameters == null)
            return;

        float viewDistance = parameters.ViewDistance;
        float viewAngle = parameters.BaseViewAngle;

        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;

        Gizmos.color = Color.yellow;

        Vector3 leftBoundary = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * forward;
        Gizmos.DrawRay(origin, leftBoundary * viewDistance);

        Vector3 rightBoundary = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * forward;
        Gizmos.DrawRay(origin, rightBoundary * viewDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(origin, forward * viewDistance);

        int segments = 30;
        float halfAngle = viewAngle * 0.5f;

        Vector3 previousPoint = origin + (Quaternion.Euler(0f, -halfAngle, 0f) * forward) * viewDistance;

        for (int i = 1; i <= segments; i++)
        {
            float angle = -halfAngle + (viewAngle / segments) * i;
            Vector3 nextPoint = origin + (Quaternion.Euler(0f, angle, 0f) * forward) * viewDistance;

            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }

        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.35f);
        Gizmos.DrawWireSphere(origin, parameters.NearMonsterDistance);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (nearMonsterAmbientClip == null)
            nearMonsterAmbientClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFXs/monster_near_ambient.mp3");

        if (footstepsClip == null)
            footstepsClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFXs/heavy-walking-footsteps.mp3");

        if (footstepsAudioSource == null)
            footstepsAudioSource = GetComponent<AudioSource>();

        parameters = InvisibleParameters.Instance;
        EnsureAmbientAudioSource();
        EnsureScreenFxVolume();

        if (parameters != null)
        {
            ConfigureFootstepAudio();
            ConfigureAmbientAudio();
            ConfigureScreenFx();
        }
    }
#endif
}
