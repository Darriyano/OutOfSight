using UnityEngine;

namespace Game.Interaction
{
    /// Универсальный предмет, который можно поднять в инвентарь.
    /// Подходит для ключей, квестовых деталей, заметок и т.п.
    public class PickupItemInteractable : MonoBehaviour, IInteractable
    {
        [Header("Item")]
        [SerializeField] private string itemId = "ItemA";
        [SerializeField] private string itemDisplayName = "Item";
        [SerializeField] private string pickupPrompt = "Pick up";

        [Header("Optional")]
        [SerializeField] private AudioSource pickupAudioSource;
        [SerializeField] private bool destroyAfterPickup = true;

        public string GetPrompt()
        {
            return $"{pickupPrompt} {itemDisplayName}";
        }

        public bool CanInteract(GameObject interactor)
        {
            var inv = interactor.GetComponent<SimpleInventory>();
            if (inv == null) return false;

            // Запрещаем подбирать дубликат того же предмета
            return !inv.Has(itemId);
        }

        public void Interact(GameObject interactor)
        {
            var inv = interactor.GetComponent<SimpleInventory>();
            if (inv == null) return;

            if (!inv.Add(itemId))
                return;

            Debug.Log($"Picked up item: {itemId}");

            if (pickupAudioSource != null)
                pickupAudioSource.Play();

            if (destroyAfterPickup)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}