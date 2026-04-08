using UnityEngine;
using UnityEngine.Events;

namespace Game.Interaction
{
    public class ItemSocketGroup : MonoBehaviour
    {
        [SerializeField] private InventoryItemSocketInteractable[] sockets;
        [SerializeField] private GameObject[] showWhenCompleted;
        [SerializeField] private GameObject[] hideWhenCompleted;
        [SerializeField] private UnityEvent onCompleted;

        [SerializeField] private bool isCompleted;

        public bool IsCompleted => isCompleted;

        private void Reset()
        {
            ResolveSockets();
            RefreshState(false);
        }

        private void Awake()
        {
            ResolveSockets();
            RefreshState(false);
        }

        private void OnValidate()
        {
            ResolveSockets();
            RefreshState(false);
        }

        public void NotifySocketStateChanged()
        {
            RefreshState(true);
        }

        private void ResolveSockets()
        {
            if (sockets != null && sockets.Length > 0)
                return;

            sockets = GetComponentsInChildren<InventoryItemSocketInteractable>(true);
        }

        private void RefreshState(bool notify)
        {
            bool completed = AreAllSocketsFilled();
            SetObjectArrayState(showWhenCompleted, completed);
            SetObjectArrayState(hideWhenCompleted, !completed);

            bool shouldNotify = notify && completed && !isCompleted;
            isCompleted = completed;

            if (shouldNotify)
                onCompleted?.Invoke();
        }

        private bool AreAllSocketsFilled()
        {
            if (sockets == null || sockets.Length == 0)
                return false;

            for (int i = 0; i < sockets.Length; i++)
            {
                if (sockets[i] == null || !sockets[i].IsFilled)
                    return false;
            }

            return true;
        }

        private static void SetObjectArrayState(GameObject[] targets, bool active)
        {
            if (targets == null)
                return;

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                    targets[i].SetActive(active);
            }
        }
    }
}
