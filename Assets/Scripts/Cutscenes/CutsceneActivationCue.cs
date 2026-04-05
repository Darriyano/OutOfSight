using UnityEngine;

public class CutsceneActivationCue : MonoBehaviour
{
    [SerializeField] private GameObject[] targets;

    public void Activate()
    {
        SetTargetsActive(true);
    }

    public void Deactivate()
    {
        SetTargetsActive(false);
    }

    public void Toggle()
    {
        GameObject firstTarget = GetFirstTarget();
        if (firstTarget == null)
            return;

        bool shouldActivate = !firstTarget.activeSelf;
        SetTargetsActive(shouldActivate);
    }

    private void SetTargetsActive(bool isActive)
    {
        if (targets == null)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
                targets[i].SetActive(isActive);
        }
    }

    private GameObject GetFirstTarget()
    {
        if (targets == null)
            return null;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
                return targets[i];
        }

        return null;
    }
}
