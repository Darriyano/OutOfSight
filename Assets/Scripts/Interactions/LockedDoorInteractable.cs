using UnityEngine;

namespace Game.Interaction
{
    public class LockedDoorInteractable : MonoBehaviour, IInteractable
    {
        [Header("Door")]
        [SerializeField] private Transform pivot;
        [SerializeField] private float openAngle = 90f;
        [SerializeField] private float speed = 6f;

        [Header("Lock")]
        [SerializeField] private string requiredKeyId = "KeyA";
        [SerializeField] private bool consumeKeyOnUnlock = true;

        private bool _open;
        private bool _unlocked;
        private Quaternion _closedRot;
        private Quaternion _openRot;

        private void Awake()
        {
            if (!pivot) pivot = transform;

            _closedRot = pivot.localRotation;
            _openRot = _closedRot * Quaternion.Euler(0f, openAngle, 0f);
        }

        private void Update()
        {
            var target = _open ? _openRot : _closedRot;
            pivot.localRotation = Quaternion.Slerp(
                pivot.localRotation,
                target,
                Time.deltaTime * speed
            );
        }

        public string GetPrompt()
        {
            return _open ? "Close" : "Open";
        }

        public bool CanInteract(GameObject interactor)
        {
            if (_open) return true;
            if (_unlocked) return true;

            var inv = interactor.GetComponent<SimpleInventory>();
            if (inv == null) return false;

            return inv.Has(requiredKeyId);
        }

        public void Interact(GameObject interactor)
        {
            // Если дверь уже открыта — просто закрываем
            if (_open)
            {
                _open = false;
                return;
            }

            // Если дверь уже была разблокирована раньше — просто открываем
            if (_unlocked)
            {
                _open = true;
                return;
            }

            var inv = interactor.GetComponent<SimpleInventory>();
            if (inv == null) return;

            if (!inv.Has(requiredKeyId))
            {
                Debug.Log($"Door is locked. Need key: {requiredKeyId}");
                return;
            }

            // Разблокируем дверь
            _unlocked = true;
            _open = true;

            // Удаляем ключ из инвентаря после использования
            if (consumeKeyOnUnlock)
            {
                inv.Remove(requiredKeyId);
                Debug.Log($"Used key: {requiredKeyId}");
            }
        }
    }
}