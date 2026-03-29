using UnityEngine;
using UnityEngine.UI;

namespace Game.Interaction
{
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Raycast")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float distance = 2.5f;

        [Header("UI (optional)")]
        [SerializeField] private Text promptText;

        [Header("Input")]
        [SerializeField] private KeyCode interactKey = KeyCode.F;

        private IInteractable current;

        private void Reset()
        {
            if (!playerCamera) playerCamera = Camera.main;
        }

        private void Update()
        {
            UpdateTarget();

            if (current != null && Input.GetKeyDown(interactKey))
            {
                if (current.CanInteract(gameObject))
                    current.Interact(gameObject);
            }
        }

        private void UpdateTarget()
        {
            current = null;

            if (!playerCamera)
            {
                SetPrompt("");
                return;
            }

            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

            if (Physics.Raycast(ray, out var hit, distance))
            {
                var interactable = hit.collider.GetComponentInParent<IInteractable>();

                if (interactable != null)
                {
                    current = interactable;
                    var prompt = interactable.GetPrompt();
                    SetPrompt(string.IsNullOrWhiteSpace(prompt) ? "" : $"{interactKey}: {prompt}");
                    return;
                }
            }

            SetPrompt("");
        }

        private void SetPrompt(string value)
        {
            if (promptText != null)
                promptText.text = value;
        }
    }
}