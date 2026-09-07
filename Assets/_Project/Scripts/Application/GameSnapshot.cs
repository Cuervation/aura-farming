using System;
using System.Collections.Generic;
using AuraFarming.Domain;

namespace AuraFarming.Application
{
    public sealed class GameSnapshot
    {
        private readonly IReadOnlyList<PlayerState> _players;

        public GameSnapshot(
            MatchPhase phase,
            int roundNumber,
            int confirmedSelectionCount,
            IReadOnlyList<PlayerState> players,
            RoundOutcome lastOutcome)
        {
            if (players == null)
            {
                throw new ArgumentNullException(nameof(players));
            }

            Phase = phase;
            RoundNumber = roundNumber;
            ConfirmedSelectionCount = confirmedSelectionCount;
            LastOutcome = lastOutcome;

            var copy = new PlayerState[players.Count];
            for (var index = 0; index < players.Count; index++)
            {
                copy[index] = players[index];
            }
            _players = Array.AsReadOnly(copy);
        }

        public MatchPhase Phase { get; }
        public int RoundNumber { get; }
        public int ConfirmedSelectionCount { get; }
        public IReadOnlyList<PlayerState> Players => _players;
        public RoundOutcome LastOutcome { get; }
    }
}
