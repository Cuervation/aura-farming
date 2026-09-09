using System.Collections.Generic;
using AuraFarming.Application;
using AuraFarming.Domain;
using AuraFarming.Presentation;
using NUnit.Framework;

namespace AuraFarming.Tests.EditMode
{
    public sealed class SelectionFlowControllerTests
    {
        [Test]
        public void SubmitCurrent_RequiresHandoverBeforeNextPrivateSelection()
        {
            var flow = CreateFlow();

            var result = flow.SubmitCurrent(new SignalCardId(1));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(flow.Phase, Is.EqualTo(SelectionFlowPhase.AwaitingHandover));
            Assert.That(flow.NextPlayer, Is.EqualTo(new PlayerId(2)));
            Assert.That(flow.Snapshot.ConfirmedSelectionCount, Is.EqualTo(1));
        }

        [Test]
        public void AcknowledgeHandover_AdvancesToNextPlayerWithoutExposingPreviousCard()
        {
            var flow = CreateFlow();
            flow.SubmitCurrent(new SignalCardId(1));

            var result = flow.AcknowledgeHandover();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(flow.Phase, Is.EqualTo(SelectionFlowPhase.AwaitingPrivateSelection));
            Assert.That(flow.CurrentPlayer, Is.EqualTo(new PlayerId(2)));
            Assert.That(flow.Snapshot.ConfirmedSelectionCount, Is.EqualTo(1));
        }

        [Test]
        public void FinalSelection_EnablesRevealAndRevealMovesToResult()
        {
            var flow = CreateFlow();
            flow.SubmitCurrent(new SignalCardId(1));
            flow.AcknowledgeHandover();
            flow.SubmitCurrent(new SignalCardId(2));

            var reveal = flow.RevealRound();

            Assert.That(reveal.IsSuccess, Is.True);
            Assert.That(flow.Phase, Is.EqualTo(SelectionFlowPhase.Result));
            Assert.That(flow.Snapshot.LastOutcome.PulseRecipients, Is.EqualTo(new[] { new PlayerId(2) }));
        }

        [Test]
        public void SelectEvent_IsPropagatedToRoundOutcome()
        {
            var flow = CreateFlow();
            var roundEvent = new RoundEvent("event-1", "Pulse Surge", RoundEffect.PulseSurge);
            flow.SelectEvent(roundEvent);
            flow.SubmitCurrent(new SignalCardId(1));
            flow.AcknowledgeHandover();
            flow.SubmitCurrent(new SignalCardId(2));

            var reveal = flow.RevealRound();

            Assert.That(reveal.IsSuccess, Is.True);
            Assert.That(flow.Snapshot.LastOutcome.AppliedEvent, Is.SameAs(roundEvent));
        }

        [Test]
        public void StartNextRound_ResetsPrivateFlowForActivePlayers()
        {
            var flow = CreateFlow();
            flow.SubmitCurrent(new SignalCardId(1));
            flow.AcknowledgeHandover();
            flow.SubmitCurrent(new SignalCardId(2));
            flow.RevealRound();

            var result = flow.StartNextRound();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(flow.Phase, Is.EqualTo(SelectionFlowPhase.AwaitingPrivateSelection));
            Assert.That(flow.CurrentPlayer, Is.EqualTo(new PlayerId(1)));
            Assert.That(flow.Snapshot.RoundNumber, Is.EqualTo(2));
            Assert.That(flow.Snapshot.ConfirmedSelectionCount, Is.EqualTo(0));
        }

        private static SelectionFlowController CreateFlow()
        {
            var session = new GameSession(
                new[] { new PlayerState(new PlayerId(1)), new PlayerState(new PlayerId(2)) },
                new Dictionary<SignalCardId, int>
                {
                    [new SignalCardId(1)] = 1,
                    [new SignalCardId(2)] = 2
                });
            return new SelectionFlowController(session);
        }
    }
}
