using System;
using UnityEngine;

namespace AuraFarming.Infrastructure
{
    [CreateAssetMenu(menuName = "Aura Farming/Signal", fileName = "Signal")]
    public sealed class SignalDefinition : ScriptableObject
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private int value;
        [SerializeField] private Sprite artwork;

        public string Id => id;
        public string DisplayName => displayName;
        public int Value => value;
        public Sprite Artwork => artwork;

        public void Initialize(string signalId, string signalName, int signalValue, Sprite signalArtwork = null)
        {
            id = signalId ?? string.Empty;
            displayName = signalName ?? string.Empty;
            value = signalValue;
            artwork = signalArtwork;
        }
    }
}
