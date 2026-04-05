using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Interaction
{
    public class DialogueSequenceTrigger : MonoBehaviour
    {
        private enum CompletionAction
        {
            None,
            DisableComponent,
            DestroyComponent
        }

        [Header("Trigger")]
        [SerializeField] private bool playOnInteraction = true;
        [SerializeField] private bool playOnce;
        [SerializeField] private bool interruptCurrentDialogue;
        [SerializeField] private bool queueIfBusy = true;
        [SerializeField, Min(0f)] private float startDelay;
        [SerializeField] private Transform playbackSource;
        [SerializeField] private CompletionAction completionAction;

        [Header("Lines")]
        [SerializeField] private DialogueLine[] lines;

        [Header("After Dialogue")]
        [SerializeField, Min(0f)] private float delayedEventDelay;
        [SerializeField] private UnityEvent onDialogueCompleted;

        private bool hasPlayed;
        private Coroutine delayedEventRoutine;

        public bool TryPlayFromInteraction(GameObject interactor)
        {
            if (!playOnInteraction)
                return false;

            return Play(interactor);
        }

        public bool Play(GameObject playbackHost = null)
        {
            if (playOnce && hasPlayed)
                return false;

            if (lines == null || lines.Length == 0)
                return false;

            DialogueSequencePlayer player = DialogueSequencePlayer.GetOrCreate(playbackHost);
            Transform resolvedPlaybackSource = playbackSource != null
                ? playbackSource
                : playbackHost != null
                    ? playbackHost.transform
                    : null;

            if (player == null)
                return false;

            if (delayedEventRoutine != null)
            {
                StopCoroutine(delayedEventRoutine);
                delayedEventRoutine = null;
            }

            bool started = player.Play(lines, resolvedPlaybackSource, interruptCurrentDialogue, queueIfBusy, startDelay, HandleDialogueCompleted);
            if (started)
                hasPlayed = true;

            return started;
        }

        public void ResetTrigger()
        {
            hasPlayed = false;
            enabled = true;

            if (delayedEventRoutine != null)
            {
                StopCoroutine(delayedEventRoutine);
                delayedEventRoutine = null;
            }
        }

        private void HandleDialogueCompleted()
        {
            if (!isActiveAndEnabled)
                return;

            if (delayedEventDelay > 0f)
            {
                delayedEventRoutine = StartCoroutine(InvokeDelayedCompletion());
                return;
            }

            InvokeCompletion();
        }

        private IEnumerator InvokeDelayedCompletion()
        {
            yield return new WaitForSeconds(delayedEventDelay);
            delayedEventRoutine = null;
            InvokeCompletion();
        }

        private void InvokeCompletion()
        {
            onDialogueCompleted?.Invoke();
            ApplyCompletionAction();
        }

        private void ApplyCompletionAction()
        {
            switch (completionAction)
            {
                case CompletionAction.DisableComponent:
                    enabled = false;
                    break;
                case CompletionAction.DestroyComponent:
                    Destroy(this);
                    break;
            }
        }
    }
}
