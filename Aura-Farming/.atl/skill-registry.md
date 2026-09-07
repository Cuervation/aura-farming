# Aura-Farming Project Skill Registry

## Compact Rules
- Unity 6 project (6000.5.10f1), URP 17.5; keep gameplay logic engine-agnostic where practical.
- Follow SDD phases stored in Engram under `sdd/{change}/...`; do not create planning files unless requested.
- Strict TDD: write EditMode tests for pure domain rules first; use PlayMode tests only for scene/component integration.
- Never build during agent work; validate via focused tests when explicitly requested.
- Keep assets organized under `Assets/`; avoid committing generated `Library/`, `Temp/`, `Logs/`, and `UserSettings/`.
- Prefer composition over inheritance, ScriptableObjects for authored data, and deterministic rules for multiplayer/card resolution.

## User Skills
- Unity/game implementation: use Unity conventions and test-first workflow.
- SDD: interactive phases, Engram artifact store, no ask-on-risk pause (exception-ok).
- Visual assets: generate original art; avoid copying third-party game assets.
