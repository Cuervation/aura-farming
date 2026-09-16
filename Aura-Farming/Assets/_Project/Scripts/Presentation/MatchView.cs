using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuraFarming.Application;
using AuraFarming.Domain;
using AuraFarming.Infrastructure;
using UnityEngine;
using UnityEngine.UI;

namespace AuraFarming.Presentation
{
    public sealed class MatchView : MonoBehaviour, IMatchView
    {
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private GameObject passPanel;
        [SerializeField] private GameObject revealPanel;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private GameObject endPanel;
        [SerializeField] private Text hudText;
        [SerializeField] private Text statusText;
        [SerializeField] private Image[] signalArtwork = Array.Empty<Image>();
        [SerializeField] private int[] signalArtworkCardIds = Array.Empty<int>();
        [SerializeField] private WinnerAnimationDirector winnerAnimationDirector;
        private Action<int> _chooseEvent;
        private Action<int> _chooseSignal;
        private Action _confirmSelection;
        private GameObject _eventPanel;
        private GameSnapshot _lastSnapshot;

        private static readonly Color[] SignalCardColors =
        {
            new(0.09f, 0.015f, 0.07f, 1f),
            new(0.12f, 0.02f, 0.09f, 1f),
            new(0.07f, 0.02f, 0.08f, 1f),
        };

        private static readonly Color EventColor = new(0.12f, 0.02f, 0.09f, 1f);
        private static readonly Color EventHighlightColor = new(1f, 0.12f, 0.58f, 1f);
        private static readonly Color ShadowColor = new(0.015f, 0.012f, 0.008f, 0.92f);

        public void Configure(
            GameObject selection,
            GameObject pass,
            GameObject reveal,
            GameObject result,
            GameObject end,
            Text hud,
            Text status)
        {
            selectionPanel = selection;
            passPanel = pass;
            revealPanel = reveal;
            resultPanel = result;
            endPanel = end;
            hudText = hud;
            statusText = status;
            if (hudText != null) hudText.color = new Color(0.95f, 0.9f, 1f);
            if (statusText != null) statusText.color = new Color(0.95f, 0.9f, 1f);
        }

        public void ConfigureSignalArtworkSlots(Image[] slots)
        {
            signalArtwork = slots ?? Array.Empty<Image>();
            signalArtworkCardIds = Array.Empty<int>();
        }

        public void ConfigureConfirmSelection(Action confirmSelection)
        {
            _confirmSelection = confirmSelection;
        }

        public void ConfigureSignalSelection(Action<int> chooseSignal)
        {
            _chooseSignal = chooseSignal;
            EnsureTenthSignalCard();
        }

        public void ConfigureSignalArtworkSlots(Image[] slots, int[] cardIds)
        {
            signalArtwork = slots ?? Array.Empty<Image>();
            signalArtworkCardIds = cardIds ?? Array.Empty<int>();
        }

