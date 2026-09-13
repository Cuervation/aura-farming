using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using AuraFarming.Domain;

namespace AuraFarming.Presentation
{
    public sealed class WinnerAnimationDirector : MonoBehaviour, IWinnerAnimationPlayer
    {
        public static string GetTrigger(SignalCardId cardId) => cardId.Value switch { 1 => "PlayCaptain", 2 => "PlaySixSeven", 3 => "PlayAuraWalk", 4 => "PlayMewing", 5 => "PlayStinkyDance", 6 => "PlayAnimePose", 7 => "PlaySigmaLook", 8 => "PlayAdjustGlasses", 9 => "PlaySilence", 10 => "PlaySnap", _ => null };
        public static bool IsWinnerCard(SignalCardId cardId) => GetTrigger(cardId) != null;
        [Serializable]
        private sealed class Binding
        {
            [Min(1)] public int signalCardValue;
            public Animator animator;
            public string trigger = "PlayAdjustGlasses";
        }

        [SerializeField] private Binding[] bindings = Array.Empty<Binding>();
        [SerializeField] private Animator defaultAnimator;

        public void ConfigureDefaultAnimator(Animator animator) => defaultAnimator = animator;

        private Coroutine completionRoutine;
        private Action pendingCompletion;

        public bool Play(SignalCardId cardId)
        {
            for (var i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (binding != null && binding.signalCardValue == cardId.Value && binding.animator != null && !string.IsNullOrWhiteSpace(binding.trigger))
                {
                    binding.animator.ResetTrigger(binding.trigger);
                    binding.animator.SetTrigger(binding.trigger);
                    return true;
                }
            }
            var trigger = GetTrigger(cardId);
            if (defaultAnimator == null || string.IsNullOrWhiteSpace(trigger)) return false;
            defaultAnimator.ResetTrigger(trigger);
            defaultAnimator.SetTrigger(trigger);
            return true;
        }

        public void Play(SignalCardId cardId, Action onComplete)
        {
            var played = Play(cardId);
            if (!played) { onComplete?.Invoke(); return; }
            if (completionRoutine != null) StopCoroutine(completionRoutine);
            pendingCompletion?.Invoke();
            pendingCompletion = onComplete;
            completionRoutine = StartCoroutine(CompleteAfterClip(cardId));
        }

        private IEnumerator CompleteAfterClip(SignalCardId cardId)
        {
            var trigger = GetTrigger(cardId);
            var clipName = trigger == null ? null : trigger.Replace("Play", "Win_");
            var clip = defaultAnimator == null || defaultAnimator.runtimeAnimatorController == null ? null : defaultAnimator.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name == clipName);
            if (clip != null) yield return new WaitForSeconds(clip.length);
            FinishPendingCallback();
        }

        private void FinishPendingCallback()
        {
            var callback = pendingCompletion;
            pendingCompletion = null;
            completionRoutine = null;
            callback?.Invoke();
        }

        private void OnDisable()
        {
            if (completionRoutine != null) StopCoroutine(completionRoutine);
            FinishPendingCallback();
        }

        public void PlayWinningAnimation(RoundOutcome outcome)
        {
            if (outcome == null) return;
            if (outcome.WinningSignalCardId.HasValue) Play(outcome.WinningSignalCardId.Value);
        }
    }
}
