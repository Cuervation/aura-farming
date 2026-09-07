using System;

namespace AuraFarming.Domain
{
    public readonly struct SignalCardId : IEquatable<SignalCardId>
    {
        public SignalCardId(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Signal card identifiers must be positive.");
            }

            Value = value;
        }

        public int Value { get; }

        public bool Equals(SignalCardId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is SignalCardId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => $"Signal {Value}";
        public static bool operator ==(SignalCardId left, SignalCardId right) => left.Equals(right);
        public static bool operator !=(SignalCardId left, SignalCardId right) => !left.Equals(right);
    }
}
