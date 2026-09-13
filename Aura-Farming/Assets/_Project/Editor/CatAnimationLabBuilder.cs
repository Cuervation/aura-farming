using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AuraFarming.Editor
{
    /// <summary>Creates the isolated BaseCat animation sandbox from the separated art layers.</summary>
    internal static class CatAnimationLabBuilder
    {
        private static AddRequest packageRequest;

        private const string ArtRoot = "Assets/Art/Cats/BaseCat";
        private const string AnimationDirectory = "Assets/_Project/Animations/Cats";
        private const string PrefabDirectory = "Assets/_Project/Prefabs/Cats";
        private const string ScenePath = "Assets/_Project/Scenes/CatAnimationLab.unity";
        private const string PrefabPath = PrefabDirectory + "/BaseCat.prefab";
        private const string IdlePath = AnimationDirectory + "/BaseCat_Idle.anim";
        private const string WinAdjustPath = AnimationDirectory + "/Win_AdjustGlasses.anim";
        private const string WinCaptainPath = AnimationDirectory + "/Win_Captain.anim";
        private const string WinSixSevenPath = AnimationDirectory + "/Win_SixSeven.anim";
        private const string WinAuraWalkPath = AnimationDirectory + "/Win_AuraWalk.anim";
        private const string WinMewingPath = AnimationDirectory + "/Win_Mewing.anim";
        private const string ControllerPath = AnimationDirectory + "/BaseCat.controller";

        private static readonly LayerDefinition[] Layers =
        {
            new("Aura", "aura.png", -2, Vector2.zero),
            new("Tail", "tail.png", -1, new Vector2(-0.36f, -0.1f)),
            new("LegLeft", "leg_left.png", 0, new Vector2(-0.22f, -0.8f)),
            new("LegRight", "leg_right.png", 0, new Vector2(0.22f, -0.8f)),
            new("Body", "body.png", 1, Vector2.zero),
            new("ArmLeft", "arm_left.png", 2, new Vector2(-0.48f, 0.2f)),
            new("ArmRight", "arm_right.png", 2, new Vector2(0.48f, 0.2f)),
            new("Head", "head.png", 3, new Vector2(0f, 0.95f)),
            new("Ears", "ears.png", 4, Vector2.zero),
            new("Eyes", "eyes.png", 5, Vector2.zero),
            new("Glasses", "glasses.png", 6, Vector2.zero),
            new("Flash", "flash.png", 7, Vector2.zero),
        };

        [MenuItem("Aura Farming/Cats/Build BaseCat Animation Lab")]
        private static void Build()
        {
            EnsureFolder(AnimationDirectory);
            EnsureFolder(PrefabDirectory);
            ConfigureSprites();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            var catRoot = CreateCat();
            CreateIdleAnimation(catRoot);
            PrefabUtility.SaveAsPrefabAsset(catRoot, PrefabPath);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = catRoot;
            Debug.Log("BaseCat animation lab created successfully.");
        }

        [MenuItem("Aura Farming/Cats/Play Adjust Glasses")]
        private static void PlayAdjustGlasses()
        {
            var animator = Object.FindFirstObjectByType<Animator>();
            if (animator == null) { Debug.LogWarning("No cat Animator found in the active scene."); return; }
            animator.SetTrigger("PlayAdjustGlasses");
        }

        [MenuItem("Aura Farming/Cats/Play Captain")]
        private static void PlayCaptain() { var a = Object.FindFirstObjectByType<Animator>(); if (a != null) a.SetTrigger("PlayCaptain"); }

        [MenuItem("Aura Farming/Cats/Play Six-Seven")]
        private static void PlaySixSeven() { var a = Object.FindFirstObjectByType<Animator>(); if (a != null) a.SetTrigger("PlaySixSeven"); }

        [MenuItem("Aura Farming/Cats/Play Aura Walk")]
        private static void PlayAuraWalk() { var a = Object.FindFirstObjectByType<Animator>(); if (a != null) a.SetTrigger("PlayAuraWalk"); }

        [MenuItem("Aura Farming/Cats/Play Mewing")]
        private static void PlayMewing() { var a = Object.FindFirstObjectByType<Animator>(); if (a != null) a.SetTrigger("PlayMewing"); }

        [MenuItem("Aura Farming/Cats/Install 2D Animation Package")]
        private static void Install2DAnimation()
        {
            packageRequest = Client.Add("com.unity.2d.animation");
            Debug.Log("Requested Unity Registry package com.unity.2d.animation.");
        }
        private static void ConfigureSprites()
        {
            foreach (var layer in Layers)
            {
                var assetPath = $"{ArtRoot}/{layer.FileName}";
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogError($"Missing BaseCat texture: {assetPath}");
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.8f;
            camera.backgroundColor = new Color(0.08f, 0.11f, 0.17f);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static GameObject CreateCat()
        {
            var root = new GameObject("CatRoot");
            root.transform.localScale = Vector3.one * 0.55f;
            var transforms = new Dictionary<string, Transform> { ["CatRoot"] = root.transform };

            foreach (var layer in Layers)
            {
                var parent = layer.Name is "Ears" or "Eyes" or "Glasses" ? transforms["Head"] : root.transform;
                var part = new GameObject(layer.Name);
                part.transform.SetParent(parent, false);
                part.transform.localPosition = layer.LocalPosition;
                var renderer = part.AddComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/{layer.FileName}");
                renderer.sortingOrder = layer.SortingOrder;
                transforms[layer.Name] = part.transform;
            }

            transforms["Flash"].gameObject.SetActive(false);
            return root;
        }

        private static void CreateIdleAnimation(GameObject root)
        {
            AssetDatabase.DeleteAsset(IdlePath);
            AssetDatabase.DeleteAsset(WinAdjustPath);
            AssetDatabase.DeleteAsset(WinCaptainPath);
            AssetDatabase.DeleteAsset(WinSixSevenPath);
            AssetDatabase.DeleteAsset(WinAuraWalkPath);
            AssetDatabase.DeleteAsset(WinMewingPath);
            AssetDatabase.DeleteAsset(ControllerPath);

            var clip = new AnimationClip { name = "BaseCat_Idle", frameRate = 30f, wrapMode = WrapMode.Loop };

            SetCurve(clip, "Body", "m_LocalScale.x", new Keyframe(0f, 1f), new Keyframe(1f, 1.025f), new Keyframe(2f, 1f));
            SetCurve(clip, "Body", "m_LocalScale.y", new Keyframe(0f, 1f), new Keyframe(1f, 1.035f), new Keyframe(2f, 1f));
            SetCurve(clip, "Head", "localEulerAnglesRaw.z", new Keyframe(0f, 0f), new Keyframe(1f, 1.2f), new Keyframe(2f, 0f));
            SetCurve(clip, "Tail", "localEulerAnglesRaw.z", new Keyframe(0f, -2f), new Keyframe(1f, 2f), new Keyframe(2f, -2f));
            SetCurve(clip, "Head/Ears", "localEulerAnglesRaw.z", new Keyframe(0f, -0.8f), new Keyframe(1f, 0.8f), new Keyframe(2f, -0.8f));
            SetCurve(clip, "Head/Eyes", "m_LocalScale.y", new Keyframe(0f, 1f), new Keyframe(0.93f, 1f), new Keyframe(1f, 0.05f), new Keyframe(1.07f, 1f), new Keyframe(2f, 1f));
            AssetDatabase.CreateAsset(clip, IdlePath);

            var win = new AnimationClip { name = "Win_AdjustGlasses", frameRate = 30f, wrapMode = WrapMode.Once };
            SetCurve(win, "Head", "localEulerAnglesRaw.z", new Keyframe(0f, 0f), new Keyframe(0.28f, -2.5f), new Keyframe(0.65f, 1.5f), new Keyframe(1.1f, 0f), new Keyframe(2.3f, 0f));
            SetCurve(win, "Body", "m_LocalScale.x", new Keyframe(0f, 1f), new Keyframe(0.28f, 0.97f), new Keyframe(0.65f, 1.02f), new Keyframe(2.3f, 1f));
            SetCurve(win, "Body", "m_LocalScale.y", new Keyframe(0f, 1f), new Keyframe(0.28f, 0.96f), new Keyframe(0.65f, 1.03f), new Keyframe(2.3f, 1f));
            SetCurve(win, "ArmRight", "localEulerAnglesRaw.z", new Keyframe(0f, 0f), new Keyframe(0.55f, 18f), new Keyframe(0.95f, 24f), new Keyframe(1.2f, 18f), new Keyframe(2.3f, 0f));
            SetCurve(win, "ArmRight", "m_LocalPosition.x", new Keyframe(0f, 0.48f), new Keyframe(0.55f, 0.32f), new Keyframe(0.95f, 0.28f), new Keyframe(1.2f, 0.32f), new Keyframe(2.3f, 0.48f));
            SetCurve(win, "ArmRight", "m_LocalPosition.y", new Keyframe(0f, 0.2f), new Keyframe(0.55f, 0.55f), new Keyframe(0.95f, 0.72f), new Keyframe(1.2f, 0.55f), new Keyframe(2.3f, 0.2f));
            SetCurve(win, "Head/Glasses", "localEulerAnglesRaw.z", new Keyframe(0f, 0f), new Keyframe(0.9f, 3f), new Keyframe(1.05f, -2f), new Keyframe(1.2f, 0f), new Keyframe(2.3f, 0f));
            SetCurve(win, "Head/Glasses", "m_LocalPosition.x", new Keyframe(0f, 0f), new Keyframe(0.9f, 0.025f), new Keyframe(1.05f, -0.02f), new Keyframe(1.2f, 0f), new Keyframe(2.3f, 0f));
            SetCurve(win, "Aura", "m_LocalScale.x", new Keyframe(0f, 1f), new Keyframe(0.65f, 1.15f), new Keyframe(1.05f, 1.45f), new Keyframe(1.45f, 1f), new Keyframe(2.3f, 1f));
            SetCurve(win, "Aura", "m_LocalScale.y", new Keyframe(0f, 1f), new Keyframe(0.65f, 1.15f), new Keyframe(1.05f, 1.45f), new Keyframe(1.45f, 1f), new Keyframe(2.3f, 1f));
            SetCurve(win, "Flash", "m_IsActive", new Keyframe(0f, 0f), new Keyframe(1.02f, 0f), new Keyframe(1.04f, 1f), new Keyframe(1.22f, 1f), new Keyframe(1.24f, 0f), new Keyframe(2.3f, 0f));
            AssetDatabase.CreateAsset(win, WinAdjustPath);

            var captain = new AnimationClip { name = "Win_Captain", frameRate = 30f, wrapMode = WrapMode.Once };
            SetCurve(captain, "Body", "m_LocalScale.x", new Keyframe(0f, 1f), new Keyframe(.35f, .95f), new Keyframe(.9f, 1.08f), new Keyframe(2.8f, 1f));
            SetCurve(captain, "Body", "m_LocalScale.y", new Keyframe(0f, 1f), new Keyframe(.35f, .96f), new Keyframe(.9f, 1.1f), new Keyframe(2.8f, 1f));
            SetCurve(captain, "Head", "localEulerAnglesRaw.z", new Keyframe(0f, 0f), new Keyframe(.55f, -3f), new Keyframe(1.1f, 4f), new Keyframe(2.8f, 0f));
            SetCurve(captain, "ArmLeft", "localEulerAnglesRaw.z", new Keyframe(0f, 0f), new Keyframe(.8f, -18f), new Keyframe(1.2f, -28f), new Keyframe(2.8f, 0f));
            SetCurve(captain, "ArmRight", "localEulerAnglesRaw.z", new Keyframe(0f, 0f), new Keyframe(.8f, 18f), new Keyframe(1.2f, 28f), new Keyframe(2.8f, 0f));
            SetCurve(captain, "Aura", "m_LocalScale.x", new Keyframe(0f, 1f), new Keyframe(.8f, 1.2f), new Keyframe(1.2f, 1.6f), new Keyframe(1.7f, 1f), new Keyframe(2.8f, 1f));
            SetCurve(captain, "Aura", "m_LocalScale.y", new Keyframe(0f, 1f), new Keyframe(.8f, 1.2f), new Keyframe(1.2f, 1.6f), new Keyframe(1.7f, 1f), new Keyframe(2.8f, 1f));
            SetCurve(captain, "Flash", "m_IsActive", new Keyframe(0f, 0f), new Keyframe(1.2f, 0f), new Keyframe(1.24f, 1f), new Keyframe(1.42f, 0f), new Keyframe(2.8f, 0f));
            AssetDatabase.CreateAsset(captain, WinCaptainPath);

            var sixSeven = new AnimationClip { name = "Win_SixSeven", frameRate = 30f, wrapMode = WrapMode.Once };
            SetCurve(sixSeven, "ArmLeft", "localEulerAnglesRaw.z", new Keyframe(0f, 0f), new Keyframe(.45f, -35f), new Keyframe(.9f, 12f), new Keyframe(1.35f, -35f), new Keyframe(2.4f, 0f));
            SetCurve(sixSeven, "ArmRight", "localEulerAnglesRaw.z", new Keyframe(0f, 0f), new Keyframe(.45f, 35f), new Keyframe(.9f, -12f), new Keyframe(1.35f, 35f), new Keyframe(2.4f, 0f));
            SetCurve(sixSeven, "Head", "localEulerAnglesRaw.z", new Keyframe(0f, 0f), new Keyframe(.6f, 5f), new Keyframe(1.2f, -5f), new Keyframe(2.4f, 0f));
            SetCurve(sixSeven, "Aura", "m_LocalScale.x", new Keyframe(0f, 1f), new Keyframe(.55f, 1.35f), new Keyframe(1.05f, 1f), new Keyframe(1.55f, 1.35f), new Keyframe(2.4f, 1f));
            SetCurve(sixSeven, "Aura", "m_LocalScale.y", new Keyframe(0f, 1f), new Keyframe(.55f, 1.35f), new Keyframe(1.05f, 1f), new Keyframe(1.55f, 1.35f), new Keyframe(2.4f, 1f));
            SetCurve(sixSeven, "Flash", "m_IsActive", new Keyframe(0f, 0f), new Keyframe(.95f, 0f), new Keyframe(.99f, 1f), new Keyframe(1.12f, 0f), new Keyframe(1.5f, 0f), new Keyframe(1.54f, 1f), new Keyframe(1.67f, 0f), new Keyframe(2.4f, 0f));
            AssetDatabase.CreateAsset(sixSeven, WinSixSevenPath);

            var auraWalk = new AnimationClip { name = "Win_AuraWalk", frameRate = 30f, wrapMode = WrapMode.Once };
            SetCurve(auraWalk, "Body", "m_LocalPosition.y", new Keyframe(0f, 0f), new Keyframe(.35f, -.08f), new Keyframe(.8f, .04f), new Keyframe(1.35f, -.04f), new Keyframe(1.9f, .04f), new Keyframe(2.45f, -.03f), new Keyframe(3f, 0f));
            SetCurve(auraWalk, "Body", "localEulerAnglesRaw.z", new Keyframe(0f, 0f), new Keyframe(.8f, -3f), new Keyframe(1.35f, 3f), new Keyframe(1.9f, -3f), new Keyframe(2.45f, 2f), new Keyframe(3f, 0f));
            SetCurve(auraWalk, "LegLeft", "m_LocalPosition.y", new Keyframe(0f, -.8f), new Keyframe(.8f, -.68f), new Keyframe(1.35f, -.86f), new Keyframe(1.9f, -.68f), new Keyframe(2.45f, -.86f), new Keyframe(3f, -.8f));
            SetCurve(auraWalk, "LegRight", "m_LocalPosition.y", new Keyframe(0f, -.8f), new Keyframe(.8f, -.86f), new Keyframe(1.35f, -.68f), new Keyframe(1.9f, -.86f), new Keyframe(2.45f, -.68f), new Keyframe(3f, -.8f));
            SetCurve(auraWalk, "LegLeft", "localEulerAnglesRaw.z", new Keyframe(0f, 0f), new Keyframe(.8f, -10f), new Keyframe(1.35f, 8f), new Keyframe(1.9f, -10f), new Keyframe(2.45f, 8f), new Keyframe(3f, 0f));
            SetCurve(auraWalk, "LegRight", "localEulerAnglesRaw.z", new Keyframe(0f, 0f), new Keyframe(.8f, 8f), new Keyframe(1.35f, -10f), new Keyframe(1.9f, 8f), new Keyframe(2.45f, -10f), new Keyframe(3f, 0f));
            SetCurve(auraWalk, "Aura", "m_LocalScale.x", new Keyframe(0f, 1f), new Keyframe(.8f, 1.2f), new Keyframe(1.35f, 1f), new Keyframe(1.9f, 1.2f), new Keyframe(2.45f, 1.05f), new Keyframe(3f, 1f));
            SetCurve(auraWalk, "Aura", "m_LocalScale.y", new Keyframe(0f, 1f), new Keyframe(.8f, 1.2f), new Keyframe(1.35f, 1f), new Keyframe(1.9f, 1.2f), new Keyframe(2.45f, 1.05f), new Keyframe(3f, 1f));
            SetCurve(auraWalk, "Flash", "m_IsActive", new Keyframe(0f, 0f), new Keyframe(2.42f, 0f), new Keyframe(2.46f, 1f), new Keyframe(2.62f, 0f), new Keyframe(3f, 0f));
            AssetDatabase.CreateAsset(auraWalk, WinAuraWalkPath);

            var mewing = new AnimationClip { name = "Win_Mewing", frameRate = 30f, wrapMode = WrapMode.Once };
            SetCurve(mewing, "Head", "localEulerAnglesRaw.z", new Keyframe(0f, 0f), new Keyframe(.45f, -8f), new Keyframe(.9f, 8f), new Keyframe(1.35f, -6f), new Keyframe(1.9f, 5f), new Keyframe(2.6f, 0f));
            SetCurve(mewing, "Head", "m_LocalPosition.x", new Keyframe(0f, 0f), new Keyframe(.45f, -.06f), new Keyframe(.9f, .06f), new Keyframe(1.35f, -.05f), new Keyframe(1.9f, .04f), new Keyframe(2.6f, 0f));
            SetCurve(mewing, "Head/Glasses", "localEulerAnglesRaw.z", new Keyframe(0f, 0f), new Keyframe(.45f, -3f), new Keyframe(.9f, 3f), new Keyframe(1.35f, -2f), new Keyframe(1.9f, 2f), new Keyframe(2.6f, 0f));
            SetCurve(mewing, "ArmRight", "localEulerAnglesRaw.z", new Keyframe(0f, 0f), new Keyframe(.55f, 28f), new Keyframe(1.1f, 34f), new Keyframe(1.6f, 28f), new Keyframe(2.6f, 0f));
            SetCurve(mewing, "ArmRight", "m_LocalPosition.y", new Keyframe(0f, .2f), new Keyframe(.55f, .5f), new Keyframe(1.1f, .62f), new Keyframe(1.6f, .5f), new Keyframe(2.6f, .2f));
            SetCurve(mewing, "Body", "m_LocalScale.y", new Keyframe(0f, 1f), new Keyframe(.55f, 1.03f), new Keyframe(1.1f, .98f), new Keyframe(1.6f, 1.03f), new Keyframe(2.6f, 1f));
            SetCurve(mewing, "Aura", "m_LocalScale.x", new Keyframe(0f, 1f), new Keyframe(.9f, 1.3f), new Keyframe(1.35f, 1f), new Keyframe(1.9f, 1.25f), new Keyframe(2.6f, 1f));
            SetCurve(mewing, "Aura", "m_LocalScale.y", new Keyframe(0f, 1f), new Keyframe(.9f, 1.3f), new Keyframe(1.35f, 1f), new Keyframe(1.9f, 1.25f), new Keyframe(2.6f, 1f));
            SetCurve(mewing, "Flash", "m_IsActive", new Keyframe(0f, 0f), new Keyframe(1.85f, 0f), new Keyframe(1.89f, 1f), new Keyframe(2.05f, 0f), new Keyframe(2.6f, 0f));
            AssetDatabase.CreateAsset(mewing, WinMewingPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var state = controller.layers[0].stateMachine.AddState("BaseCat_Idle");
            state.motion = clip;
            controller.layers[0].stateMachine.defaultState = state;
            controller.AddParameter("PlayAdjustGlasses", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("PlayCaptain", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("PlaySixSeven", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("PlayAuraWalk", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("PlayMewing", AnimatorControllerParameterType.Trigger);
            var winState = controller.layers[0].stateMachine.AddState("Win_AdjustGlasses");
            winState.motion = win;
            var toWin = state.AddTransition(winState);
            toWin.hasExitTime = false;
            toWin.duration = 0.05f;
            toWin.AddCondition(AnimatorConditionMode.If, 0f, "PlayAdjustGlasses");
            var toIdle = winState.AddTransition(state);
            toIdle.hasExitTime = true;
            toIdle.exitTime = 1f;
            toIdle.duration = 0.08f;
            root.AddComponent<Animator>().runtimeAnimatorController = controller;

            var captainState = controller.layers[0].stateMachine.AddState("Win_Captain"); captainState.motion = captain;
            var sixState = controller.layers[0].stateMachine.AddState("Win_SixSeven"); sixState.motion = sixSeven;
            var auraWalkState = controller.layers[0].stateMachine.AddState("Win_AuraWalk"); auraWalkState.motion = auraWalk;
            var mewingState = controller.layers[0].stateMachine.AddState("Win_Mewing"); mewingState.motion = mewing;
            var toCaptain = state.AddTransition(captainState); toCaptain.hasExitTime = false; toCaptain.duration = .05f; toCaptain.AddCondition(AnimatorConditionMode.If, 0f, "PlayCaptain");
            var toSix = state.AddTransition(sixState); toSix.hasExitTime = false; toSix.duration = .05f; toSix.AddCondition(AnimatorConditionMode.If, 0f, "PlaySixSeven");
            var captainIdle = captainState.AddTransition(state); captainIdle.hasExitTime = true; captainIdle.exitTime = 1f; captainIdle.duration = .08f;
            var sixIdle = sixState.AddTransition(state); sixIdle.hasExitTime = true; sixIdle.exitTime = 1f; sixIdle.duration = .08f;
            var toAuraWalk = state.AddTransition(auraWalkState); toAuraWalk.hasExitTime = false; toAuraWalk.duration = .05f; toAuraWalk.AddCondition(AnimatorConditionMode.If, 0f, "PlayAuraWalk");
            var toMewing = state.AddTransition(mewingState); toMewing.hasExitTime = false; toMewing.duration = .05f; toMewing.AddCondition(AnimatorConditionMode.If, 0f, "PlayMewing");
            var auraWalkIdle = auraWalkState.AddTransition(state); auraWalkIdle.hasExitTime = true; auraWalkIdle.exitTime = 1f; auraWalkIdle.duration = .08f;
            var mewingIdle = mewingState.AddTransition(state); mewingIdle.hasExitTime = true; mewingIdle.exitTime = 1f; mewingIdle.duration = .08f;
        }

        private static void SetCurve(AnimationClip clip, string path, string property, params Keyframe[] keys)
        {
            clip.SetCurve(path, typeof(Transform), property, new AnimationCurve(keys));
        }

        private static void EnsureFolder(string assetPath)
        {
            var segments = assetPath.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }

        private readonly struct LayerDefinition
        {
            public LayerDefinition(string name, string fileName, int sortingOrder, Vector2 localPosition)
            {
                Name = name;
                FileName = fileName;
                SortingOrder = sortingOrder;
                LocalPosition = localPosition;
            }

            public string Name { get; }
            public string FileName { get; }
            public int SortingOrder { get; }
            public Vector2 LocalPosition { get; }
        }
    }
}
