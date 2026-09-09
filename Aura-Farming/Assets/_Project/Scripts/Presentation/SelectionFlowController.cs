using System;
using System.Collections.Generic;
using System.Linq;
using AuraFarming.Application;
using AuraFarming.Domain;

namespace AuraFarming.Presentation
{
    public enum SelectionFlowPhase
    {
        AwaitingPrivateSelection,
        AwaitingHandover,
        ReadyToReveal,
        Result,
        Ended
    }

    public sealed class SelectionFlowController
    {
        private readonly GameSession _session;
        private readonly HashSet<PlayerId> _submittedPlayers = new HashSet<PlayerId>();
        private PlayerId _nextPlayer;
        private RoundEvent _roundEvent;

        public SelectionFlowController(GameSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            CurrentPlayer = FirstActivePlayer();
            Phase = SelectionFlowPhase.AwaitingPrivateSelection;
        }

        public SelectionFlowPhase Phase { get; private set; }
        public PlayerId CurrentPlayer { get; private set; }
        public PlayerId NextPlayer => _nextPlayer;
        public RoundEvent SelectedEvent => _roundEvent;
        public GameSnapshot Snapshot => _session.Snapshot;

        public CommandResult SubmitCurrent(SignalCardId cardId)
        {
            if (Phase != SelectionFlowPhase.AwaitingPrivateSelection)
            {
                return CommandResult.Failure(CommandError.InvalidPhase);
            }

            var result = _session.SubmitSelection(CurrentPlayer, cardId);
            if (!result.IsSuccess)
            {
                return result;
            }

            _submittedPlayers.Add(CurrentPlayer);
            var next = _session.Players.FirstOrDefault(player => !player.IsEliminated && !_submittedPlayers.Contains(player.Id));
            if (next == null)
            {
                Phase = SelectionFlowPhase.ReadyToReveal;
            }
            else
            {
                _nextPlayer = next.Id;
                Phase = SelectionFlowPhase.AwaitingHandover;
            }

            return result;
        }

        public CommandResult AcknowledgeHandover()
        {
            if (Phase != SelectionFlowPhase.AwaitingHandover)
            {
                return CommandResult.Failure(CommandError.InvalidPhase);
            }

            CurrentPlayer = _nextPlayer;
            _nextPlayer = default;
            Phase = SelectionFlowPhase.AwaitingPrivateSelection;
            return CommandResult.Success();
        }

        public CommandResult RevealRound()
        {
            if (Phase != SelectionFlowPhase.ReadyToReveal)
            {
                return CommandResult.Failure(CommandError.InvalidPhase);
            }

            var result = _session.RevealRound(_roundEvent);
            if (result.IsSuccess)
            {
                Phase = _session.Phase == MatchPhase.Ended
                    ? SelectionFlowPhase.Ended
                    : SelectionFlowPhase.Result;
            }

            return result;
        }

        public CommandResult SelectEvent(RoundEvent roundEvent)
        {
            if (Phase != SelectionFlowPhase.AwaitingPrivateSelection && Phase != SelectionFlowPhase.ReadyToReveal)
                return CommandResult.Failure(CommandError.InvalidPhase);
            _roundEvent = roundEvent;
            return CommandResult.Success();
        }

        public CommandResult StartNextRound()
        {
            if (Phase != SelectionFlowPhase.Result)
            {
                return Phase == SelectionFlowPhase.Ended
                    ? CommandResult.Failure(CommandError.MatchEnded)
                    : CommandResult.Failure(CommandError.InvalidPhase);
            }

            var result = _session.StartNextRound();
            if (result.IsSuccess)
            {
                _submittedPlayers.Clear();
                _roundEvent = null;
                CurrentPlayer = FirstActivePlayer();
                Phase = SelectionFlowPhase.AwaitingPrivateSelection;
            }

            return result;
        }

        private PlayerId FirstActivePlayer()
        {
            return _session.Players.First(player => !player.IsEliminated).Id;
        }
    }
}
