namespace AuraFarming.Domain
{
    public enum RoundEffect
    {
        None,
        PulseSurge,
        StaticStorm,
        Sanctuary
    }

    public sealed class RoundEvent
    {
        public RoundEvent(string id, string displayName, RoundEffect effect)
        {
            Id = id;
            DisplayName = displayName;
            Effect = effect;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public RoundEffect Effect { get; }
    }
}
