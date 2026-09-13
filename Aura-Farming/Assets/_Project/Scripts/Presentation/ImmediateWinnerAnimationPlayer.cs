using System;
using AuraFarming.Domain;
namespace AuraFarming.Presentation
{
    public sealed class ImmediateWinnerAnimationPlayer : IWinnerAnimationPlayer
    {
        public void Play(SignalCardId signalCardId, Action onComplete) => onComplete?.Invoke();
    }
}