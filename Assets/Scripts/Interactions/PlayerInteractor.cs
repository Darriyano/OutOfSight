using UnityEngine;        // Базовые типы Unity (GameObject, MonoBehaviour, Camera, Physics, Ray и т.д.)
using UnityEngine.UI;     // UI-компоненты старой системы Unity UI (Text)

namespace Game.Interaction // Пространство имён: чтобы ваши классы не конфликтовали с чужими
{
    /// Компонент, который вешается на объект игрока (или на камеру игрока).
    /// 
    /// Задача:
    /// 1) Каждый кадр "смотреть" лучом (Raycast) из камеры вперёд на небольшую дистанцию.
    /// 2) Если луч попал в объект с компонентом IInteractable (или в его дочерний коллайдер) —
    ///    запомнить этот объект как текущий (current) и показать подсказку ("E: Open").
    /// 3) Если игрок нажал кнопку (E) и current существует — вызвать Interact() у этого объекта.
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Raycast")]
        [SerializeField] private Camera playerCamera;
        // Камера, из которой мы "целимся" лучом.
        // Обычно это Main Camera в сцене (камера игрока).

        [SerializeField] private float distance = 2.5f;
        // Максимальная дистанция взаимодействия.
        // Raycast будет проверять попадания только до этой длины.

        [Header("UI (optional)")]
        [SerializeField] private Text promptText;
        // UI Text, куда выводим подсказку (например, "E: Open").
        // "optional" потому что можно оставить пустым — тогда просто ничего не показываем.

        [Header("Input")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        // Какая клавиша считается "взаимодействием".
        // В текущей упрощенной версии используем старый Input: Input.GetKeyDown()

        private IInteractable current;
        // "Текущий" объект, на который мы сейчас смотрим и с которым можем взаимодействовать.
        // Тип IInteractable — это интерфейс. Любой объект, который его реализует, можно "юзать".

        /// Reset вызывается Unity, когда ты:
        /// - добавляешь компонент на объект
        /// - нажимаешь "Reset" в инспекторе
        /// 
        /// Здесь мы пытаемся автоматически подставить камеру.
        /// Camera.main = камера с тегом MainCamera.
        private void Reset()
        {
            if (!playerCamera) playerCamera = Camera.main;
        }

        /// Update вызывается каждый кадр.
        /// Мы:
        /// 1) Находим, на что смотрим (UpdateTarget)
        /// 2) Если есть current и нажали E — вызываем интеракцию.
        private void Update()
        {
            // 1) Обновляем "цель" (что сейчас под прицелом в центре экрана)
            UpdateTarget();

            // 2) Если мы смотрим на интерактивный объект И нажали кнопку
            if (current != null && Input.GetKeyDown(interactKey))
            {
                // Дополнительная проверка: можно ли сейчас взаимодействовать?
                // Например: дверь может быть закрыта на ключ -> CanInteract вернет false.
                if (current.CanInteract(gameObject))
                    // gameObject = объект, на котором висит PlayerInteractor.
                    // Мы передаем "кто взаимодействует" в объект — иногда это нужно
                    // (например, чтобы проверить инвентарь игрока).
                    current.Interact(gameObject);
            }
        }

        /// Определяет, на что мы смотрим сейчас:
        /// - если попали лучом в коллайдер объекта, у которого есть IInteractable (на нем или на родителе),
        ///   то current = этот IInteractable, и показываем подсказку.
        /// - если ни во что интерактивное не попали, current = null и подсказку очищаем.
        private void UpdateTarget()
        {
            // По умолчанию считаем, что ни на что интерактивное не смотрим.
            current = null;

            // Если камера не назначена, мы не можем бросать луч.
            // Тогда просто очищаем подсказку и выходим.
            if (!playerCamera)
            {
                SetPrompt("");
                return;
            }

            // Создаём луч:
            // - начало луча: позиция камеры
            // - направление луча: "вперёд" от камеры (куда смотрит)
            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

            // Physics.Raycast проверяет пересечение луча с коллайдерами в мире.
            // out var hit — результат попадания (в какую точку попали, какой коллайдер и т.д.)
            // distance — ограничиваем дальность.
            if (Physics.Raycast(ray, out var hit, distance))
            {
                // Мы попали в какой-то коллайдер (hit.collider).
                // Теперь проверим: есть ли на этом объекте (или на его родителях)
                // компонент, реализующий IInteractable.
                //
                // Почему InParent:
                // Часто коллайдер находится на дочернем объекте (MeshCollider/BoxCollider),
                // а логика интеракции висит на родителе (например, "Door").
                var interactable = hit.collider.GetComponentInParent<IInteractable>();

                if (interactable != null)
                {
                    // Запоминаем, что мы смотрим на интерактивный объект
                    current = interactable;

                    // Просим у объекта текст подсказки (например: "Open" / "Pick up")
                    var prompt = interactable.GetPrompt();

                    // Если prompt пустой — убираем подсказку
                    // Если не пустой — показываем "E: {prompt}"
                    SetPrompt(string.IsNullOrWhiteSpace(prompt) ? "" : $"E: {prompt}");
                    return; // Важно: выходим, потому что цель найдена.
                }
            }

            // Если мы сюда дошли — значит:
            // - либо Raycast ни во что не попал
            // - либо попал, но объект не интерактивный
            // => очищаем подсказку
            SetPrompt("");
        }

        /// Устанавливает текст подсказки на экране.
        /// Если promptText не назначен — просто ничего не делаем (без ошибок).
        private void SetPrompt(string value)
        {
            if (promptText != null) promptText.text = value;
        }
    }
}