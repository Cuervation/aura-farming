namespace AuraFarming.Domain
{
    public sealed class Selection
    {
        public Selection(PlayerId playerId, SignalCardId cardId, int signalValue)
        {
            PlayerId = playerId;
            CardId = cardId;
            SignalValue = signalValue;
        }

        public PlayerId PlayerId { get; }
        public SignalCardId CardId { get; }
        public int SignalValue { get; }
    }
}
