using UnityEngine;

namespace Game.Interaction
{
    public class PickupKeyInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string keyId = "KeyA";
        [SerializeField] private string prompt = "Pick up key";

        public string GetPrompt() => prompt;

        public bool CanInteract(GameObject interactor)
        {
            SimpleInventory inventory = interactor != null ? interactor.GetComponent<SimpleInventory>() : null;
            return inventory != null && !inventory.Has(keyId);
        }

        public void Interact(GameObject interactor)
        {
            SimpleInventory inventory = interactor != null ? interactor.GetComponent<SimpleInventory>() : null;
            if (inventory != null && inventory.Add(keyId))
                Destroy(gameObject);
        }
    }
}
