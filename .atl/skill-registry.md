# Skill Registry

## Project

- Unity 6 (6000.5.10f1), C#, URP, Input System, Unity Test Framework.
- Architecture: Domain -> Application -> Infrastructure/Presentation via assembly definitions.

## Compact Rules

- Keep domain and application code free of UnityEngine dependencies.
- Add or update EditMode tests before implementation changes (Strict TDD).
- Keep scene wiring in Presentation/Editor; validate ScriptableObject content before play.
- Do not modify generated Unity folders: Library/, Temp/, Logs/, obj/.

## Relevant Skills

| Trigger | Standard |
| --- | --- |
| Unity/C# code or tests | Strict TDD; preserve assembly dependency direction. |
| Visual assets | Generate original art; do not reproduce third-party branding or illustrations. |
