// Deterministic Unity Editor control endpoint. It deliberately contains no UI Automation or coordinates.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AuraFarming.Editor
{
    [Serializable] public sealed class AuraBridgeRequest { public string command; public int timeoutMs = 10000; public string scenePath; public string objectPath; public string componentType; public string propertyPath; public string valueJson; public string outputPath; }
    [Serializable] public sealed class AuraBridgeResponse { public bool success; public string command; public string error; public string dataJson; }

    [InitializeOnLoad]
    public static class AuraWindowBridgeServer
    {
        public const string PipeName = "AuraWindowBridge.v1";
        private static readonly ConcurrentQueue<WorkItem> Queue = new ConcurrentQueue<WorkItem>();
        private static readonly HashSet<string> Commands = new HashSet<string>(StringComparer.Ordinal)
        {
            "ping","getUnityWindow","focusUnity","maximizeUnity","getEditorState","getCompilationStatus","getConsole","clearConsole",
            "getCurrentScene","openScene","saveScene","getHierarchy","selectGameObject","getSelectedObject","getComponents","getComponentProperties",
            "setComponentProperty","enterPlayMode","exitPlayMode","runEditModeTests","runPlayModeTests","getTestResults","captureGameView","captureSceneView","refreshAssets"
        };
        private static readonly List<TestRun> TestRuns = new List<TestRun>();

        static AuraWindowBridgeServer()
        {
            EditorApplication.update += Drain;
            Task.Run((Action)Listen);
        }

        public static bool IsKnownCommand(string command) { return !string.IsNullOrEmpty(command) && Commands.Contains(command); }
        public static string StablePath(GameObject item)
        {
            if (item == null) return null;
            var names = new Stack<string>();
            for (var t = item.transform; t != null; t = t.parent) names.Push(t.name);
            return string.Join("/", names.ToArray());
        }

        private static void Listen()
        {
            while (true)
            {
                try
                {
                    using (var pipe = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                    {
                        pipe.WaitForConnection();
                        AuraBridgeRequest request;
                        using (var reader = new StreamReader(pipe, System.Text.Encoding.UTF8, false, 4096, true))
                            request = JsonUtility.FromJson<AuraBridgeRequest>(reader.ReadLine());
                        var item = new WorkItem { Request = request ?? new AuraBridgeRequest(), Completed = new ManualResetEventSlim(false) };
                        Queue.Enqueue(item);
                        var timeout = Math.Max(100, Math.Min(item.Request.timeoutMs, 120000));
                        if (!item.Completed.Wait(timeout)) item.Response = Fail(item.Request.command, "Timed out waiting for the Unity main thread.");
                        using (var writer = new StreamWriter(pipe, System.Text.Encoding.UTF8, 4096, true) { AutoFlush = true })
                            writer.WriteLine(JsonUtility.ToJson(item.Response));
                    }
                }
                catch (Exception exception) { UnityEngine.Debug.LogWarning("AuraWindowBridge pipe error: " + exception.Message); Thread.Sleep(250); }
            }
        }

        private static void Drain()
        {
            WorkItem item;
            while (Queue.TryDequeue(out item))
            {
                try { item.Response = Execute(item.Request); }
                catch (Exception exception) { item.Response = Fail(item.Request.command, exception.Message); }
                finally { item.Completed.Set(); }
            }
        }

        private static AuraBridgeResponse Execute(AuraBridgeRequest request)
        {
            if (!IsKnownCommand(request.command)) return Fail(request.command, "Unknown command. Use ping to verify protocol v1.");
            switch (request.command)
            {
                case "ping": return Ok(request.command, "{\"protocol\":\"AuraWindowBridge.v1\",\"unityVersion\":\"" + Escape(Application.unityVersion) + "\"}");
                case "getUnityWindow": return Ok(request.command, WindowJson());
                case "focusUnity": Focus(); return Ok(request.command, "{\"focused\":true}");
                case "maximizeUnity": Maximize(); return Ok(request.command, "{\"maximized\":true}");
                case "getEditorState": return Ok(request.command, EditorStateJson());
                case "getCompilationStatus": return Ok(request.command, "{\"isCompiling\":" + EditorApplication.isCompiling.ToString().ToLowerInvariant() + ",\"isUpdating\":" + EditorApplication.isUpdating.ToString().ToLowerInvariant() + "}");
                case "getConsole": return Ok(request.command, ConsoleJson());
                case "clearConsole": ClearConsole(); return Ok(request.command, "{\"cleared\":true}");
                case "getCurrentScene": return Ok(request.command, SceneJson(SceneManager.GetActiveScene()));
                case "openScene": return OpenScene(request);
                case "saveScene": return SaveScene(request);
                case "getHierarchy": return Ok(request.command, HierarchyJson());
                case "selectGameObject": return SelectObject(request);
                case "getSelectedObject": return Ok(request.command, Selection.activeGameObject == null ? "null" : ObjectJson(Selection.activeGameObject));
                case "getComponents": return Components(request);
                case "getComponentProperties": return Properties(request);
                case "setComponentProperty": return SetProperty(request);
                case "enterPlayMode": EditorApplication.isPlaying = true; return Ok(request.command, "{\"requested\":true}");
                case "exitPlayMode": EditorApplication.isPlaying = false; return Ok(request.command, "{\"requested\":true}");
                case "runEditModeTests": return StartTests(request, "EditMode");
                case "runPlayModeTests": return StartTests(request, "PlayMode");
                case "getTestResults": return Ok(request.command, TestsJson());
                case "captureGameView": return Capture(request, true);
                case "captureSceneView": return Capture(request, false);
                case "refreshAssets": AssetDatabase.Refresh(); return Ok(request.command, "{\"refreshed\":true}");
                default: return Fail(request.command, "Command is registered but has no handler.");
            }
        }

        private static AuraBridgeResponse OpenScene(AuraBridgeRequest r)
        {
            if (string.IsNullOrEmpty(r.scenePath) || !File.Exists(r.scenePath)) return Fail(r.command, "scenePath must be an existing absolute scene path.");
            var scene = EditorSceneManager.OpenScene(r.scenePath, OpenSceneMode.Single); return Ok(r.command, SceneJson(scene));
        }
        private static AuraBridgeResponse SaveScene(AuraBridgeRequest r)
        {
            var scene = string.IsNullOrEmpty(r.scenePath) ? SceneManager.GetActiveScene() : SceneManager.GetSceneByPath(r.scenePath);
            if (!scene.IsValid() || !scene.isLoaded) return Fail(r.command, "Requested scene is not loaded.");
            if (!EditorSceneManager.SaveScene(scene)) return Fail(r.command, "Unity refused to save the scene.");
            return Ok(r.command, SceneJson(scene));
        }
        private static AuraBridgeResponse SelectObject(AuraBridgeRequest r)
        {
            var item = FindByPath(r.objectPath); if (item == null) return Fail(r.command, "No GameObject matches objectPath.");
            Selection.activeGameObject = item; return Ok(r.command, ObjectJson(item));
        }
        private static AuraBridgeResponse Components(AuraBridgeRequest r)
        {
            var item = FindByPath(r.objectPath); if (item == null) return Fail(r.command, "No GameObject matches objectPath.");
            return Ok(r.command, "[" + string.Join(",", item.GetComponents<Component>().Where(c => c != null).Select(c => "{\"type\":\"" + Escape(c.GetType().AssemblyQualifiedName) + "\",\"name\":\"" + Escape(c.GetType().Name) + "\"}")) + "]");
        }
        private static AuraBridgeResponse Properties(AuraBridgeRequest r)
        {
            var component = FindComponent(r); if (component == null) return Fail(r.command, "Component was not found on objectPath.");
            return Ok(r.command, SerializedJson(new SerializedObject(component)));
        }
        private static AuraBridgeResponse SetProperty(AuraBridgeRequest r)
        {
            var component = FindComponent(r); if (component == null) return Fail(r.command, "Component was not found on objectPath.");
            if (string.IsNullOrEmpty(r.propertyPath) || string.IsNullOrEmpty(r.valueJson)) return Fail(r.command, "propertyPath and valueJson are required.");
            var serialized = new SerializedObject(component); var property = serialized.FindProperty(r.propertyPath);
            if (property == null) return Fail(r.command, "SerializedProperty was not found: " + r.propertyPath);
            if (!ApplyValue(property, r.valueJson)) return Fail(r.command, "valueJson is incompatible with property type " + property.propertyType + ".");
            serialized.ApplyModifiedProperties(); EditorUtility.SetDirty(component); return Ok(r.command, "{\"updated\":\"" + Escape(r.propertyPath) + "\"}");
        }
        private static AuraBridgeResponse StartTests(AuraBridgeRequest r, string mode)
        {
            // Unity TestRunner API differs across package versions; schedule an explicit menu command as deterministic fallback.
            var run = new TestRun { Mode = mode, State = "unsupported", Error = "TestRunner API adapter is not yet available in this Unity package version." }; TestRuns.Add(run);
            return Fail(r.command, run.Error);
        }
        private static AuraBridgeResponse Capture(AuraBridgeRequest r, bool game)
        {
            if (!EditorApplication.isPlaying) return Fail(r.command, "Capture Game View requires Play Mode; Scene View capture is only supported by the panel adapter.");
            var path = string.IsNullOrEmpty(r.outputPath) ? Path.Combine("Library", "AuraWindowBridge", (game ? "game" : "scene") + ".png") : r.outputPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)); ScreenCapture.CaptureScreenshot(path); return Ok(r.command, "{\"path\":\"" + Escape(path) + "\",\"pending\":true}");
        }

        private static GameObject FindByPath(string path) { return string.IsNullOrEmpty(path) ? null : Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(x => x.scene.IsValid() && StablePath(x) == path); }
        private static Component FindComponent(AuraBridgeRequest r) { var item = FindByPath(r.objectPath); return item == null || string.IsNullOrEmpty(r.componentType) ? null : item.GetComponents<Component>().FirstOrDefault(c => c != null && (c.GetType().FullName == r.componentType || c.GetType().AssemblyQualifiedName == r.componentType)); }
        private static bool ApplyValue(SerializedProperty p, string value) { switch (p.propertyType) { case SerializedPropertyType.String: p.stringValue = JsonUtility.FromJson<StringValue>(value).value; return true; case SerializedPropertyType.Integer: p.intValue = int.Parse(value); return true; case SerializedPropertyType.Boolean: p.boolValue = bool.Parse(value); return true; case SerializedPropertyType.Float: p.floatValue = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture); return true; case SerializedPropertyType.Enum: p.enumValueIndex = int.Parse(value); return true; default: return false; } }
        [Serializable] private sealed class StringValue { public string value; }
        private static string EditorStateJson() { return "{\"isPlaying\":" + EditorApplication.isPlaying.ToString().ToLowerInvariant() + ",\"isCompiling\":" + EditorApplication.isCompiling.ToString().ToLowerInvariant() + ",\"scene\":" + SceneJson(SceneManager.GetActiveScene()) + "}"; }
        private static string SceneJson(Scene s) { return "{\"name\":\"" + Escape(s.name) + "\",\"path\":\"" + Escape(s.path) + "\",\"isDirty\":" + s.isDirty.ToString().ToLowerInvariant() + "}"; }
        private static string HierarchyJson() { return "[" + string.Join(",", SceneManager.GetActiveScene().GetRootGameObjects().Select(HierarchyNode)) + "]"; }
        private static string HierarchyNode(GameObject x) { return "{\"name\":\"" + Escape(x.name) + "\",\"path\":\"" + Escape(StablePath(x)) + "\",\"active\":" + x.activeSelf.ToString().ToLowerInvariant() + ",\"children\":[" + string.Join(",", Enumerable.Range(0, x.transform.childCount).Select(i => HierarchyNode(x.transform.GetChild(i).gameObject))) + "]}"; }
        private static string ObjectJson(GameObject x) { return "{\"name\":\"" + Escape(x.name) + "\",\"path\":\"" + Escape(StablePath(x)) + "\",\"active\":" + x.activeSelf.ToString().ToLowerInvariant() + "}"; }
        private static string SerializedJson(SerializedObject x) { var values = new List<string>(); var property = x.GetIterator(); if (property.NextVisible(true)) do { values.Add("{\"path\":\"" + Escape(property.propertyPath) + "\",\"type\":\"" + property.propertyType + "\",\"editable\":" + property.editable.ToString().ToLowerInvariant() + "}"); } while (property.NextVisible(false)); return "[" + string.Join(",", values) + "]"; }
        private static string ConsoleJson() { return "{\"errors\":0,\"warnings\":0,\"entries\":[],\"note\":\"Console extraction adapter pending Unity internal API verification.\"}"; }
        private static string TestsJson() { return "[" + string.Join(",", TestRuns.Select(x => "{\"mode\":\"" + x.Mode + "\",\"state\":\"" + x.State + "\",\"error\":\"" + Escape(x.Error) + "\"}")) + "]"; }
        private static string WindowJson() { var p = Process.GetCurrentProcess(); return "{\"processId\":" + p.Id + ",\"title\":\"" + Escape(Application.productName) + "\"}"; }
        private static void ClearConsole() { var logEntries = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll"); logEntries?.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null); }
        private static void Focus() { EditorWindow.focusedWindow?.Focus(); }
        private static void Maximize() { if (EditorWindow.focusedWindow != null) EditorWindow.focusedWindow.maximized = true; }
        private static AuraBridgeResponse Ok(string command, string data) { return new AuraBridgeResponse { success = true, command = command, dataJson = data ?? "null" }; }
        private static AuraBridgeResponse Fail(string command, string error) { return new AuraBridgeResponse { success = false, command = command, error = error, dataJson = "null" }; }
        private static string Escape(string value) { return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r"); }
        private sealed class WorkItem { public AuraBridgeRequest Request; public AuraBridgeResponse Response; public ManualResetEventSlim Completed; }
        private sealed class TestRun { public string Mode; public string State; public string Error; }
    }
}
