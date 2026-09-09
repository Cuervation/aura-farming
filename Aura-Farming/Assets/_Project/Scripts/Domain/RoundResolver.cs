using System;
using System.Collections.Generic;
using System.Linq;

namespace AuraFarming.Domain
{
    public sealed class RoundResolver
    {
        private const int MinimumActivePlayers = 2;
        private const int MaximumActivePlayers = 5;
        private const int StaticEliminationThreshold = 3;
        private const int PulseVictoryThreshold = 5;

        public RoundOutcome Resolve(
            IReadOnlyList<PlayerState> players,
            IReadOnlyList<Selection> selections)
        {
            return Resolve(players, selections, null);
        }

        public RoundOutcome Resolve(
            IReadOnlyList<PlayerState> players,
            IReadOnlyList<Selection> selections,
            RoundEvent roundEvent)
        {
            var activePlayers = Validate(players, selections);
            var signalGroups = selections.GroupBy(selection => selection.SignalValue).ToArray();
            var staticRecipients = signalGroups
                .Where(group => group.Count() > 1)
                .SelectMany(group => group.Select(selection => selection.PlayerId))
                .ToArray();
            var pulseRecipients = signalGroups
                .Where(group => group.Count() == 1)
                .OrderByDescending(group => group.Key)
                .Take(1)
                .Select(group => group.Single().PlayerId)
                .ToArray();

            var staticSet = new HashSet<PlayerId>(staticRecipients);
            var pulseSet = new HashSet<PlayerId>(pulseRecipients);
            var pulseReward = roundEvent != null && roundEvent.Effect == RoundEffect.PulseSurge ? 2 : 1;
            var staticReward = roundEvent != null && roundEvent.Effect == RoundEffect.StaticStorm ? 2 : 1;
            if (roundEvent != null && roundEvent.Effect == RoundEffect.Sanctuary)
            {
                staticRecipients = Array.Empty<PlayerId>();
                staticSet.Clear();
            }
            var activeSet = new HashSet<PlayerId>(activePlayers.Select(player => player.Id));
            var updatedPlayers = players.Select(player =>
            {
                if (!activeSet.Contains(player.Id))
                {
                    return player;
                }

                var scored = player.AddScore(
                    pulseSet.Contains(player.Id) ? pulseReward : 0,
                    staticSet.Contains(player.Id) ? staticReward : 0);
                return scored.WithElimination(scored.Static >= StaticEliminationThreshold);
            }).ToArray();

            var survivors = updatedPlayers.Where(player => !player.IsEliminated).ToArray();
            var isDraw = survivors.Length == 0;
            PlayerId[] winners;
            if (survivors.Length == 1)
            {
                winners = new[] { survivors[0].Id };
            }
            else
            {
                winners = survivors
                    .Where(player => player.Pulse >= PulseVictoryThreshold)
                    .Select(player => player.Id)
                    .ToArray();
            }

            return new RoundOutcome(updatedPlayers, pulseRecipients, staticRecipients, winners, isDraw, roundEvent);
        }

        private static IReadOnlyList<PlayerState> Validate(
            IReadOnlyList<PlayerState> players,
            IReadOnlyList<Selection> selections)
        {
            if (players == null)
            {
                throw new ArgumentNullException(nameof(players));
            }

            if (selections == null)
            {
                throw new ArgumentNullException(nameof(selections));
            }

            var playerIds = new HashSet<PlayerId>();
            for (var index = 0; index < players.Count; index++)
            {
                if (players[index] == null || !playerIds.Add(players[index].Id))
                {
                    throw new ArgumentException("Players must be non-null and have unique identifiers.", nameof(players));
                }
            }

            var activePlayers = players.Where(player => !player.IsEliminated).ToArray();
            if (activePlayers.Length < MinimumActivePlayers || activePlayers.Length > MaximumActivePlayers)
            {
                throw new ArgumentException("A round requires between two and five active players.", nameof(players));
            }

            if (selections.Count != activePlayers.Length)
            {
                throw new ArgumentException("Every active player must submit exactly one selection.", nameof(selections));
            }

            var activeIds = new HashSet<PlayerId>(activePlayers.Select(player => player.Id));
            var selectedPlayers = new HashSet<PlayerId>();
            for (var index = 0; index < selections.Count; index++)
            {
                var selection = selections[index];
                if (selection == null || !activeIds.Contains(selection.PlayerId) || !selectedPlayers.Add(selection.PlayerId))
                {
                    throw new ArgumentException("Selections must contain each active player exactly once.", nameof(selections));
                }
            }

            return activePlayers;
        }
    }
}
