using System.Collections.Generic;
using AuraFarming.Application;
using AuraFarming.Domain;
using NUnit.Framework;

namespace AuraFarming.Tests.EditMode
{
    public sealed class GameSessionTests
    {
        [Test]
        public void SubmitSelection_AcceptsOnlyOnePrivateSelectionPerActivePlayer()
        {
            var session = CreateSession(new[] { new PlayerState(new PlayerId(1)), new PlayerState(new PlayerId(2)) });

            var first = session.SubmitSelection(new PlayerId(1), new SignalCardId(1));
            var duplicate = session.SubmitSelection(new PlayerId(1), new SignalCardId(2));

            Assert.That(first.IsSuccess, Is.True);
            Assert.That(duplicate.IsSuccess, Is.False);
            Assert.That(duplicate.Error, Is.EqualTo(CommandError.SelectionAlreadySubmitted));
            Assert.That(session.ConfirmedSelectionCount, Is.EqualTo(1));
        }

        [Test]
        public void RevealRound_BeforeEveryoneConfirmsIsBlockedWithPendingPlayer()
        {
            var session = CreateSession(new[] { new PlayerState(new PlayerId(1)), new PlayerState(new PlayerId(2)) });
            session.SubmitSelection(new PlayerId(1), new SignalCardId(1));

            var result = session.RevealRound();

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo(CommandError.PendingSelections));
            Assert.That(result.HasPendingPlayer, Is.True);
            Assert.That(result.PendingPlayer, Is.EqualTo(new PlayerId(2)));
            Assert.That(session.Phase, Is.EqualTo(MatchPhase.Selecting));
        }

        [Test]
        public void RevealRound_AfterEveryoneConfirmsMovesToResult()
        {
            var session = CreateSession(new[] { new PlayerState(new PlayerId(1)), new PlayerState(new PlayerId(2)) });
            session.SubmitSelection(new PlayerId(1), new SignalCardId(1));
            session.SubmitSelection(new PlayerId(2), new SignalCardId(2));

            var result = session.RevealRound();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(session.Phase, Is.EqualTo(MatchPhase.Result));
            Assert.That(session.LastOutcome.PulseRecipients, Is.EqualTo(new[] { new PlayerId(2) }));
        }

        [Test]
        public void MatchEnded_BlocksEveryFurtherCommand()
        {
            var session = CreateSession(new[]
            {
                new PlayerState(new PlayerId(1), @static: 2),
                new PlayerState(new PlayerId(2))
            });
            session.SubmitSelection(new PlayerId(1), new SignalCardId(3));
            session.SubmitSelection(new PlayerId(2), new SignalCardId(3));
            session.RevealRound();

            var submit = session.SubmitSelection(new PlayerId(2), new SignalCardId(1));
            var reveal = session.RevealRound();
            var nextRound = session.StartNextRound();

            Assert.That(session.Phase, Is.EqualTo(MatchPhase.Ended));
            Assert.That(submit.Error, Is.EqualTo(CommandError.MatchEnded));
            Assert.That(reveal.Error, Is.EqualTo(CommandError.MatchEnded));
            Assert.That(nextRound.Error, Is.EqualTo(CommandError.MatchEnded));
        }

        private static GameSession CreateSession(IReadOnlyList<PlayerState> players)
        {
            return new GameSession(players, new Dictionary<SignalCardId, int>
            {
                [new SignalCardId(1)] = 1,
                [new SignalCardId(2)] = 2,
                [new SignalCardId(3)] = 3
            });
        }
    }
}
