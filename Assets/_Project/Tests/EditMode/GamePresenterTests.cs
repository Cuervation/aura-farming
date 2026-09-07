using System.Collections.Generic;
using AuraFarming.Application;
using AuraFarming.Domain;
using AuraFarming.Presentation;
using NUnit.Framework;

namespace AuraFarming.Tests.EditMode
{
    public sealed class GamePresenterTests
    {
        [Test]
        public void PassAndPlayFlow_ShowsOnlyCurrentOrNextPlayerAtEachStep()
        {
            var view = new RecordingMatchView();
            var presenter = new GamePresenter(CreateFlow(), view);

            presenter.Start();
            presenter.ChooseSignal(new SignalCardId(1));

            Assert.That(view.LastPrivatePlayer, Is.EqualTo(new PlayerId(1)));
            Assert.That(view.LastPassPlayer, Is.EqualTo(new PlayerId(2)));
            Assert.That(view.LastSnapshot.ConfirmedSelectionCount, Is.EqualTo(1));
        }

        [Test]
        public void CompleteRound_PresentsRevealAndResultFromDomainOutcome()
        {
            var view = new RecordingMatchView();
            var presenter = new GamePresenter(CreateFlow(), view);
            presenter.Start();
            presenter.ChooseSignal(new SignalCardId(1));
            presenter.ContinueAfterHandover();
            presenter.ChooseSignal(new SignalCardId(2));

            presenter.RevealRound();

            Assert.That(view.RevealedOutcome.PulseRecipients, Is.EqualTo(new[] { new PlayerId(2) }));
            Assert.That(view.LastSnapshot.Phase, Is.EqualTo(MatchPhase.Result));
        }

        private static SelectionFlowController CreateFlow()
        {
            return new SelectionFlowController(new GameSession(
                new[] { new PlayerState(new PlayerId(1)), new PlayerState(new PlayerId(2)) },
                new Dictionary<SignalCardId, int>
                {
                    [new SignalCardId(1)] = 1,
                    [new SignalCardId(2)] = 2
                }));
        }

        private sealed class RecordingMatchView : IMatchView
        {
            public PlayerId LastPrivatePlayer { get; private set; }
            public PlayerId LastPassPlayer { get; private set; }
            public GameSnapshot LastSnapshot { get; private set; }
            public RoundOutcome RevealedOutcome { get; private set; }

            public void Render(GameSnapshot snapshot) => LastSnapshot = snapshot;
            public void ShowPrivateSelection(PlayerId player) => LastPrivatePlayer = player;
            public void ShowPassScreen(PlayerId nextPlayer) => LastPassPlayer = nextPlayer;
            public void ShowReadyToReveal() { }
            public void ShowReveal(RoundOutcome outcome) => RevealedOutcome = outcome;
            public void ShowEnd(RoundOutcome outcome) => RevealedOutcome = outcome;
            public void ShowError(CommandError error) { }
        }
    }
}
