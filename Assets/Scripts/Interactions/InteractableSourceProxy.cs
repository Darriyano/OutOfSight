using UnityEngine;

namespace Game.Interaction
{
    internal interface IInteractableSourceProxy
    {
        Component SourceComponent { get; }
    }
}
