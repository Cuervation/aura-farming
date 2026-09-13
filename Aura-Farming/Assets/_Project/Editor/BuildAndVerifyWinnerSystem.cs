using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.Animations;
using AuraFarming.Presentation;

namespace AuraFarming.Editor
{
    public static class BuildAndVerifyWinnerSystem
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string ReportPath = "Assets/_Project/Reports/WinnerSystemBuildReport.json";
        private static readonly string[] Triggers = { "PlayCaptain", "PlaySixSeven", "PlayAuraWalk", "PlayMewing", "PlayStinkyDance", "PlayAnimePose", "PlaySigmaLook", "PlayAdjustGlasses", "PlaySilence", "PlaySnap" };
        private static readonly string[] Clips = { "Win_Captain", "Win_SixSeven", "Win_AuraWalk", "Win_Mewing", "Win_StinkyDance", "Win_AnimePose", "Win_SigmaLook", "Win_AdjustGlasses", "Win_Silence", "Win_Snap" };
        [MenuItem("Aura Farming/Setup/Build And Verify Winner System")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { WriteReport("FAIL", new[] { "UNSAVED_SCENE_RISK" }); return; }
            CatAdditionalAnimationsBuilder.Sync();
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = Object.FindFirstObjectByType<GameCompositionRoot>();
            var director = root == null ? null : root.GetComponent<WinnerAnimationDirector>() ?? root.gameObject.AddComponent<WinnerAnimationDirector>();
            var animator = root == null ? null : root.GetComponentInChildren<Animator>(true);
            if (director != null && animator != null) director.ConfigureDefaultAnimator(animator);
            if (director != null) EditorUtility.SetDirty(director);
            if (scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);
            if (scene.IsValid()) EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Validate(root, director, animator);
        }
        private static void WriteReport(string status, string[] errors)
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "_Project/Reports"));
            var json = "{\n  \"status\": \"" + status + "\",\n  \"errors\": [" + string.Join(",", errors.Select(e => "\"" + e + "\"")) + "]\n}";
            File.WriteAllText(Path.Combine(Application.dataPath, "_Project/Reports/WinnerSystemBuildReport.json"), json);

        }

        private static void Validate(GameCompositionRoot root, WinnerAnimationDirector director, Animator animator)
        {
            var missingClips = Clips.Where(n => AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/Cats/" + n + ".anim") == null).ToArray();
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/_Project/Animations/Cats/BaseCat.controller");
            var parameters = controller == null ? new AnimatorControllerParameter[0] : controller.parameters;
            var foundTriggers = Triggers.Count(n => parameters.Count(p => p.name == n && p.type == AnimatorControllerParameterType.Trigger) == 1);
            var machine = controller == null ? null : controller.layers[0].stateMachine;
            var foundStates = machine == null ? 0 : Clips.Count(n => machine.states.Any(s => s.state != null && s.state.name == n && s.state.motion != null));
            var entryTransitions = machine == null ? 0 : Clips.Count(n => machine.anyStateTransitions.Any(t => t.destinationState != null && t.destinationState.name == n));
            var returnTransitions = machine == null ? 0 : machine.states.Where(s => s.state != null && s.state.name.StartsWith("Win_")).Sum(s => s.state.transitions.Count(t => t.destinationState != null && t.destinationState.name == "BaseCat_Idle"));
            var report = "{\n  \"status\": \"" + (missingClips.Length == 0 && foundTriggers == 10 && foundStates == 10 && entryTransitions == 10 && returnTransitions == 10 && director != null && animator != null ? "PASS" : "FAIL") + "\",\n  \"clips_found\": " + (10 - missingClips.Length) + ",\n  \"clips_expected\": 10,\n  \"triggers_found\": " + foundTriggers + ",\n  \"triggers_expected\": 10,\n  \"states_found\": " + foundStates + ",\n  \"states_expected\": 10,\n  \"entry_transitions\": " + entryTransitions + ",\n  \"return_transitions\": " + returnTransitions + ",\n  \"director_count\": " + (director == null ? 0 : 1) + ",\n  \"animator_assigned\": " + (animator != null ? "true" : "false") + ",\n  \"scene_wired\": " + (director != null && animator != null ? "true" : "false") + ",\n  \"missing_clips\": [" + string.Join(",", missingClips.Select(n => "\"" + n + "\"")) + "],\n  \"errors\": []\n}";
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath)); File.WriteAllText(Path.Combine(Application.dataPath, "_Project/Reports/WinnerSystemBuildReport.json"), report);
            Debug.Log("WINNER SYSTEM BUILD: " + (report.Contains("\"PASS\"") ? "PASS" : "FAIL"));
        }
    }
}
