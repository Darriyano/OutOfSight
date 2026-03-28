using UnityEngine;

public class PlayerThrowableTestItem : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode throwKey = KeyCode.G;
    [SerializeField] private float throwCooldown = 0.35f;

    [Header("Throw")]
    [SerializeField] private Transform throwOrigin;
    [SerializeField] private GameObject throwablePrefab;
    [SerializeField] private float throwForce = 16f;
    [SerializeField] private float upwardForce = 1.5f;
    [SerializeField] private float spawnForwardOffset = 0.8f;
    [SerializeField] private float spawnUpOffset = -0.15f;

    [Header("Auto Projectile")]
    [SerializeField] private float autoProjectileScale = 0.2f;
    [SerializeField] private float autoProjectileMass = 0.35f;
    [SerializeField] private float autoProjectileBounciness = 0.75f;

    private Camera playerCamera;
    private Collider[] ownerColliders;
    private float lastThrowTime = -999f;
    private static PhysicsMaterial cachedBounceMaterial;

    private void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>(true);
        ownerColliders = GetComponentsInChildren<Collider>(true);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(throwKey))
            return;

        if (Time.time - lastThrowTime < throwCooldown)
            return;

        ThrowTestItem();
    }

    public void ThrowTestItem()
    {
        Transform origin = throwOrigin != null ? throwOrigin : (playerCamera != null ? playerCamera.transform : transform);
        Vector3 spawnPosition = origin.position + origin.forward * spawnForwardOffset + origin.up * spawnUpOffset;
        Quaternion spawnRotation = Quaternion.LookRotation(origin.forward, Vector3.up);

        GameObject projectile = throwablePrefab != null
            ? Instantiate(throwablePrefab, spawnPosition, spawnRotation)
            : CreateDefaultProjectile(spawnPosition, spawnRotation);

        Rigidbody projectileRigidbody = projectile.GetComponent<Rigidbody>();
        if (projectileRigidbody == null)
            projectileRigidbody = projectile.AddComponent<Rigidbody>();

        Collider projectileCollider = projectile.GetComponent<Collider>();
        if (projectileCollider == null)
            projectileCollider = projectile.AddComponent<SphereCollider>();

        NoiseProjectile noiseProjectile = projectile.GetComponent<NoiseProjectile>();
        if (noiseProjectile == null)
            noiseProjectile = projectile.AddComponent<NoiseProjectile>();

        noiseProjectile.SetOwner(gameObject);

        IgnoreOwnerCollisions(projectile);

        projectileRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        projectileRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        projectileRigidbody.linearVelocity = Vector3.zero;
        projectileRigidbody.angularVelocity = Vector3.zero;
        projectileRigidbody.AddForce(origin.forward * throwForce + origin.up * upwardForce, ForceMode.VelocityChange);

        lastThrowTime = Time.time;
    }

    private GameObject CreateDefaultProjectile(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = "NoiseTestThrowable";
        projectile.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        projectile.transform.localScale = Vector3.one * autoProjectileScale;

        Rigidbody rigidbody = projectile.AddComponent<Rigidbody>();
        rigidbody.mass = autoProjectileMass;

        SphereCollider sphereCollider = projectile.GetComponent<SphereCollider>();
        if (sphereCollider != null)
            sphereCollider.material = GetBounceMaterial();

        return projectile;
    }

    private void IgnoreOwnerCollisions(GameObject projectile)
    {
        if (ownerColliders == null || ownerColliders.Length == 0 || projectile == null)
            return;

        Collider[] projectileColliders = projectile.GetComponentsInChildren<Collider>(true);
        if (projectileColliders == null || projectileColliders.Length == 0)
            return;

        for (int i = 0; i < ownerColliders.Length; i++)
        {
            Collider ownerCollider = ownerColliders[i];
            if (ownerCollider == null)
                continue;

            for (int j = 0; j < projectileColliders.Length; j++)
            {
                Collider projectileCollider = projectileColliders[j];
                if (projectileCollider == null)
                    continue;

                Physics.IgnoreCollision(ownerCollider, projectileCollider, true);
            }
        }
    }

    private PhysicsMaterial GetBounceMaterial()
    {
        if (cachedBounceMaterial != null)
            return cachedBounceMaterial;

        cachedBounceMaterial = new PhysicsMaterial("NoiseTestThrowableBounce")
        {
            bounciness = autoProjectileBounciness,
            dynamicFriction = 0.2f,
            staticFriction = 0.2f,
            bounceCombine = PhysicsMaterialCombine.Maximum,
            frictionCombine = PhysicsMaterialCombine.Minimum
        };

        return cachedBounceMaterial;
    }
}
