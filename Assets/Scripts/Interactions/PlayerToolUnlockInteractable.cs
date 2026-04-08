using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Interaction
{
    [AddComponentMenu("Game/Interaction/Player Tool Unlock Interactable")]
    public class PlayerToolUnlockInteractable : MonoBehaviour, IInteractable
    {
        [Serializable]
        private sealed class BehaviourUnlockEntry
        {
            [SerializeField] private string componentTypeName = "WallListeningDevice";
            [SerializeField] private bool includeChildren = true;
            [SerializeField] private bool activateOwningGameObject = true;

            public string ComponentTypeName => componentTypeName;
            public bool IncludeChildren => includeChildren;
            public bool ActivateOwningGameObject => activateOwningGameObject;

            public bool Matches(Behaviour behaviour)
            {
                if (behaviour == null || string.IsNullOrWhiteSpace(componentTypeName))
                    return false;

                Type behaviourType = behaviour.GetType();
                return string.Equals(behaviourType.Name, componentTypeName, StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(behaviourType.FullName, componentTypeName, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Header("Prompt")]
        [SerializeField] private string pickupPrompt = "Pick up";
        [SerializeField] private string toolDisplayName = "Tool";

        [Header("Unlock")]
        [SerializeField] private BehaviourUnlockEntry[] behavioursToEnable;

        [Header("Dialogue")]
        [SerializeField] private DialogueLine[] pickupDialogueLines;
        [SerializeField] private Transform dialoguePlaybackSource;
        [SerializeField] private bool interruptCurrentDialogue;
        [SerializeField] private bool queueDialogueIfBusy = true;
        [SerializeField, Min(0f)] private float dialogueStartDelay;

        [Header("Optional")]
        [SerializeField] private AudioSource pickupAudioSource;
        [SerializeField] private bool destroyAfterPickup = true;

        private bool collected;
        private string cachedPrompt;
        private readonly List<Renderer> cachedRenderers = new List<Renderer>();
        private readonly List<Collider> cachedColliders = new List<Collider>();

        private void Awake()
        {
            RefreshPrompt();
            CachePresentationComponents();
        }

        private void OnValidate()
        {
            RefreshPrompt();
        }

        public string GetPrompt()
        {
            return cachedPrompt;
        }

        public bool CanInteract(GameObject interactor)
        {
            if (collected)
                return false;

            return HasAnyPendingUnlock(interactor) || HasDialogue();
        }

        public void Interact(GameObject interactor)
        {
            if (collected)
                return;

            bool unlockedAny = EnableConfiguredBehaviours(interactor);
            bool hasDialogue = HasDialogue();

            if (!unlockedAny && HasUnlockConfiguration() && !hasDialogue)
            {
                Debug.LogWarning($"{name} could not find matching player behaviours to enable.", this);
                return;
            }

            collected = true;
            PlayPickupAudio();
            HidePresentation();

            if (hasDialogue)
            {
                DialogueSequencePlayer player = DialogueSequencePlayer.GetOrCreate(interactor);
                if (player != null)
                {
                    Transform playbackSource = dialoguePlaybackSource != null
                        ? dialoguePlaybackSource
                        : interactor != null
                            ? interactor.transform
                            : null;

                    bool started = player.Play(
                        pickupDialogueLines,
                        playbackSource,
                        interruptCurrentDialogue,
                        queueDialogueIfBusy,
                        dialogueStartDelay,
                        FinalizePickup);

                    if (started)
                        return;
                }
            }

            FinalizePickup();
        }

        private bool HasAnyPendingUnlock(GameObject interactor)
        {
            if (!HasUnlockConfiguration())
                return false;

            Behaviour[] behaviours = interactor != null
                ? interactor.GetComponentsInChildren<Behaviour>(true)
                : Array.Empty<Behaviour>();

            for (int i = 0; i < behavioursToEnable.Length; i++)
            {
                BehaviourUnlockEntry entry = behavioursToEnable[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.ComponentTypeName))
                    continue;

                for (int j = 0; j < behaviours.Length; j++)
                {
                    Behaviour behaviour = behaviours[j];
                    if (behaviour == null || !entry.Matches(behaviour))
                        continue;

                    if (!entry.IncludeChildren && behaviour.gameObject != interactor)
                        continue;

                    if (!behaviour.enabled || !behaviour.gameObject.activeInHierarchy)
                        return true;
                }
            }

            return false;
        }

        private bool EnableConfiguredBehaviours(GameObject interactor)
        {
            if (!HasUnlockConfiguration() || interactor == null)
                return false;

            Behaviour[] behaviours = interactor.GetComponentsInChildren<Behaviour>(true);
            HashSet<Behaviour> enabledBehaviours = new HashSet<Behaviour>();

            for (int i = 0; i < behavioursToEnable.Length; i++)
            {
                BehaviourUnlockEntry entry = behavioursToEnable[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.ComponentTypeName))
                    continue;

                for (int j = 0; j < behaviours.Length; j++)
                {
                    Behaviour behaviour = behaviours[j];
                    if (behaviour == null || !entry.Matches(behaviour))
                        continue;

                    if (!entry.IncludeChildren && behaviour.gameObject != interactor)
                        continue;

                    if (entry.ActivateOwningGameObject && !behaviour.gameObject.activeSelf)
                        behaviour.gameObject.SetActive(true);

                    behaviour.enabled = true;
                    enabledBehaviours.Add(behaviour);
                }
            }

            return enabledBehaviours.Count > 0;
        }

        private bool HasUnlockConfiguration()
        {
            return behavioursToEnable != null && behavioursToEnable.Length > 0;
        }

        private bool HasDialogue()
        {
            return pickupDialogueLines != null && pickupDialogueLines.Length > 0;
        }

        private void FinalizePickup()
        {
            if (destroyAfterPickup)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);
        }

        private void RefreshPrompt()
        {
            if (string.IsNullOrWhiteSpace(toolDisplayName))
                cachedPrompt = pickupPrompt;
            else
                cachedPrompt = $"{pickupPrompt} {toolDisplayName}".Trim();
        }

        private void CachePresentationComponents()
        {
            cachedRenderers.Clear();
            cachedColliders.Clear();

            GetComponentsInChildren(true, cachedRenderers);
            GetComponentsInChildren(true, cachedColliders);
        }

        private void HidePresentation()
        {
            for (int i = 0; i < cachedRenderers.Count; i++)
            {
                if (cachedRenderers[i] != null)
                    cachedRenderers[i].enabled = false;
            }

            for (int i = 0; i < cachedColliders.Count; i++)
            {
                if (cachedColliders[i] != null)
                    cachedColliders[i].enabled = false;
            }
        }

        private void PlayPickupAudio()
        {
            if (pickupAudioSource == null || pickupAudioSource.clip == null)
                return;

            if (!pickupAudioSource.transform.IsChildOf(transform))
            {
                pickupAudioSource.Play();
                return;
            }

            GameObject audioObject = new GameObject($"{name}_PickupAudio");
            audioObject.transform.SetPositionAndRotation(pickupAudioSource.transform.position, pickupAudioSource.transform.rotation);

            AudioSource tempAudioSource = audioObject.AddComponent<AudioSource>();
            CopyAudioSettings(pickupAudioSource, tempAudioSource);
            tempAudioSource.PlayOneShot(pickupAudioSource.clip);

            float clipLifetime = pickupAudioSource.clip.length / Mathf.Max(0.01f, tempAudioSource.pitch);
            Destroy(audioObject, clipLifetime + 0.1f);
        }

        private static void CopyAudioSettings(AudioSource source, AudioSource destination)
        {
            destination.outputAudioMixerGroup = source.outputAudioMixerGroup;
            destination.mute = source.mute;
            destination.bypassEffects = source.bypassEffects;
            destination.bypassListenerEffects = source.bypassListenerEffects;
            destination.bypassReverbZones = source.bypassReverbZones;
            destination.playOnAwake = false;
            destination.loop = false;
            destination.priority = source.priority;
            destination.volume = source.volume;
            destination.pitch = source.pitch;
            destination.panStereo = source.panStereo;
            destination.spatialBlend = source.spatialBlend;
            destination.reverbZoneMix = source.reverbZoneMix;
            destination.dopplerLevel = source.dopplerLevel;
            destination.spread = source.spread;
            destination.rolloffMode = source.rolloffMode;
            destination.minDistance = source.minDistance;
            destination.maxDistance = source.maxDistance;
        }
    }
}
