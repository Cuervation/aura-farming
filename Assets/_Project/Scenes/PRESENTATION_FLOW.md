# Aura Farming — Presentation Flow

## Main Menu

1. **Title** — original logo/title treatment, `Jugar`, `Cómo jugar`, `Salir`.
2. **How to play** — choose one signal in secret; repeated values gain Static; the highest unique value gains Pulse; three Static eliminate a player; five Pulse wins.
3. **Setup** — player count selector from 2 to 5 and `Comenzar partida`.

## In-match flow

1. **Private Selection** — player name, nine signal cards, selected-card confirmation.
2. **Pass Device** — hide the selected card; display the next player and one continue action.
3. **Ready to Reveal** — confirm that every active player selected.
4. **Reveal / Result** — animate signals simultaneously, show Pulse/Static changes, eliminated players and next-round action.
5. **End** — winner(s) or draw, `Nueva partida` and `Volver al menú`.

## Visual Direction

- Palette: deep teal, forest green, warm gold, restrained violet.
- Use the original card-back asset at `Assets/_Project/Art/Cards/aura-signal-card-back-v1.png` as the visual anchor.
- Never display a selected signal during handover or before reveal.
- All scene objects and Inspector references follow `SCENE_SETUP.md`.
