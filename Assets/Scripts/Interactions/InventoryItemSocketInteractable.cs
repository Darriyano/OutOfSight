using UnityEngine;
using UnityEngine.Events;
using Game.Interaction;

namespace Game.Interaction
{
    public class InventoryItemSocketInteractable : MonoBehaviour, IInteractable
    {
        [Header("Requirement")]
        [SerializeField] private string requiredItemId = "ElevatorFuse";
        [SerializeField] private string requiredDisplayName = "item";
        [SerializeField] private int requiredItemCount = 1;
        [SerializeField] private bool consumeItemOnInsert = true;
        [SerializeField] private ItemSocketGroup socketGroup;
        [SerializeField] private bool allowInteractionWithoutRequiredItem = true;

        [Header("Prompt")]
        [SerializeField] private string insertPromptFormat = "Insert {0}";
        [SerializeField] private string filledPrompt = "Installed";

        [Header("Visuals")]
        [SerializeField] private GameObject[] showWhenFilled;
        [SerializeField] private GameObject[] hideWhenFilled;
        [SerializeField] private Collider interactionCollider;

        [Header("Audio")]
        [SerializeField] private AudioSource insertAudioSource;

        [Header("Dialogue")]
        [SerializeField] private Transform dialoguePlaybackSource;
        [SerializeField] private bool interruptCurrentDialogue;
        [SerializeField] private bool queueDialogueIfBusy = true;
        [SerializeField] private DialogueLine[] missingItemDialogueLines;
        [SerializeField] private DialogueLine[] successDialogueLines;

        [Header("Events")]
        [SerializeField] private UnityEvent onFilled;

        [SerializeField] private bool isFilled;

        private string cachedPrompt;

        public bool IsFilled => isFilled;

        private void Reset()
        {
            ResolveReferences();
            RefreshPrompt();
            ApplyVisualState();
        }

        private void Awake()
        {
            ResolveReferences();
            RefreshPrompt();
            ApplyVisualState();
        }

        private void OnValidate()
        {
            ResolveReferences();
            RefreshPrompt();
            ApplyVisualState();
        }

        public string GetPrompt()
        {
            return isFilled ? filledPrompt : cachedPrompt;
        }

        public bool CanInteract(GameObject interactor)
        {
            if (isFilled)
                return false;

            SimpleInventory inventory = GetInventory(interactor);
            if (inventory == null)
                return false;

            return allowInteractionWithoutRequiredItem || inventory.Has(requiredItemId, requiredItemCount);
        }

        public void Interact(GameObject interactor)
        {
            if (isFilled)
                return;

            SimpleInventory inventory = GetInventory(interactor);
            if (inventory == null)
                return;

            if (!inventory.Has(requiredItemId, requiredItemCount))
            {
                TryPlayDialogue(interactor, missingItemDialogueLines, BuildMissingItemFallbackText());
                return;
            }

            if (consumeItemOnInsert)
            {
                if (!inventory.Remove(requiredItemId, requiredItemCount))
                    return;
            }
            else if (!inventory.Has(requiredItemId, requiredItemCount))
            {
                return;
            }

            isFilled = true;
            ApplyVisualState();
            PlayInsertAudio();
            TryPlayDialogue(interactor, successDialogueLines, BuildSuccessFallbackText());
            onFilled?.Invoke();
            socketGroup?.NotifySocketStateChanged();
        }

        public void ResetSocket()
        {
            isFilled = false;
            ApplyVisualState();
            socketGroup?.NotifySocketStateChanged();
        }

        private void ResolveReferences()
        {
            if (interactionCollider == null)
                interactionCollider = GetComponent<Collider>();

            if (socketGroup == null)
                socketGroup = GetComponentInParent<ItemSocketGroup>();
        }

        private void RefreshPrompt()
        {
            string displayName = string.IsNullOrWhiteSpace(requiredDisplayName)
                ? requiredItemId
                : requiredDisplayName;

            if (requiredItemCount > 1)
                displayName = $"{displayName} x{requiredItemCount}";

            cachedPrompt = string.IsNullOrWhiteSpace(insertPromptFormat)
                ? displayName
                : string.Format(insertPromptFormat, displayName);
        }

        private void ApplyVisualState()
        {
            SetObjectArrayState(showWhenFilled, isFilled);
            SetObjectArrayState(hideWhenFilled, !isFilled);

            if (interactionCollider != null)
                interactionCollider.enabled = !isFilled;
        }

        private void SetObjectArrayState(GameObject[] targets, bool active)
        {
            if (targets == null)
                return;

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                    targets[i].SetActive(active);
            }
        }

        private void PlayInsertAudio()
        {
            if (insertAudioSource == null || insertAudioSource.clip == null)
                return;

            insertAudioSource.PlayOneShot(insertAudioSource.clip);
        }

        private void TryPlayDialogue(GameObject interactor, DialogueLine[] lines, string fallbackText)
        {
            if ((lines == null || lines.Length == 0) && string.IsNullOrWhiteSpace(fallbackText))
                return;

            DialogueSequencePlayer player = DialogueSequencePlayer.GetOrCreate(interactor);
            if (player == null)
                return;

            Transform playbackSource = dialoguePlaybackSource != null
                ? dialoguePlaybackSource
                : interactor != null
                    ? interactor.transform
                    : null;

            DialogueLine[] playbackLines = lines != null && lines.Length > 0
                ? lines
                : new[] { new DialogueLine(fallbackText) };

            player.Play(playbackLines, playbackSource, interruptCurrentDialogue, queueDialogueIfBusy);
        }

        private string BuildMissingItemFallbackText()
        {
            return requiredItemId switch
            {
                "ElevatorButton" => "Нужна кнопка для панели лифта.",
                "ElectricalTape" => "Нужна изолента, чтобы замотать провод.",
                "ElevatorFuse" when requiredItemCount > 1 => $"Нужно ещё предохранителей: {requiredItemCount}.",
                "ElevatorFuse" => "Нужен предохранитель.",
                _ => string.IsNullOrWhiteSpace(requiredDisplayName)
                    ? "Пока здесь нечего установить."
                    : $"Нужен предмет: {requiredDisplayName}."
            };
        }

        private string BuildSuccessFallbackText()
        {
            return requiredItemId switch
            {
                "ElevatorButton" => "Кнопка установлена.",
                "ElectricalTape" => "Провод замотан изолентой.",
                "ElevatorFuse" when requiredItemCount > 1 => "Предохранители установлены.",
                "ElevatorFuse" => "Предохранитель установлен.",
                _ => string.IsNullOrWhiteSpace(requiredDisplayName)
                    ? "Установлено."
                    : $"{requiredDisplayName} установлено."
            };
        }

        private static SimpleInventory GetInventory(GameObject interactor)
        {
            return interactor != null ? interactor.GetComponent<SimpleInventory>() : null;
        }
    }
}
