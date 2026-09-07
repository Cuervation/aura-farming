# Unity Integration Checklist

Perform these tasks in Unity 6000.5.10f1; do not edit scene YAML manually.

1. Import the PNGs in `Assets/_Project/Art/Cards/` using `Art/IMPORT_GUIDE.md`.
2. Assign `aura-signal-card-back-v1.png` to the shared hidden-card Image.
3. Assign `signal-01-first-sprout-v1.png` to Signal 01 through its `SignalDefinition.artwork` Inspector field.
4. Open `MainMenu.unity`: assign title, help and setup panels to `MainMenuView`; ensure player count offers 2–5.
5. Open `Game.unity`: assign Selection, Pass, Reveal, Result and End panels plus HUD/Status controls to `MatchView`.
6. Assign validated `MatchConfig` and `MatchView` to `GameCompositionRoot`.
7. Wire IDs 1–9 through `GameCompositionRoot.ChooseSignal(int)` and the pass/reveal/next buttons.
8. Add Bootstrap, MainMenu and Game to Build Settings in that order.
9. Manually play one 2-player full round, confirming that handover hides previous choices.
