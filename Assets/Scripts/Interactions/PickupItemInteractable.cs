using UnityEngine;

namespace Game.Interaction
{
    public class PickupItemInteractable : MonoBehaviour, IInteractable
    {
        [Header("Item")]
        [SerializeField] private string itemId = "ItemA";
        [SerializeField] private string itemDisplayName = "Item";
        [SerializeField] private string pickupPrompt = "Pick up";
        [SerializeField] private bool allowDuplicatePickup;

        [Header("Optional")]
        [SerializeField] private AudioSource pickupAudioSource;
        [SerializeField] private bool destroyAfterPickup = true;

        private string cachedPrompt;

        private void Awake()
        {
            RefreshPrompt();
        }

        private void OnValidate()
        {
            RefreshPrompt();
        }

        public string GetPrompt()
        {
            return cachedPrompt;
        }

        public bool CanInteract(GameObject interactor)
        {
            SimpleInventory inventory = interactor != null ? interactor.GetComponent<SimpleInventory>() : null;
            return inventory != null && (allowDuplicatePickup || !inventory.Has(itemId));
        }

        public void Interact(GameObject interactor)
        {
            SimpleInventory inventory = interactor != null ? interactor.GetComponent<SimpleInventory>() : null;
            if (inventory == null || !inventory.Add(itemId))
                return;

            PlayPickupAudio();

            if (destroyAfterPickup)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);
        }

        private void RefreshPrompt()
        {
            if (string.IsNullOrWhiteSpace(itemDisplayName))
                cachedPrompt = pickupPrompt;
            else
                cachedPrompt = $"{pickupPrompt} {itemDisplayName}".Trim();
        }

        private void PlayPickupAudio()
        {
            if (pickupAudioSource == null || pickupAudioSource.clip == null)
                return;

            if (!pickupAudioSource.transform.IsChildOf(transform))
            {
                pickupAudioSource.Play();
                return;
            }

            GameObject audioObject = new GameObject($"{name}_PickupAudio");
            audioObject.transform.SetPositionAndRotation(pickupAudioSource.transform.position, pickupAudioSource.transform.rotation);

            AudioSource tempAudioSource = audioObject.AddComponent<AudioSource>();
            CopyAudioSettings(pickupAudioSource, tempAudioSource);
            tempAudioSource.PlayOneShot(pickupAudioSource.clip);

            float clipLifetime = pickupAudioSource.clip.length / Mathf.Max(0.01f, tempAudioSource.pitch);
            Destroy(audioObject, clipLifetime + 0.1f);
        }

        private static void CopyAudioSettings(AudioSource source, AudioSource destination)
        {
            destination.outputAudioMixerGroup = source.outputAudioMixerGroup;
            destination.mute = source.mute;
            destination.bypassEffects = source.bypassEffects;
            destination.bypassListenerEffects = source.bypassListenerEffects;
            destination.bypassReverbZones = source.bypassReverbZones;
            destination.playOnAwake = false;
            destination.loop = false;
            destination.priority = source.priority;
            destination.volume = source.volume;
            destination.pitch = source.pitch;
            destination.panStereo = source.panStereo;
            destination.spatialBlend = source.spatialBlend;
            destination.reverbZoneMix = source.reverbZoneMix;
            destination.dopplerLevel = source.dopplerLevel;
            destination.spread = source.spread;
            destination.rolloffMode = source.rolloffMode;
            destination.minDistance = source.minDistance;
            destination.maxDistance = source.maxDistance;
        }
    }
}
