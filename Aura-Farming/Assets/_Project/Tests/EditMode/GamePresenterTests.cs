using System.Collections.Generic;
using AuraFarming.Application;
using AuraFarming.Domain;
using AuraFarming.Presentation;
using NUnit.Framework;
using System;

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
            presenter.ChooseSignal(new SignalCardId(1)); presenter.ConfirmSignal();

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
            presenter.ChooseSignal(new SignalCardId(1)); presenter.ConfirmSignal();
            presenter.ContinueAfterHandover();
            presenter.ChooseSignal(new SignalCardId(2)); presenter.ConfirmSignal();

            presenter.RevealRound();

            Assert.That(view.RevealedOutcome.PulseRecipients, Is.EqualTo(new[] { new PlayerId(2) }));
            Assert.That(view.LastSnapshot.Phase, Is.EqualTo(MatchPhase.Result));
        }

        [Test]
        public void RevealWithWinningCard_WaitsForAnimationAndPassesExactCardId()
        {
            var view = new RecordingMatchView();
            var player = new FakeWinnerAnimationPlayer();
            var presenter = new GamePresenter(CreateFlow(), view, player);
            presenter.Start(); presenter.ChooseSignal(new SignalCardId(1)); presenter.ConfirmSignal(); presenter.ContinueAfterHandover(); presenter.ChooseSignal(new SignalCardId(8)); presenter.ConfirmSignal(); presenter.RevealRound();
            Assert.That(player.PlayCount, Is.EqualTo(1));
            Assert.That(player.CardId, Is.EqualTo(new SignalCardId(8)));
            Assert.That(view.RevealCount, Is.EqualTo(0));
            player.Complete();
            Assert.That(view.RevealCount, Is.EqualTo(1));
        }

        [Test]
        public void RevealWithoutWinningCard_ContinuesImmediately()
        {
            var view = new RecordingMatchView();
            var player = new FakeWinnerAnimationPlayer();
            var presenter = new GamePresenter(CreateFlow(), view, player);
            presenter.Start(); presenter.ChooseSignal(new SignalCardId(1)); presenter.ConfirmSignal(); presenter.ContinueAfterHandover(); presenter.ChooseSignal(new SignalCardId(1)); presenter.ConfirmSignal(); presenter.RevealRound();
            Assert.That(player.PlayCount, Is.EqualTo(0));
            Assert.That(view.RevealCount, Is.EqualTo(1));
        }

        [Test]
        public void DuplicateAnimationCallback_DoesNotDuplicateReveal()
        {
            var view = new RecordingMatchView();
            var player = new FakeWinnerAnimationPlayer();
            var presenter = new GamePresenter(CreateFlow(), view, player);
            presenter.Start(); presenter.ChooseSignal(new SignalCardId(1)); presenter.ConfirmSignal(); presenter.ContinueAfterHandover(); presenter.ChooseSignal(new SignalCardId(8)); presenter.ConfirmSignal(); presenter.RevealRound();
            player.Complete(); player.Complete();
            Assert.That(view.RevealCount, Is.EqualTo(1));
        }

        [Test]
        public void LegacyConstructor_UsesImmediateFallback()
        {
            var view = new RecordingMatchView();
            var presenter = new GamePresenter(CreateFlow(), view);
            presenter.Start(); presenter.ChooseSignal(new SignalCardId(1)); presenter.ConfirmSignal(); presenter.ContinueAfterHandover(); presenter.ChooseSignal(new SignalCardId(8)); presenter.ConfirmSignal(); presenter.RevealRound();
            Assert.That(view.RevealCount, Is.EqualTo(1));
        }

        private static SelectionFlowController CreateFlow()
        {
            return new SelectionFlowController(new GameSession(
                new[] { new PlayerState(new PlayerId(1)), new PlayerState(new PlayerId(2)) },
                new Dictionary<SignalCardId, int>
                {
                    [new SignalCardId(1)] = 1,
                    [new SignalCardId(2)] = 2,
                    [new SignalCardId(8)] = 8
                }));
        }

        private sealed class RecordingMatchView : IMatchView
        {
            public PlayerId LastPrivatePlayer { get; private set; }
            public PlayerId LastPassPlayer { get; private set; }
            public GameSnapshot LastSnapshot { get; private set; }
            public RoundOutcome RevealedOutcome { get; private set; }
            public int RevealCount { get; private set; }

            public void Render(GameSnapshot snapshot) => LastSnapshot = snapshot;
            public void ShowPrivateSelection(PlayerId player) => LastPrivatePlayer = player;
            public void ShowPassScreen(PlayerId nextPlayer) => LastPassPlayer = nextPlayer;
            public void ShowReadyToReveal() { }
            public void ShowReveal(RoundOutcome outcome) { RevealedOutcome = outcome; RevealCount++; }
            public void ShowEnd(RoundOutcome outcome) => RevealedOutcome = outcome;
            public void ShowError(CommandError error) { }
        }

        private sealed class FakeWinnerAnimationPlayer : IWinnerAnimationPlayer
        {
            private Action _completion;
            public int PlayCount { get; private set; }
            public SignalCardId CardId { get; private set; }
            public void Play(SignalCardId signalCardId, Action onComplete) { PlayCount++; CardId = signalCardId; _completion = onComplete; }
            public void Complete() => _completion?.Invoke();
        }
    }
}
