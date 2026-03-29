using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Interaction
{
    /// Простой debug UI для отображения содержимого инвентаря на экране.
    public class InventoryDebugUI : MonoBehaviour
    {
        [SerializeField] private SimpleInventory inventory;
        [SerializeField] private Text inventoryText;

        private readonly StringBuilder _builder = new();

        private void Reset()
        {
            if (!inventory)
                inventory = FindFirstObjectByType<SimpleInventory>();
        }

        private void Update()
        {
            if (inventoryText == null) return;

            if (inventory == null)
            {
                inventoryText.text = "Inventory:\n<missing inventory>";
                return;
            }

            var items = inventory.GetItems();

            _builder.Clear();
            _builder.AppendLine("Inventory:");

            if (items.Count == 0)
            {
                _builder.AppendLine("- empty");
            }
            else
            {
                foreach (var item in items)
                {
                    _builder.Append("- ");
                    _builder.AppendLine(item);
                }
            }

            inventoryText.text = _builder.ToString();
        }
    }
}