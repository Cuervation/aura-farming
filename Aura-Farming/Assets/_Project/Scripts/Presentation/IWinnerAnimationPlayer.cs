using System;
using AuraFarming.Domain;
namespace AuraFarming.Presentation
{
    public interface IWinnerAnimationPlayer
    {
        void Play(SignalCardId signalCardId, Action onComplete);
    }
}