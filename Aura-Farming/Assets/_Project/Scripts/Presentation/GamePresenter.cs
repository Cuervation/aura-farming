using System;
using AuraFarming.Application;
using AuraFarming.Domain;

namespace AuraFarming.Presentation
{
    public interface IMatchView
    {
        void Render(GameSnapshot snapshot);
        void ShowPrivateSelection(PlayerId player);
        void ShowPassScreen(PlayerId nextPlayer);
        void ShowReadyToReveal();
        void ShowReveal(RoundOutcome outcome);
        void ShowEnd(RoundOutcome outcome);
        void ShowError(CommandError error);
    }

    public sealed class GamePresenter
    {
        private readonly SelectionFlowController _flow;
        private readonly IMatchView _view;
        private readonly IWinnerAnimationPlayer _winnerAnimationPlayer;

        public GamePresenter(SelectionFlowController flow, IMatchView view, IWinnerAnimationPlayer winnerAnimationPlayer = null)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _winnerAnimationPlayer = winnerAnimationPlayer ?? new ImmediateWinnerAnimationPlayer();
        }

        public void Start()
        {
            _view.Render(_flow.Snapshot);
            _view.ShowPrivateSelection(_flow.CurrentPlayer);
        }

        public void ChooseSignal(SignalCardId cardId)
        {
            var result = _flow.SubmitCurrent(cardId);
            RenderResult(result);
            if (!result.IsSuccess)
            {
                return;
            }

            if (_flow.Phase == SelectionFlowPhase.AwaitingHandover)
            {
                _view.ShowPassScreen(_flow.NextPlayer);
            }
            else
            {
                _view.ShowReadyToReveal();
            }
        }

        public void ContinueAfterHandover()
        {
            var result = _flow.AcknowledgeHandover();
            RenderResult(result);
            if (result.IsSuccess)
            {
                _view.ShowPrivateSelection(_flow.CurrentPlayer);
            }
        }

        public void RevealRound()
        {
            var result = _flow.RevealRound();
            RenderResult(result);
            if (!result.IsSuccess)
            {
                return;
            }

            var outcome = _flow.Snapshot.LastOutcome;
            var continued = false;
            Action continueReveal = () =>
            {
                if (continued) return;
                continued = true;
                _view.ShowReveal(outcome);
                if (_flow.Phase == SelectionFlowPhase.Ended) _view.ShowEnd(outcome);
            };
            if (outcome.WinningSignalCardId.HasValue)
                _winnerAnimationPlayer.Play(outcome.WinningSignalCardId.Value, continueReveal);
            else
                continueReveal();
        }

        public void SelectEvent(RoundEvent roundEvent)
        {
            var result = _flow.SelectEvent(roundEvent);
            RenderResult(result);
        }

        public void StartNextRound()
        {
            var result = _flow.StartNextRound();
            RenderResult(result);
            if (result.IsSuccess)
            {
                _view.ShowPrivateSelection(_flow.CurrentPlayer);
            }
        }

        private void RenderResult(CommandResult result)
        {
            _view.Render(_flow.Snapshot);
            if (!result.IsSuccess)
            {
                _view.ShowError(result.Error);
            }
        }
    }
}
