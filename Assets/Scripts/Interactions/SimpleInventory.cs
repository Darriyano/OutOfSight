using System.Collections.Generic;
using UnityEngine;

namespace Game.Interaction
{
    /// Очень простой инвентарь.
    /// Хранит набор строковых ID предметов (например "KeyA", "Battery", "Note1").
    /// 
    /// Почему HashSet:
    /// - хранит уникальные значения (один и тот же ключ нельзя "добавить" 100 раз случайно)
    /// - быстрые проверки Contains/Has
    /// 
    /// Важно:
    /// - это MVP. Позже можно заменить на полноценный инвентарь (слоты, количество, UI).
    public class SimpleInventory : MonoBehaviour
    {
        /// HashSet = "множество" уникальных значений.
        /// Здесь мы храним ID предметов, которые игрок собрал.
        /// Пример: если игрок поднял ключ "KeyA" — он попадает сюда.
        private readonly HashSet<string> _items = new();

        /// Добавить предмет в инвентарь.
        /// Возвращает true, если реально добавили (то есть такого ID ещё не было).
        /// Возвращает false, если строка пустая/некорректная или предмет уже был.
        public bool Add(string itemId)
        {
            // Защита от пустых строк.
            if (string.IsNullOrWhiteSpace(itemId)) return false;

            // _items.Add вернет true только если itemId был новым.
            return _items.Add(itemId);
        }

        /// Проверить, есть ли в инвентаре предмет с таким ID.
        /// Например: Has("KeyA") -> true, если игрок уже поднял KeyA.
        public bool Has(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return false;
            return _items.Contains(itemId);
        }
    }
}