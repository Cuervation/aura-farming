# Agent Instructions

## Unity window

- Whenever Unity must be opened, inspected, or controlled, connect through @oai/sky first.
- Sky may automatically change Unity window size, resolution, focus, or maximized state; these tool-induced changes are allowed and valid.
- After connecting through Sky, do NOT maximize, restore, minimize, move, resize, or call activate_window as an obligatory recovery step.
- Work with the viewport and window state exposed by Sky; do not require a specific resolution or maximized state.
- Prefer a fresh accessibility tree and fresh element_index for every menu action. Never reuse indices after UI changes.
- Use coordinates only when Sky confirms valid input geometry. If geometry is unavailable, use accessibility or deterministic Editor tooling.
- A Sky-induced window-state change alone must never stop the task.

## Mandatory Harness Routing

- Always use skills/aura-farming-harnesses/SKILL.md for any Aura Farming task before acting.
- Follow the skill's routing table as the single source of truth. Validate supported model/effort and distinguish requested routing from verified effective execution; the Markdown rules do not switch models automatically.
- Apply its Unity closure rule before every final response.

- For Sky window recovery, run tools/Maximize-UnityWindow.ps1 before connecting Sky and after Sky if needed; use the Windows guardian, not Sky activation or coordinates, and refresh accessibility afterward.
