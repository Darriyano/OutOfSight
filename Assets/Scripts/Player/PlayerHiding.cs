using UnityEngine;
using Game.Interaction;

[RequireComponent(typeof(CharacterController))]
public class PlayerHiding : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode hideKey = KeyCode.Space;

    [Header("Movement While Hidden")]
    [SerializeField] private Behaviour[] disableWhileHidden;

    [Header("Dialogue")]
    [SerializeField] private bool playFirstHideDialogue = true;
    [SerializeField] private DialogueLine[] firstHideDialogueLines;
    [SerializeField] private bool interruptFirstHideDialogue;
    [SerializeField] private bool queueFirstHideDialogueIfBusy = true;
    [SerializeField, Min(0f)] private float firstHideDialogueDelay;

    public bool IsHidden { get; private set; }
    public HidingSpotInteractable CurrentSpot { get; private set; }

    private CharacterController characterController;
    private bool hasPlayedFirstHideDialogue;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (disableWhileHidden == null || disableWhileHidden.Length == 0)
        {
            var playerMovement = GetComponent<PlayerMovement>();
            var simpleController = GetComponent<SimpleFpsController>();

            if (playerMovement != null && simpleController != null)
            {
                disableWhileHidden = new Behaviour[] { playerMovement, simpleController };
            }
            else if (playerMovement != null)
            {
                disableWhileHidden = new Behaviour[] { playerMovement };
            }
            else if (simpleController != null)
            {
                disableWhileHidden = new Behaviour[] { simpleController };
            }
            else
            {
                disableWhileHidden = new Behaviour[0];
            }
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(hideKey))
            return;

        if (IsHidden)
        {
            ExitHiding();
            return;
        }

        TryEnterClosestSpot();
    }

    public bool IsCrouching()
    {
        PlayerMovement playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement != null)
            return playerMovement.IsSneaking;

        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
    }

    public void ToggleHide(HidingSpotInteractable spot)
    {
        if (spot == null)
            return;

        if (IsHidden)
        {
            if (CurrentSpot == spot)
                ExitHiding();

            return;
        }

        if (!spot.CanPlayerEnter(this))
            return;

        EnterHiding(spot);
    }

    public void ForceExitFromHiding()
    {
        ExitHiding();
    }

    private void TryEnterClosestSpot()
    {
        var allSpots = HidingSpotInteractable.AllSpots;

        HidingSpotInteractable closestSpot = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < allSpots.Count; i++)
        {
            HidingSpotInteractable spot = allSpots[i];
            if (spot == null || !spot.CanPlayerEnter(this))
                continue;

            float distance = Vector3.Distance(transform.position, spot.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestSpot = spot;
            }
        }

        if (closestSpot != null)
            EnterHiding(closestSpot);
    }

    private void EnterHiding(HidingSpotInteractable spot)
    {
        IsHidden = true;
        CurrentSpot = spot;
        CurrentSpot.SetOccupant(this);

        SetMovementEnabled(false);
        Teleport(spot.GetHidePosition(), spot.GetHideRotation());
        TryPlayFirstHideDialogue();
    }

    private void ExitHiding()
    {
        if (!IsHidden)
            return;

        HidingSpotInteractable oldSpot = CurrentSpot;

        if (oldSpot != null)
            oldSpot.ClearOccupant(this);

        IsHidden = false;
        CurrentSpot = null;

        SetMovementEnabled(true);

        if (oldSpot != null)
            Teleport(oldSpot.GetExitPosition(), oldSpot.GetExitRotation(transform.rotation));
    }

    private void TryPlayFirstHideDialogue()
    {
        if (!playFirstHideDialogue || hasPlayedFirstHideDialogue)
            return;

        DialogueLine[] dialogueLines = firstHideDialogueLines != null && firstHideDialogueLines.Length > 0
            ? firstHideDialogueLines
            : new[]
            {
                new DialogueLine("Здесь можно спрятаться, но монстр проверяет такие места.", null, 1f, 3f, 0f)
            };

        DialogueSequencePlayer player = DialogueSequencePlayer.GetOrCreate(gameObject);
        if (player == null)
            return;

        bool started = player.Play(
            dialogueLines,
            transform,
            interruptFirstHideDialogue,
            queueFirstHideDialogueIfBusy,
            firstHideDialogueDelay);

        if (started)
            hasPlayedFirstHideDialogue = true;
    }

    private void SetMovementEnabled(bool enabled)
    {
        if (disableWhileHidden == null)
            return;

        for (int i = 0; i < disableWhileHidden.Length; i++)
        {
            if (disableWhileHidden[i] != null)
                disableWhileHidden[i].enabled = enabled;
        }
    }

    private void Teleport(Vector3 position, Quaternion rotation)
    {
        bool controllerWasEnabled = characterController != null && characterController.enabled;

        if (controllerWasEnabled)
            characterController.enabled = false;

        transform.SetPositionAndRotation(position, rotation);

        if (controllerWasEnabled)
            characterController.enabled = true;
    }
}
