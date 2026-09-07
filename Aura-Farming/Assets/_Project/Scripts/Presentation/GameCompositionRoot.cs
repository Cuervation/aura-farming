using System.Collections.Generic;
using System.Linq;
using AuraFarming.Application;
using AuraFarming.Domain;
using AuraFarming.Infrastructure;
using UnityEngine;

namespace AuraFarming.Presentation
{
    public sealed class GameCompositionRoot : MonoBehaviour
    {
        private const string PlayerCountKey = "AuraFarming.PlayerCount";

        [SerializeField] private MatchConfig matchConfig;
        [SerializeField] private MatchView matchView;
        [SerializeField, Range(2, 5)] private int fallbackPlayerCount = 2;

        private GamePresenter _presenter;

        public void Configure(MatchConfig config, MatchView view, int playerCount = 2)
        {
            matchConfig = config;
            matchView = view;
            fallbackPlayerCount = playerCount;
        }

        private void Start()
        {
            var validation = ContentValidator.Validate(matchConfig);
            if (!validation.IsValid)
            {
                Debug.LogError($"Invalid match content: {string.Join(", ", validation.Errors)}", this);
                enabled = false;
                return;
            }

            var count = Mathf.Clamp(PlayerPrefs.GetInt(PlayerCountKey, fallbackPlayerCount), 2, 5);
            var players = Enumerable.Range(1, count)
                .Select(index => new PlayerState(new PlayerId(index)))
                .ToArray();
            var signalValues = new Dictionary<SignalCardId, int>();
            for (var index = 0; index < matchConfig.Signals.Count; index++)
            {
                signalValues.Add(new SignalCardId(index + 1), matchConfig.Signals[index].Value);
            }

            var session = new GameSession(players, signalValues);
            _presenter = new GamePresenter(new SelectionFlowController(session), matchView);
            _presenter.Start();
        }

        public void ChooseSignal(int signalId) => _presenter?.ChooseSignal(new SignalCardId(signalId));
        public void ContinueAfterHandover() => _presenter?.ContinueAfterHandover();
        public void RevealRound() => _presenter?.RevealRound();
        public void StartNextRound() => _presenter?.StartNextRound();
    }
}
