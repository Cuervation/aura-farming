using System.Collections.Generic;
using AuraFarming.Infrastructure;
using AuraFarming.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AuraFarming.Tests.EditMode
{
    public sealed class MatchViewArtworkTests
    {
        private readonly List<Object> _created = new List<Object>();

        [Test]
        public void ApplySignalArtwork_RendersConfiguredArtAndPreservesAspectRatio()
        {
            var view = Create<MatchView>("View");
            var firstSlot = Create<Image>("First slot");
            var secondSlot = Create<Image>("Second slot");
            var firstArtwork = CreateSprite(Color.cyan);
            var firstSignal = CreateAsset<SignalDefinition>("First signal");
            var secondSignal = CreateAsset<SignalDefinition>("Second signal");
            firstSignal.Initialize("signal-1", "Signal 1", 1, firstArtwork);
            secondSignal.Initialize("signal-2", "Signal 2", 2);

            view.ConfigureSignalArtworkSlots(new[] { firstSlot, secondSlot });
            view.ApplySignalArtwork(new[] { firstSignal, secondSignal });

            Assert.That(firstSlot.sprite, Is.EqualTo(firstArtwork));
            Assert.That(firstSlot.preserveAspect, Is.True);
            Assert.That(firstSlot.enabled, Is.True);
            Assert.That(secondSlot.sprite, Is.Null);
            Assert.That(secondSlot.enabled, Is.False);
        }

        [Test]
        public void ApplySignalArtwork_UsesCardIdInsteadOfCatalogOrder()
        {
            var view = Create<MatchView>("View");
            var firstSlot = Create<Image>("First slot");
            var secondSlot = Create<Image>("Second slot");
            var firstArtwork = CreateSprite(Color.cyan);
            var secondArtwork = CreateSprite(Color.magenta);
            var firstSignal = CreateAsset<SignalDefinition>("Signal 1");
            var secondSignal = CreateAsset<SignalDefinition>("Signal 2");
            firstSignal.Initialize("signal-1", "Signal 1", 1, firstArtwork);
            secondSignal.Initialize("signal-2", "Signal 2", 2, secondArtwork);

            view.ConfigureSignalArtworkSlots(new[] { firstSlot, secondSlot });
            view.ApplySignalArtwork(new[] { secondSignal, firstSignal });

            Assert.That(firstSlot.sprite, Is.EqualTo(firstArtwork));
            Assert.That(secondSlot.sprite, Is.EqualTo(secondArtwork));
        }

        [Test]
        public void ApplySignalArtwork_LeavesMissingCardIdSlotEmpty()
        {
            var view = Create<MatchView>("View");
            var firstSlot = Create<Image>("First slot");
            var eighthSlot = Create<Image>("Eighth slot");
            var tenthSlot = Create<Image>("Tenth slot");
            var eighthArtwork = CreateSprite(Color.yellow);
            var eighthSignal = CreateAsset<SignalDefinition>("Signal 8");
            var tenthSignal = CreateAsset<SignalDefinition>("Snap");
            eighthSignal.Initialize("signal-8", "Signal 8", 8, eighthArtwork);
            tenthSignal.Initialize("signal-10", "Snap", 10);

            view.ConfigureSignalArtworkSlots(new[] { firstSlot, eighthSlot, tenthSlot }, new[] { 1, 8, 10 });
            view.ApplySignalArtwork(new[] { tenthSignal, eighthSignal });

            Assert.That(firstSlot.sprite, Is.Null);
            Assert.That(firstSlot.enabled, Is.False);
            Assert.That(eighthSlot.sprite, Is.EqualTo(eighthArtwork));
            Assert.That(eighthSlot.enabled, Is.True);
            Assert.That(tenthSlot.sprite, Is.Null);
            Assert.That(tenthSlot.enabled, Is.False);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _created)
            {
                Object.DestroyImmediate(created);
            }
        }

        private T Create<T>(string name) where T : Component
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            _created.Add(gameObject);
            return gameObject.AddComponent<T>();
        }

        private T CreateAsset<T>(string name) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = name;
            _created.Add(asset);
            return asset;
        }

        private Sprite CreateSprite(Color color)
        {
            var texture = new Texture2D(2, 2);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            _created.Add(sprite);
            _created.Add(texture);
            return sprite;
        }
    }
}

