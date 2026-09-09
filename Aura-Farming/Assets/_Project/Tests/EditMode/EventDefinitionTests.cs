using AuraFarming.Domain;
using AuraFarming.Infrastructure;
using NUnit.Framework;

namespace AuraFarming.Tests.EditMode
{
    public sealed class EventDefinitionTests
    {
        [Test]
        public void ToDomainEvent_MapsKnownEffectKey()
        {
            var definition = UnityEngine.ScriptableObject.CreateInstance<EventDefinition>();
            definition.Initialize("event-1", "Pulse Surge", "Double pulse", "pulse-surge");

            var result = definition.ToDomainEvent();

            Assert.That(result.Id, Is.EqualTo("event-1"));
            Assert.That(result.DisplayName, Is.EqualTo("Pulse Surge"));
            Assert.That(result.Effect, Is.EqualTo(RoundEffect.PulseSurge));
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }
}
