using UnityEngine;

public class CutsceneDialogueCue : MonoBehaviour
{
    [SerializeField] private ApartmentIntroCutscene cutscene;
    [SerializeField] private string dialogueText;
    [SerializeField] private AudioClip voiceClip;
    [SerializeField] private float minimumDuration = 2f;

    private void Reset()
    {
        if (cutscene == null)
            cutscene = GetComponentInParent<ApartmentIntroCutscene>();
    }

    public void Play()
    {
        if (cutscene == null)
            return;

        cutscene.PlayCustomDialogue(dialogueText, voiceClip, minimumDuration);
    }

    public void Clear()
    {
        if (cutscene == null)
            return;

        cutscene.ClearDialogueCue();
    }
}
