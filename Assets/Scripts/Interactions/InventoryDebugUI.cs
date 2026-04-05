using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Interaction
{
    public class InventoryDebugUI : MonoBehaviour
    {
        [SerializeField] private SimpleInventory inventory;
        [SerializeField] private Text inventoryText;
        [SerializeField] private string emptyText = "- empty";

        private readonly StringBuilder builder = new();

        private void Reset()
        {
            ResolveInventory();
        }

        private void OnEnable()
        {
            ResolveInventory();
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void ResolveInventory()
        {
            if (inventory == null)
                inventory = FindFirstObjectByType<SimpleInventory>();
        }

        private void Subscribe()
        {
            if (inventory != null)
                inventory.Changed += Refresh;
        }

        private void Unsubscribe()
        {
            if (inventory != null)
                inventory.Changed -= Refresh;
        }

        private void Refresh()
        {
            if (inventoryText == null)
                return;

            if (inventory == null)
            {
                inventoryText.text = "Inventory:\n<missing inventory>";
                return;
            }

            builder.Clear();
            builder.AppendLine("Inventory:");

            IReadOnlyCollection<string> items = inventory.GetItems();
            if (items.Count == 0)
            {
                builder.AppendLine(emptyText);
            }
            else
            {
                foreach (string item in items)
                    builder.Append("- ").AppendLine(item);
            }

            inventoryText.text = builder.ToString();
        }
    }
}
