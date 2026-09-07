using NUnit.Framework;
using AuraFarming.Editor;
using AuraFarming.Infrastructure;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AuraFarming.Tests.SceneSetup
{
    public class SceneSetupTests
    {
        [Test]
        public void SignalArtworkIsAppliedToItsMatchingSelectionButton()
        {
            var root = new GameObject("MatchView");
            var selection = new GameObject("Selection");
            var firstButton = new GameObject("Signal 1", typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            firstButton.transform.SetParent(selection.transform, false);
            var secondButton = new GameObject("Signal 2", typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            secondButton.transform.SetParent(selection.transform, false);
            var texture = new Texture2D(2, 2);
            var artwork = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            var signal = ScriptableObject.CreateInstance<SignalDefinition>();
            signal.Initialize("signal-1", "First Sprout", 1, artwork);
            var view = root.AddComponent<MatchView>();
            try
            {
                view.Configure(selection, null, null, null, null, null, null);
                view.ConfigureSignalArtwork(new[] { signal });

                Assert.That(firstButton.GetComponent<UnityEngine.UI.Image>().sprite, Is.SameAs(artwork));
                Assert.That(secondButton.GetComponent<UnityEngine.UI.Image>().sprite, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(selection);
                Object.DestroyImmediate(signal);
                Object.DestroyImmediate(artwork);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void PresentationCameraRepairIsIdempotentAndRendersDisplayZero()
        {
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                SceneSetupBuilder.EnsurePresentationCamera(scene);
                SceneSetupBuilder.EnsurePresentationCamera(scene);
                var cameras = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.SelectMany(
                    scene.GetRootGameObjects(), g => g.GetComponentsInChildren<Camera>(true)));
                Assert.That(cameras.Length, Is.EqualTo(1));
                Assert.That(cameras[0].enabled, Is.True);
                Assert.That(cameras[0].targetDisplay, Is.Zero);
                Assert.That(cameras[0].targetTexture, Is.Null);
                Assert.That(cameras[0].cullingMask, Is.Zero);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                SceneManager.SetActiveScene(previous);
            }
        }

        [Test]
        public void EmptyGameSceneReportsMissingWiring()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try { Assert.That(SceneSetupBuilder.ValidateScene(scene, "Game"), Is.Not.Empty); }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void GeneratedMenuHasValidWiring()
        {
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                SceneSetupBuilder.PopulateMainMenu();
                Assert.That(SceneSetupBuilder.ValidateScene(scene, "MainMenu"), Is.Empty);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                SceneManager.SetActiveScene(previous);
            }
        }
    }
}
