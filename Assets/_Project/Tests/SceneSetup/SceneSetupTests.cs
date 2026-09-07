using NUnit.Framework;
using AuraFarming.Editor;
using AuraFarming.Infrastructure;
using AuraFarming.Presentation;
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
            var firstArtworkSlot = new GameObject("Artwork", typeof(UnityEngine.UI.Image));
            firstArtworkSlot.transform.SetParent(firstButton.transform, false);
            var secondArtworkSlot = new GameObject("Artwork", typeof(UnityEngine.UI.Image));
            secondArtworkSlot.transform.SetParent(secondButton.transform, false);
            var texture = new Texture2D(2, 2);
            var artwork = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            var signal = ScriptableObject.CreateInstance<SignalDefinition>();
            signal.Initialize("signal-1", "First Sprout", 1, artwork);
            var view = root.AddComponent<MatchView>();
            try
            {
                view.ConfigureSignalArtworkSlots(new[]
                {
                    firstArtworkSlot.GetComponent<UnityEngine.UI.Image>(),
                    secondArtworkSlot.GetComponent<UnityEngine.UI.Image>()
                });
                view.ApplySignalArtwork(new[] { signal });

                Assert.That(firstArtworkSlot.GetComponent<UnityEngine.UI.Image>().sprite, Is.SameAs(artwork));
                Assert.That(firstArtworkSlot.GetComponent<UnityEngine.UI.Image>().preserveAspect, Is.True);
                Assert.That(secondArtworkSlot.GetComponent<UnityEngine.UI.Image>().sprite, Is.Null);
                Assert.That(secondArtworkSlot.GetComponent<UnityEngine.UI.Image>().enabled, Is.False);
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
