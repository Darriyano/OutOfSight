using System.Collections.Generic;
using UnityEngine;

namespace Game.Interaction
{
    /// Очень простой инвентарь.
    /// Хранит набор строковых ID предметов.
    public class SimpleInventory : MonoBehaviour
    {
        private readonly HashSet<string> _items = new();

        public bool Add(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return false;

            bool added = _items.Add(itemId);

            if (added)
                Debug.Log($"Picked up item: {itemId}");

            return added;
        }

        public bool Has(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return false;
            return _items.Contains(itemId);
        }

        public bool Remove(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return false;
            return _items.Remove(itemId);
        }

        public IReadOnlyCollection<string> GetItems()
        {
            return _items;
        }
    }
}