using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Serialization;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
#endif

public class ApartmentIntroCutscene : MonoBehaviour
{
    private const int CurrentGeneratedTimelineVersion = 9;

    private enum DoorMotionMode
    {
        SelfRotation,
        ExternalPivotRotation,
        ChildPivotOrbit
    }

    private enum IntroPlaybackMode
    {
        Scripted,
        Timeline
    }

    [System.Serializable]
    private class DialogueLine
    {
        public string text;
        public AudioClip voiceClip;
        public float minimumDuration = 2f;
    }

    [Header("Flow")]
    [SerializeField] private IntroPlaybackMode playbackMode = IntroPlaybackMode.Scripted;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool skippable = true;
    [SerializeField] private KeyCode skipKey = KeyCode.Escape;

    [Header("Timeline Integration")]
    [SerializeField] private CutsceneController cutsceneController;
    [SerializeField] private PlayableDirector timelineDirector;
    [SerializeField, HideInInspector] private int generatedTimelineVersion;
    [SerializeField, HideInInspector] private int generatedTimelineConfigHash;

    [Header("Player")]
    [SerializeField] private GameObject gameplayPlayerObject;
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform cameraPivot;

    [Header("Gameplay Runtime")]
    [SerializeField] private GameObject gameplayMonsterObject;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Camera cinematicCamera;

    [Header("Presentation")]
    [SerializeField] private bool hideGameplayPlayerDuringIntro = false;

    [Header("Path")]
    [SerializeField] private Transform introStartPoint;
    [SerializeField] private Transform doorApproachPoint;
    [SerializeField] private Transform apartmentEntryPoint;
    [SerializeField] private Transform corridorSneakPoint;
    [SerializeField] private Transform kitchenRevealPoint;
    [SerializeField] private Transform kitchenLookTarget;

    [Header("Generated Timeline Route")]
    [SerializeField] private Transform[] indoorPathWaypoints;
    [SerializeField] private Transform[] cameraIndoorPathWaypoints;
    [SerializeField] private float finalCameraTurnDuration = 0.85f;
    [SerializeField] private bool forceClockwiseFinalCameraTurn = true;

    [Header("Door")]
    [SerializeField] private Transform doorPivot;
    [SerializeField] private float doorOpenAngle = 80f;
    [SerializeField] private float doorOpenDuration = 1.1f;

    [Header("Timing")]
    [SerializeField] private float initialBlackHold = 1.8f;
    [SerializeField] private float footstepsLeadInBeforeFade = 0.8f;
    [SerializeField] private float blackFadeDuration = 2.4f;
    [SerializeField] private float approachSpeed = 1.7f;
    [SerializeField] private float sneakSpeed = 1.0f;
    [SerializeField] private float rotationSpeed = 240f;

    [Header("Footsteps")]
    [SerializeField] private float doorApproachFootstepsVolume = 0.85f;
    [SerializeField] private float doorApproachFootstepsPitch = 1.2f;
    [SerializeField] private bool playIndoorFootstepsAfterDoor = false;
    [SerializeField] private float indoorFootstepsVolume = 0.45f;
    [SerializeField] private float indoorFootstepsPitch = 0.72f;

    [Header("Scene State")]
    [SerializeField] private Behaviour[] disableBehavioursDuringIntro;
    [SerializeField] private GameObject[] disableObjectsDuringIntro;
    [SerializeField] private GameObject[] activateOnDoorOpen;
    [SerializeField] private GameObject[] activateOnMonsterReveal;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource effectsAudioSource;
    [SerializeField] private AudioSource footstepsAudioSource;
    [SerializeField] private AudioSource voiceAudioSource;

    [Header("Monster Eating")]
    [FormerlySerializedAs("monsterAudioSource")]
    [SerializeField] private AudioSource monsterEatingAudioSource;
    [SerializeField] private Transform monsterEatingSourcePoint;
    [SerializeField] private float monsterEatingVolume = 0.85f;
    [SerializeField] private float monsterEatingPitch = 1f;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip elevatorClip;
    [SerializeField] private AudioClip corridorFootstepsClip;
    [SerializeField] private AudioClip keysSearchClip;
    [SerializeField] private AudioClip keyInsertClip;
    [SerializeField] private AudioClip doorOpenClip;
    [SerializeField] private AudioClip monsterEatingClip;

    [Header("Dialogue")]
    [SerializeField] private DialogueLine doorOpenLine = new DialogueLine
    {
        text = "Странно, дверь открыта...",
        minimumDuration = 2.5f
    };

    [SerializeField] private DialogueLine stayQuietLine = new DialogueLine
    {
        text = "Надо вести себя тихо, чтобы никого не разбудить.",
        minimumDuration = 3.4f
    };

    [SerializeField] private DialogueLine kitchenNoiseLine = new DialogueLine
    {
        text = "Что за странный шум с кухни?",
        minimumDuration = 2.8f
    };

    [SerializeField] private DialogueLine revealLine = new DialogueLine
    {
        text = "Что за чёрт?!",
        minimumDuration = 2.1f
    };

    private readonly Dictionary<Behaviour, bool> behaviourStates = new Dictionary<Behaviour, bool>();
    private readonly Dictionary<GameObject, bool> objectStates = new Dictionary<GameObject, bool>();

    private Canvas overlayCanvas;
    private Image blackOverlayImage;
    private Text subtitleText;
    private CharacterController playerCharacterController;
    private Quaternion doorClosedRotation;
    private Quaternion doorOpenRotation;
    private Vector3 doorClosedPosition;
    private Vector3 doorOpenPosition;
    private Quaternion doorClosedWorldRotation;
    private Quaternion doorOpenWorldRotation;
    private DoorMotionMode doorMotionMode;
    private bool isPlaying;
    private bool skipRequested;
    private bool characterControllerWasEnabled;
    private bool playerActiveStateBeforeIntro = true;
    private Coroutine introRoutine;
    private Coroutine overlayFadeRoutine;
    private Coroutine activeDialogueRoutine;
    private Coroutine doorOpenRoutine;

#if UNITY_EDITOR
    private static bool editorTimelineRebuildScheduled;
    private static bool editorTimelineRebuildInProgress;
#endif

    private void Awake()
    {
        ResolveReferences();
        EnsurePlayerActiveTracer();
        PrepareSceneForIntro();
        EnsureGameplayPlayerVisible();
        BuildOverlayIfNeeded();
        CacheDoorRotations();
        CreateFallbackClipsIfNeeded();

        if (playOnStart && playbackMode == IntroPlaybackMode.Timeline && cutsceneController != null)
            PrepareTimelineIntro();
    }

    private void Start()
    {
        EnsureGameplayPlayerVisible();

        if (playOnStart)
        {
            if (playbackMode == IntroPlaybackMode.Timeline && cutsceneController != null)
                return;

            PlayIntro();
        }
    }

    private void OnDestroy()
    {
        PlayableDirector activeDirector = GetTimelineDirector();
        if (activeDirector != null)
            activeDirector.stopped -= HandleTimelineStopped;
    }

    private void Update()
    {
        if (isPlaying)
            EnsureGameplayPlayerVisible();

        if (!isPlaying || !skippable)
            return;

        if (Input.GetKeyDown(skipKey))
        {
            if (playbackMode == IntroPlaybackMode.Timeline)
                SkipTimelineIntro();
            else
                skipRequested = true;
        }
    }

    public void PlayIntro()
    {
        if (isPlaying)
            return;

        if (playbackMode == IntroPlaybackMode.Timeline)
        {
            PlayTimelineIntro();
            return;
        }

        if (introRoutine != null)
            StopCoroutine(introRoutine);

        introRoutine = StartCoroutine(RunIntro());
    }

    public void PlayTimelineIntro()
    {
        if (isPlaying)
            return;

        PlayableDirector activeDirector = GetTimelineDirector();
        if (activeDirector == null)
        {
            Debug.LogWarning("ApartmentIntroCutscene timeline mode requires a PlayableDirector.");
            return;
        }

        BeginIntroPlayback();
        activeDirector.stopped -= HandleTimelineStopped;
        activeDirector.stopped += HandleTimelineStopped;
        activeDirector.time = 0d;
        activeDirector.Evaluate();
        activeDirector.Play();
    }

    public void PrepareTimelineIntro()
    {
        if (isPlaying)
            return;

#if UNITY_EDITOR
        EnsureTimelineGeneratedUpToDateInEditor();
#endif

        BeginIntroPlayback();
    }

    public void FinishTimelineIntro()
    {
        if (!isPlaying)
            return;

        CompleteIntro();
    }

    public void SkipTimelineIntro()
    {
        if (!isPlaying)
            return;

        PlayableDirector activeDirector = GetTimelineDirector();
        if (activeDirector != null)
        {
            activeDirector.stopped -= HandleTimelineStopped;
            activeDirector.Stop();
        }

        ApplyFinalSceneState();
        CompleteIntro();
    }

    private void BeginIntroPlayback()
    {
        isPlaying = true;
        skipRequested = false;

        CacheSceneStates();
        ApplyIntroState();
        EnsureGameplayPlayerVisible();
        TeleportPlayer(introStartPoint);
        SetOverlayAlpha(1f);
        ClearDialogue();
        SetObjectsActive(activateOnDoorOpen, false);
        SetObjectsActive(activateOnMonsterReveal, false);
        StopLoopingAudio();
    }

