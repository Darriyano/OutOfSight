using UnityEngine;

namespace Game.Interaction
{
    internal sealed class DoorInteractableForwarder : MonoBehaviour, IInteractable, IInteractableSourceProxy
    {
        private MonoBehaviour sourceBehaviour;
        private IInteractable sourceInteractable;

        public Component SourceComponent => sourceBehaviour;

        public void Initialize(MonoBehaviour source)
        {
            sourceBehaviour = source;
            sourceInteractable = source as IInteractable;
        }

        public string GetPrompt()
        {
            return sourceInteractable != null ? sourceInteractable.GetPrompt() : string.Empty;
        }

        public bool CanInteract(GameObject interactor)
        {
            return sourceInteractable != null && sourceInteractable.CanInteract(interactor);
        }

        public void Interact(GameObject interactor)
        {
            sourceInteractable?.Interact(interactor);
        }
    }
}
