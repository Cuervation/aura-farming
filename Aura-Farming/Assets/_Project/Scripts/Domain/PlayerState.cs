using System;

namespace AuraFarming.Domain
{
    public sealed class PlayerState
    {
        public PlayerState(PlayerId id, int pulse = 0, int @static = 0, bool isEliminated = false)
        {
            if (pulse < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pulse));
            }

            if (@static < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(@static));
            }

            Id = id;
            Pulse = pulse;
            Static = @static;
            IsEliminated = isEliminated;
        }

        public PlayerId Id { get; }
        public int Pulse { get; }
        public int Static { get; }
        public bool IsEliminated { get; }

        public PlayerState AddScore(int pulse, int @static)
        {
            return new PlayerState(Id, Pulse + pulse, Static + @static, IsEliminated);
        }

        public PlayerState WithElimination(bool isEliminated)
        {
            return new PlayerState(Id, Pulse, Static, isEliminated);
        }
    }
}
