using System;
using System.Collections.Generic;
using System.Linq;
using AuraFarming.Domain;

namespace AuraFarming.Application
{
    public enum MatchPhase
    {
        Selecting,
        Result,
        Ended
    }

    public enum CommandError
    {
        None,
        InvalidPhase,
        MatchEnded,
        UnknownPlayer,
        EliminatedPlayer,
        UnknownSignalCard,
        SelectionAlreadySubmitted,
        PendingSelections
    }

    public sealed class CommandResult
    {
        private CommandResult(bool isSuccess, CommandError error, bool hasPendingPlayer, PlayerId pendingPlayer)
        {
            IsSuccess = isSuccess;
            Error = error;
            HasPendingPlayer = hasPendingPlayer;
            PendingPlayer = pendingPlayer;
        }

        public bool IsSuccess { get; }
        public CommandError Error { get; }
        public bool HasPendingPlayer { get; }
        public PlayerId PendingPlayer { get; }

        public static CommandResult Success() => new CommandResult(true, CommandError.None, false, default);
        public static CommandResult Failure(CommandError error) => new CommandResult(false, error, false, default);
        public static CommandResult Pending(PlayerId player) => new CommandResult(false, CommandError.PendingSelections, true, player);
    }

    public sealed class GameSession
    {
        private readonly RoundResolver _resolver;
        private readonly Dictionary<SignalCardId, int> _signalValues;
        private readonly Dictionary<PlayerId, Selection> _selections = new Dictionary<PlayerId, Selection>();
        private IReadOnlyList<PlayerState> _players;

        public GameSession(
            IReadOnlyList<PlayerState> players,
            IReadOnlyDictionary<SignalCardId, int> signalValues,
            RoundResolver resolver = null)
        {
            if (players == null)
            {
                throw new ArgumentNullException(nameof(players));
            }

            if (signalValues == null)
            {
                throw new ArgumentNullException(nameof(signalValues));
            }

            _players = Copy(players);
            _signalValues = new Dictionary<SignalCardId, int>(signalValues);
            _resolver = resolver ?? new RoundResolver();
            Phase = MatchPhase.Selecting;
            RoundNumber = 1;
        }

        public MatchPhase Phase { get; private set; }
        public int RoundNumber { get; private set; }
        public int ConfirmedSelectionCount => _selections.Count;
        public RoundOutcome LastOutcome { get; private set; }
        public IReadOnlyList<PlayerState> Players => _players;
        public GameSnapshot Snapshot => new GameSnapshot(Phase, RoundNumber, ConfirmedSelectionCount, _players, LastOutcome);

        public CommandResult SubmitSelection(PlayerId playerId, SignalCardId cardId)
        {
            var phaseError = RejectUnavailablePhase();
            if (phaseError != null)
            {
                return phaseError;
            }

            var player = _players.FirstOrDefault(candidate => candidate.Id == playerId);
            if (player == null)
            {
                return CommandResult.Failure(CommandError.UnknownPlayer);
            }

            if (player.IsEliminated)
            {
                return CommandResult.Failure(CommandError.EliminatedPlayer);
            }

            if (!_signalValues.TryGetValue(cardId, out var signalValue))
            {
                return CommandResult.Failure(CommandError.UnknownSignalCard);
            }

            if (_selections.ContainsKey(playerId))
            {
                return CommandResult.Failure(CommandError.SelectionAlreadySubmitted);
            }

            _selections.Add(playerId, new Selection(playerId, cardId, signalValue));
            return CommandResult.Success();
        }

        public CommandResult RevealRound()
        {
            var phaseError = RejectUnavailablePhase();
            if (phaseError != null)
            {
                return phaseError;
            }

            var pending = _players.FirstOrDefault(player => !player.IsEliminated && !_selections.ContainsKey(player.Id));
            if (pending != null)
            {
                return CommandResult.Pending(pending.Id);
            }

            LastOutcome = _resolver.Resolve(_players, _selections.Values.ToArray());
            _players = Copy(LastOutcome.Players);
            Phase = LastOutcome.IsMatchEnded ? MatchPhase.Ended : MatchPhase.Result;
            return CommandResult.Success();
        }

        public CommandResult StartNextRound()
        {
            if (Phase == MatchPhase.Ended)
            {
                return CommandResult.Failure(CommandError.MatchEnded);
            }

            if (Phase != MatchPhase.Result)
            {
                return CommandResult.Failure(CommandError.InvalidPhase);
            }

            _selections.Clear();
            LastOutcome = null;
            Phase = MatchPhase.Selecting;
            RoundNumber++;
            return CommandResult.Success();
        }

        private CommandResult RejectUnavailablePhase()
        {
            if (Phase == MatchPhase.Ended)
            {
                return CommandResult.Failure(CommandError.MatchEnded);
            }

            return Phase == MatchPhase.Selecting
                ? null
                : CommandResult.Failure(CommandError.InvalidPhase);
        }

        private static IReadOnlyList<PlayerState> Copy(IReadOnlyList<PlayerState> players)
        {
            var copy = new PlayerState[players.Count];
            for (var index = 0; index < players.Count; index++)
            {
                copy[index] = players[index];
            }

            return Array.AsReadOnly(copy);
        }
    }
}



