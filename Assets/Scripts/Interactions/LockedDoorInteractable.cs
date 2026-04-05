using UnityEngine;

namespace Game.Interaction
{
    public class LockedDoorInteractable : MonoBehaviour, IInteractable
    {
        private enum MotionMode
        {
            SelfRotation,
            ExternalPivotRotation,
            ChildPivotOrbit
        }

        [Header("Door")]
        [SerializeField] private Transform pivot;
        [SerializeField] private float openAngle = 90f;
        [SerializeField] private float speed = 6f;

        [Header("Lock")]
        [SerializeField] private string requiredKeyId = "KeyA";
        [SerializeField] private bool consumeKeyOnUnlock = true;

        [Header("Prompt")]
        [SerializeField] private string openPrompt = "Open";
        [SerializeField] private string closePrompt = "Close";
        [SerializeField] private string lockedPromptFormat = "Open (needs {0})";

        private bool isOpen;
        private bool isUnlocked;
        private MotionMode motionMode;
        private Quaternion closedLocalRotation;
        private Quaternion openLocalRotation;
        private Vector3 closedWorldPosition;
        private Vector3 openWorldPosition;
        private Quaternion closedWorldRotation;
        private Quaternion openWorldRotation;
        private string lockedPrompt;

        private void Awake()
        {
            ResolveMotionMode();
            CacheRotations();
            UpdateLockedPrompt();
        }

        private void OnValidate()
        {
            UpdateLockedPrompt();
            ResolveMotionMode();
            CacheRotations();
        }

        private void Update()
        {
            switch (motionMode)
            {
                case MotionMode.ChildPivotOrbit:
                    UpdateOrbitMotion();
                    break;
                case MotionMode.ExternalPivotRotation:
                case MotionMode.SelfRotation:
                    UpdateRotationMotion();
                    break;
            }
        }

        public string GetPrompt()
        {
            if (isOpen)
                return closePrompt;

            return isUnlocked ? openPrompt : lockedPrompt;
        }

        public bool CanInteract(GameObject interactor)
        {
            return isOpen || isUnlocked || HasRequiredKey(interactor);
        }

        public void Interact(GameObject interactor)
        {
            if (isOpen)
            {
                isOpen = false;
                return;
            }

            if (!isUnlocked)
            {
                SimpleInventory inventory = GetInventory(interactor);
                if (inventory == null || !inventory.Has(requiredKeyId))
                    return;

                isUnlocked = true;

                if (consumeKeyOnUnlock)
                    inventory.Remove(requiredKeyId);
            }

            isOpen = true;
        }

        private void CacheRotations()
        {
            if (motionMode == MotionMode.ChildPivotOrbit)
            {
                closedWorldPosition = transform.position;
                closedWorldRotation = transform.rotation;

                Vector3 hingeLocalOffset = transform.InverseTransformPoint(pivot.position);
                Vector3 hingeWorldPosition = pivot.position;

                openWorldRotation = closedWorldRotation * Quaternion.Euler(0f, openAngle, 0f);
                openWorldPosition = hingeWorldPosition - (openWorldRotation * hingeLocalOffset);
                return;
            }

            Transform animatedTransform = motionMode == MotionMode.ExternalPivotRotation ? pivot : transform;
            if (animatedTransform == null)
                return;

            closedLocalRotation = animatedTransform.localRotation;
            openLocalRotation = closedLocalRotation * Quaternion.Euler(0f, openAngle, 0f);
        }

        private void UpdateLockedPrompt()
        {
            lockedPrompt = string.IsNullOrEmpty(lockedPromptFormat)
                ? string.Empty
                : string.Format(lockedPromptFormat, requiredKeyId);
        }

        private bool HasRequiredKey(GameObject interactor)
        {
            SimpleInventory inventory = GetInventory(interactor);
            return inventory != null && inventory.Has(requiredKeyId);
        }

        private SimpleInventory GetInventory(GameObject interactor)
        {
            return interactor != null ? interactor.GetComponent<SimpleInventory>() : null;
        }

        private void ResolveMotionMode()
        {
            if (pivot == null || pivot == transform)
            {
                pivot = transform;
                motionMode = MotionMode.SelfRotation;
                return;
            }

            motionMode = pivot.IsChildOf(transform)
                ? MotionMode.ChildPivotOrbit
                : MotionMode.ExternalPivotRotation;
        }

        private void UpdateRotationMotion()
        {
            Transform animatedTransform = motionMode == MotionMode.ExternalPivotRotation ? pivot : transform;
            if (animatedTransform == null)
                return;

            Quaternion targetRotation = isOpen ? openLocalRotation : closedLocalRotation;
            if (Quaternion.Angle(animatedTransform.localRotation, targetRotation) <= 0.01f)
                return;

            animatedTransform.localRotation = Quaternion.Slerp(
                animatedTransform.localRotation,
                targetRotation,
                Time.deltaTime * speed);
        }

        private void UpdateOrbitMotion()
        {
            if (pivot == null)
                return;

            Vector3 targetPosition = isOpen ? openWorldPosition : closedWorldPosition;
            Quaternion targetRotation = isOpen ? openWorldRotation : closedWorldRotation;

            if (Vector3.Distance(transform.position, targetPosition) <= 0.0001f &&
                Quaternion.Angle(transform.rotation, targetRotation) <= 0.01f)
                return;

            transform.SetPositionAndRotation(
                Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * speed),
                Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speed));
        }
    }
}