    private IEnumerator RunIntro()
    {
        BeginIntroPlayback();

        yield return WaitSkippable(initialBlackHold);
        if (TryFinishOnSkip())
            yield break;

        PlayOneShot(effectsAudioSource, elevatorClip);
        yield return WaitSkippable(Mathf.Max(initialBlackHold, GetClipDuration(elevatorClip, 1.8f) * 0.9f));
        if (TryFinishOnSkip())
            yield break;

        StartLoopingAudio(footstepsAudioSource, corridorFootstepsClip, doorApproachFootstepsVolume, doorApproachFootstepsPitch);
        yield return WaitSkippable(footstepsLeadInBeforeFade);
        if (TryFinishOnSkip())
            yield break;

        StartCoroutine(FadeOverlay(0f, blackFadeDuration));
        yield return MovePlayerTo(doorApproachPoint, approachSpeed);
        if (TryFinishOnSkip())
            yield break;

        StopLoopingAudio(footstepsAudioSource);
        yield return WaitSkippable(0.35f);
        if (TryFinishOnSkip())
            yield break;

        PlayOneShot(effectsAudioSource, keysSearchClip);
        yield return WaitSkippable(GetClipDuration(keysSearchClip, 1.5f));
        if (TryFinishOnSkip())
            yield break;

        PlayOneShot(effectsAudioSource, keyInsertClip);
        yield return WaitSkippable(GetClipDuration(keyInsertClip, 0.65f));
        if (TryFinishOnSkip())
            yield break;

        PlayOneShot(effectsAudioSource, doorOpenClip);
        yield return AnimateDoorOpen();
        if (TryFinishOnSkip())
            yield break;

        SetObjectsActive(activateOnDoorOpen, true);
        StartLoopingAudio(monsterEatingAudioSource, monsterEatingClip, monsterEatingVolume, monsterEatingPitch);

        yield return ShowDialogue(doorOpenLine);
        if (TryFinishOnSkip())
            yield break;

        yield return ShowDialogue(stayQuietLine);
        if (TryFinishOnSkip())
            yield break;

        if (playIndoorFootstepsAfterDoor)
            StartLoopingAudio(footstepsAudioSource, corridorFootstepsClip, indoorFootstepsVolume, indoorFootstepsPitch);

        yield return MovePlayerTo(apartmentEntryPoint, sneakSpeed, kitchenLookTarget);
        if (TryFinishOnSkip())
            yield break;

        yield return ShowDialogue(kitchenNoiseLine);
        if (TryFinishOnSkip())
            yield break;

        yield return MovePlayerTo(corridorSneakPoint, sneakSpeed, kitchenLookTarget);
        if (TryFinishOnSkip())
            yield break;

        yield return MovePlayerTo(kitchenRevealPoint, sneakSpeed * 0.92f, kitchenLookTarget);
        if (TryFinishOnSkip())
            yield break;

        StopLoopingAudio(footstepsAudioSource);
        SetObjectsActive(activateOnMonsterReveal, true);
        yield return RotateTowards(kitchenLookTarget, 0.5f);
        if (TryFinishOnSkip())
            yield break;

        yield return ShowDialogue(revealLine);
        CompleteIntro();
    }

    private bool TryFinishOnSkip()
    {
        if (!skipRequested)
            return false;

        ApplyFinalSceneState();
        CompleteIntro();
        return true;
    }

    private void HandleTimelineStopped(PlayableDirector stoppedDirector)
    {
        if (stoppedDirector != GetTimelineDirector() || !isPlaying || playbackMode != IntroPlaybackMode.Timeline)
            return;

        CompleteIntro();
    }

    private void CompleteIntro()
    {
        PlayableDirector activeDirector = GetTimelineDirector();
        if (activeDirector != null)
            activeDirector.stopped -= HandleTimelineStopped;

        StopOverlayFade();
        StopActiveDialogue();
        StopDoorAnimation();
        SetSubtitle(string.Empty);
        SetOverlayAlpha(0f);
        RestoreSceneStates();
        ApplyGameplayPresentation();
        isPlaying = false;
        introRoutine = null;
    }

    private void ApplyFinalSceneState()
    {
        SetObjectsActive(activateOnDoorOpen, true);
        SetObjectsActive(activateOnMonsterReveal, true);
        ApplyDoorPose(1f);
        TeleportPlayer(kitchenRevealPoint != null ? kitchenRevealPoint : apartmentEntryPoint);
        LookAtTargetInstant(kitchenLookTarget);
        StartLoopingAudio(monsterEatingAudioSource, monsterEatingClip, monsterEatingVolume, monsterEatingPitch);
        StopLoopingAudio(footstepsAudioSource);
    }

    public void SetBlackOverlay()
    {
        StopOverlayFade();
        SetOverlayAlpha(1f);
    }

    public void ClearBlackOverlay()
    {
        StopOverlayFade();
        SetOverlayAlpha(0f);
    }

    public void FadeFromBlack()
    {
        StartOverlayFade(0f, blackFadeDuration);
    }

    public void FadeToBlack()
    {
        StartOverlayFade(1f, blackFadeDuration);
    }

    public void PlayElevatorCue()
    {
        PlayOneShot(effectsAudioSource, elevatorClip);
    }

    public void StartDoorApproachFootstepsCue()
    {
        StartLoopingAudio(footstepsAudioSource, corridorFootstepsClip, doorApproachFootstepsVolume, doorApproachFootstepsPitch);
    }

    public void StartIndoorFootstepsCue()
    {
        if (!playIndoorFootstepsAfterDoor)
            return;

        StartLoopingAudio(footstepsAudioSource, corridorFootstepsClip, indoorFootstepsVolume, indoorFootstepsPitch);
    }

    public void StopFootstepsCue()
    {
        StopLoopingAudio(footstepsAudioSource);
    }

    public void PlayKeysSearchCue()
    {
        PlayOneShot(effectsAudioSource, keysSearchClip);
    }

    public void PlayKeyInsertCue()
    {
        PlayOneShot(effectsAudioSource, keyInsertClip);
    }

    public void PlayDoorOpenCue()
    {
        PlayOneShot(effectsAudioSource, doorOpenClip);
    }

    public void AnimateDoorOpenCue()
    {
        if (playbackMode == IntroPlaybackMode.Timeline)
            return;

        StopDoorAnimation();
        doorOpenRoutine = StartCoroutine(AnimateDoorOpenCueRoutine());
    }

    public void ForceDoorOpenCue()
    {
        StopDoorAnimation();
        ApplyDoorPose(1f);
    }

    public void ResetDoorClosedCue()
    {
        StopDoorAnimation();
        ApplyDoorPose(0f);
    }

    public void ActivateDoorOpenCue()
    {
        SetObjectsActive(activateOnDoorOpen, true);
    }

    public void DeactivateDoorOpenCue()
    {
        SetObjectsActive(activateOnDoorOpen, false);
    }

    public void ActivateMonsterRevealCue()
    {
        SetObjectsActive(activateOnMonsterReveal, true);
    }

    public void DeactivateMonsterRevealCue()
    {
        SetObjectsActive(activateOnMonsterReveal, false);
    }

    public void StartMonsterEatingCue()
    {
        StartLoopingAudio(monsterEatingAudioSource, monsterEatingClip, monsterEatingVolume, monsterEatingPitch);
    }

    public void StopMonsterEatingCue()
    {
        StopLoopingAudio(monsterEatingAudioSource);
    }

    public void ShowDoorOpenDialogueCue()
    {
        PlayDialogueCue(doorOpenLine);
    }

    public void ShowStayQuietDialogueCue()
    {
        PlayDialogueCue(stayQuietLine);
    }

    public void ShowKitchenNoiseDialogueCue()
    {
        PlayDialogueCue(kitchenNoiseLine);
    }

    public void ShowRevealDialogueCue()
    {
        PlayDialogueCue(revealLine);
    }

    public void PlayCustomDialogue(string text, AudioClip voiceClip, float minimumDuration = 2f)
    {
        PlayDialogueCue(new DialogueLine
        {
            text = text,
            voiceClip = voiceClip,
            minimumDuration = minimumDuration
        });
    }

    public void ClearDialogueCue()
    {
        ClearDialogue();
    }

    private IEnumerator MovePlayerTo(Transform targetPoint, float moveSpeed, Transform lookTarget = null)
    {
        if (playerRoot == null || targetPoint == null)
            yield break;

        while (Vector3.Distance(playerRoot.position, targetPoint.position) > 0.05f)
        {
            if (skipRequested)
                yield break;

            Vector3 nextPosition = Vector3.MoveTowards(
                playerRoot.position,
                targetPoint.position,
                Mathf.Max(0.01f, moveSpeed) * Time.deltaTime);

            playerRoot.position = nextPosition;
            RotatePlayerTowards(lookTarget != null ? lookTarget.position : targetPoint.position);
            ResetCameraPitch();
            yield return null;
        }

        playerRoot.position = targetPoint.position;
        RotatePlayerTowards(lookTarget != null ? lookTarget.position : targetPoint.position);
        ResetCameraPitch(true);
    }

    private IEnumerator RotateTowards(Transform lookTarget, float holdDuration)
    {
        if (lookTarget == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < holdDuration)
        {
            if (skipRequested)
                yield break;

            elapsed += Time.deltaTime;
            RotatePlayerTowards(lookTarget.position);
            ResetCameraPitch();
            yield return null;
        }
    }

    private IEnumerator AnimateDoorOpen()
    {
        if (doorPivot == null)
            yield break;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, doorOpenDuration);

        while (elapsed < duration)
        {
            if (skipRequested)
                yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            ApplyDoorPose(t);
            yield return null;
        }

