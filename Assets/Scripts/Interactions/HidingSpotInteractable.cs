using System.Collections.Generic;
using UnityEngine;

namespace Game.Interaction
{
    public class HidingSpotInteractable : MonoBehaviour, IInteractable
    {
        private enum ExitRotationMode
        {
            UseExitPointRotation,
            KeepCurrentRotation,
            FaceAwayFromSpot
        }

        private static readonly List<HidingSpotInteractable> Spots = new List<HidingSpotInteractable>();

        [Header("Points")]
        [SerializeField] private Transform hidePoint;
        [SerializeField] private Transform exitPoint;

        [Header("Rules")]
        [SerializeField] private float activationDistance = 0.7f;
        [SerializeField] private bool requiresCrouch;
        [SerializeField, Range(0f, 1f)] private float inspectionChance = 1f;

        [Header("Exit")]
        [SerializeField] private ExitRotationMode exitRotationMode = ExitRotationMode.UseExitPointRotation;

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

        public Quaternion GetExitRotation(Quaternion currentRotation)
        {
            switch (exitRotationMode)
            {
                case ExitRotationMode.KeepCurrentRotation:
                    return ToYawRotation(currentRotation * Vector3.forward, currentRotation);
                case ExitRotationMode.FaceAwayFromSpot:
                {
                    Vector3 exitPosition = GetExitPosition();
                    Vector3 direction = exitPosition - transform.position;
                    return ToYawRotation(direction, currentRotation);
                }
                default:
                    if (exitPoint != null)
                        return ToYawRotation(exitPoint.forward, currentRotation);

                    return ToYawRotation(transform.forward, currentRotation);
            }
        }

        private static Quaternion ToYawRotation(Vector3 forward, Quaternion fallback)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                Vector3 fallbackForward = fallback * Vector3.forward;
                fallbackForward.y = 0f;

                if (fallbackForward.sqrMagnitude <= 0.0001f)
                    return Quaternion.identity;

                forward = fallbackForward;
            }

            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }
    }
}
