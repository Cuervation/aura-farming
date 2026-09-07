using System;
using System.Collections.Generic;
using UnityEngine;

namespace AuraFarming.Infrastructure
{
    [CreateAssetMenu(menuName = "Aura Farming/Match Config", fileName = "MatchConfig")]
    public sealed class MatchConfig : ScriptableObject
    {
        [SerializeField] private SignalDefinition[] signals = Array.Empty<SignalDefinition>();
        [SerializeField] private EventDefinition[] events = Array.Empty<EventDefinition>();

        public IReadOnlyList<SignalDefinition> Signals => signals;
        public IReadOnlyList<EventDefinition> Events => events;

        public void Initialize(SignalDefinition[] signalDefinitions, EventDefinition[] eventDefinitions)
        {
            signals = signalDefinitions == null
                ? Array.Empty<SignalDefinition>()
                : (SignalDefinition[])signalDefinitions.Clone();
            events = eventDefinitions == null
                ? Array.Empty<EventDefinition>()
                : (EventDefinition[])eventDefinitions.Clone();
        }
    }
}
