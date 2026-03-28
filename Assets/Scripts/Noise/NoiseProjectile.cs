using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class NoiseProjectile : MonoBehaviour
{
    [Header("Impact Noise")]
    [SerializeField] private float baseImpactRadius = 18f;
    [SerializeField] private float baseImpactStrength = 1f;
    [SerializeField] private float referenceImpactSpeed = 10f;
    [SerializeField] private float minImpactSpeed = 1.5f;
    [SerializeField] private int maxNoiseBounces = 4;
    [SerializeField] private float noiseBounceDamping = 0.72f;
    [SerializeField] private float impactCooldown = 0.08f;

    [Header("Lifetime")]
    [SerializeField] private float maxLifetime = 12f;

    private float lastImpactTime = -999f;
    public GameObject Owner { get; private set; }

    public void SetOwner(GameObject owner)
    {
        Owner = owner;
    }

    private void Start()
    {
        Destroy(gameObject, maxLifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < minImpactSpeed)
            return;

        if (Time.time - lastImpactTime < impactCooldown)
            return;

        lastImpactTime = Time.time;

        float speedRatio = referenceImpactSpeed > 0.01f ? impactSpeed / referenceImpactSpeed : 1f;
        float noiseMultiplier = Mathf.Clamp(speedRatio, 0.35f, 1.5f);
        Vector3 impactPoint = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;

        NoiseSystem.Emit(new NoiseEventData(
            impactPoint,
            baseImpactRadius * noiseMultiplier,
            baseImpactStrength * noiseMultiplier,
            gameObject,
            maxNoiseBounces,
            noiseBounceDamping));
    }
}
