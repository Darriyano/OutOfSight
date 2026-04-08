using UnityEngine;

namespace Game.Interaction
{
    public class SlidingDoorInteractable : MonoBehaviour, IInteractable
    {
        [Header("Panels")]
        [SerializeField] private Transform leftPanel;
        [SerializeField] private Transform rightPanel;

        [Header("Motion")]
        [SerializeField] private Vector3 leftOpenOffset = new Vector3(-0.72f, 0f, 0f);
        [SerializeField] private Vector3 rightOpenOffset = new Vector3(0.72f, 0f, 0f);
        [SerializeField] private float speed = 4.5f;
        [SerializeField] private bool startOpen;

        [Header("Prompt")]
        [SerializeField] private string openPrompt = "Open elevator";
        [SerializeField] private string closePrompt = "Close elevator";

        [Header("Power Requirements")]
        [SerializeField] private bool requireSystemReady = true;
        [SerializeField] private ItemSocketGroup[] requiredGroups;
        [SerializeField] private bool autoFindRequiredGroupsFromParent = true;

        [Header("Dialogue")]
        [SerializeField] private Transform dialoguePlaybackSource;
        [SerializeField] private bool interruptCurrentDialogue;
        [SerializeField] private bool queueDialogueIfBusy = true;
        [SerializeField] private bool playReadyDialogueOnlyOnce = true;
        [SerializeField] private DialogueLine[] unavailableDialogueLines;
        [SerializeField] private DialogueLine[] readyDialogueLines;

        private Vector3 leftClosedLocalPosition;
        private Vector3 rightClosedLocalPosition;
        private Vector3 leftOpenLocalPosition;
        private Vector3 rightOpenLocalPosition;
        private bool isOpen;
        private bool hasPlayedReadyDialogue;

        private void Awake()
        {
            ResolvePanels();
            ResolveRequiredGroups();
            CachePositions();
            ApplyImmediatePose(startOpen);
        }

        private void OnValidate()
        {
            ResolvePanels();
            ResolveRequiredGroups();
            CachePositions();
            ApplyImmediatePose(startOpen);
        }

        private void Update()
        {
            UpdatePanel(leftPanel, isOpen ? leftOpenLocalPosition : leftClosedLocalPosition);
            UpdatePanel(rightPanel, isOpen ? rightOpenLocalPosition : rightClosedLocalPosition);
        }

        public string GetPrompt()
        {
            return isOpen ? closePrompt : openPrompt;
        }

        public bool CanInteract(GameObject interactor)
        {
            return leftPanel != null || rightPanel != null;
        }

        public void Interact(GameObject interactor)
        {
            if (requireSystemReady && !AreRequirementsMet())
            {
                TryPlayDialogue(interactor, unavailableDialogueLines, "Лифт пока не запустить. Нужно восстановить щиток и панель.");
                return;
            }

            if (!hasPlayedReadyDialogue || !playReadyDialogueOnlyOnce)
            {
                TryPlayDialogue(interactor, readyDialogueLines, "Теперь лифт должен работать.");
                hasPlayedReadyDialogue = true;
            }

            isOpen = !isOpen;
        }

        private void ResolvePanels()
        {
            if ((leftPanel == null || rightPanel == null) && transform.childCount >= 2)
            {
                if (leftPanel == null)
                    leftPanel = transform.GetChild(0);

                if (rightPanel == null)
                    rightPanel = transform.GetChild(1);
            }
        }

        private void ResolveRequiredGroups()
        {
            if (!autoFindRequiredGroupsFromParent || (requiredGroups != null && requiredGroups.Length > 0))
                return;

            Transform searchRoot = transform.parent != null ? transform.parent : transform;
            requiredGroups = searchRoot.GetComponentsInChildren<ItemSocketGroup>(true);
        }

        private void CachePositions()
        {
            if (leftPanel != null)
            {
                leftClosedLocalPosition = leftPanel.localPosition;
                leftOpenLocalPosition = leftClosedLocalPosition + leftOpenOffset;
            }

            if (rightPanel != null)
            {
                rightClosedLocalPosition = rightPanel.localPosition;
                rightOpenLocalPosition = rightClosedLocalPosition + rightOpenOffset;
            }
        }

        private void ApplyImmediatePose(bool open)
        {
            isOpen = open;

            if (leftPanel != null)
                leftPanel.localPosition = open ? leftOpenLocalPosition : leftClosedLocalPosition;

            if (rightPanel != null)
                rightPanel.localPosition = open ? rightOpenLocalPosition : rightClosedLocalPosition;
        }

        private void UpdatePanel(Transform panel, Vector3 targetLocalPosition)
        {
            if (panel == null)
                return;

            if ((panel.localPosition - targetLocalPosition).sqrMagnitude <= 0.000001f)
                return;

            panel.localPosition = Vector3.MoveTowards(
                panel.localPosition,
                targetLocalPosition,
                speed * Time.deltaTime);
        }

        private bool AreRequirementsMet()
        {
            if (requiredGroups == null || requiredGroups.Length == 0)
                return false;

            for (int i = 0; i < requiredGroups.Length; i++)
            {
                if (requiredGroups[i] == null || !requiredGroups[i].IsCompleted)
                    return false;
            }

            return true;
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
    }
}
