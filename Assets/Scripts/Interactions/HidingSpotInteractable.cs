using System.Collections.Generic;
using UnityEngine;

namespace Game.Interaction
{
    public class HidingSpotInteractable : MonoBehaviour, IInteractable
    {
        private static readonly List<HidingSpotInteractable> Spots = new List<HidingSpotInteractable>();

        [Header("Points")]
        [SerializeField] private Transform hidePoint;
        [SerializeField] private Transform exitPoint;

        [Header("Rules")]
        [SerializeField] private float activationDistance = 0.7f;
        [SerializeField] private bool requiresCrouch;
        [SerializeField, Range(0f, 1f)] private float inspectionChance = 1f;

        private PlayerHiding occupant;

        public static IReadOnlyList<HidingSpotInteractable> AllSpots => Spots;
        public bool IsOccupied => occupant != null;
        public float ActivationDistance => activationDistance;

        private void OnEnable()
        {
            if (!Spots.Contains(this))
                Spots.Add(this);
        }

        private void OnDisable()
        {
            Spots.Remove(this);

            if (occupant != null)
            {
                PlayerHiding oldOccupant = occupant;
                occupant = null;
                oldOccupant.ForceExitFromHiding();
            }
        }

        public string GetPrompt()
        {
            if (occupant != null)
                return "Exit hiding";

            return "Hide";
        }

        public bool CanInteract(GameObject interactor)
        {
            PlayerHiding playerHiding = interactor.GetComponent<PlayerHiding>();

            if (playerHiding == null)
                playerHiding = interactor.GetComponentInParent<PlayerHiding>();

            if (playerHiding == null)
                return false;

            if (playerHiding.IsHidden)
                return playerHiding.CurrentSpot == this;

            return CanPlayerEnter(playerHiding);
        }

        public void Interact(GameObject interactor)
        {
            PlayerHiding playerHiding = interactor.GetComponent<PlayerHiding>();

            if (playerHiding == null)
                playerHiding = interactor.GetComponentInParent<PlayerHiding>();

            if (playerHiding == null)
                return;

            playerHiding.ToggleHide(this);
        }

        public bool CanPlayerEnter(PlayerHiding playerHiding)
        {
            if (playerHiding == null)
                return false;

            if (IsOccupied)
                return false;

            float distance = Vector3.Distance(playerHiding.transform.position, transform.position);
            if (distance > activationDistance)
                return false;

            if (requiresCrouch && !playerHiding.IsCrouching())
                return false;

            return true;
        }

        public void SetOccupant(PlayerHiding playerHiding)
        {
            occupant = playerHiding;
        }

        public void ClearOccupant(PlayerHiding playerHiding)
        {
            if (occupant == playerHiding)
                occupant = null;
        }

        public bool ShouldBeInspected()
        {
            return Random.value <= inspectionChance;
        }

        public Vector3 GetHidePosition()
        {
            return hidePoint != null ? hidePoint.position : transform.position;
        }

        public Quaternion GetHideRotation()
        {
            return hidePoint != null ? hidePoint.rotation : transform.rotation;
        }

        public Vector3 GetExitPosition()
        {
            if (exitPoint != null)
                return exitPoint.position;

            return transform.position + transform.forward;
        }

        public Quaternion GetExitRotation()
        {
            if (exitPoint != null)
                return exitPoint.rotation;

            return transform.rotation;
        }
    }
}
