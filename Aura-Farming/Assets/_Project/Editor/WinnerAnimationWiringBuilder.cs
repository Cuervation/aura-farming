using UnityEditor;
using UnityEngine;
using AuraFarming.Presentation;

namespace AuraFarming.Editor
{
    public static class WinnerAnimationWiringBuilder
    {
        [MenuItem("Aura Farming/Setup/Wire Winner Animations")]
        public static void Wire()
        {
            var root = Object.FindFirstObjectByType<GameCompositionRoot>();
            if (root == null) { Debug.LogWarning("GameCompositionRoot not found."); return; }
            var director = root.GetComponent<WinnerAnimationDirector>() ?? root.gameObject.AddComponent<WinnerAnimationDirector>();
            var animator = root.GetComponentInChildren<Animator>(true);
            if (animator == null) { Debug.LogWarning("Winner Animator not found."); return; }
            director.ConfigureDefaultAnimator(animator);
            EditorUtility.SetDirty(director);
            Debug.Log("WinnerAnimationDirector wired to " + animator.name + ".");
        }
    }
}