# Unity scene assembly (required in Editor)

Scene YAML is intentionally not handwritten. Open this project in Unity 6000.5.10f1 and create the following scenes under this folder.

## 1. Bootstrap.unity
1. Create an empty scene.
2. Add an empty GameObject named `Bootstrap`.
3. Add `BootstrapLoader`.
4. Save as `Bootstrap.unity`.

## 2. MainMenu.unity
1. Create a Canvas and EventSystem using **GameObject > UI** so Unity configures the active Input System correctly.
2. Add full-screen `Title`, `Help`, and `Setup` panels.
3. Add `MainMenuView` to the Canvas and assign those panels plus a Dropdown whose option indices 0–3 represent 2–5 players.
4. Wire buttons to `ShowHelp`, `ShowSetup`, `ShowTitle`, and `StartGame`.
5. Save as `MainMenu.unity`.

## 3. Game.unity
1. Create a Canvas and EventSystem using **GameObject > UI**.
2. Add panels named `Selection`, `Pass`, `Reveal`, `Result`, and `End`, plus HUD and Status Text controls.
3. Add `MatchView` and assign all panels/text through the Inspector.
4. Add `GameCompositionRoot`; assign the validated MatchConfig and MatchView.
5. Create nine placeholder Signal buttons. Wire each button to `GameCompositionRoot.ChooseSignal(int)` using IDs 1–9.
6. Wire Pass/Reveal/Next buttons to `ContinueAfterHandover`, `RevealRound`, and `StartNextRound`.
7. Save as `Game.unity`.

## Content assets
Create nine SignalDefinition assets, six EventDefinition assets, and one MatchConfig through **Assets > Create > Aura Farming**. Signal IDs and values must be unique; Event IDs, descriptions and effect keys must be non-empty. Assign all assets to MatchConfig.

## Build Settings
Add scenes in this order: Bootstrap (0), MainMenu (1), Game (2). Enter Play Mode from Bootstrap and run one complete 2-player round.
