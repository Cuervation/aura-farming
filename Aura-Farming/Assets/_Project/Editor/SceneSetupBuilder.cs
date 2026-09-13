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
        // Original visual language: warm paper, ink and a single electric accent.
        // Do not imitate the commercial card faces; this only establishes an original,
        // table-top-card-game hierarchy for the digital prototype.
        private static readonly Color Background = new Color(0.055f, 0.05f, 0.035f);
        private static readonly Color Accent = new Color(0.96f, 0.67f, 0.12f);
        private static readonly Color Ink = new Color(0.10f, 0.075f, 0.035f);

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

        [MenuItem("Aura Farming/Setup/Rebuild Presentation UI")]
        public static void RebuildPresentationUi()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before rebuilding the presentation UI.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var config = AssetDatabase.LoadAssetAtPath<MatchConfig>($"{ContentFolder}/MatchConfig.asset");
            if (!ContentValidator.Validate(config).IsValid)
                throw new InvalidOperationException("MatchConfig is invalid; UI was not rebuilt.");

            foreach (var name in new[] { "MainMenu", "Game" })
            {
                var path = ScenePath(name);
                // Single mode removes any loaded copy of the target before we save to its
                // path. It also avoids Unity's prohibition on opening an additive scene while
                // an unsaved temporary scene is active.
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                try
                {
                    if (name == "MainMenu") PopulateMainMenu(); else PopulateGame(config);
                    var errors = ValidateScene(scene, name);
                    if (errors.Count != 0) throw new InvalidOperationException(string.Join("; ", errors));
                    if (!EditorSceneManager.SaveScene(scene, path, true))
                        throw new IOException("Could not save " + path);
                }
                finally
                {
                    // Keep the final rebuilt scene open so the editor never falls back to a
                    // stale loaded copy of the scene that has just been overwritten.
                }
            }
            AssetDatabase.SaveAssets();
            ValidateSavedSetup();
            Debug.Log("Aura Farming presentation UI rebuilt. No player build performed.");
        }

        private static MatchConfig CreateContent()
        {
            var signals = new SignalDefinition[10];
            var events = new EventDefinition[6];
            for (var i = 0; i < signals.Length; i++)
            {
                var n = i + 1;
                var catalog = new[] { ("captain", "Modo Capitán", 2500), ("six-seven", "Six-Seven", 2000), ("aura-walk", "Aura Walk", 1800), ("mewing", "Mewing", 1500), ("stinky-dance", "Stinky Dance", 1200), ("anime-pose", "Pose Anime", 1000), ("sigma-look", "Mirada Sigma", 800), ("adjust-glasses", "Ajuste de Gafas", 600), ("silence", "Gesto de Silencio", 400), ("snap", "El Chasquido", 300) };
                var item = catalog[i];
                signals[i] = LoadOrCreate<SignalDefinition>($"{ContentFolder}/Signal{n:00}.asset",
                    s => s.Initialize(n, item.Item1, item.Item2, item.Item3));
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
            Label(title.transform, "AURA FARMING", 64, 190);
            Label(title.transform, "CHOOSE IN SECRET  ·  REVEAL TOGETHER  ·  FARM AURA", 19, 122);
            Label(title.transform, "THE TABLE IS YOUR STAGE", 28, 48);
            Button(title.transform, "PLAY A MATCH", 0, -42, view.ShowSetup);
            Button(title.transform, "HOW IT WORKS", 0, -125, view.ShowHelp);
            var help = Panel(canvas, "Help");
            Label(help.transform, "THE RULES", 52, 215);
            Label(help.transform, "1  PICK A SIGNAL IN SECRET\n2  PASS THE DEVICE\n3  REVEAL AT THE SAME TIME\n4  THE HIGHEST UNIQUE SIGNAL FARMS PULSE\n\n5 PULSE WINS. 3 STATIC ELIMINATES.", 25, 15, 270);
            Button(help.transform, "BACK", 0, -205, view.ShowTitle);
            var setup = Panel(canvas, "Setup");
            Label(setup.transform, "SET THE TABLE", 52, 205);
            Label(setup.transform, "HOW MANY PLAYERS?", 20, 135);
            var dropdownObject = DefaultControls.CreateDropdown(new DefaultControls.Resources());
            dropdownObject.name = "PlayerCount";
            dropdownObject.transform.SetParent(setup.transform, false);
            Place(dropdownObject.GetComponent<RectTransform>(), 0, 65, 320, 60);
            var dropdown = dropdownObject.GetComponent<Dropdown>();
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string> { "2 players", "3 players", "4 players", "5 players" });
            foreach (var text in dropdownObject.GetComponentsInChildren<Text>(true))
            { text.font = Font(); text.fontSize = 22; }
            Button(setup.transform, "START THE MATCH", 0, -40, view.StartGame);
            Button(setup.transform, "BACK", 0, -125, view.ShowTitle);
            view.Configure(title, help, setup, dropdown);
            view.ShowTitle();
        }

        public static void PopulateGame(MatchConfig config)
        {
            var canvas = CreateCanvas();
            var view = canvas.gameObject.AddComponent<MatchView>();
            var root = new GameObject("GameCompositionRoot").AddComponent<GameCompositionRoot>();
            var hud = Label(canvas, "Score", 18, 316, 44);
            var status = Label(canvas, "Status", 25, 258, 54);
            var selection = Panel(canvas, "Selection", false);
            var signalArtwork = new Image[10];
            for (var i = 1; i <= 10; i++)
            {
                var button = SignalCard(selection.transform, i, (i - 1) % 3 * 200 - 200,
                    135 - (i - 1) / 3 * 185);
                UnityEventTools.AddIntPersistentListener(button.onClick, root.ChooseSignal, i);
                signalArtwork[i - 1] = Artwork(button);
            }
            var pass = Panel(canvas, "Pass", false);
            Label(pass.transform, "KEEP YOUR SIGNAL PRIVATE", 34, 65);
            Label(pass.transform, "PASS THE DEVICE ONLY WHEN YOU ARE READY", 17, 18);
            Button(pass.transform, "I AM READY", 0, -68, root.ContinueAfterHandover);
            var reveal = Panel(canvas, "Reveal", false);
            Label(reveal.transform, "EVERYONE LOCKED IN", 34, 70);
            Button(reveal.transform, "REVEAL SIGNALS", 0, -20, root.RevealRound);
            var result = Panel(canvas, "Result", false);
            Button(result.transform, "NEXT ROUND", 0, -10, root.StartNextRound);
            var end = Panel(canvas, "End", false);
            Label(end.transform, "MATCH COMPLETE", 52, 72);
            Label(end.transform, "THE TABLE HAS SPOKEN", 21, 16);
            Button(end.transform, "PLAY AGAIN", -170, -82, root.StartNewGame, 300);
            Button(end.transform, "MAIN MENU", 170, -82, root.ReturnToMainMenu, 300);
            view.Configure(selection, pass, reveal, result, end, hud, status);
            view.ConfigureSignalArtworkSlots(signalArtwork);
            view.ApplySignalArtwork(config.Signals);
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
            text.color = Ink;
            text.fontStyle = FontStyle.Bold;
            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            if (action != null) UnityEventTools.AddPersistentListener(button.onClick, action);
            return button;
        }

        private static Button SignalCard(Transform parent, int id, float x, float y)
        {
            var button = Button(parent, $"Signal {id}", x, y, null, 170);
            var rect = button.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(170, 170);
            button.GetComponent<Image>().color = new Color(0.15f, 0.12f, 0.075f);
            var label = button.GetComponentInChildren<Text>();
            label.text = $"SIGNAL {id:00}";
            label.fontSize = 18;
            label.color = new Color(1f, 0.92f, 0.70f);
            label.alignment = TextAnchor.MiddleLeft;
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = new Vector2(1f, 0f);
            label.rectTransform.pivot = new Vector2(.5f, 0f);
            label.rectTransform.anchoredPosition = new Vector2(14f, 8f);
            label.rectTransform.sizeDelta = new Vector2(-28f, 30f);
            return button;
        }

        private static Image Artwork(Button button)
        {
            var go = new GameObject("Artwork", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(button.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, .28f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(9f, 7f);
            rect.offsetMax = new Vector2(-9f, -7f);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.enabled = false;
            go.transform.SetSiblingIndex(0);
            return image;
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

        [MenuItem("Aura Farming/Setup/Refresh Signal Artwork")]
        public static void RefreshSignalArtwork()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before refreshing artwork.");
            var config = AssetDatabase.LoadAssetAtPath<MatchConfig>(ContentFolder + "/MatchConfig.asset");
            if (!ContentValidator.Validate(config).IsValid)
                throw new InvalidOperationException("Invalid MatchConfig; artwork was not refreshed.");
            var previous = SceneManager.GetActiveScene();
            var path = ScenePath("Game");
            if (!File.Exists(path))
                throw new FileNotFoundException("Missing Game scene.", path);
            var scene = SceneManager.GetSceneByPath(path);
            var alreadyOpen = scene.IsValid() && scene.isLoaded;
            if (!alreadyOpen) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var components = scene.GetRootGameObjects()
                    .SelectMany(go => go.GetComponentsInChildren<Component>(true))
                    .ToArray();
                var view = components.OfType<MatchView>().SingleOrDefault();
                if (view == null)
                    throw new InvalidOperationException("Game scene has no MatchView.");
                var buttons = components.OfType<Button>().ToArray();
                var slots = Enumerable.Range(1, 10)
                    .Select(id => buttons.SingleOrDefault(button => button.name == $"Signal {id}"))
                    .Select(button => button == null
                        ? throw new InvalidOperationException("Game scene is missing a Signal button.")
                        : button.GetComponentsInChildren<Image>(true)
                            .FirstOrDefault(image => image.gameObject.name == "Artwork") ?? Artwork(button))
                    .ToArray();
                view.ConfigureSignalArtworkSlots(slots);
                view.ApplySignalArtwork(config.Signals);
                EditorUtility.SetDirty(view);
                if (!EditorSceneManager.SaveScene(scene, path))
                    throw new IOException("Could not save " + path);
            }
            finally
            {
                if (!alreadyOpen) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
            ValidateSavedSetup();
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
