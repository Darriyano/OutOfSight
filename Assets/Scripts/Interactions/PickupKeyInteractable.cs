using UnityEngine;

namespace Game.Interaction
{
    /// Интерактивный "подбираемый предмет", конкретно ключ.
    /// Реализует интерфейс IInteractable, чтобы PlayerInteractor мог с ним работать.
    public class PickupKeyInteractable : MonoBehaviour, IInteractable
    {
        /// ID предмета. Это просто строка.
        /// Должна совпадать с тем, что требуется двери (requiredKeyId).
        /// Пример: "KeyA"
        [SerializeField] private string keyId = "KeyA";

        /// <summary>
        /// Что писать в подсказке на экране.
        /// </summary>
        [SerializeField] private string prompt = "Pick up key";

        /// PlayerInteractor будет спрашивать у объекта:
        /// "Что показать игроку как подсказку?"
        public string GetPrompt() => prompt;

        /// PlayerInteractor спросит:
        /// "Вообще можно взаимодействовать сейчас или нет?"
        /// Здесь мы запрещаем интеракцию, если на игроке нет SimpleInventory.
        /// (Чтобы не было ошибок: "пытаюсь добавить в инвентарь, но инвентаря нет")
        public bool CanInteract(GameObject interactor)
        {
            // interactor — это объект, который взаимодействует (обычно игрок).
            // В твоём тесте interactor = Main Camera (там висит PlayerInteractor).
            return interactor.GetComponent<SimpleInventory>() != null;
        }

        /// Основной метод: что произойдет, когда игрок нажмет E на этом объекте.
        public void Interact(GameObject interactor)
        {
            // Берём инвентарь у игрока
            var inv = interactor.GetComponent<SimpleInventory>();
            if (inv == null) return; // если инвентаря нет — просто выходим

            // Пытаемся добавить ключ в инвентарь
            // Add вернет true, если предмет реально добавился (его не было раньше)
            if (inv.Add(keyId))
            {
                // Удаляем объект из сцены: ключ "подобран"
                Destroy(gameObject);
            }
            // Если Add вернул false, значит ключ уже был — можно ничего не делать.
        }
    }
}