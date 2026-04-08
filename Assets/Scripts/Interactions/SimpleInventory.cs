using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Interaction
{
    public class SimpleInventory : MonoBehaviour
    {
        private readonly Dictionary<string, int> itemCounts = new(StringComparer.Ordinal);
        private int totalCount;

        public event Action Changed;

        public int Count => totalCount;
        public int DistinctCount => itemCounts.Count;

        public bool Add(string itemId, int amount = 1)
        {
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
                return false;

            itemCounts.TryGetValue(itemId, out int currentCount);
            itemCounts[itemId] = currentCount + amount;
            totalCount += amount;

            Changed?.Invoke();
            return true;
        }

        public bool Has(string itemId, int minimumCount = 1)
        {
            return !string.IsNullOrWhiteSpace(itemId) &&
                   minimumCount > 0 &&
                   itemCounts.TryGetValue(itemId, out int count) &&
                   count >= minimumCount;
        }

        public int GetCount(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return 0;

            return itemCounts.TryGetValue(itemId, out int count) ? count : 0;
        }

        public bool Remove(string itemId, int amount = 1)
        {
            if (string.IsNullOrWhiteSpace(itemId) ||
                amount <= 0 ||
                !itemCounts.TryGetValue(itemId, out int currentCount) ||
                currentCount < amount)
                return false;

            int remainingCount = currentCount - amount;
            if (remainingCount > 0)
                itemCounts[itemId] = remainingCount;
            else
                itemCounts.Remove(itemId);

            totalCount -= amount;
            Changed?.Invoke();
            return true;
        }

        public IReadOnlyDictionary<string, int> GetItemCounts()
        {
            return itemCounts;
        }
    }
}
