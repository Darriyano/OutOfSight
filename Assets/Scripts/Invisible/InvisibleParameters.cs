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
    [SerializeField] public float viewDistance = 15f;
    [SerializeField] public float baseViewAngle = 180f;
    [SerializeField] public float focusedViewAngle = 60f;
    [SerializeField] public float slowSpeed = 3f;
    [SerializeField] public float usualSpeed = 7f;
    [SerializeField] public float investigationSpeed = 10f;
    [SerializeField] public float chaseSpeed = 30f;

    public float ViewDistance => viewDistance;
    public float BaseViewAngle => baseViewAngle;
    public float FocusedViewAngle => focusedViewAngle;
    public float SlowSpeed => slowSpeed;
    public float UsualSpeed => usualSpeed;
    public float InvestigationSpeed => investigationSpeed;
    public float ChaseSpeed => chaseSpeed;
}
