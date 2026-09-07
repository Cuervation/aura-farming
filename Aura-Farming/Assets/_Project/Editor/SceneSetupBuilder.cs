using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AuraFarming.Infrastructure;
using AuraFarming.Presentation;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AuraFarming.Editor
{
    // Editor-only: Unity serializes all scene references and persistent button calls.
    public static class SceneSetupBuilder
    {
        private const string SceneFolder = "Assets/_Project/Scenes";
        private const string ContentFolder = "Assets/_Project/Content";
        private static readonly string[] SceneNames = { "Bootstrap", "MainMenu", "Game" };
        private static readonly Color Background = new Color(0.035f, 0.045f, 0.095f);
        private static readonly Color Accent = new Color(0.12f, 0.75f, 0.8f);

        [MenuItem("Aura Farming/Setup/Create Missing Scenes and Content")]
        public static void CreateMissingScenesAndContent()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before scene setup.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolder(SceneFolder);
            EnsureFolder(ContentFolder);
            var config = CreateContent();
            var validation = ContentValidator.Validate(config);
            if (!validation.IsValid)
                throw new InvalidOperationException("Existing content is invalid; preserved without changes: " + string.Join(", ", validation.Errors));
            var previous = SceneManager.GetActiveScene();
            foreach (var name in SceneNames)
            {
                var path = ScenePath(name);
                if (File.Exists(path)) { Debug.Log("Preserved existing scene: " + path); continue; }
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                try
                {
                    SceneManager.SetActiveScene(scene);
                    if (name == "Bootstrap") new GameObject("Bootstrap").AddComponent<BootstrapLoader>();
                    else if (name == "MainMenu") PopulateMainMenu();
                    else PopulateGame(config);
                    var errors = ValidateScene(scene, name);
                    if (errors.Count != 0) throw new InvalidOperationException(string.Join("; ", errors));
                    if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("Could not save " + path);
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                    if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                }
            }
            var paths = SceneNames.Select(ScenePath).ToArray();
            EditorBuildSettings.scenes = paths.Select(p => new EditorBuildSettingsScene(p, true))
                .Concat(EditorBuildSettings.scenes.Where(s => !paths.Contains(s.path))).ToArray();
            AssetDatabase.SaveAssets();
            ValidateSavedSetup();
            Debug.Log("Aura Farming setup saved. Open Bootstrap and enter Play Mode. No player build performed.");
        }

        private static MatchConfig CreateContent()
        {
            var signals = new SignalDefinition[9];
            var events = new EventDefinition[6];
            for (var i = 0; i < signals.Length; i++)
            {
                var n = i + 1;
                signals[i] = LoadOrCreate<SignalDefinition>($"{ContentFolder}/Signal{n:00}.asset",
                    s => s.Initialize($"signal-{n}", $"Signal {n}", n));
            }
            for (var i = 0; i < events.Length; i++)
            {
                var n = i + 1;
                // Current runtime does not execute effects. Do not misrepresent placeholders as mechanics.
                events[i] = LoadOrCreate<EventDefinition>($"{ContentFolder}/Event{n:00}.asset",
                    e => e.Initialize($"event-{n}", $"Event {n} (placeholder)",
                        "Placeholder only. Event effects are not implemented in the current runtime.", $"pending-{n}"));
            }
            return LoadOrCreate<MatchConfig>($"{ContentFolder}/MatchConfig.asset", c => c.Initialize(signals, events));
        }

        private static T LoadOrCreate<T>(string path, Action<T> initialize) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            if (File.Exists(path)) throw new IOException("Refusing to overwrite incompatible asset: " + path);
            var asset = ScriptableObject.CreateInstance<T>();
            initialize(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static void PopulateMainMenu()
        {
            var canvas = CreateCanvas();
            var view = canvas.gameObject.AddComponent<MainMenuView>();
            var title = Panel(canvas, "Title");
            Label(title.transform, "NEON SIGNAL CLASH", 52, 180);
            Label(title.transform, "LOCAL PASS & PLAY  /  PROTOTYPE", 23, 110);
            Button(title.transform, "Play", 0, 10, view.ShowSetup);
            Button(title.transform, "How to play", 0, -75, view.ShowHelp);
            var help = Panel(canvas, "Help");
            Label(help.transform, "HOW TO PLAY", 42, 210);
            Label(help.transform, "Choose one Signal in private, then pass the device.\nAll players reveal together. Matching Signals receive Static.\nThe highest unique Signal receives Pulse.\n5 Pulse wins. 3 Static eliminates. Last active player wins.\nEvent effects, artwork and audio are still pending.", 25, 30, 220);
            Button(help.transform, "Back", 0, -180, view.ShowTitle);
            var setup = Panel(canvas, "Setup");
            Label(setup.transform, "PLAYERS", 42, 180);
            var dropdownObject = DefaultControls.CreateDropdown(new DefaultControls.Resources());
            dropdownObject.name = "PlayerCount";
            dropdownObject.transform.SetParent(setup.transform, false);
            Place(dropdownObject.GetComponent<RectTransform>(), 0, 65, 320, 60);
            var dropdown = dropdownObject.GetComponent<Dropdown>();
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string> { "2 players", "3 players", "4 players", "5 players" });
            foreach (var text in dropdownObject.GetComponentsInChildren<Text>(true))
            { text.font = Font(); text.fontSize = 22; }
            Button(setup.transform, "Start game", 0, -40, view.StartGame);
            Button(setup.transform, "Back", 0, -125, view.ShowTitle);
            view.Configure(title, help, setup, dropdown);
            view.ShowTitle();
        }

        public static void PopulateGame(MatchConfig config)
        {
            var canvas = CreateCanvas();
            var view = canvas.gameObject.AddComponent<MatchView>();
            var root = new GameObject("GameCompositionRoot").AddComponent<GameCompositionRoot>();
            var hud = Label(canvas, "Score", 20, 280, 95);
            var status = Label(canvas, "Status", 26, 190, 75);
            var selection = Panel(canvas, "Selection", false);
            for (var i = 1; i <= 9; i++)
            {
                var button = Button(selection.transform, $"Signal {i}", (i - 1) % 3 * 240 - 240,
                    80 - (i - 1) / 3 * 95, null, 215);
                UnityEventTools.AddIntPersistentListener(button.onClick, root.ChooseSignal, i);
            }
            var pass = Panel(canvas, "Pass", false);
            Label(pass.transform, "KEEP YOUR SIGNAL PRIVATE", 30, 50);
            Button(pass.transform, "I am ready", 0, -60, root.ContinueAfterHandover);
            var reveal = Panel(canvas, "Reveal", false);
            Button(reveal.transform, "Reveal signals", 0, 0, root.RevealRound);
            var result = Panel(canvas, "Result", false);
            Button(result.transform, "Next round", 0, 0, root.StartNextRound);
            var end = Panel(canvas, "End", false);
            Label(end.transform, "MATCH COMPLETE", 44, 20);
            Label(end.transform, "Stop Play Mode to return to the editor.", 22, -60);
            view.Configure(selection, pass, reveal, result, end, hud, status);
            root.Configure(config, view);
            selection.SetActive(true);
            pass.SetActive(false); reveal.SetActive(false); result.SetActive(false); end.SetActive(false);
        }

        private static Transform CreateCanvas()
        {
            EnsurePresentationCamera(SceneManager.GetActiveScene());
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            var background = Panel(go.transform, "Background");
            background.GetComponent<Image>().raycastTarget = false;
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            return go.transform;
        }

        private static GameObject Panel(Transform parent, string name, bool background = true)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            if (background) { var image = go.AddComponent<Image>(); image.color = Background; }
            return go;
        }

        private static Text Label(Transform parent, string value, int size, float y, float height = 80)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, 0, y, 1100, height);
            var text = go.GetComponent<Text>();
            text.text = value; text.font = Font(); text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter; text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Button Button(Transform parent, string label, float x, float y, UnityAction action, float width = 320)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, x, y, width, 65);
            go.GetComponent<Image>().color = Accent;
            var text = Label(go.transform, label, 24, 0, 60);
            text.rectTransform.sizeDelta = new Vector2(width - 12, 60);
            text.color = Background;
            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            if (action != null) UnityEventTools.AddPersistentListener(button.onClick, action);
            return button;
        }

        private static Font Font() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
        }
        private static string ScenePath(string name) => $"{SceneFolder}/{name}.unity";

        public static void EnsurePresentationCamera(Scene scene)
        {
            var exists = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Camera>(true))
                .Any(c => c.isActiveAndEnabled && c.targetTexture == null && c.targetDisplay == 0);
            if (exists) return;
            var go = new GameObject("PresentationCamera");
            SceneManager.MoveGameObjectToScene(go, scene);
            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            camera.cullingMask = 0; // UI uses ScreenSpaceOverlay; only clear the display.
            camera.orthographic = true;
            camera.depth = -100;
        }

        [MenuItem("Aura Farming/Setup/Repair Missing Presentation Cameras")]
        public static void RepairMissingPresentationCameras()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before scene repair.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var previous = SceneManager.GetActiveScene();
            foreach (var name in new[] { "MainMenu", "Game" })
            {
                var path = ScenePath(name);
                if (!File.Exists(path)) continue;
                var scene = SceneManager.GetSceneByPath(path);
                var alreadyOpen = scene.IsValid() && scene.isLoaded;
                if (!alreadyOpen) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    EnsurePresentationCamera(scene);
                    if (!EditorSceneManager.SaveScene(scene, path))
                        throw new IOException("Could not save " + path);
                }
                finally
                {
                    if (!alreadyOpen) EditorSceneManager.CloseScene(scene, true);
                    if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                }
            }
            ValidateSavedSetup();
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }

        [MenuItem("Aura Farming/Setup/Validate Saved Setup")]
        public static void ValidateSavedSetup()
        {
            var errors = new List<string>();
            var config = AssetDatabase.LoadAssetAtPath<MatchConfig>($"{ContentFolder}/MatchConfig.asset");
            if (!ContentValidator.Validate(config).IsValid) errors.Add("Invalid MatchConfig");
            var previous = SceneManager.GetActiveScene();
            foreach (var name in SceneNames)
            {
                var path = ScenePath(name);
                if (!File.Exists(path)) { errors.Add("Missing " + path); continue; }
                var scene = SceneManager.GetSceneByPath(path);
                var alreadyOpen = scene.IsValid() && scene.isLoaded;
                if (!alreadyOpen) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try { errors.AddRange(ValidateScene(scene, name)); }
                finally { if (!alreadyOpen) EditorSceneManager.CloseScene(scene, true); }
            }
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            var enabled = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).Take(3);
            if (!enabled.SequenceEqual(SceneNames.Select(ScenePath))) errors.Add("Build scene order is not Bootstrap, MainMenu, Game");
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            Debug.Log("Aura Farming scene validation passed (references, buttons, content, input, scene order). Gameplay tests not included.");
        }

        public static List<string> ValidateScene(Scene scene, string kind)
        {
            var errors = new List<string>();
            var components = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true)).ToArray();
            if (components.Any(c => c == null)) errors.Add(kind + ": missing script");
            if (kind == "Bootstrap")
            {
                if (!components.OfType<BootstrapLoader>().Any()) errors.Add("BootstrapLoader missing");
                return errors;
            }
            if (!components.OfType<Canvas>().Any()) errors.Add(kind + ": Canvas missing");
            if (!components.OfType<Camera>().Any(c => c.isActiveAndEnabled && c.targetTexture == null && c.targetDisplay == 0))
                errors.Add(kind + ": active display camera missing");
            if (components.OfType<EventSystem>().Count() != 1) errors.Add(kind + ": require one EventSystem");
            if (!components.OfType<InputSystemUIInputModule>().Any()) errors.Add(kind + ": Input System module missing");
            var expected = kind == "MainMenu" ? new[] { typeof(MainMenuView) } : new[] { typeof(MatchView), typeof(GameCompositionRoot) };
            foreach (var type in expected)
            {
                var component = components.FirstOrDefault(c => c != null && c.GetType() == type);
                if (component == null) { errors.Add(kind + ": missing " + type.Name); continue; }
                var property = new SerializedObject(component).GetIterator();
                while (property.NextVisible(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                        errors.Add(kind + ": unassigned " + type.Name + "." + property.name);
            }
            var buttons = components.OfType<Button>().ToArray();
            if (buttons.Length < (kind == "MainMenu" ? 5 : 12)) errors.Add(kind + ": missing buttons");
            foreach (var button in buttons)
                if (button.onClick.GetPersistentEventCount() == 0 || button.onClick.GetPersistentTarget(0) == null)
                    errors.Add(kind + ": button not wired: " + button.name);
            return errors;
        }
    }
}
