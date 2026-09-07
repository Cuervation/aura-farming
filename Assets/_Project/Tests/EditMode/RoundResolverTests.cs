using System;
using System.Collections.Generic;
using System.Linq;
using AuraFarming.Domain;
using NUnit.Framework;

namespace AuraFarming.Tests.EditMode
{
    public sealed class RoundResolverTests
    {
        private readonly RoundResolver _resolver = new RoundResolver();

        [TestCase(2)]
        [TestCase(5)]
        public void Resolve_AcceptsSupportedPlayerCounts(int playerCount)
        {
            var players = CreatePlayers(playerCount);
            var selections = players.Select((player, index) => Select(player, index + 1)).ToArray();

            var outcome = _resolver.Resolve(players, selections);

            Assert.That(outcome.Players, Has.Count.EqualTo(playerCount));
            Assert.That(outcome.PulseRecipients, Has.Count.EqualTo(1));
            Assert.That(outcome.PulseRecipients[0], Is.EqualTo(players[playerCount - 1].Id));
        }

        [TestCase(1)]
        [TestCase(6)]
        public void Resolve_RejectsUnsupportedActivePlayerCounts(int playerCount)
        {
            var players = CreatePlayers(playerCount);
            var selections = players.Select((player, index) => Select(player, index + 1)).ToArray();

            Assert.That(() => _resolver.Resolve(players, selections), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Resolve_DuplicateSignalsGiveStaticToEveryCollidingPlayer()
        {
            var players = CreatePlayers(3);
            var outcome = _resolver.Resolve(players, new[]
            {
                Select(players[0], 8), Select(players[1], 8), Select(players[2], 4)
            });

            Assert.That(outcome.StaticRecipients, Is.EquivalentTo(new[] { players[0].Id, players[1].Id }));
            Assert.That(outcome.Player(players[0].Id).Static, Is.EqualTo(1));
            Assert.That(outcome.Player(players[1].Id).Static, Is.EqualTo(1));
            Assert.That(outcome.Player(players[2].Id).Static, Is.EqualTo(0));
        }

        [Test]
        public void Resolve_HighestUniqueSignalGivesExactlyOnePulse()
        {
            var players = CreatePlayers(3);
            var outcome = _resolver.Resolve(players, new[]
            {
                Select(players[0], 2), Select(players[1], 7), Select(players[2], 5)
            });

            Assert.That(outcome.PulseRecipients, Is.EqualTo(new[] { players[1].Id }));
            Assert.That(outcome.Player(players[0].Id).Pulse, Is.EqualTo(0));
            Assert.That(outcome.Player(players[1].Id).Pulse, Is.EqualTo(1));
            Assert.That(outcome.Player(players[2].Id).Pulse, Is.EqualTo(0));
        }

        [Test]
        public void Resolve_HighestCollisionLetsLowerUniqueSignalWinPulse()
        {
            var players = CreatePlayers(3);
            var outcome = _resolver.Resolve(players, new[]
            {
                Select(players[0], 8), Select(players[1], 8), Select(players[2], 4)
            });

            Assert.That(outcome.PulseRecipients, Is.EqualTo(new[] { players[2].Id }));
            Assert.That(outcome.Player(players[2].Id).Pulse, Is.EqualTo(1));
        }

        [Test]
        public void Resolve_WhenEverySignalIsDuplicated_GivesNoPulse()
        {
            var players = CreatePlayers(3);
            var outcome = _resolver.Resolve(players, new[]
            {
                Select(players[0], 6), Select(players[1], 6), Select(players[2], 6)
            });

            Assert.That(outcome.PulseRecipients, Is.Empty);
            Assert.That(outcome.StaticRecipients, Is.EquivalentTo(players.Select(player => player.Id)));
            Assert.That(outcome.Players.All(player => player.Pulse == 0), Is.True);
            Assert.That(outcome.Players.All(player => player.Static == 1), Is.True);
        }

        [Test]
        public void Resolve_PlayerReachingThreeStaticIsEliminated()
        {
            var players = new[]
            {
                new PlayerState(new PlayerId(1), @static: 2),
                new PlayerState(new PlayerId(2))
            };

            var outcome = _resolver.Resolve(players, new[] { Select(players[0], 6), Select(players[1], 6) });

            Assert.That(outcome.Player(players[0].Id).IsEliminated, Is.True);
            Assert.That(outcome.Player(players[1].Id).IsEliminated, Is.False);
        }

        [Test]
        public void Resolve_SurvivorReachingFivePulseWins()
        {
            var players = new[]
            {
                new PlayerState(new PlayerId(1), pulse: 4),
                new PlayerState(new PlayerId(2)),
                new PlayerState(new PlayerId(3))
            };

            var outcome = _resolver.Resolve(players, new[]
            {
                Select(players[0], 9), Select(players[1], 5), Select(players[2], 5)
            });

            Assert.That(outcome.IsMatchEnded, Is.True);
            Assert.That(outcome.Winners, Is.EqualTo(new[] { players[0].Id }));
            Assert.That(outcome.Player(players[0].Id).Pulse, Is.EqualTo(5));
        }

        [Test]
        public void Resolve_EliminationPrecedesPulseThresholdEvaluation()
        {
            var players = new[]
            {
                new PlayerState(new PlayerId(1), pulse: 5, @static: 2),
                new PlayerState(new PlayerId(2)),
                new PlayerState(new PlayerId(3))
            };

            var outcome = _resolver.Resolve(players, new[]
            {
                Select(players[0], 8), Select(players[1], 8), Select(players[2], 4)
            });

            Assert.That(outcome.Player(players[0].Id).IsEliminated, Is.True);
            Assert.That(outcome.Winners, Has.None.EqualTo(players[0].Id));
            Assert.That(outcome.IsMatchEnded, Is.False);
        }

        [Test]
        public void Resolve_LastActivePlayerWinsBelowPulseThreshold()
        {
            var players = new[]
            {
                new PlayerState(new PlayerId(1), @static: 2),
                new PlayerState(new PlayerId(2))
            };

            var outcome = _resolver.Resolve(players, new[] { Select(players[0], 3), Select(players[1], 3) });

            Assert.That(outcome.IsMatchEnded, Is.True);
            Assert.That(outcome.Winners, Is.EqualTo(new[] { players[1].Id }));
            Assert.That(outcome.Player(players[1].Id).Pulse, Is.EqualTo(0));
        }

        [Test]
        public void Resolve_IgnoresPreviouslyEliminatedPlayersAndTheirSelections()
        {
            var players = new[]
            {
                new PlayerState(new PlayerId(1), isEliminated: true),
                new PlayerState(new PlayerId(2)),
                new PlayerState(new PlayerId(3))
            };

            var outcome = _resolver.Resolve(players, new[] { Select(players[1], 2), Select(players[2], 7) });

            Assert.That(outcome.Player(players[0].Id).IsEliminated, Is.True);
            Assert.That(outcome.PulseRecipients, Is.EqualTo(new[] { players[2].Id }));
        }

        [Test]
        public void Resolve_WhenEveryActivePlayerIsEliminated_EndsInDraw()
        {
            var players = new[]
            {
                new PlayerState(new PlayerId(1), @static: 2),
                new PlayerState(new PlayerId(2), @static: 2)
            };

            var outcome = _resolver.Resolve(players, new[] { Select(players[0], 4), Select(players[1], 4) });

            Assert.That(outcome.IsMatchEnded, Is.True);
            Assert.That(outcome.IsDraw, Is.True);
            Assert.That(outcome.Winners, Is.Empty);
        }

        [Test]
        public void Resolve_MultipleSurvivorsAtPulseThresholdAreCoWinners()
        {
            var players = new[]
            {
                new PlayerState(new PlayerId(1), pulse: 5),
                new PlayerState(new PlayerId(2), pulse: 5),
                new PlayerState(new PlayerId(3))
            };

            var outcome = _resolver.Resolve(players, new[]
            {
                Select(players[0], 2), Select(players[1], 4), Select(players[2], 6)
            });

            Assert.That(outcome.IsMatchEnded, Is.True);
            Assert.That(outcome.IsDraw, Is.False);
            Assert.That(outcome.Winners, Is.EquivalentTo(new[] { players[0].Id, players[1].Id }));
        }
        private static IReadOnlyList<PlayerState> CreatePlayers(int count)
        {
            return Enumerable.Range(1, count).Select(value => new PlayerState(new PlayerId(value))).ToArray();
        }

        private static Selection Select(PlayerState player, int signalValue)
        {
            return new Selection(player.Id, new SignalCardId(signalValue), signalValue);
        }
    }
}

