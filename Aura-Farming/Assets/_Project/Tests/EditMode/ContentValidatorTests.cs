using System.Collections.Generic;
using System.Linq;
using AuraFarming.Infrastructure;
using NUnit.Framework;
using UnityEngine;

namespace AuraFarming.Tests.EditMode
{
    public sealed class ContentValidatorTests
    {
        private readonly List<ScriptableObject> _created = new List<ScriptableObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in _created)
            {
                Object.DestroyImmediate(asset);
            }
            _created.Clear();
        }

        [Test]
        public void Validate_AcceptsNineUniqueSignalsAndSixCompleteEvents()
        {
            var config = CreateConfig(CreateSignals(9), CreateEvents(6));

            var result = ContentValidator.Validate(config);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void Validate_RejectsDuplicateSignalIdentifiers()
        {
            var signals = CreateSignals(9);
            signals[8].Initialize("signal-1", "Duplicate", 9);
            var config = CreateConfig(signals, CreateEvents(6));

            var result = ContentValidator.Validate(config);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Does.Contain(ContentValidationError.DuplicateSignalId));
        }

        [Test]
        public void Validate_RejectsAnyEventCountOtherThanSix()
        {
            var config = CreateConfig(CreateSignals(9), CreateEvents(5));

            var result = ContentValidator.Validate(config);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Does.Contain(ContentValidationError.InvalidEventCount));
        }

        [Test]
        public void Validate_RejectsEventWithoutDefinedEffect()
        {
            var events = CreateEvents(6);
            events[0].Initialize("event-1", "Silent Shift", "Shown before selection", string.Empty);
            var config = CreateConfig(CreateSignals(9), events);

            var result = ContentValidator.Validate(config);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Does.Contain(ContentValidationError.UndefinedEventEffect));
        }

        private SignalDefinition[] CreateSignals(int count)
        {
            return Enumerable.Range(1, count).Select(index =>
            {
                var signal = Track<SignalDefinition>();
                signal.Initialize($"signal-{index}", $"Signal {index}", index);
                return signal;
            }).ToArray();
        }

        private EventDefinition[] CreateEvents(int count)
        {
            return Enumerable.Range(1, count).Select(index =>
            {
                var definition = Track<EventDefinition>();
                definition.Initialize($"event-{index}", $"Event {index}", $"Description {index}", $"effect-{index}");
                return definition;
            }).ToArray();
        }

        private MatchConfig CreateConfig(SignalDefinition[] signals, EventDefinition[] events)
        {
            var config = Track<MatchConfig>();
            config.Initialize(signals, events);
            return config;
        }

        private T Track<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _created.Add(asset);
            return asset;
        }
    }
}
