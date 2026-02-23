using UnityEngine;

namespace Game.Interaction
{
    /// Дверь с замком.
    /// Откроется только если в SimpleInventory есть нужный ключ.
    public class LockedDoorInteractable : MonoBehaviour, IInteractable
    {
        [Header("Door")]
        [SerializeField] private Transform pivot;
        [SerializeField] private float openAngle = 90f;
        [SerializeField] private float speed = 6f;

        [Header("Lock")]
        /// Какой ключ нужен. Должен совпадать с keyId на объекте-ключе.
        /// Пример: если requiredKeyId = "KeyA", то ключ должен иметь keyId = "KeyA".
        [SerializeField] private string requiredKeyId = "KeyA";

        private bool _open;
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
            pivot.localRotation = Quaternion.Slerp(pivot.localRotation, target, Time.deltaTime * speed);
        }

        /// Подсказка.
        /// Здесь я показываю "needs KeyA", чтобы было понятно в тесте.
        /// В финальной игре можно сделать:
        /// - если нет ключа: "Locked"
        /// - если есть ключ: "Open"
        public string GetPrompt()
        {
            if (_open) return "Close";
            return $"Open (needs {requiredKeyId})";
        }

        /// Можно ли взаимодействовать.
        /// Если ключа нет — вернем false, и PlayerInteractor НЕ вызовет Interact().
        public bool CanInteract(GameObject interactor)
        {
            var inv = interactor.GetComponent<SimpleInventory>();
            if (inv == null) return false;

            return inv.Has(requiredKeyId);
        }

        /// Что происходит при взаимодействии.
        /// На всякий случай дублируем проверку ключа здесь тоже.
        /// (потому что иногда CanInteract может поменяться между кадрами)
        public void Interact(GameObject interactor)
        {
            var inv = interactor.GetComponent<SimpleInventory>();
            if (inv == null) return;

            if (!inv.Has(requiredKeyId)) return;

            _open = !_open;
        }
    }
}