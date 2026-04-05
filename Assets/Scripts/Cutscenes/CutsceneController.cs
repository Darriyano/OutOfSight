using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using Unity.Cinemachine;

public class CutsceneController : MonoBehaviour
{
    [Header("Director")]
    [SerializeField] private PlayableDirector director;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool skippable = true;
    [SerializeField] private KeyCode skipKey = KeyCode.Escape;

    [Header("Cameras")]
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Camera cutsceneCamera;
    [SerializeField] private bool switchToCutsceneCamera = true;
    [SerializeField] private bool mirrorGameplayCameraToCutsceneCamera = true;
    [SerializeField] private bool disableCutsceneBrainWhileMirroring = true;

    [Header("Player")]
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Transform gameplayResumePoint;

    [Header("Gameplay Lock")]
    [SerializeField] private Behaviour[] disableBehavioursDuringCutscene;
    [SerializeField] private GameObject[] disableObjectsDuringCutscene;
    [SerializeField] private GameObject[] enableOnlyDuringCutscene;
    [SerializeField] private bool lockCursorDuringCutscene = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onCutsceneStarted;
    [SerializeField] private UnityEvent onCutsceneFinished;

    private readonly Dictionary<Behaviour, bool> behaviourStates = new Dictionary<Behaviour, bool>();
    private readonly Dictionary<GameObject, bool> objectStates = new Dictionary<GameObject, bool>();

    private bool isPlaying;
    private bool hasFinished;
    private bool gameplayCameraInitialState;
    private bool cutsceneCameraInitialState;
    private bool gameplayCameraObjectInitialState;
    private bool cutsceneCameraObjectInitialState;
    private bool gameplayAudioListenerInitialState;
    private bool cutsceneAudioListenerInitialState;
    private bool gameplayBrainInitialState;
    private bool cutsceneBrainInitialState;
    private CursorLockMode cursorLockModeBeforeCutscene;
    private bool cursorVisibleBeforeCutscene;
    private AudioListener gameplayAudioListener;
    private AudioListener cutsceneAudioListener;
    private CinemachineBrain gameplayCameraBrain;
    private CinemachineBrain cutsceneCameraBrain;

    public PlayableDirector Director
    {
        get
        {
            if (director == null)
                director = GetComponent<PlayableDirector>();

            return director;
        }
    }

    public bool SwitchToCutsceneCamera => switchToCutsceneCamera;
    public bool MirrorGameplayCameraToCutsceneCamera => switchToCutsceneCamera && mirrorGameplayCameraToCutsceneCamera;

    private void Awake()
    {
        if (director == null)
            director = GetComponent<PlayableDirector>();

        if (director != null)
            director.stopped += HandleDirectorStopped;

        gameplayAudioListener = gameplayCamera != null ? gameplayCamera.GetComponent<AudioListener>() : null;
        cutsceneAudioListener = cutsceneCamera != null ? cutsceneCamera.GetComponent<AudioListener>() : null;
        gameplayCameraBrain = gameplayCamera != null ? gameplayCamera.GetComponent<CinemachineBrain>() : null;
        cutsceneCameraBrain = cutsceneCamera != null ? cutsceneCamera.GetComponent<CinemachineBrain>() : null;

        SetObjectsActive(enableOnlyDuringCutscene, false);
    }

    private void Start()
    {
        if (playOnStart)
            PlayCutscene();
    }

    private void Update()
    {
        if (!isPlaying || !skippable)
            return;

        if (Input.GetKeyDown(skipKey))
            SkipCutscene();
    }

    private void LateUpdate()
    {
        if (!isPlaying || !MirrorGameplayCameraToCutsceneCamera || gameplayCamera == null || cutsceneCamera == null)
            return;

        SyncCutsceneCameraToGameplayCamera();
    }

    private void OnDestroy()
    {
        if (director != null)
            director.stopped -= HandleDirectorStopped;
    }

    public void PlayCutscene()
    {
        if (isPlaying)
            return;

        if (director == null)
        {
            Debug.LogWarning("CutsceneController requires a PlayableDirector.");
            return;
        }

        hasFinished = false;
        isPlaying = true;

        CacheStates();
        ApplyCutsceneState();

        onCutsceneStarted?.Invoke();
        director.time = 0d;
        director.Play();
    }

    public void SkipCutscene()
    {
        if (!isPlaying || director == null)
            return;

        director.time = director.duration;
        director.Evaluate();
        director.Pause();
        FinishCutscene();
    }

    public void FinishCutsceneFromSignal()
    {
        FinishCutscene();
    }

    private void HandleDirectorStopped(PlayableDirector stoppedDirector)
    {
        if (stoppedDirector != director)
            return;

        FinishCutscene();
    }

    private void FinishCutscene()
    {
        if (!isPlaying || hasFinished)
            return;

        hasFinished = true;
        isPlaying = false;

        RestoreState();
        TeleportPlayerToResumePoint();

        onCutsceneFinished?.Invoke();
    }

