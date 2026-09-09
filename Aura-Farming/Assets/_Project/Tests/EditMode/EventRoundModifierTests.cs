using AuraFarming.Domain;
using NUnit.Framework;

namespace AuraFarming.Tests.EditMode
{
    public sealed class EventRoundModifierTests
    {
        [Test]
        public void PulseSurge_DoublesTheHighestUniqueSignalReward()
        {
            var outcome = Resolve(RoundEffect.PulseSurge, 1, 2);

            Assert.That(outcome.Player(new PlayerId(2)).Pulse, Is.EqualTo(2));
            Assert.That(outcome.AppliedEvent.Effect, Is.EqualTo(RoundEffect.PulseSurge));
        }

        [Test]
        public void StaticStorm_DoublesStaticForMatchingSignals()
        {
            var outcome = Resolve(RoundEffect.StaticStorm, 2, 2);

            Assert.That(outcome.Player(new PlayerId(1)).Static, Is.EqualTo(2));
            Assert.That(outcome.Player(new PlayerId(2)).Static, Is.EqualTo(2));
        }

        [Test]
        public void Sanctuary_PreventsStaticForMatchingSignals()
        {
            var outcome = Resolve(RoundEffect.Sanctuary, 2, 2);

            Assert.That(outcome.StaticRecipients, Is.Empty);
            Assert.That(outcome.Player(new PlayerId(1)).Static, Is.Zero);
            Assert.That(outcome.Player(new PlayerId(2)).Static, Is.Zero);
        }

        private static RoundOutcome Resolve(RoundEffect effect, int firstSignal, int secondSignal)
        {
            var resolver = new RoundResolver();
            return resolver.Resolve(
                new[] { new PlayerState(new PlayerId(1)), new PlayerState(new PlayerId(2)) },
                new[]
                {
                    new Selection(new PlayerId(1), new SignalCardId(1), firstSignal),
                    new Selection(new PlayerId(2), new SignalCardId(2), secondSignal)
                },
                new RoundEvent("test-event", "Test Event", effect));
        }
    }
}
