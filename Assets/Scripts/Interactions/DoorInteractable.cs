using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Простая дверь: открывается/закрывается при взаимодействии.
    /// 
    /// Важно: дверь "анимируется" через поворот pivot вокруг локальной оси Y.
    /// pivot обычно ставят в месте петель двери, чтобы она открывалась реалистично.
    /// Если pivot не задан — используем сам объект двери.
    /// </summary>
    public class DoorInteractable : MonoBehaviour, IInteractable
    {
        /// <summary>
        /// Pivot = объект, который мы вращаем.
        /// Часто это "родитель" двери, стоящий в месте петель.
        /// </summary>
        [SerializeField] private Transform pivot;

        /// <summary>
        /// На сколько градусов повернуть дверь при открытии.
        /// 90 = классика.
        /// </summary>
        [SerializeField] private float openAngle = 90f;

        /// <summary>
        /// Скорость плавного поворота (чем больше — тем быстрее).
        /// </summary>
        [SerializeField] private float speed = 6f;

        /// <summary>
        /// Текущее состояние двери.
        /// false = закрыта
        /// true = открыта
        /// </summary>
        private bool _open;

        /// <summary>
        /// Запоминаем, как дверь выглядит в закрытом состоянии (поворот).
        /// </summary>
        private Quaternion _closedRot;

        /// <summary>
        /// Запоминаем поворот, когда дверь открыта.
        /// </summary>
        private Quaternion _openRot;

        private void Awake()
        {
            // Если pivot не задан — вращаем сам объект (не идеально, но работает)
            if (!pivot) pivot = transform;

            // Запоминаем исходный поворот как "закрыто"
            _closedRot = pivot.localRotation;

            // Рассчитываем "открыто": это "закрыто" + поворот по Y на openAngle
            _openRot = _closedRot * Quaternion.Euler(0f, openAngle, 0f);
        }

        private void Update()
        {
            // В Update мы НЕ переключаем состояние.
            // Мы только плавно двигаем текущий поворот к нужному.
            // Это дает "анимацию" открывания/закрывания.

            // Куда хотим прийти сейчас:
            var target = _open ? _openRot : _closedRot;

            // Плавно интерполируем (Slerp) текущий поворот к target
            pivot.localRotation = Quaternion.Slerp(
                pivot.localRotation,
                target,
                Time.deltaTime * speed
            );
        }

        /// <summary>
        /// Текст подсказки.
        /// </summary>
        public string GetPrompt() => _open ? "Close" : "Open";

        /// <summary>
        /// Дверь без замка — всегда можно.
        /// </summary>
        public bool CanInteract(GameObject interactor) => true;

        /// <summary>
        /// Нажали E -> меняем состояние.
        /// Дальше Update сам плавно повернет pivot.
        /// </summary>
        public void Interact(GameObject interactor)
        {
            _open = !_open; // переключаем
        }
    }
}