    private void CacheStates()
    {
        behaviourStates.Clear();
        objectStates.Clear();

        CacheBehaviourStates(disableBehavioursDuringCutscene);
        CacheObjectStates(disableObjectsDuringCutscene);
        CacheObjectStates(enableOnlyDuringCutscene);

        gameplayCameraInitialState = gameplayCamera != null && gameplayCamera.enabled;
        cutsceneCameraInitialState = cutsceneCamera != null && cutsceneCamera.enabled;
        gameplayCameraObjectInitialState = gameplayCamera != null && gameplayCamera.gameObject.activeSelf;
        cutsceneCameraObjectInitialState = cutsceneCamera != null && cutsceneCamera.gameObject.activeSelf;
        gameplayAudioListenerInitialState = gameplayAudioListener != null && gameplayAudioListener.enabled;
        cutsceneAudioListenerInitialState = cutsceneAudioListener != null && cutsceneAudioListener.enabled;
        gameplayBrainInitialState = gameplayCameraBrain != null && gameplayCameraBrain.enabled;
        cutsceneBrainInitialState = cutsceneCameraBrain != null && cutsceneCameraBrain.enabled;
        cursorLockModeBeforeCutscene = Cursor.lockState;
        cursorVisibleBeforeCutscene = Cursor.visible;
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

    private void ApplyCutsceneState()
    {
        SetBehavioursEnabled(disableBehavioursDuringCutscene, false);
        SetObjectsActive(disableObjectsDuringCutscene, false);
        SetObjectsActive(enableOnlyDuringCutscene, true);

        if (switchToCutsceneCamera)
        {
            SetCameraPresentation(gameplayCamera, false, MirrorGameplayCameraToCutsceneCamera);
            SetCameraPresentation(cutsceneCamera, true);

            if (MirrorGameplayCameraToCutsceneCamera)
            {
                if (disableCutsceneBrainWhileMirroring && cutsceneCameraBrain != null)
                    cutsceneCameraBrain.enabled = false;

                SyncCutsceneCameraToGameplayCamera();
            }
        }

        if (lockCursorDuringCutscene)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void RestoreState()
    {
        RestoreBehaviourStates();
        RestoreObjectStates();

        if (switchToCutsceneCamera)
        {
            RestoreCameraPresentation(gameplayCamera, gameplayCameraInitialState, gameplayCameraObjectInitialState, gameplayAudioListenerInitialState, gameplayBrainInitialState);
            RestoreCameraPresentation(cutsceneCamera, cutsceneCameraInitialState, cutsceneCameraObjectInitialState, cutsceneAudioListenerInitialState, cutsceneBrainInitialState);
        }

        if (lockCursorDuringCutscene)
        {
            Cursor.lockState = cursorLockModeBeforeCutscene;
            Cursor.visible = cursorVisibleBeforeCutscene;
        }
    }

    private void RestoreBehaviourStates()
    {
        foreach (KeyValuePair<Behaviour, bool> behaviourState in behaviourStates)
        {
            if (behaviourState.Key != null)
                behaviourState.Key.enabled = behaviourState.Value;
        }
    }

    private void RestoreObjectStates()
    {
        foreach (KeyValuePair<GameObject, bool> objectState in objectStates)
        {
            if (objectState.Key != null)
                objectState.Key.SetActive(objectState.Value);
        }
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

    private void SetCameraPresentation(Camera targetCamera, bool isVisible, bool keepObjectActiveWhenHidden = false)
    {
        if (targetCamera == null)
            return;

        GameObject cameraObject = targetCamera.gameObject;
        bool shouldKeepObjectActive = isVisible || keepObjectActiveWhenHidden;
        if (cameraObject != null && cameraObject.activeSelf != shouldKeepObjectActive)
            cameraObject.SetActive(shouldKeepObjectActive);

        targetCamera.enabled = isVisible;

        AudioListener audioListener = targetCamera.GetComponent<AudioListener>();
        if (audioListener != null)
            audioListener.enabled = isVisible;

        if (!isVisible)
        {
            CinemachineBrain brain = targetCamera.GetComponent<CinemachineBrain>();
            if (brain != null && keepObjectActiveWhenHidden)
                brain.enabled = false;
        }
    }

    private void SyncCutsceneCameraToGameplayCamera()
    {
        cutsceneCamera.transform.SetPositionAndRotation(gameplayCamera.transform.position, gameplayCamera.transform.rotation);
        cutsceneCamera.fieldOfView = gameplayCamera.fieldOfView;
        cutsceneCamera.nearClipPlane = gameplayCamera.nearClipPlane;
        cutsceneCamera.farClipPlane = gameplayCamera.farClipPlane;
        cutsceneCamera.orthographic = gameplayCamera.orthographic;
        cutsceneCamera.orthographicSize = gameplayCamera.orthographicSize;
    }

    private void RestoreCameraPresentation(Camera targetCamera, bool enabledState, bool objectActiveState, bool audioListenerState, bool brainState)
    {
        if (targetCamera == null)
            return;

        GameObject cameraObject = targetCamera.gameObject;
        if (cameraObject != null && cameraObject.activeSelf != objectActiveState)
            cameraObject.SetActive(objectActiveState);

        targetCamera.enabled = enabledState;

        AudioListener audioListener = targetCamera.GetComponent<AudioListener>();
        if (audioListener != null)
            audioListener.enabled = audioListenerState;

        CinemachineBrain brain = targetCamera.GetComponent<CinemachineBrain>();
        if (brain != null)
            brain.enabled = brainState;
    }

    private void TeleportPlayerToResumePoint()
    {
        if (playerRoot == null || gameplayResumePoint == null)
            return;

        CharacterController characterController = playerRoot.GetComponent<CharacterController>();
        bool controllerWasEnabled = characterController != null && characterController.enabled;

        if (controllerWasEnabled)
            characterController.enabled = false;

        playerRoot.SetPositionAndRotation(gameplayResumePoint.position, gameplayResumePoint.rotation);

        if (controllerWasEnabled)
            characterController.enabled = true;
    }
}
