using System.Collections.Generic;
using UnityEngine;

public struct NoiseEventData
{
    public Vector3 Position;
    public GameObject Source;
    public float Radius;
    public float Strength;
    public int MaxBounces;
    public float BounceDamping;

    public NoiseEventData(Vector3 position, float radius, float strength, GameObject source = null, int maxBounces = 3, float bounceDamping = 0.72f)
    {
        Position = position;
        Radius = radius;
        Strength = strength;
        Source = source;
        MaxBounces = Mathf.Max(0, maxBounces);
        BounceDamping = Mathf.Clamp01(bounceDamping);
    }
}

public struct NoiseHeardInfo
{
    public Vector3 SourcePosition;
    public Vector3 ArrivalPosition;
    public GameObject Source;
    public float Strength;
    public float TravelDistance;
    public int BounceCount;
}

public interface INoiseListener
{
    Component NoiseListenerComponent { get; }
    Vector3 HearingPosition { get; }
    float HearingCaptureRadius { get; }
    float MinimumHeardNoiseStrength { get; }
    bool CanReceiveNoise { get; }
    void OnNoiseHeard(NoiseHeardInfo heardNoise);
}

public class NoiseSystem : MonoBehaviour
{
    private static NoiseSystem instance;
    private static readonly List<INoiseListener> listeners = new List<INoiseListener>();

    [Header("Propagation")]
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private int rayCount = 96;
    [SerializeField] private float horizontalRayHeight = 0.75f;
    [SerializeField] private float bounceSurfaceOffset = 0.05f;

    [Header("Debug")]
    [SerializeField] private bool debugDrawPropagation;
    [SerializeField] private float debugDrawDuration = 1f;

    public static bool HasInstance => instance != null;