        public void ConfigureEvents(IReadOnlyList<EventDefinition> events, Action<int> chooseEvent)
        {
            _chooseEvent = chooseEvent;
            if (selectionPanel == null || events == null || events.Count == 0) return;
            StyleSignalCards();
            _eventPanel = new GameObject("Event Selection", typeof(RectTransform), typeof(Image));
            _eventPanel.transform.SetParent(selectionPanel.transform, false);
            var root = _eventPanel.GetComponent<RectTransform>();
            var columns = Mathf.Min(3, events.Count);
            var rows = Mathf.CeilToInt(events.Count / (float)columns);
            root.anchorMin = new Vector2(0f, 0f); root.anchorMax = new Vector2(1f, 0f);
            root.pivot = new Vector2(.5f, 0f); root.anchoredPosition = new Vector2(0f, 14f);
            root.sizeDelta = new Vector2(0f, rows * 38f + (rows - 1) * 6f + 16f);
            var panelBackground = _eventPanel.GetComponent<Image>();
            panelBackground.color = new Color(0.05f, 0.04f, 0.025f, .92f);
            panelBackground.raycastTarget = false;
            for (var index = 0; index < events.Count; index++)
            {
                var buttonObject = new GameObject($"Event {index + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(root, false);
                StyleButton(buttonObject.GetComponent<Button>(), EventColor, EventHighlightColor, 1.06f);
                var buttonRect = buttonObject.GetComponent<RectTransform>();
                var column = index % columns;
                var row = index / columns;
                buttonRect.anchorMin = new Vector2(column / (float)columns, 1f - (row + 1f) / rows);
                buttonRect.anchorMax = new Vector2((column + 1f) / columns, 1f - row / (float)rows);
                buttonRect.offsetMin = new Vector2(4f, 3f); buttonRect.offsetMax = new Vector2(-4f, -3f);
                var label = new GameObject("Label", typeof(RectTransform), typeof(Text));
                label.transform.SetParent(buttonObject.transform, false);
                var text = label.GetComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.text = events[index].DisplayName;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = new Color(1f, .93f, .74f);
                text.fontStyle = FontStyle.Bold;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 12;
                text.resizeTextMaxSize = 20;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
                AddOutline(label, new Color(0.04f, 0.03f, 0.015f, 0.95f), new Vector2(1f, -1f));
                var labelRect = label.GetComponent<RectTransform>(); labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.offsetMin = Vector2.zero; labelRect.offsetMax = Vector2.zero;
                var captured = index; buttonObject.GetComponent<Button>().onClick.AddListener(() => _chooseEvent?.Invoke(captured));
            }
        }

        private void StyleSignalCards()
        {
            var signalButtons = selectionPanel.GetComponentsInChildren<Button>(true)
                .Where(button => button.name.StartsWith("Signal", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            for (var index = 0; index < signalButtons.Length; index++)
            {
                var baseColor = SignalCardColors[index % SignalCardColors.Length];
                StyleButton(signalButtons[index], baseColor, EventHighlightColor, 1.04f);
                var rect = signalButtons[index].GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(170f, 170f);

                foreach (var label in signalButtons[index].GetComponentsInChildren<Text>(true))
                {
                    label.color = new Color(1f, .92f, .70f);
                    label.fontStyle = FontStyle.Bold;
                    AddOutline(label.gameObject, new Color(0.04f, 0.03f, 0.015f, 0.95f), new Vector2(1.5f, -1.5f));
                }
            }
        }

        private void EnsureTenthSignalCard()
        {
            if (selectionPanel == null || _chooseSignal == null ||
                selectionPanel.GetComponentsInChildren<Button>(true).Any(button => button.name == "Signal 10")) return;

            var cardObject = new GameObject("Signal 10", typeof(RectTransform), typeof(Image), typeof(Button));
            cardObject.transform.SetParent(selectionPanel.transform, false);
            var rect = cardObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.pivot = new Vector2(.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 18f);
            rect.sizeDelta = new Vector2(170f, 170f);
            var button = cardObject.GetComponent<Button>();
            button.onClick.AddListener(() => _chooseSignal(10));
            StyleButton(button, SignalCardColors[0], EventHighlightColor, 1.04f);

            var artworkObject = new GameObject("Artwork", typeof(RectTransform), typeof(Image));
            artworkObject.transform.SetParent(cardObject.transform, false);
            var artworkRect = artworkObject.GetComponent<RectTransform>();
            artworkRect.anchorMin = new Vector2(0f, .28f);
            artworkRect.anchorMax = new Vector2(1f, 1f);
            artworkRect.offsetMin = new Vector2(9f, 7f);
            artworkRect.offsetMax = new Vector2(-9f, -7f);
            var artwork = artworkObject.GetComponent<Image>();
            artwork.preserveAspect = true;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(cardObject.transform, false);
            var label = labelObject.GetComponent<Text>();
            label.text = "SIGNAL 10";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 18;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = new Color(1f, .92f, .70f);
            var labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(.5f, 0f);
            labelRect.anchoredPosition = new Vector2(14f, 8f);
            labelRect.sizeDelta = new Vector2(-28f, 30f);

            signalArtwork = signalArtwork.Concat(new[] { artwork }).ToArray();
            signalArtworkCardIds = signalArtworkCardIds.Length == 0
                ? Enumerable.Range(1, signalArtwork.Length).ToArray()
                : signalArtworkCardIds.Concat(new[] { 10 }).ToArray();
        }

        private static void StyleButton(Button button, Color normal, Color highlighted, float pressedScale)
        {
            var image = button.GetComponent<Image>();
            image.color = normal;

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.Lerp(highlighted, Color.white, .08f);
            colors.pressedColor = new Color(pressedScale, pressedScale, pressedScale, 1f);
            colors.selectedColor = highlighted;
            colors.disabledColor = new Color(.35f, .35f, .35f, .65f);
            colors.fadeDuration = .12f;
            button.colors = colors;

            AddOutline(button.gameObject, new Color(1f, 0.12f, 0.58f, .9f), new Vector2(1.5f, -1.5f));
            var shadow = button.GetComponent<Shadow>() ?? button.gameObject.AddComponent<Shadow>();
            shadow.effectColor = ShadowColor;
            shadow.effectDistance = new Vector2(3f, -4f);
        }

        private static void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            var outline = target.GetComponent<Outline>() ?? target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        public void ApplySignalArtwork(IReadOnlyList<SignalDefinition> signals)
        {
            if (signals == null)
            {
                return;
            }

            for (var index = 0; index < signalArtwork.Length; index++)
            {
                var slot = signalArtwork[index];
                if (slot == null)
                {
                    continue;
                }

                slot.sprite = null;
                slot.enabled = false;
                slot.preserveAspect = false;

                SignalDefinition signal = null;
                var cardId = index < signalArtworkCardIds.Length && signalArtworkCardIds[index] > 0
                    ? signalArtworkCardIds[index]
                    : index + 1;
                for (var signalIndex = 0; signalIndex < signals.Count; signalIndex++)
                {
                    if (signals[signalIndex] != null && signals[signalIndex].CardIdValue == cardId)
                    {
                        signal = signals[signalIndex];
                        break;
                    }
                }

                var artwork = signal?.Artwork;
                slot.sprite = artwork;
                // Each artwork is now presented inside a card frame. Preserve its original
                // proportions and crop-to-cover the art well; stretching is what made the old
                // UI read as nine banner ads, while letterboxing reduced portrait art to a line.
                slot.preserveAspect = artwork != null;
                slot.type = Image.Type.Simple;
                var fitter = slot.GetComponent<AspectRatioFitter>() ?? slot.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                slot.color = Color.white;
                slot.transform.SetAsFirstSibling();
                slot.enabled = artwork != null;
            }

        }

        public void Render(GameSnapshot snapshot)
        {
            var builder = new StringBuilder($"Round {snapshot.RoundNumber}  |  ");
            foreach (var player in snapshot.Players)
            {
                builder.Append($"P{player.Id.Value}: {player.Pulse} Pulse / {player.Static} Static");
                if (player.IsEliminated)
                {
                    builder.Append(" [ELIMINATED]");
                }
                builder.Append("   ");
            }
            _lastSnapshot = snapshot;
            hudText.text = builder.ToString();
        }

        public void ShowPrivateSelection(PlayerId player)
        {
            ShowOnly(selectionPanel);
            EnsureConfirmSelectionButton();
            ApplyPanelStyle(selectionPanel);
            statusText.text = $"Player {player.Value}: choose your Signal privately";
        }

        private void EnsureConfirmSelectionButton()
        {
            if (selectionPanel == null || _confirmSelection == null) return;
            if (selectionPanel.GetComponentsInChildren<Button>(true)
                .Any(button => button.name == "LOCK IN")) return;

            var buttonObject = new GameObject("LOCK IN", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(selectionPanel.transform, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-28f, 28f);
            rect.sizeDelta = new Vector2(230f, 64f);
            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.82f, 0.04f, 0.48f, 1f);
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => _confirmSelection());
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);
            var label = labelObject.GetComponent<Text>();
            label.text = "LOCK IN";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 24;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        }

        public void ShowPassScreen(PlayerId nextPlayer)
        {
            ShowOnly(passPanel);
            ApplyPanelStyle(passPanel);
            statusText.text = $"Hide the screen, then pass to Player {nextPlayer.Value}";
            if (passPanel != null)
            {
                foreach (var text in passPanel.GetComponentsInChildren<Text>(true)) text.color = Color.white;
                foreach (var button in passPanel.GetComponentsInChildren<Button>(true))
                {
                    var image = button.GetComponent<Image>(); if (image != null) image.color = new Color(0.78f, 0.03f, 0.42f, 1f);
                }
            }
        }

        public void ShowReadyToReveal()
        {
            ShowOnly(revealPanel);
            ApplyPanelStyle(revealPanel);
            statusText.text = "All Signals locked. Reveal when everyone is ready.";
        }

        public void ShowReveal(RoundOutcome outcome)
        {
            ShowOnly(resultPanel);
            ApplyPanelStyle(resultPanel);
            winnerAnimationDirector?.PlayWinningAnimation(outcome);
            var pulse = outcome.PulseRecipients.Count == 0
                ? "No Pulse"
                : $"Pulse: {string.Join(", ", outcome.PulseRecipients.Select(id => $"P{id.Value}"))}";
            var jam = outcome.StaticRecipients.Count == 0
                ? "No Static"
                : $"Static: {string.Join(", ", outcome.StaticRecipients.Select(id => $"P{id.Value} (+{outcome.StaticReward})"))}";
            if (outcome.PulseRecipients.Count > 0)
            {
                pulse = $"Pulse: {string.Join(", ", outcome.PulseRecipients.Select(id => $"P{id.Value} (+{outcome.PulseReward})"))}";
            }
            var selections = outcome.Selections.Count == 0
                ? "No public selections"
                : string.Join("  |  ", outcome.Selections.OrderBy(selection => selection.PlayerId.Value)
                    .Select(selection => $"P{selection.PlayerId.Value}: SIGNAL {selection.CardId.Value:00} ({selection.SignalValue})"));
            var eliminated = outcome.Players.Where(player => player.IsEliminated)
                .Select(player => $"P{player.Id.Value}").ToArray();
            var elimination = eliminated.Length == 0 ? string.Empty : $"\nEliminated: {string.Join(", ", eliminated)}";
            // Winners are match-level winners (used by the end screen), not a
            // winner for this individual round. Reveal only reports the
            // scoring recipients so an ordinary round cannot claim the match
            // has been won.
            statusText.text = $"ROUND RESULT\n{selections}\n{pulse}  |  {jam}{elimination}";
        }

        public void ShowEnd(RoundOutcome outcome)
        {
            ShowOnly(endPanel);
            ApplyPanelStyle(endPanel);
            var winner = outcome.IsDraw ? "DRAW" : $"WINNER: {string.Join(", ", outcome.Winners.Select(id => $"PLAYER {id.Value}"))}";
            var scores = _lastSnapshot == null ? string.Empty : string.Join("   ", _lastSnapshot.Players.Select(player => $"P{player.Id.Value} AURA: {player.Pulse - player.Static}"));
            var summary = $"{winner}\n{scores}";
            var subtitle = endPanel == null ? null : endPanel.GetComponentsInChildren<Text>(true).FirstOrDefault(text => text.text == "THE TABLE HAS SPOKEN");
            if (subtitle != null) subtitle.text = summary;
            else statusText.text = summary;
        }

        public void ShowError(CommandError error)
        {
            statusText.text = $"Cannot continue: {error}";
        }

        private static void ApplyPanelStyle(GameObject panel)
        {
            if (panel == null) return;
            var image = panel.GetComponent<Image>();
            if (image != null) image.color = new Color(0.02f, 0.015f, 0.035f, 0.96f);
            foreach (var text in panel.GetComponentsInChildren<Text>(true))
            {
                text.color = new Color(0.95f, 0.93f, 1f);
                text.fontStyle = FontStyle.Normal;
            }
            foreach (var button in panel.GetComponentsInChildren<Button>(true))
            {
                var buttonImage = button.GetComponent<Image>();
                if (buttonImage != null) buttonImage.color = new Color(0.82f, 0.04f, 0.48f, 1f);
                var colors = button.colors;
                colors.highlightedColor = new Color(1f, 0.16f, 0.62f, 1f);
                colors.pressedColor = new Color(0.55f, 0.02f, 0.28f, 1f);
                button.colors = colors;
            }
        }

        private void ShowOnly(GameObject visible)
        {
            selectionPanel.SetActive(visible == selectionPanel);
            passPanel.SetActive(visible == passPanel);
            revealPanel.SetActive(visible == revealPanel);
            resultPanel.SetActive(visible == resultPanel);
            endPanel.SetActive(visible == endPanel);
        }
    }
}