        ApplyDoorPose(1f);
    }

    private IEnumerator AnimateDoorOpenCueRoutine()
    {
        yield return AnimateDoorOpen();
        doorOpenRoutine = null;
    }

    private IEnumerator ShowDialogue(DialogueLine line)
    {
        if (line == null)
            yield break;

        SetSubtitle(line.text);
        if (line.voiceClip != null)
            PlayOneShot(voiceAudioSource, line.voiceClip);

        float duration = Mathf.Max(
            Mathf.Max(1f, line.minimumDuration),
            line.voiceClip != null ? line.voiceClip.length : 0f);

        yield return WaitSkippable(duration);
        SetSubtitle(string.Empty);
    }

    private void PlayDialogueCue(DialogueLine line)
    {
        StopActiveDialogue();
        activeDialogueRoutine = StartCoroutine(PlayDialogueCueRoutine(line));
    }

    private IEnumerator PlayDialogueCueRoutine(DialogueLine line)
    {
        yield return ShowDialogue(line);
        activeDialogueRoutine = null;
    }

    private IEnumerator FadeOverlay(float targetAlpha, float duration)
    {
        if (blackOverlayImage == null)
            yield break;

        float startAlpha = blackOverlayImage.color.a;
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            if (skipRequested)
                yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            SetOverlayAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetOverlayAlpha(targetAlpha);
    }

    private void StartOverlayFade(float targetAlpha, float duration)
    {
        StopOverlayFade();
        overlayFadeRoutine = StartCoroutine(FadeOverlayRoutine(targetAlpha, duration));
    }

    private IEnumerator FadeOverlayRoutine(float targetAlpha, float duration)
    {
        yield return FadeOverlay(targetAlpha, duration);
        overlayFadeRoutine = null;
    }

    private IEnumerator WaitSkippable(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (skipRequested)
                yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void ApplyIntroState()
    {
        CacheCharacterControllerState();
        SetBehavioursEnabled(disableBehavioursDuringIntro, false);
        SetObjectsActive(disableObjectsDuringIntro, false);
        EnsureGameplayPlayerVisible();
    }

    private void RestoreSceneStates()
    {
        RestoreBehaviourStates();
        RestoreObjectStates();
        RestoreCharacterControllerState();
    }

    private void CacheSceneStates()
    {
        behaviourStates.Clear();
        objectStates.Clear();

        if (gameplayPlayerObject != null)
            playerActiveStateBeforeIntro = gameplayPlayerObject.activeSelf;

        CacheBehaviourStates(disableBehavioursDuringIntro);
        CacheObjectStates(disableObjectsDuringIntro);
        CacheObjectStates(activateOnDoorOpen);
        CacheObjectStates(activateOnMonsterReveal);
    }

    private void CacheBehaviourStates(Behaviour[] behaviours)
    {
        if (behaviours == null)
            return;

        for (int i = 0; i < behaviours.Length; i++)
        {
            Behaviour behaviour = behaviours[i];
            if (behaviour != null && !behaviourStates.ContainsKey(behaviour))
                behaviourStates.Add(behaviour, behaviour.enabled);
        }
    }

    private void CacheObjectStates(GameObject[] objects)
    {
        if (objects == null)
            return;

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject targetObject = objects[i];
            if (targetObject != null && !objectStates.ContainsKey(targetObject))
                objectStates.Add(targetObject, targetObject.activeSelf);
        }
    }

    private void RestoreBehaviourStates()
    {
        foreach (KeyValuePair<Behaviour, bool> state in behaviourStates)
        {
            if (state.Key != null)
                state.Key.enabled = state.Value;
        }
    }

    private void RestoreObjectStates()
    {
        foreach (KeyValuePair<GameObject, bool> state in objectStates)
        {
            if (state.Key != null)
                state.Key.SetActive(state.Value);
        }
    }

    private void CacheCharacterControllerState()
    {
        if (playerCharacterController == null && playerRoot != null)
            playerCharacterController = playerRoot.GetComponent<CharacterController>();

        characterControllerWasEnabled = playerCharacterController != null && playerCharacterController.enabled;
        if (characterControllerWasEnabled)
            playerCharacterController.enabled = false;
    }

    private void RestoreCharacterControllerState()
    {
        if (playerCharacterController != null)
            playerCharacterController.enabled = characterControllerWasEnabled;
    }

    private void SetBehavioursEnabled(Behaviour[] behaviours, bool isEnabled)
    {
        if (behaviours == null)
            return;

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null)
                behaviours[i].enabled = isEnabled;
        }
    }

    private void SetObjectsActive(GameObject[] objects, bool isActive)
    {
        if (objects == null)
            return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
                objects[i].SetActive(isActive);
        }
    }

    private void EnsureGameplayPlayerVisible()
    {
        if (ShouldHideGameplayPlayerDuringIntro() || gameplayPlayerObject == null)
            return;

        if (!gameplayPlayerObject.activeSelf)
        {
            Debug.LogWarning("ApartmentIntroCutscene restored Player active state during intro.");
            gameplayPlayerObject.SetActive(true);
        }
    }

    private void EnsurePlayerActiveTracer()
    {
        if (gameplayPlayerObject == null)
            return;

        ActiveStateTracer tracer = gameplayPlayerObject.GetComponent<ActiveStateTracer>();
        if (tracer == null)
            tracer = gameplayPlayerObject.AddComponent<ActiveStateTracer>();

        tracer.Configure("Gameplay Player", true);
    }

    private bool ShouldHideGameplayPlayerDuringIntro()
    {
        return hideGameplayPlayerDuringIntro && playbackMode == IntroPlaybackMode.Scripted;
    }

    private void TeleportPlayer(Transform targetPoint)
    {
        if (playerRoot == null || targetPoint == null)
            return;

        playerRoot.SetPositionAndRotation(targetPoint.position, targetPoint.rotation);
        ResetCameraPitch(true);
    }

    private void RotatePlayerTowards(Vector3 worldTarget)
    {
        if (playerRoot == null)
            return;

        Vector3 direction = worldTarget - playerRoot.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        playerRoot.rotation = Quaternion.RotateTowards(
            playerRoot.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);
    }

    private void LookAtTargetInstant(Transform lookTarget)
    {
        if (playerRoot == null || lookTarget == null)
            return;

        Vector3 direction = lookTarget.position - playerRoot.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        playerRoot.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        ResetCameraPitch(true);
    }

    private void ResetCameraPitch(bool instant = false)
    {
        if (cameraPivot == null)
            return;

        cameraPivot.localRotation = instant
            ? Quaternion.identity
            : Quaternion.RotateTowards(cameraPivot.localRotation, Quaternion.identity, 240f * Time.deltaTime);
    }

    private void BuildOverlayIfNeeded()
    {
        if (overlayCanvas != null && blackOverlayImage != null && subtitleText != null)
            return;

        GameObject canvasObject = new GameObject("IntroCutsceneOverlay");
        canvasObject.transform.SetParent(transform, false);

        overlayCanvas = canvasObject.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 5000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject blackImageObject = new GameObject("BlackOverlay");
        blackImageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform blackRect = blackImageObject.AddComponent<RectTransform>();
        blackRect.anchorMin = Vector2.zero;
        blackRect.anchorMax = Vector2.one;
        blackRect.offsetMin = Vector2.zero;
        blackRect.offsetMax = Vector2.zero;

        blackOverlayImage = blackImageObject.AddComponent<Image>();
        blackOverlayImage.color = Color.black;
        blackOverlayImage.raycastTarget = false;

        GameObject subtitleObject = new GameObject("SubtitleText");
        subtitleObject.transform.SetParent(canvasObject.transform, false);

        RectTransform subtitleRect = subtitleObject.AddComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0.1f, 0.06f);
        subtitleRect.anchorMax = new Vector2(0.9f, 0.18f);
        subtitleRect.offsetMin = Vector2.zero;
        subtitleRect.offsetMax = Vector2.zero;

        subtitleText = subtitleObject.AddComponent<Text>();
        subtitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subtitleText.fontSize = 28;
        subtitleText.alignment = TextAnchor.LowerCenter;
        subtitleText.color = new Color(1f, 1f, 1f, 0.96f);
        subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        subtitleText.verticalOverflow = VerticalWrapMode.Overflow;
        subtitleText.text = string.Empty;
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (blackOverlayImage == null)
            return;

        Color color = blackOverlayImage.color;
        color.a = Mathf.Clamp01(alpha);
        blackOverlayImage.color = color;
    }

    private void SetSubtitle(string text)
    {
        if (subtitleText != null)
            subtitleText.text = text;
    }

    private void PlayOneShot(AudioSource audioSource, AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }

    private void StartLoopingAudio(AudioSource audioSource, AudioClip clip, float volume, float pitch)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.volume = volume;
        audioSource.pitch = pitch;

        if (!audioSource.isPlaying)
            audioSource.Play();
    }

    private void StopLoopingAudio(AudioSource audioSource = null)
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            return;
        }

        if (footstepsAudioSource != null)
            footstepsAudioSource.Stop();

        if (monsterEatingAudioSource != null && !skipRequested)
            monsterEatingAudioSource.Stop();
    }

    private void StopOverlayFade()
    {
        if (overlayFadeRoutine == null)
            return;

        StopCoroutine(overlayFadeRoutine);
        overlayFadeRoutine = null;
    }

    private void StopActiveDialogue()
    {
        if (activeDialogueRoutine != null)
        {
            StopCoroutine(activeDialogueRoutine);
            activeDialogueRoutine = null;
        }
    }

    private void StopDoorAnimation()
    {
        if (doorOpenRoutine == null)
            return;

        StopCoroutine(doorOpenRoutine);
        doorOpenRoutine = null;
    }

    private void ClearDialogue()
    {
        StopActiveDialogue();

        if (voiceAudioSource != null)
            voiceAudioSource.Stop();

        SetSubtitle(string.Empty);
    }

    private float GetClipDuration(AudioClip clip, float fallbackDuration)
    {
        return clip != null ? clip.length : fallbackDuration;
    }

    private void CacheDoorRotations()
    {
        if (doorPivot == null)
            return;

        ResolveDoorMotionMode();

        if (doorMotionMode == DoorMotionMode.ChildPivotOrbit)
        {
            Transform animatedTransform = doorPivot.parent;
            if (animatedTransform == null)
                return;

            doorClosedPosition = animatedTransform.position;
            doorClosedWorldRotation = animatedTransform.rotation;

            Vector3 hingeLocalOffset = animatedTransform.InverseTransformPoint(doorPivot.position);
            Vector3 hingeWorldPosition = doorPivot.position;

            doorOpenWorldRotation = doorClosedWorldRotation * Quaternion.Euler(0f, doorOpenAngle, 0f);
            doorOpenPosition = hingeWorldPosition - (doorOpenWorldRotation * hingeLocalOffset);
            return;
        }

        Transform rotationTarget = GetDoorRotationTarget();
        if (rotationTarget == null)
            return;

        doorClosedRotation = rotationTarget.localRotation;
        doorOpenRotation = doorClosedRotation * Quaternion.Euler(0f, doorOpenAngle, 0f);
    }

    private void ResolveDoorMotionMode()
    {
        if (doorPivot == null)
        {
            doorMotionMode = DoorMotionMode.SelfRotation;
            return;
        }

        if (doorPivot.childCount > 0)
        {
            doorMotionMode = DoorMotionMode.ExternalPivotRotation;
            return;
        }

        if (doorPivot.parent != null)
        {
            doorMotionMode = DoorMotionMode.ChildPivotOrbit;
            return;
        }

        if (doorPivot.parent == null)
        {
            doorMotionMode = DoorMotionMode.SelfRotation;
            return;
        }
    }

    private Transform GetDoorAnimatedTransform()
    {
        if (doorPivot == null)
            return null;

        return doorMotionMode == DoorMotionMode.ChildPivotOrbit ? doorPivot.parent : doorPivot;
    }

    private Transform GetDoorRotationTarget()
    {
        return doorMotionMode == DoorMotionMode.ChildPivotOrbit ? GetDoorAnimatedTransform() : doorPivot;
    }

    private void ApplyDoorPose(float normalizedTime)
    {
        float t = Mathf.Clamp01(normalizedTime);

        if (doorMotionMode == DoorMotionMode.ChildPivotOrbit)
        {
            Transform animatedTransform = GetDoorAnimatedTransform();
            if (animatedTransform == null)
                return;

            animatedTransform.SetPositionAndRotation(
                Vector3.Lerp(doorClosedPosition, doorOpenPosition, t),
                Quaternion.Slerp(doorClosedWorldRotation, doorOpenWorldRotation, t));
            return;
        }

        Transform rotationTarget = GetDoorRotationTarget();
        if (rotationTarget == null)
            return;

        rotationTarget.localRotation = Quaternion.Slerp(doorClosedRotation, doorOpenRotation, t);
    }

    private void ResolveReferences()
    {
        if (cutsceneController == null)
            cutsceneController = GetComponent<CutsceneController>() ?? GetComponentInParent<CutsceneController>();

        if (timelineDirector == null)
            timelineDirector = GetComponent<PlayableDirector>();

        if (playerRoot == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
                playerRoot = playerObject.transform;
        }

        if (gameplayPlayerObject == null && playerRoot != null)
            gameplayPlayerObject = playerRoot.gameObject;

        if (playerCamera == null && playerRoot != null)
            playerCamera = playerRoot.GetComponentInChildren<Camera>(true);

        if (gameplayCamera == null)
            gameplayCamera = playerCamera;

        if (cameraPivot == null && playerCamera != null && playerCamera.transform.parent != null)
            cameraPivot = playerCamera.transform.parent;

        EnsureAudioSource(ref effectsAudioSource, "IntroEffectsAudio", transform, false);
        EnsureAudioSource(ref footstepsAudioSource, "IntroFootstepsAudio", transform, false);
        EnsureAudioSource(ref voiceAudioSource, "IntroVoiceAudio", transform, false);

        Transform monsterAudioParent = monsterEatingSourcePoint != null ? monsterEatingSourcePoint : transform;
        EnsureAudioSource(ref monsterEatingAudioSource, "IntroMonsterEatingAudio", monsterAudioParent, true);

        if (monsterEatingAudioSource != null)
        {
            monsterEatingAudioSource.spatialBlend = 1f;
            monsterEatingAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            monsterEatingAudioSource.minDistance = 1.5f;
            monsterEatingAudioSource.maxDistance = 18f;
        }
    }

    private PlayableDirector GetTimelineDirector()
    {
        if (cutsceneController != null && cutsceneController.Director != null)
            return cutsceneController.Director;

        return timelineDirector;
    }

    private void EnsureAudioSource(ref AudioSource source, string name, Transform parent, bool spatial)
    {
        if (source != null)
            return;

        GameObject sourceObject = new GameObject(name);
        sourceObject.transform.SetParent(parent, false);
        source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = spatial ? 1f : 0f;
    }

    private void PrepareSceneForIntro()
    {
        if (ShouldHideGameplayPlayerDuringIntro())
            SetActiveIfAssigned(gameplayPlayerObject, false);

        SetActiveIfAssigned(gameplayMonsterObject, false);

        if (cutsceneController == null)
        {
            SetCameraPresentation(gameplayCamera, false);
            SetCameraPresentation(cinematicCamera, true);
        }
    }

    private void ApplyGameplayPresentation()
    {
        if (ShouldHideGameplayPlayerDuringIntro())
            SetActiveIfAssigned(gameplayPlayerObject, true);
        else if (gameplayPlayerObject != null && !playerActiveStateBeforeIntro)
            gameplayPlayerObject.SetActive(false);

        SetActiveIfAssigned(gameplayMonsterObject, true);

        if (cutsceneController == null)
        {
            SetCameraPresentation(gameplayCamera, true);
            SetCameraPresentation(cinematicCamera, false);
        }
    }

    private void SetActiveIfAssigned(GameObject targetObject, bool isActive)
    {
        if (targetObject != null)
            targetObject.SetActive(isActive);
    }

    private void SetCameraPresentation(Camera targetCamera, bool isVisible)
    {
        if (targetCamera == null)
            return;

        GameObject cameraObject = targetCamera.gameObject;
        if (cameraObject != null && cameraObject.activeSelf != isVisible)
            cameraObject.SetActive(isVisible);

        targetCamera.enabled = isVisible;
    }

    private void CreateFallbackClipsIfNeeded()
    {
        if (elevatorClip == null)
            elevatorClip = CreateElevatorPlaceholder();

        if (keysSearchClip == null)
            keysSearchClip = CreateKeysPlaceholder();

        if (keyInsertClip == null)
            keyInsertClip = CreateLockPlaceholder();

        if (doorOpenClip == null)
            doorOpenClip = CreateDoorPlaceholder();

        if (monsterEatingClip == null)
            monsterEatingClip = CreateMonsterEatingPlaceholder();
    }

    private AudioClip CreateElevatorPlaceholder()
    {
        const int sampleRate = 44100;
        const float duration = 2.2f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float hum = Mathf.Sin(t * Mathf.PI * 2f * 55f) * 0.18f;
            hum += Mathf.Sin(t * Mathf.PI * 2f * 110f) * 0.08f;

            float dingStart = duration - 0.55f;
            float ding = 0f;

            if (t >= dingStart)
            {
                float dingTime = t - dingStart;
                float envelope = Mathf.Exp(-dingTime * 5f);
                ding = Mathf.Sin(dingTime * Mathf.PI * 2f * 880f) * envelope * 0.3f;
            }

            samples[i] = hum + ding;
        }

        return CreateClip("IntroElevatorPlaceholder", samples, sampleRate);
    }

    private AudioClip CreateKeysPlaceholder()
    {
        const int sampleRate = 44100;
        const float duration = 1.3f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Clamp01(1f - (t / duration));
            float noise = (Mathf.PerlinNoise(t * 80f, 0.1f) * 2f - 1f) * 0.06f;
            float metallic = Mathf.Sin(t * Mathf.PI * 2f * 1700f) * 0.11f;
            metallic += Mathf.Sin(t * Mathf.PI * 2f * 2400f) * 0.07f;
            samples[i] = (noise + metallic) * envelope;
        }

        return CreateClip("IntroKeysPlaceholder", samples, sampleRate);
    }

    private AudioClip CreateLockPlaceholder()
    {
        const int sampleRate = 44100;
        const float duration = 0.55f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float click = 0f;

            click += CreateImpulse(t, 0.08f, 2400f, 0.18f);
            click += CreateImpulse(t, 0.26f, 1500f, 0.24f);
            click += CreateImpulse(t, 0.33f, 900f, 0.2f);

            samples[i] = click;
        }

        return CreateClip("IntroLockPlaceholder", samples, sampleRate);
    }

    private AudioClip CreateDoorPlaceholder()
    {
        const int sampleRate = 44100;
        const float duration = 1.0f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Clamp01(1f - (t / duration));
            float creak = Mathf.Sin(t * Mathf.PI * 2f * Mathf.Lerp(140f, 70f, t / duration)) * 0.12f;
            float rasp = (Mathf.PerlinNoise(t * 35f, 0.7f) * 2f - 1f) * 0.05f;
            float knock = CreateImpulse(t, 0.12f, 160f, 0.18f);
            samples[i] = (creak + rasp) * envelope + knock;
        }

        return CreateClip("IntroDoorPlaceholder", samples, sampleRate);
    }

    private AudioClip CreateMonsterEatingPlaceholder()
    {
        const int sampleRate = 44100;
        const float duration = 3.4f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;

            float wetBody = Mathf.Sin(t * Mathf.PI * 2f * 72f) * 0.05f;
            wetBody += Mathf.Sin(t * Mathf.PI * 2f * 104f) * 0.03f;

            float squelchNoise = (Mathf.PerlinNoise(t * 16f, 0.23f) * 2f - 1f) * 0.06f;
            float grindNoise = (Mathf.PerlinNoise(t * 43f, 1.41f) * 2f - 1f) * 0.025f;

            float bitePulse = 0f;
            bitePulse += CreateImpulse(t, 0.34f, 210f, 0.16f);
            bitePulse += CreateImpulse(t, 0.92f, 180f, 0.13f);
            bitePulse += CreateImpulse(t, 1.47f, 240f, 0.18f);
            bitePulse += CreateImpulse(t, 2.08f, 160f, 0.11f);
            bitePulse += CreateImpulse(t, 2.72f, 220f, 0.15f);

            float chewEnvelope = 0.82f + Mathf.Sin(t * Mathf.PI * 2f * 0.61f) * 0.18f;
            samples[i] = (wetBody + squelchNoise + grindNoise) * chewEnvelope + bitePulse;
        }

        return CreateClip("IntroMonsterEatingPlaceholder", samples, sampleRate);
    }

    private float CreateImpulse(float time, float center, float frequency, float amplitude)
    {
        float delta = Mathf.Abs(time - center);
        if (delta > 0.08f)
            return 0f;

        float envelope = Mathf.Exp(-delta * 50f);
        return Mathf.Sin((time - center) * Mathf.PI * 2f * frequency) * envelope * amplitude;
    }

    private AudioClip CreateClip(string clipName, float[] samples, int sampleRate)
    {
        AudioClip clip = AudioClip.Create(clipName, samples.Length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

#if UNITY_EDITOR
    private struct TimelineBuildTimes
    {
        public double elevatorStart;
        public double elevatorEnd;
        public double footstepsStart;
        public double fadeStart;
        public double doorApproachEnd;
        public double keysStart;
        public double keyInsertStart;
        public double doorOpenStart;
        public double doorOpenEnd;
        public double doorOpenDialogueStart;
        public double doorOpenDialogueEnd;
        public double stayQuietStart;
        public double stayQuietEnd;
        public double indoorMoveStart;
        public double apartmentEntryEnd;
        public double kitchenNoiseStart;
        public double kitchenNoiseEnd;
        public double corridorMoveStart;
        public double corridorSneakEnd;
        public double kitchenRevealEnd;
        public double revealDialogueStart;
        public double endTime;
    }

    private void OnValidate()
    {
        ResolveReferences();

        if (corridorFootstepsClip == null)
            corridorFootstepsClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFXs/heavy-walking-footsteps.mp3");

        TryAssignVoiceClip(doorOpenLine,
            "Assets/SFXs/Cutscenes/stranno.mp3",
            "Assets/SFXs/Cutscenes/stranno.MP3");

        TryAssignVoiceClip(stayQuietLine,
            "Assets/SFXs/Cutscenes/nadovestisebia.mp3",
            "Assets/SFXs/Cutscenes/nadovestisebia.MP3");

        TryAssignVoiceClip(kitchenNoiseLine,
            "Assets/SFXs/Cutscenes/chto-za.mp3",
            "Assets/SFXs/Cutscenes/chto-za.MP3");

        TryAssignVoiceClip(revealLine,
            "Assets/SFXs/Cutscenes/kakogo cherta.mp3",
            "Assets/SFXs/Cutscenes/kakogo cherta.MP3");

        ScheduleTimelineRebuildIfNeeded();
    }

    private void ScheduleTimelineRebuildIfNeeded()
    {
        if (Application.isPlaying || editorTimelineRebuildScheduled || editorTimelineRebuildInProgress)
            return;

        if (playbackMode != IntroPlaybackMode.Timeline)
            return;

        PlayableDirector activeDirector = GetTimelineDirector();
        if (activeDirector == null)
            return;

        TimelineAsset timelineAsset = activeDirector.playableAsset as TimelineAsset;
        if (timelineAsset == null)
            return;

        if (!ShouldRebuildGeneratedTimeline(timelineAsset))
            return;

        ApartmentIntroCutscene target = this;
        editorTimelineRebuildScheduled = true;
        EditorApplication.delayCall += () =>
        {
            editorTimelineRebuildScheduled = false;

            if (editorTimelineRebuildInProgress || target == null)
                return;

            editorTimelineRebuildInProgress = true;
            try
            {
                target.RebuildTimelineFromCurrentSetup();
                EditorUtility.SetDirty(target);
            }
            finally
            {
                editorTimelineRebuildInProgress = false;
            }
        };
    }

    private bool TimelineHasGeneratedIntroTracks(TimelineAsset timelineAsset)
    {
        bool hasIntroEvents = false;
        bool hasPlayer = false;
        bool hasCamera = false;

        foreach (TrackAsset track in timelineAsset.GetOutputTracks())
        {
            if (track == null)
                continue;

            if (track.name == "Intro Events")
                hasIntroEvents = true;
            else if (track.name == "Player")
                hasPlayer = true;
            else if (track.name == "Cinematic Camera")
                hasCamera = true;
        }

        return hasIntroEvents && hasPlayer && (!ShouldGenerateSeparateCameraTrack() || hasCamera);
    }

    [ContextMenu("Rebuild Timeline From Current Setup")]
    public void RebuildTimelineFromCurrentSetup()
    {
        ResolveReferences();
        CacheDoorRotations();
        CreateFallbackClipsIfNeeded();

        PlayableDirector activeDirector = GetTimelineDirector();

        if (activeDirector == null)
        {
            if (cutsceneController != null && cutsceneController.Director == null)
                activeDirector = Undo.AddComponent<PlayableDirector>(cutsceneController.gameObject);
            else if (timelineDirector == null)
                timelineDirector = Undo.AddComponent<PlayableDirector>(gameObject);

            activeDirector = GetTimelineDirector();
        }

        TimelineAsset timelineAsset = CreateOrReuseTimelineAsset();
        if (timelineAsset == null)
            return;

        SignalReceiver receiver = EnsureSignalReceiver();
        ClearSignalReceiver(receiver);
        ClearTimelineAsset(timelineAsset);

        BuildTimelineAsset(timelineAsset, receiver);

        activeDirector.playableAsset = timelineAsset;
        playbackMode = IntroPlaybackMode.Timeline;
        generatedTimelineVersion = CurrentGeneratedTimelineVersion;
        generatedTimelineConfigHash = ComputeGeneratedTimelineConfigHash();

        EditorUtility.SetDirty(activeDirector);
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public Object GetTimelineAssetForEditor()
    {
        PlayableDirector activeDirector = GetTimelineDirector();
        return activeDirector != null ? activeDirector.playableAsset : null;
    }

    private TimelineAsset CreateOrReuseTimelineAsset()
    {
        PlayableDirector activeDirector = GetTimelineDirector();
        TimelineAsset existingTimeline = activeDirector != null ? activeDirector.playableAsset as TimelineAsset : null;
        if (existingTimeline != null)
            return existingTimeline;

        string folderPath = EnsureGeneratedTimelineFolder();
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{sceneName}_ApartmentIntroTimeline.playable");

        TimelineAsset timelineAsset = ScriptableObject.CreateInstance<TimelineAsset>();
        timelineAsset.name = $"{sceneName}_ApartmentIntroTimeline";
        timelineAsset.editorSettings.frameRate = 60.0;
        timelineAsset.durationMode = TimelineAsset.DurationMode.FixedLength;
        AssetDatabase.CreateAsset(timelineAsset, assetPath);
        return timelineAsset;
    }

    private bool ShouldRebuildGeneratedTimeline(TimelineAsset timelineAsset)
    {
        if (timelineAsset == null)
            return false;

        if (!TimelineHasGeneratedIntroTracks(timelineAsset))
            return true;

        if (generatedTimelineVersion < CurrentGeneratedTimelineVersion)
            return true;

        return generatedTimelineConfigHash != ComputeGeneratedTimelineConfigHash();
    }

    private int ComputeGeneratedTimelineConfigHash()
    {
        unchecked
        {
            int hash = 17;

            AddHash(ref hash, playbackMode);
            AddHash(ref hash, introStartPoint);
            AddHash(ref hash, doorApproachPoint);
            AddHash(ref hash, apartmentEntryPoint);
            AddHash(ref hash, corridorSneakPoint);
            AddHash(ref hash, kitchenRevealPoint);
            AddHash(ref hash, kitchenLookTarget);
            AddHash(ref hash, monsterEatingSourcePoint);
            AddHash(ref hash, doorPivot);
            AddHash(ref hash, indoorPathWaypoints);
            AddHash(ref hash, cameraIndoorPathWaypoints);
            AddHash(ref hash, finalCameraTurnDuration);
            AddHash(ref hash, forceClockwiseFinalCameraTurn);
            AddHash(ref hash, initialBlackHold);
            AddHash(ref hash, footstepsLeadInBeforeFade);
            AddHash(ref hash, blackFadeDuration);
            AddHash(ref hash, approachSpeed);
            AddHash(ref hash, sneakSpeed);
            AddHash(ref hash, doorOpenAngle);
            AddHash(ref hash, doorOpenDuration);
            AddHash(ref hash, playIndoorFootstepsAfterDoor);

            return hash;
        }
    }

    private static void AddHash(ref int hash, float value)
    {
        hash = (hash * 31) + value.GetHashCode();
    }

    private static void AddHash(ref int hash, bool value)
    {
        hash = (hash * 31) + (value ? 1 : 0);
    }

    private static void AddHash<T>(ref int hash, T value) where T : System.Enum
    {
        hash = (hash * 31) + value.GetHashCode();
    }

    private static void AddHash(ref int hash, Transform transform)
    {
        if (transform == null)
        {
            hash *= 31;
            return;
        }

        hash = (hash * 31) + transform.GetInstanceID();
        hash = (hash * 31) + transform.position.GetHashCode();
        hash = (hash * 31) + transform.rotation.GetHashCode();
    }

    private static void AddHash(ref int hash, Transform[] transforms)
    {
        if (transforms == null)
        {
            hash *= 31;
            return;
        }

        hash = (hash * 31) + transforms.Length;
        for (int i = 0; i < transforms.Length; i++)
            AddHash(ref hash, transforms[i]);
    }

#if UNITY_EDITOR
    private void EnsureTimelineGeneratedUpToDateInEditor()
    {
        if (playbackMode != IntroPlaybackMode.Timeline)
            return;

        PlayableDirector activeDirector = GetTimelineDirector();
        TimelineAsset timelineAsset = activeDirector != null ? activeDirector.playableAsset as TimelineAsset : null;
        if (!ShouldRebuildGeneratedTimeline(timelineAsset))
            return;

        bool previousRebuildState = editorTimelineRebuildInProgress;
        editorTimelineRebuildInProgress = true;
        try
        {
            RebuildTimelineFromCurrentSetup();
        }
        finally
        {
            editorTimelineRebuildInProgress = previousRebuildState;
        }
    }
#endif

    private string EnsureGeneratedTimelineFolder()
    {
        const string rootFolder = "Assets/Cutscenes";
        const string generatedFolder = "Assets/Cutscenes/Generated";

        if (!AssetDatabase.IsValidFolder(rootFolder))
            AssetDatabase.CreateFolder("Assets", "Cutscenes");

        if (!AssetDatabase.IsValidFolder(generatedFolder))
            AssetDatabase.CreateFolder(rootFolder, "Generated");

        return generatedFolder;
    }

    private void ClearTimelineAsset(TimelineAsset timelineAsset)
    {
        if (timelineAsset == null)
            return;

        List<TrackAsset> tracks = new List<TrackAsset>(timelineAsset.GetOutputTracks());
        for (int i = 0; i < tracks.Count; i++)
            timelineAsset.DeleteTrack(tracks[i]);

        string assetPath = AssetDatabase.GetAssetPath(timelineAsset);
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        for (int i = 0; i < subAssets.Length; i++)
        {
            Object subAsset = subAssets[i];
            if (subAsset == null || subAsset == timelineAsset)
                continue;

            if (subAsset is MonoScript)
                continue;

            Object.DestroyImmediate(subAsset, true);
        }
    }

    private SignalReceiver EnsureSignalReceiver()
    {
        GameObject receiverHost = cutsceneController != null ? cutsceneController.gameObject : gameObject;
        SignalReceiver receiver = receiverHost.GetComponent<SignalReceiver>();
        if (receiver == null)
            receiver = Undo.AddComponent<SignalReceiver>(receiverHost);

        return receiver;
    }

    private void ClearSignalReceiver(SignalReceiver receiver)
    {
        if (receiver == null)
            return;

        for (int i = receiver.Count() - 1; i >= 0; i--)
            receiver.RemoveAtIndex(i);

        EditorUtility.SetDirty(receiver);
    }

    private void BuildTimelineAsset(TimelineAsset timelineAsset, SignalReceiver receiver)
    {
        TimelineBuildTimes times = CalculateTimelineBuildTimes();
        timelineAsset.fixedDuration = times.endTime;
        PlayableDirector activeDirector = GetTimelineDirector();

        SignalTrack signalTrack = timelineAsset.CreateTrack<SignalTrack>("Intro Events");
        if (activeDirector != null)
            activeDirector.SetGenericBinding(signalTrack, receiver);

        AddSignalEvent(timelineAsset, signalTrack, receiver, 0d, "PrepareIntro", PrepareTimelineIntro);
        AddSignalEvent(timelineAsset, signalTrack, receiver, 0d, "SetBlackOverlay", SetBlackOverlay);
        AddSignalEvent(timelineAsset, signalTrack, receiver, 0d, "ResetDoorClosed", ResetDoorClosedCue);
        AddSignalEvent(timelineAsset, signalTrack, receiver, 0d, "DeactivateDoorObjects", DeactivateDoorOpenCue);
        AddSignalEvent(timelineAsset, signalTrack, receiver, 0d, "DeactivateRevealObjects", DeactivateMonsterRevealCue);
        AddSignalEvent(timelineAsset, signalTrack, receiver, 0d, "StopMonsterEating", StopMonsterEatingCue);
        AddSignalEvent(timelineAsset, signalTrack, receiver, 0d, "StopFootsteps", StopFootstepsCue);
        AddSignalEvent(timelineAsset, signalTrack, receiver, 0d, "ClearDialogue", ClearDialogueCue);

        AddSignalEvent(timelineAsset, signalTrack, receiver, times.elevatorStart, "PlayElevator", PlayElevatorCue);
        AddSignalEvent(timelineAsset, signalTrack, receiver, times.footstepsStart, "StartDoorApproachFootsteps", StartDoorApproachFootstepsCue);
        AddSignalEvent(timelineAsset, signalTrack, receiver, times.fadeStart, "FadeFromBlack", FadeFromBlack);
        AddSignalEvent(timelineAsset, signalTrack, receiver, times.doorApproachEnd, "StopDoorApproachFootsteps", StopFootstepsCue);
        AddSignalEvent(timelineAsset, signalTrack, receiver, times.keysStart, "PlayKeysSearch", PlayKeysSearchCue);
        AddSignalEvent(timelineAsset, signalTrack, receiver, times.keyInsertStart, "PlayKeyInsert", PlayKeyInsertCue);
        AddSignalEvent(timelineAsset, signalTrack, receiver, times.doorOpenStart, "PlayDoorOpen", PlayDoorOpenCue);
        AddSignalEvent(timelineAsset, signalTrack, receiver, times.doorOpenStart, "AnimateDoorOpen", AnimateDoorOpenCue);
        AddSignalEvent(timelineAsset, signalTrack, receiver, times.doorOpenEnd, "ActivateDoorOpen", ActivateDoorOpenCue);
        AddSignalEvent(timelineAsset, signalTrack, receiver, times.doorOpenEnd, "StartMonsterEating", StartMonsterEatingCue);
        AddSignalEvent(timelineAsset, signalTrack, receiver, times.doorOpenDialogueStart, "DoorOpenDialogue", ShowDoorOpenDialogueCue);
        AddSignalEvent(timelineAsset, signalTrack, receiver, times.stayQuietStart, "StayQuietDialogue", ShowStayQuietDialogueCue);

        if (playIndoorFootstepsAfterDoor)
            AddSignalEvent(timelineAsset, signalTrack, receiver, times.indoorMoveStart, "StartIndoorFootsteps", StartIndoorFootstepsCue);

        AddSignalEvent(timelineAsset, signalTrack, receiver, times.kitchenNoiseStart, "KitchenNoiseDialogue", ShowKitchenNoiseDialogueCue);
        AddSignalEvent(timelineAsset, signalTrack, receiver, times.kitchenRevealEnd, "StopIndoorFootsteps", StopFootstepsCue);
        AddSignalEvent(timelineAsset, signalTrack, receiver, times.kitchenRevealEnd, "ActivateMonsterReveal", ActivateMonsterRevealCue);
        AddSignalEvent(timelineAsset, signalTrack, receiver, times.revealDialogueStart, "RevealDialogue", ShowRevealDialogueCue);
        AddSignalEvent(timelineAsset, signalTrack, receiver, times.endTime, "FinishIntro", FinishTimelineIntro);

        BuildDoorTrack(timelineAsset, times);
        BuildPlayerTrack(timelineAsset, times);

        if (ShouldGenerateSeparateCameraTrack())
            BuildCameraTrack(timelineAsset, times);
    }

    private bool ShouldGenerateSeparateCameraTrack()
    {
        if (cutsceneController != null)
            return cutsceneController.SwitchToCutsceneCamera && !cutsceneController.MirrorGameplayCameraToCutsceneCamera && cinematicCamera != null;

        return cinematicCamera != null && cinematicCamera != playerCamera;
    }

    private TimelineBuildTimes CalculateTimelineBuildTimes()
    {
        TimelineBuildTimes times = new TimelineBuildTimes();
        Vector3 entryPosition = GetPointPosition(apartmentEntryPoint, GetPointPosition(doorApproachPoint, Vector3.zero));
        Vector3 corridorPosition = GetPointPosition(corridorSneakPoint, entryPosition);
        Vector3 revealPosition = GetPointPosition(kitchenRevealPoint, corridorPosition);
        Vector3[] indoorPathPoints = BuildIndoorPathPoints(entryPosition, corridorPosition, revealPosition, indoorPathWaypoints, out int corridorIndex);
        float indoorPathLength = GetPathLength(indoorPathPoints);
        float distanceToCorridor = GetDistanceAlongPathToIndex(indoorPathPoints, corridorIndex);
        float safeSneakSpeed = Mathf.Max(0.01f, sneakSpeed);
        double indoorPathDuration = indoorPathLength / safeSneakSpeed;

        double currentTime = 0d;
        currentTime += initialBlackHold;

        times.elevatorStart = currentTime;
        currentTime += System.Math.Max(initialBlackHold, GetClipDuration(elevatorClip, 1.8f) * 0.9f);
        times.elevatorEnd = currentTime;

        times.footstepsStart = currentTime;
        currentTime += footstepsLeadInBeforeFade;
        times.fadeStart = currentTime;

        currentTime += GetMoveDuration(introStartPoint, doorApproachPoint, approachSpeed);
        times.doorApproachEnd = currentTime;

        currentTime += 0.35d;
        times.keysStart = currentTime;
        currentTime += GetClipDuration(keysSearchClip, 1.5f);

        times.keyInsertStart = currentTime;
        currentTime += GetClipDuration(keyInsertClip, 0.65f);

        times.doorOpenStart = currentTime;
        currentTime += System.Math.Max(0.01d, doorOpenDuration);
        times.doorOpenEnd = currentTime;

        times.doorOpenDialogueStart = currentTime;
        currentTime += GetDialogueDuration(doorOpenLine);
        times.doorOpenDialogueEnd = currentTime;

        times.stayQuietStart = currentTime;
        currentTime += GetDialogueDuration(stayQuietLine);
        times.stayQuietEnd = currentTime;

        times.indoorMoveStart = currentTime;
        currentTime += GetMoveDuration(doorApproachPoint, apartmentEntryPoint, sneakSpeed);
        times.apartmentEntryEnd = currentTime;

        times.kitchenNoiseStart = currentTime;
        currentTime += GetDialogueDuration(kitchenNoiseLine);
        times.kitchenNoiseEnd = currentTime;

        times.corridorMoveStart = currentTime;
        currentTime += indoorPathDuration;
        if (indoorPathLength <= 0.0001f)
        {
            times.corridorSneakEnd = currentTime;
        }
        else
        {
            double corridorRatio = Mathf.Clamp01(distanceToCorridor / indoorPathLength);
            times.corridorSneakEnd = times.corridorMoveStart + indoorPathDuration * corridorRatio;
        }

        times.kitchenRevealEnd = currentTime;

        times.revealDialogueStart = currentTime + 0.5d;
        currentTime = times.revealDialogueStart + GetDialogueDuration(revealLine);
        times.endTime = currentTime;

        return times;
    }

    private double GetMoveDuration(Transform from, Transform to, float moveSpeed)
    {
        if (from == null || to == null)
            return 0d;

        float safeSpeed = Mathf.Max(0.01f, moveSpeed);
        return Vector3.Distance(from.position, to.position) / safeSpeed;
    }

    private double GetDialogueDuration(DialogueLine line)
    {
        if (line == null)
            return 0d;

        return Mathf.Max(Mathf.Max(1f, line.minimumDuration), line.voiceClip != null ? line.voiceClip.length : 0f);
    }

    private void AddSignalEvent(TimelineAsset timelineAsset, SignalTrack signalTrack, SignalReceiver receiver, double time, string signalName, UnityEngine.Events.UnityAction action)
    {
        SignalAsset signalAsset = ScriptableObject.CreateInstance<SignalAsset>();
        signalAsset.name = signalName;
        AssetDatabase.AddObjectToAsset(signalAsset, timelineAsset);

        UnityEngine.Events.UnityEvent reaction = new UnityEngine.Events.UnityEvent();
        UnityEventTools.AddPersistentListener(reaction, action);
        receiver.AddReaction(signalAsset, reaction);

        SignalEmitter emitter = signalTrack.CreateMarker<SignalEmitter>(time);
        emitter.asset = signalAsset;
        emitter.emitOnce = true;
        emitter.retroactive = false;

        EditorUtility.SetDirty(signalAsset);
    }

    private void BuildDoorTrack(TimelineAsset timelineAsset, TimelineBuildTimes times)
    {
        if (doorPivot == null)
            return;

        Transform animatedTransform = GetDoorAnimatedTransform();
        if (animatedTransform == null)
            return;

        Animator animator = EnsureAnimator(animatedTransform.gameObject);
        AnimationTrack track = timelineAsset.CreateTrack<AnimationTrack>("Door");
        track.trackOffset = TrackOffset.ApplySceneOffsets;

        PlayableDirector activeDirector = GetTimelineDirector();
        if (activeDirector != null)
            activeDirector.SetGenericBinding(track, animator);

        TimelineClip clip = track.CreateClip<AnimationPlayableAsset>();
        clip.displayName = "Door Open";
        clip.start = times.doorOpenStart;

        AnimationClip animationClip = CreateDoorAnimationClip();
        animationClip.name = "DoorOpen_Generated";
        AssetDatabase.AddObjectToAsset(animationClip, timelineAsset);

        AnimationPlayableAsset playableAsset = (AnimationPlayableAsset)clip.asset;
        playableAsset.clip = animationClip;
        playableAsset.removeStartOffset = false;
        playableAsset.useTrackMatchFields = false;
        clip.duration = animationClip.length;
    }

    private void BuildPlayerTrack(TimelineAsset timelineAsset, TimelineBuildTimes times)
    {
        if (playerRoot == null)
            return;

        Animator animator = EnsureAnimator(playerRoot.gameObject);
        AnimationTrack track = timelineAsset.CreateTrack<AnimationTrack>("Player");
        track.trackOffset = TrackOffset.ApplyTransformOffsets;

        PlayableDirector activeDirector = GetTimelineDirector();
        if (activeDirector != null)
            activeDirector.SetGenericBinding(track, animator);

        TimelineClip clip = track.CreateClip<AnimationPlayableAsset>();
        clip.displayName = "Player Path";
        clip.start = 0d;

        AnimationClip animationClip = CreatePlayerAnimationClip(playerRoot, times);
        animationClip.name = "PlayerPath_Generated";
        AssetDatabase.AddObjectToAsset(animationClip, timelineAsset);

        AnimationPlayableAsset playableAsset = (AnimationPlayableAsset)clip.asset;
        playableAsset.clip = animationClip;
        playableAsset.removeStartOffset = false;
        playableAsset.useTrackMatchFields = false;
        clip.duration = animationClip.length;
    }

    private void BuildCameraTrack(TimelineAsset timelineAsset, TimelineBuildTimes times)
    {
        bool useGameplayCameraTrack = cutsceneController != null
            && (!cutsceneController.SwitchToCutsceneCamera || cutsceneController.MirrorGameplayCameraToCutsceneCamera);
        Camera targetCamera = useGameplayCameraTrack || cinematicCamera == null ? playerCamera : cinematicCamera;
        if (targetCamera == null)
            return;

        Animator animator = EnsureAnimator(targetCamera.gameObject);
        AnimationTrack track = timelineAsset.CreateTrack<AnimationTrack>("Cinematic Camera");
        track.trackOffset = TrackOffset.ApplyTransformOffsets;

        PlayableDirector activeDirector = GetTimelineDirector();
        if (activeDirector != null)
            activeDirector.SetGenericBinding(track, animator);

        TimelineClip clip = track.CreateClip<AnimationPlayableAsset>();
        clip.displayName = "Camera Path";
        clip.start = 0d;

        AnimationClip animationClip = CreateCameraAnimationClip(targetCamera.transform, times);
        animationClip.name = "CameraPath_Generated";
        AssetDatabase.AddObjectToAsset(animationClip, timelineAsset);

        AnimationPlayableAsset playableAsset = (AnimationPlayableAsset)clip.asset;
        playableAsset.clip = animationClip;
        playableAsset.removeStartOffset = false;
        playableAsset.useTrackMatchFields = false;
        clip.duration = animationClip.length;
    }

    private AnimationClip CreateDoorAnimationClip()
    {
        AnimationClip clip = new AnimationClip
        {
            frameRate = 60f
        };

        float duration = Mathf.Max(0.01f, doorOpenDuration);

        if (doorMotionMode == DoorMotionMode.ChildPivotOrbit)
        {
            Transform animatedTransform = GetDoorAnimatedTransform();
            Transform parent = animatedTransform != null ? animatedTransform.parent : null;

            SetLocalPositionCurves(clip, parent, new[]
            {
                new TimedVector3(0f, doorClosedPosition),
                new TimedVector3(duration, doorOpenPosition)
            });

            SetLocalRotationCurves(clip, parent, new[]
            {
                new TimedQuaternion(0f, doorClosedWorldRotation),
                new TimedQuaternion(duration, doorOpenWorldRotation)
            });
        }
        else
        {
            SetLocalRotationCurves(clip, new[]
            {
                new TimedQuaternion(0f, doorClosedRotation),
                new TimedQuaternion(duration, doorOpenRotation)
            });
        }

        clip.EnsureQuaternionContinuity();
        return clip;
    }

    private AnimationClip CreatePlayerAnimationClip(Transform targetTransform, TimelineBuildTimes times)
    {
        Transform parent = targetTransform.parent;
        Vector3 fallbackPosition = targetTransform.position;
        Quaternion fallbackRotation = targetTransform.rotation;

        Vector3 introPosition = GetPointPosition(introStartPoint, fallbackPosition);
        Vector3 doorPosition = GetPointPosition(doorApproachPoint, introPosition);
        Vector3 entryPosition = GetPointPosition(apartmentEntryPoint, doorPosition);
        Vector3 corridorPosition = GetPointPosition(corridorSneakPoint, entryPosition);
        Vector3 revealPosition = GetPointPosition(kitchenRevealPoint, corridorPosition);

        Vector3 outsideLookTarget = GetPointPosition(doorPivot, doorPosition + targetTransform.forward);
        Vector3 insideLookTarget = GetPointPosition(monsterEatingSourcePoint, GetPointPosition(kitchenLookTarget, revealPosition + targetTransform.forward));
        Vector3[] indoorPathPoints = BuildIndoorPathPoints(entryPosition, corridorPosition, revealPosition, indoorPathWaypoints, out _);

        Quaternion introRotation = introStartPoint != null ? introStartPoint.rotation : fallbackRotation;
        Quaternion doorRotation = GetPlanarLookRotation(doorPosition, outsideLookTarget, introRotation);
        Quaternion entryRotation = GetPathFacingRotation(indoorPathPoints, 0, doorRotation, true);
        TimedVector3[] indoorMovementSamples = BuildTimedPathSamples(indoorPathPoints, (float)times.corridorMoveStart, (float)times.kitchenRevealEnd, false);
        TimedQuaternion[] indoorRotationSamples = BuildPathRotationSamples(indoorPathPoints, (float)times.corridorMoveStart, (float)times.kitchenRevealEnd, insideLookTarget, entryRotation, false, true);

        AnimationClip clip = new AnimationClip
        {
            frameRate = 60f
        };

        List<TimedVector3> positionSamples = new List<TimedVector3>
        {
            new TimedVector3(0f, introPosition),
            new TimedVector3((float)times.fadeStart, introPosition),
            new TimedVector3((float)times.doorApproachEnd, doorPosition),
            new TimedVector3((float)times.indoorMoveStart, doorPosition),
            new TimedVector3((float)times.apartmentEntryEnd, entryPosition),
            new TimedVector3((float)times.kitchenNoiseEnd, entryPosition)
        };
        positionSamples.AddRange(indoorMovementSamples);
        positionSamples.Add(new TimedVector3((float)times.endTime, revealPosition));
        SetLocalPositionCurves(clip, parent, positionSamples.ToArray());

        List<TimedQuaternion> rotationSamples = new List<TimedQuaternion>
        {
            new TimedQuaternion(0f, introRotation),
            new TimedQuaternion((float)times.fadeStart, introRotation),
            new TimedQuaternion((float)times.doorApproachEnd, doorRotation),
            new TimedQuaternion((float)times.indoorMoveStart, doorRotation),
            new TimedQuaternion((float)times.apartmentEntryEnd, entryRotation),
            new TimedQuaternion((float)times.kitchenNoiseEnd, entryRotation)
        };
        rotationSamples.AddRange(indoorRotationSamples);
        rotationSamples.Add(new TimedQuaternion((float)times.endTime, GetPlanarLookRotation(revealPosition, insideLookTarget, entryRotation)));
        SetLocalEulerRotationCurves(clip, parent, rotationSamples.ToArray(), false);

        clip.EnsureQuaternionContinuity();
        return clip;
    }

    private AnimationClip CreateCameraAnimationClip(Transform cameraTransform, TimelineBuildTimes times)
    {
        Transform parent = cameraTransform.parent;
        Vector3 fallbackPosition = cameraTransform.position;
        Quaternion fallbackRotation = cameraTransform.rotation;

        Vector3 introPosition = GetPointPosition(introStartPoint, fallbackPosition);
        Vector3 doorPosition = GetPointPosition(doorApproachPoint, introPosition);
        Vector3 entryPosition = GetPointPosition(apartmentEntryPoint, doorPosition);
        Vector3 corridorPosition = GetPointPosition(corridorSneakPoint, entryPosition);
        Vector3 revealPosition = GetPointPosition(kitchenRevealPoint, corridorPosition);
        Transform[] effectiveCameraWaypoints = cameraIndoorPathWaypoints != null && cameraIndoorPathWaypoints.Length > 0
            ? cameraIndoorPathWaypoints
            : indoorPathWaypoints;
        Vector3[] cameraPathPoints = BuildIndoorPathPoints(entryPosition, corridorPosition, revealPosition, effectiveCameraWaypoints, out _);

        Vector3 outsideLookTarget = GetPointPosition(doorPivot, doorPosition + Vector3.forward);
        Vector3 insideLookTarget = GetPointPosition(monsterEatingSourcePoint, GetPointPosition(kitchenLookTarget, revealPosition + Vector3.forward));

        Quaternion introRotation = GetLookRotation(introPosition, outsideLookTarget, fallbackRotation);
        Quaternion doorRotation = GetLookRotation(doorPosition, outsideLookTarget, introRotation);
        Quaternion travelRotation = GetPathFacingRotation(cameraPathPoints, 0, GetLookRotation(entryPosition, insideLookTarget, doorRotation));
        Quaternion revealRotation = GetLookRotation(revealPosition, insideLookTarget, travelRotation);
        float revealTurnStart = Mathf.Max((float)times.corridorMoveStart, (float)times.kitchenRevealEnd - Mathf.Max(0.01f, finalCameraTurnDuration));
        TimedVector3[] indoorMovementSamples = BuildTimedPathSamples(cameraPathPoints, (float)times.corridorMoveStart, (float)times.kitchenRevealEnd, false);

        AnimationClip clip = new AnimationClip
        {
            frameRate = 60f
        };

        List<TimedVector3> positionSamples = new List<TimedVector3>
        {
            new TimedVector3(0f, introPosition),
            new TimedVector3((float)times.fadeStart, introPosition),
            new TimedVector3((float)times.doorApproachEnd, doorPosition),
            new TimedVector3((float)times.indoorMoveStart, doorPosition),
            new TimedVector3((float)times.apartmentEntryEnd, entryPosition),
            new TimedVector3((float)times.kitchenNoiseEnd, entryPosition)
        };
        positionSamples.AddRange(indoorMovementSamples);
        positionSamples.Add(new TimedVector3((float)times.endTime, revealPosition));
        SetLocalPositionCurves(clip, parent, positionSamples.ToArray());

        SetLocalEulerRotationCurves(clip, parent, new[]
        {
            new TimedQuaternion(0f, introRotation),
            new TimedQuaternion((float)times.fadeStart, introRotation),
            new TimedQuaternion((float)times.doorApproachEnd, doorRotation),
            new TimedQuaternion((float)times.indoorMoveStart, travelRotation),
            new TimedQuaternion((float)times.apartmentEntryEnd, travelRotation),
            new TimedQuaternion((float)times.kitchenNoiseEnd, travelRotation),
            new TimedQuaternion(revealTurnStart, travelRotation),
            new TimedQuaternion((float)times.kitchenRevealEnd, revealRotation),
            new TimedQuaternion((float)times.endTime, revealRotation)
        }, forceClockwiseFinalCameraTurn);

        return clip;
    }

    private Animator EnsureAnimator(GameObject targetObject)
    {
        Animator animator = targetObject.GetComponent<Animator>();
        if (animator == null)
            animator = Undo.AddComponent<Animator>(targetObject);

        animator.applyRootMotion = false;
        return animator;
    }

    private Vector3 GetPointPosition(Transform point, Vector3 fallbackPosition)
    {
        return point != null ? point.position : fallbackPosition;
    }

    private Quaternion GetLookRotation(Vector3 fromPosition, Vector3 toPosition, Quaternion fallbackRotation)
    {
        Vector3 direction = toPosition - fromPosition;
        if (direction.sqrMagnitude <= 0.0001f)
            return fallbackRotation;

        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private Quaternion GetPlanarLookRotation(Vector3 fromPosition, Vector3 toPosition, Quaternion fallbackRotation)
    {
        Vector3 direction = toPosition - fromPosition;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
            return fallbackRotation;

        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private Vector3[] BuildIndoorPathPoints(Vector3 entryPosition, Vector3 corridorPosition, Vector3 revealPosition, Transform[] extraWaypoints, out int corridorIndex)
    {
        List<Vector3> points = new List<Vector3>();
        AddPathPoint(points, entryPosition);

        corridorIndex = points.Count - 1;
        bool hasCustomWaypoints = false;
        if (extraWaypoints != null)
        {
            for (int i = 0; i < extraWaypoints.Length; i++)
            {
                if (extraWaypoints[i] != null)
                {
                    hasCustomWaypoints = true;
                    AddPathPoint(points, extraWaypoints[i].position);
                }
            }
        }

        if (hasCustomWaypoints)
        {
            corridorIndex = Mathf.Min(points.Count - 1, 1);
        }
        else if (corridorSneakPoint != null)
        {
            AddPathPoint(points, corridorPosition);
            corridorIndex = points.Count - 1;
        }

        AddPathPoint(points, revealPosition);
        corridorIndex = Mathf.Clamp(corridorIndex, 0, points.Count - 1);
        return points.ToArray();
    }

    private void AddPathPoint(List<Vector3> points, Vector3 position)
    {
        if (points.Count == 0 || (points[points.Count - 1] - position).sqrMagnitude > 0.0001f)
            points.Add(position);
    }

    private float GetPathLength(IReadOnlyList<Vector3> points)
    {
        if (points == null || points.Count <= 1)
            return 0f;

        float length = 0f;
        for (int i = 1; i < points.Count; i++)
            length += Vector3.Distance(points[i - 1], points[i]);

        return length;
    }

    private float GetDistanceAlongPathToIndex(IReadOnlyList<Vector3> points, int index)
    {
        if (points == null || points.Count <= 1)
            return 0f;

        int clampedIndex = Mathf.Clamp(index, 0, points.Count - 1);
        float distance = 0f;
        for (int i = 1; i <= clampedIndex; i++)
            distance += Vector3.Distance(points[i - 1], points[i]);

        return distance;
    }

    private TimedVector3[] BuildTimedPathSamples(IReadOnlyList<Vector3> points, float startTime, float endTime, bool includeFirstPoint)
    {
        if (points == null || points.Count == 0)
            return new TimedVector3[0];

        if (points.Count == 1 || endTime <= startTime)
            return includeFirstPoint
                ? new[] { new TimedVector3(startTime, points[0]) }
                : new TimedVector3[0];

        float totalLength = GetPathLength(points);
        if (totalLength <= 0.0001f)
            return includeFirstPoint
                ? new[] { new TimedVector3(startTime, points[0]) }
                : new TimedVector3[0];

        List<TimedVector3> samples = new List<TimedVector3>();
        if (includeFirstPoint)
            samples.Add(new TimedVector3(startTime, points[0]));

        float accumulatedDistance = 0f;
        for (int i = 1; i < points.Count; i++)
        {
            accumulatedDistance += Vector3.Distance(points[i - 1], points[i]);
            float ratio = Mathf.Clamp01(accumulatedDistance / totalLength);
            float time = Mathf.Lerp(startTime, endTime, ratio);
            samples.Add(new TimedVector3(time, points[i]));
        }

        return samples.ToArray();
    }

    private TimedQuaternion[] BuildPathRotationSamples(IReadOnlyList<Vector3> points, float startTime, float endTime, Vector3 finalLookTarget, Quaternion fallbackRotation, bool includeFirstPoint, bool planarOnly = false)
    {
        if (points == null || points.Count == 0)
            return new TimedQuaternion[0];

        TimedVector3[] timedPoints = BuildTimedPathSamples(points, startTime, endTime, true);
        List<TimedQuaternion> samples = new List<TimedQuaternion>();

        for (int i = 0; i < timedPoints.Length; i++)
        {
            if (!includeFirstPoint && i == 0)
                continue;

            Quaternion rotation = i < timedPoints.Length - 1
                ? (planarOnly
                    ? GetPlanarLookRotation(timedPoints[i].value, timedPoints[i + 1].value, fallbackRotation)
                    : GetLookRotation(timedPoints[i].value, timedPoints[i + 1].value, fallbackRotation))
                : (planarOnly
                    ? GetPlanarLookRotation(timedPoints[i].value, finalLookTarget, fallbackRotation)
                    : GetLookRotation(timedPoints[i].value, finalLookTarget, fallbackRotation));

            fallbackRotation = rotation;
            samples.Add(new TimedQuaternion(timedPoints[i].time, rotation));
        }

        return samples.ToArray();
    }

    private Quaternion GetPathFacingRotation(IReadOnlyList<Vector3> points, int fromIndex, Quaternion fallbackRotation, bool planarOnly = false)
    {
        if (points == null || points.Count <= 1)
            return fallbackRotation;

        int clampedIndex = Mathf.Clamp(fromIndex, 0, points.Count - 2);
        return planarOnly
            ? GetPlanarLookRotation(points[clampedIndex], points[clampedIndex + 1], fallbackRotation)
            : GetLookRotation(points[clampedIndex], points[clampedIndex + 1], fallbackRotation);
    }

    private void SetLocalPositionCurves(AnimationClip clip, Transform parent, TimedVector3[] samples)
    {
        AnimationCurve xCurve = new AnimationCurve();
        AnimationCurve yCurve = new AnimationCurve();
        AnimationCurve zCurve = new AnimationCurve();

        for (int i = 0; i < samples.Length; i++)
        {
            Vector3 localPosition = parent != null ? parent.InverseTransformPoint(samples[i].value) : samples[i].value;
            xCurve.AddKey(samples[i].time, localPosition.x);
            yCurve.AddKey(samples[i].time, localPosition.y);
            zCurve.AddKey(samples[i].time, localPosition.z);
        }

        SetCurveTangentsLinear(xCurve);
        SetCurveTangentsLinear(yCurve);
        SetCurveTangentsLinear(zCurve);

        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "m_LocalPosition.x"), xCurve);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "m_LocalPosition.y"), yCurve);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "m_LocalPosition.z"), zCurve);
    }

    private void SetLocalRotationCurves(AnimationClip clip, TimedQuaternion[] samples)
    {
        SetLocalRotationCurves(clip, null, samples);
    }

    private void SetLocalRotationCurves(AnimationClip clip, Transform parent, TimedQuaternion[] samples)
    {
        AnimationCurve xCurve = new AnimationCurve();
        AnimationCurve yCurve = new AnimationCurve();
        AnimationCurve zCurve = new AnimationCurve();
        AnimationCurve wCurve = new AnimationCurve();

        for (int i = 0; i < samples.Length; i++)
        {
            Quaternion localRotation = parent != null
                ? Quaternion.Inverse(parent.rotation) * samples[i].value
                : samples[i].value;

            xCurve.AddKey(samples[i].time, localRotation.x);
            yCurve.AddKey(samples[i].time, localRotation.y);
            zCurve.AddKey(samples[i].time, localRotation.z);
            wCurve.AddKey(samples[i].time, localRotation.w);
        }

        SetCurveTangentsLinear(xCurve);
        SetCurveTangentsLinear(yCurve);
        SetCurveTangentsLinear(zCurve);
        SetCurveTangentsLinear(wCurve);

        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "m_LocalRotation.x"), xCurve);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "m_LocalRotation.y"), yCurve);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "m_LocalRotation.z"), zCurve);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "m_LocalRotation.w"), wCurve);
    }

    private void SetLocalEulerRotationCurves(AnimationClip clip, Transform parent, TimedQuaternion[] samples, bool forceClockwiseFinalYaw)
    {
        AnimationCurve xCurve = new AnimationCurve();
        AnimationCurve yCurve = new AnimationCurve();
        AnimationCurve zCurve = new AnimationCurve();
        Vector3 previousEuler = Vector3.zero;
        bool hasPreviousEuler = false;

        for (int i = 0; i < samples.Length; i++)
        {
            Quaternion localRotation = parent != null
                ? Quaternion.Inverse(parent.rotation) * samples[i].value
                : samples[i].value;

            Vector3 euler = localRotation.eulerAngles;
            if (hasPreviousEuler)
            {
                euler.x = previousEuler.x + Mathf.DeltaAngle(previousEuler.x, euler.x);
                euler.z = previousEuler.z + Mathf.DeltaAngle(previousEuler.z, euler.z);

                float yDelta = Mathf.DeltaAngle(previousEuler.y, euler.y);
                if (forceClockwiseFinalYaw && i == samples.Length - 1)
                {
                    while (yDelta < 0f)
                        yDelta += 360f;
                }

                euler.y = previousEuler.y + yDelta;
            }

            xCurve.AddKey(samples[i].time, euler.x);
            yCurve.AddKey(samples[i].time, euler.y);
            zCurve.AddKey(samples[i].time, euler.z);
            previousEuler = euler;
            hasPreviousEuler = true;
        }

        SetCurveTangentsLinear(xCurve);
        SetCurveTangentsLinear(yCurve);
        SetCurveTangentsLinear(zCurve);

        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "localEulerAngles.x"), xCurve);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "localEulerAngles.y"), yCurve);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "localEulerAngles.z"), zCurve);
    }

    private void SetCurveTangentsLinear(AnimationCurve curve)
    {
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
        }
    }

    private readonly struct TimedVector3
    {
        public readonly float time;
        public readonly Vector3 value;

        public TimedVector3(float time, Vector3 value)
        {
            this.time = time;
            this.value = value;
        }
    }

    private readonly struct TimedQuaternion
    {
        public readonly float time;
        public readonly Quaternion value;

        public TimedQuaternion(float time, Quaternion value)
        {
            this.time = time;
            this.value = value;
        }
    }

    private void TryAssignVoiceClip(DialogueLine line, params string[] candidatePaths)
    {
        if (line == null || line.voiceClip != null || candidatePaths == null)
            return;

        for (int i = 0; i < candidatePaths.Length; i++)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(candidatePaths[i]);
            if (clip == null)
                continue;

            line.voiceClip = clip;
            return;
        }
    }
#endif
}

internal sealed class ActiveStateTracer : MonoBehaviour
{
    [SerializeField] private string targetLabel;
    [SerializeField] private bool logEnable = true;
    [SerializeField] private bool logDisable = true;
    [SerializeField] private bool includeStackTraceOnDisable = true;

    public void Configure(string label, bool includeDisableStackTrace)
    {
        targetLabel = label;
        includeStackTraceOnDisable = includeDisableStackTrace;
    }

    private void OnEnable()
    {
        if (!logEnable)
            return;

        Debug.Log($"[ActiveStateTracer] Enabled: {GetLabel()}", this);
    }

    private void OnDisable()
    {
        if (!logDisable)
            return;

        if (includeStackTraceOnDisable)
        {
            Debug.LogWarning(
                $"[ActiveStateTracer] Disabled: {GetLabel()}\n{System.Environment.StackTrace}",
                this);
            return;
        }

        Debug.LogWarning($"[ActiveStateTracer] Disabled: {GetLabel()}", this);
    }

    private string GetLabel()
    {
        return string.IsNullOrWhiteSpace(targetLabel) ? gameObject.name : targetLabel;
    }
}