    public static NoiseSystem Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<NoiseSystem>();
                if (instance == null)
                {
                    GameObject noiseSystemObject = new GameObject("NoiseSystem");
                    instance = noiseSystemObject.AddComponent<NoiseSystem>();
                }
            }

            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    public static void RegisterListener(INoiseListener listener)
    {
        if (listener == null)
            return;

        var activeInstance = Instance;
        if (!listeners.Contains(listener))
            listeners.Add(listener);

        activeInstance.CleanupListeners();
    }

    public static void UnregisterListener(INoiseListener listener)
    {
        listeners.Remove(listener);
    }

    public static void Emit(NoiseEventData noiseEvent)
    {
        if (noiseEvent.Radius <= 0f || noiseEvent.Strength <= 0f)
            return;

        Instance.EmitInternal(noiseEvent);
    }

    private void EmitInternal(NoiseEventData noiseEvent)
    {
        CleanupListeners();
        if (listeners.Count == 0)
            return;

        Dictionary<INoiseListener, NoiseHeardInfo> strongestHeardNoise = new Dictionary<INoiseListener, NoiseHeardInfo>();
        HashSet<Collider> ignoredColliders = CollectIgnoredColliders(noiseEvent.Source);

        int propagationRayCount = Mathf.Max(8, rayCount);
        Vector3 origin = noiseEvent.Position + Vector3.up * horizontalRayHeight;

        for (int i = 0; i < propagationRayCount; i++)
        {
            float angle = i * (360f / propagationRayCount);
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            TraceNoiseRay(origin, direction, noiseEvent, ignoredColliders, strongestHeardNoise);
        }

        foreach (KeyValuePair<INoiseListener, NoiseHeardInfo> heardPair in strongestHeardNoise)
        {
            INoiseListener listener = heardPair.Key;
            if (!IsValidListener(listener) || !listener.CanReceiveNoise)
                continue;

            if (heardPair.Value.Strength < listener.MinimumHeardNoiseStrength)
                continue;

            listener.OnNoiseHeard(heardPair.Value);
        }
    }

    private void TraceNoiseRay(
        Vector3 origin,
        Vector3 initialDirection,
        NoiseEventData noiseEvent,
        HashSet<Collider> ignoredColliders,
        Dictionary<INoiseListener, NoiseHeardInfo> strongestHeardNoise)
    {
        float remainingDistance = noiseEvent.Radius;
        float traveledDistance = 0f;
        float currentStrength = noiseEvent.Strength;
        int bounceCount = 0;
        Vector3 currentOrigin = origin;
        Vector3 direction = initialDirection.normalized;

        while (remainingDistance > 0.01f && currentStrength > 0.01f)
        {
            RaycastHit? nearestHit = FindNearestHit(currentOrigin, direction, remainingDistance, ignoredColliders);

            Vector3 segmentEnd = currentOrigin + direction * remainingDistance;
            float segmentLength = remainingDistance;

            if (nearestHit.HasValue)
            {
                RaycastHit hit = nearestHit.Value;
                segmentEnd = hit.point;
                segmentEnd.y = currentOrigin.y;
                segmentLength = hit.distance;
            }

            RegisterListenersAlongSegment(
                noiseEvent,
                currentOrigin,
                segmentEnd,
                traveledDistance,
                currentStrength,
                bounceCount,
                strongestHeardNoise);

            DrawDebugSegment(currentOrigin, segmentEnd, nearestHit.HasValue, currentStrength);

            traveledDistance += segmentLength;
            remainingDistance -= segmentLength;

            if (!nearestHit.HasValue || bounceCount >= noiseEvent.MaxBounces)
                break;

            RaycastHit reflectedHit = nearestHit.Value;
            Vector3 wallNormal = Vector3.ProjectOnPlane(reflectedHit.normal, Vector3.up).normalized;
            if (wallNormal.sqrMagnitude < 0.0001f)
                break;

            direction = Vector3.Reflect(direction, wallNormal).normalized;
            currentStrength *= noiseEvent.BounceDamping;
            bounceCount++;

            currentOrigin = reflectedHit.point + direction * bounceSurfaceOffset;
            currentOrigin.y = origin.y;
        }
    }

    private void RegisterListenersAlongSegment(
        NoiseEventData noiseEvent,
        Vector3 segmentStart,
        Vector3 segmentEnd,
        float traveledBeforeSegment,
        float segmentStrength,
        int bounceCount,
        Dictionary<INoiseListener, NoiseHeardInfo> strongestHeardNoise)
    {
        float segmentLength = Vector3.Distance(segmentStart, segmentEnd);
        if (segmentLength <= 0.001f)
            return;

        for (int i = 0; i < listeners.Count; i++)
        {
            INoiseListener listener = listeners[i];
            if (!IsValidListener(listener) || !listener.CanReceiveNoise)
                continue;

            Vector3 hearingPosition = listener.HearingPosition;
            hearingPosition.y = segmentStart.y;

            float captureRadius = Mathf.Max(0.01f, listener.HearingCaptureRadius);
            float segmentParameter = GetClosestPointParameterOnSegment(segmentStart, segmentEnd, hearingPosition);
            Vector3 closestPoint = Vector3.Lerp(segmentStart, segmentEnd, segmentParameter);
            float distanceToSegment = Vector3.Distance(hearingPosition, closestPoint);

            if (distanceToSegment > captureRadius)
                continue;

            float traveledToListener = traveledBeforeSegment + (segmentLength * segmentParameter);
            float distanceFalloff = 1f - Mathf.Clamp01(traveledToListener / noiseEvent.Radius);
            float lateralFalloff = 1f - Mathf.Clamp01(distanceToSegment / captureRadius);
            float heardStrength = segmentStrength * distanceFalloff * Mathf.Lerp(0.65f, 1f, lateralFalloff);

            if (heardStrength < listener.MinimumHeardNoiseStrength)
                continue;

            NoiseHeardInfo heardInfo = new NoiseHeardInfo
            {
                SourcePosition = noiseEvent.Position,
                ArrivalPosition = closestPoint,
                Source = noiseEvent.Source,
                Strength = heardStrength,
                TravelDistance = traveledToListener,
                BounceCount = bounceCount
            };

            if (!strongestHeardNoise.TryGetValue(listener, out NoiseHeardInfo existingInfo) || heardStrength > existingInfo.Strength)
                strongestHeardNoise[listener] = heardInfo;
        }
    }

    private RaycastHit? FindNearestHit(Vector3 origin, Vector3 direction, float distance, HashSet<Collider> ignoredColliders)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, obstacleMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            return null;

        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null)
                continue;

            if (ignoredColliders != null && ignoredColliders.Contains(hit.collider))
                continue;

            return hit;
        }

        return null;
    }

    private HashSet<Collider> CollectIgnoredColliders(GameObject source)
    {
        HashSet<Collider> ignoredColliders = new HashSet<Collider>();

        if (source != null)
        {
            AddCollidersFromGameObject(source, ignoredColliders);

            NoiseProjectile projectile = source.GetComponent<NoiseProjectile>();
            if (projectile != null && projectile.Owner != null)
                AddCollidersFromGameObject(projectile.Owner, ignoredColliders);
        }

        for (int i = 0; i < listeners.Count; i++)
        {
            INoiseListener listener = listeners[i];
            if (!IsValidListener(listener))
                continue;

            AddCollidersFromGameObject(listener.NoiseListenerComponent.gameObject, ignoredColliders);
        }

        return ignoredColliders.Count > 0 ? ignoredColliders : null;
    }

    private void CleanupListeners()
    {
        for (int i = listeners.Count - 1; i >= 0; i--)
        {
            if (!IsValidListener(listeners[i]))
                listeners.RemoveAt(i);
        }
    }

    private bool IsValidListener(INoiseListener listener)
    {
        if (listener == null)
            return false;

        return listener.NoiseListenerComponent != null;
    }

    private void AddCollidersFromGameObject(GameObject gameObject, HashSet<Collider> targetSet)
    {
        if (gameObject == null || targetSet == null)
            return;

        Collider[] colliders = gameObject.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                targetSet.Add(colliders[i]);
        }
    }

    private float GetClosestPointParameterOnSegment(Vector3 segmentStart, Vector3 segmentEnd, Vector3 point)
    {
        Vector3 segment = segmentEnd - segmentStart;
        float segmentLengthSqr = segment.sqrMagnitude;
        if (segmentLengthSqr <= 0.0001f)
            return 0f;

        float projection = Vector3.Dot(point - segmentStart, segment) / segmentLengthSqr;
        return Mathf.Clamp01(projection);
    }

    private void DrawDebugSegment(Vector3 segmentStart, Vector3 segmentEnd, bool wasReflected, float currentStrength)
    {
        if (!debugDrawPropagation)
            return;

        float intensity = Mathf.Clamp01(currentStrength);
        Color lineColor = wasReflected
            ? new Color(1f, 0.4f, 0.1f, intensity)
            : new Color(0.25f, 0.8f, 1f, intensity);

        Debug.DrawLine(segmentStart, segmentEnd, lineColor, debugDrawDuration);
    }
}
