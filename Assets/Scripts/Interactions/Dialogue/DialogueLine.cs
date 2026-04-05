using System;
using UnityEngine;

namespace Game.Interaction
{
    [Serializable]
    public class DialogueLine
    {
        [SerializeField] private string subtitleText;
        [SerializeField] private AudioClip voiceClip;
        [SerializeField, Range(0.1f, 10f)] private float volumeMultiplier = 1f;
        [SerializeField, Min(0f)] private float minimumDuration = 1.5f;
        [SerializeField, Min(0f)] private float delayAfter;

        public string SubtitleText => subtitleText;
        public AudioClip VoiceClip => voiceClip;
        public float VolumeMultiplier => volumeMultiplier;
        public float MinimumDuration => minimumDuration;
        public float DelayAfter => delayAfter;

        public bool IsEmpty()
        {
            return string.IsNullOrWhiteSpace(subtitleText) && voiceClip == null;
        }

        public float GetPlaybackDuration()
        {
            float clipDuration = voiceClip != null ? voiceClip.length : 0f;
            return Mathf.Max(minimumDuration, clipDuration);
        }
    }
}
