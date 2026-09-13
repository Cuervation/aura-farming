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
        private GameObject _eventPanel;

        private static readonly Color[] SignalCardColors =
        {
            new(0.15f, 0.12f, 0.075f, 1f),
            new(0.18f, 0.14f, 0.08f, 1f),
            new(0.12f, 0.11f, 0.075f, 1f),
        };

        private static readonly Color EventColor = new(0.21f, 0.16f, 0.08f, 1f);
        private static readonly Color EventHighlightColor = new(0.96f, 0.67f, 0.12f, 1f);
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
        }

        public void ConfigureSignalArtworkSlots(Image[] slots)
        {
            signalArtwork = slots ?? Array.Empty<Image>();
            signalArtworkCardIds = Array.Empty<int>();
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

            AddOutline(button.gameObject, new Color(0.96f, 0.67f, 0.12f, .9f), new Vector2(1.5f, -1.5f));
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
            hudText.text = builder.ToString();
        }

        public void ShowPrivateSelection(PlayerId player)
        {
            ShowOnly(selectionPanel);
            statusText.text = $"Player {player.Value}: choose your Signal privately";
        }

        public void ShowPassScreen(PlayerId nextPlayer)
        {
            ShowOnly(passPanel);
            statusText.text = $"Hide the screen, then pass to Player {nextPlayer.Value}";
        }

        public void ShowReadyToReveal()
        {
            ShowOnly(revealPanel);
            statusText.text = "All Signals locked. Reveal when everyone is ready.";
        }

        public void ShowReveal(RoundOutcome outcome)
        {
            ShowOnly(resultPanel);
            winnerAnimationDirector?.PlayWinningAnimation(outcome);
            var pulse = outcome.PulseRecipients.Count == 0
                ? "No Pulse"
                : $"Pulse: {string.Join(", ", outcome.PulseRecipients.Select(id => $"P{id.Value}"))}";
            var jam = outcome.StaticRecipients.Count == 0
                ? "No Static"
                : $"Static: {string.Join(", ", outcome.StaticRecipients.Select(id => $"P{id.Value}"))}";
            statusText.text = $"{pulse}  |  {jam}";
        }

        public void ShowEnd(RoundOutcome outcome)
        {
            ShowOnly(endPanel);
            statusText.text = outcome.IsDraw
                ? "Signal collapse: draw"
                : $"Winner: {string.Join(", ", outcome.Winners.Select(id => $"Player {id.Value}"))}";
        }

        public void ShowError(CommandError error)
        {
            statusText.text = $"Cannot continue: {error}";
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



