using System;

namespace AuraFarming.Domain
{
    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        public PlayerId(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Player identifiers must be positive.");
            }

            Value = value;
        }

        public int Value { get; }

        public bool Equals(PlayerId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is PlayerId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => $"Player {Value}";
        public static bool operator ==(PlayerId left, PlayerId right) => left.Equals(right);
        public static bool operator !=(PlayerId left, PlayerId right) => !left.Equals(right);
    }
}
