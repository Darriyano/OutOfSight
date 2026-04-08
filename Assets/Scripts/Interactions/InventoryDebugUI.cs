using System;
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

            IReadOnlyDictionary<string, int> items = inventory.GetItemCounts();
            if (items.Count == 0)
            {
                builder.AppendLine(emptyText);
            }
            else
            {
                List<string> itemIds = new List<string>(items.Keys);
                itemIds.Sort(StringComparer.Ordinal);

                foreach (string itemId in itemIds)
                {
                    builder.Append("- ").Append(itemId);

                    int count = items[itemId];
                    if (count > 1)
                        builder.Append(" x").Append(count);

                    builder.AppendLine();
                }
            }

            inventoryText.text = builder.ToString();
        }
    }
}
