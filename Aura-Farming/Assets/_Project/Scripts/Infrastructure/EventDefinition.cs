using UnityEngine;
using AuraFarming.Domain;

namespace AuraFarming.Infrastructure
{
    [CreateAssetMenu(menuName = "Aura Farming/Event", fileName = "Event")]
    public sealed class EventDefinition : ScriptableObject
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea] private string description = string.Empty;
        [SerializeField] private string effectKey = string.Empty;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public string EffectKey => effectKey;

        public RoundEvent ToDomainEvent()
        {
            var effect = effectKey switch
            {
                "pulse-surge" => RoundEffect.PulseSurge,
                "static-storm" => RoundEffect.StaticStorm,
                "sanctuary" => RoundEffect.Sanctuary,
                _ => RoundEffect.None
            };
            return new RoundEvent(id, displayName, effect);
        }

        public void Initialize(string eventId, string eventName, string eventDescription, string eventEffectKey)
        {
            id = eventId ?? string.Empty;
            displayName = eventName ?? string.Empty;
            description = eventDescription ?? string.Empty;
            effectKey = eventEffectKey ?? string.Empty;
        }
    }
}
