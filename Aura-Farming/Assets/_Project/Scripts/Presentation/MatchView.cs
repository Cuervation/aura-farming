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
        }

        public void ApplySignalArtwork(IReadOnlyList<SignalDefinition> signals)
        {
            if (signals == null)
            {
                return;
            }

            var count = Math.Min(signalArtwork.Length, signals.Count);
            for (var index = 0; index < count; index++)
            {
                var slot = signalArtwork[index];
                if (slot == null)
                {
                    continue;
                }

                var artwork = signals[index]?.Artwork;
                slot.sprite = artwork;
                slot.preserveAspect = true;
                slot.enabled = artwork != null;
            }

            for (var index = count; index < signalArtwork.Length; index++)
            {
                var slot = signalArtwork[index];
                if (slot == null)
                {
                    continue;
                }

                slot.sprite = null;
                slot.preserveAspect = true;
                slot.enabled = false;
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
