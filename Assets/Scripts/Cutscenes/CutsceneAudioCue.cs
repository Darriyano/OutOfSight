using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CutsceneAudioCue : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool rewindOnPlay = true;
    [SerializeField] private float fadeDuration = 0.75f;

    private Coroutine fadeRoutine;
    private float defaultVolume = 1f;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
            defaultVolume = audioSource.volume;
    }

    public void Play()
    {
        if (audioSource == null)
            return;

        StopFade();

        if (rewindOnPlay)
            audioSource.time = 0f;

        audioSource.volume = defaultVolume;
        audioSource.Play();
    }

    public void PlayIfStopped()
    {
        if (audioSource == null || audioSource.isPlaying)
            return;

        Play();
    }

    public void Stop()
    {
        if (audioSource == null)
            return;

        StopFade();
        audioSource.Stop();
    }

    public void FadeIn()
    {
        if (audioSource == null)
            return;

        StopFade();
        fadeRoutine = StartCoroutine(FadeTo(defaultVolume, true));
    }

    public void FadeOut()
    {
        if (audioSource == null)
            return;

        StopFade();
        fadeRoutine = StartCoroutine(FadeTo(0f, false));
    }

    private IEnumerator FadeTo(float targetVolume, bool ensurePlaying)
    {
        if (audioSource == null)
            yield break;

        if (ensurePlaying)
        {
            if (rewindOnPlay)
                audioSource.time = 0f;

            if (!audioSource.isPlaying)
                audioSource.Play();
        }

        float startVolume = audioSource.volume;
        float duration = Mathf.Max(0.01f, fadeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        audioSource.volume = targetVolume;
        fadeRoutine = null;

        if (targetVolume <= 0.001f)
            audioSource.Stop();
    }

    private void StopFade()
    {
        if (fadeRoutine == null)
            return;

        StopCoroutine(fadeRoutine);
        fadeRoutine = null;
    }
}
