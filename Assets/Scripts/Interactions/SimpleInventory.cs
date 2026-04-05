using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Interaction
{
    public class SimpleInventory : MonoBehaviour
    {
        private readonly HashSet<string> items = new();

        public event Action Changed;

        public int Count => items.Count;

        public bool Add(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || !items.Add(itemId))
                return false;

            Changed?.Invoke();
            return true;
        }

        public bool Has(string itemId)
        {
            return !string.IsNullOrWhiteSpace(itemId) && items.Contains(itemId);
        }

        public bool Remove(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || !items.Remove(itemId))
                return false;

            Changed?.Invoke();
            return true;
        }

        public IReadOnlyCollection<string> GetItems()
        {
            return items;
        }
    }
}
