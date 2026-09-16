using System;
using System.Collections.Generic;

namespace AuraFarming.Domain
{
    public sealed class RoundOutcome
    {
        private readonly IReadOnlyList<PlayerState> _players;
        private readonly IReadOnlyList<PlayerId> _pulseRecipients;
        private readonly IReadOnlyList<PlayerId> _staticRecipients;
        private readonly IReadOnlyList<PlayerId> _winners;
        private readonly IReadOnlyList<Selection> _selections;
        private readonly SignalCardId? _winningSignalCardId;

        public RoundOutcome(
            IReadOnlyList<PlayerState> players,
            IReadOnlyList<PlayerId> pulseRecipients,
            IReadOnlyList<PlayerId> staticRecipients,
            IReadOnlyList<PlayerId> winners,
            bool isDraw,
            RoundEvent appliedEvent = null,
            SignalCardId? winningSignalCardId = null,
            IReadOnlyList<Selection> selections = null,
            int pulseReward = 1,
            int staticReward = 1)
        {
            _players = Copy(players);
            _pulseRecipients = Copy(pulseRecipients);
            _staticRecipients = Copy(staticRecipients);
            _winners = Copy(winners);
            _selections = Copy(selections ?? Array.Empty<Selection>());
            _winningSignalCardId = winningSignalCardId;
            IsDraw = isDraw;
            AppliedEvent = appliedEvent;
            PulseReward = pulseReward;
            StaticReward = staticReward;
        }

        public IReadOnlyList<PlayerState> Players => _players;
        public IReadOnlyList<PlayerId> PulseRecipients => _pulseRecipients;
        public IReadOnlyList<PlayerId> StaticRecipients => _staticRecipients;
        public IReadOnlyList<PlayerId> Winners => _winners;
        public IReadOnlyList<Selection> Selections => _selections;
        public SignalCardId? WinningSignalCardId => _winningSignalCardId;
        public bool IsDraw { get; }
        public RoundEvent AppliedEvent { get; }
        public int PulseReward { get; }
        public int StaticReward { get; }
        public bool IsMatchEnded => IsDraw || _winners.Count > 0;

        public PlayerState Player(PlayerId id)
        {
            for (var index = 0; index < _players.Count; index++)
            {
                if (_players[index].Id == id)
                {
                    return _players[index];
                }
            }

            throw new ArgumentException("The outcome does not contain the requested player.", nameof(id));
        }

        private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var copy = new T[source.Count];
            for (var index = 0; index < source.Count; index++)
            {
                copy[index] = source[index];
            }

            return Array.AsReadOnly(copy);
        }
    }
}
