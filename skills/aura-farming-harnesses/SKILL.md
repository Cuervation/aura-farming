---
name: aura-farming-harnesses
description: "Trigger: Aura Farming, Unity, UI, Play Mode, Inspector, tests, commit, push. Route every task through the token-efficient project harness."
license: Apache-2.0
metadata:
  author: gentleman-programming
  version: "1.1"
---

## Activation Contract
Use this skill for every Aura Farming task. Select one harness before taking tools or editing files.

This file is routing policy, not an automatic model switch. Read it once per task; reuse relevant rules unless the file changes.

## Dispatch and Context Budget
- Distinguish requested model/effort from effective model/effort. Verify the latter from runtime or dispatch metadata; otherwise report unknown, never claim a switch.
- Validate model and effort against the available tool schema before dispatch. Current subagent metadata supports LUNA low, not minimal. If unavailable, disclose the fallback; do not silently substitute an explicitly requested model.
- Keep atomic checks and Git closure inline when delegation would duplicate context. Delegate coherent independent work only when authorized; pass objective, relevant compact rules, exact file scope, acceptance checks and a short handoff, not full history.
- Load only relevant files and memory observations. Expand searches after evidence of a missing dependency; do not repeatedly load full logs or unchanged screenshots.

## Hard Rules
- Default to the smallest harness and lowest reasoning that can safely finish the task.
- Do not combine research, UI execution, implementation and Git closeout in an unbounded loop.
- Never end a Unity task until Play Mode is stopped and Edit Mode is visibly stable. Sky-induced window-state changes are allowed; do not require maximization or fixed resolution.
- Never build the player. Run only targeted verification justified by a code change.

## Unity Task Classes
- UNITY_MECHANICAL: prefer deterministic Editor tooling and MenuItem invocation; avoid coordinate geometry when tooling exists.
- UNITY_VISUAL: use Sky accessibility with fresh observations; coordinates are fallback only.
- TOOL_FAILURE is not MODEL_FAILURE and does not justify escalation. Stop only when accessibility, geometry, and deterministic alternatives are unavailable.

## Decision Gates
| Task | Harness | Model / effort |
| --- | --- | --- |
| Unity UI with inspection, screenshots or more than one action | Unity Runner | ASTRA / low |
| UI design, layout, prefabs, styling, visual QA | UI Implementer | ASTRA / low |
| Small deterministic check (one click, status, focused test) | Fast Path | LUNA / low |
| Routine deterministic C# implementation | Code Implementer | TERRA / low |
| Git status, commit, push, handoff | Git Closeout | LUNA / low |
| Cross-file architecture or unresolved failure after one focused attempt | Architect | ASTRA / low, then medium only with evidence |

## Execution Steps
1. **Unity Runner:** connect with @oai/sky before Unity interaction. The title-bar square/restore-maximize button shown in the reference image is ABSOLUTELY FORBIDDEN. Do not click the title bar, any system button, or borders, and do not change the main window state. Batch only known deterministic actions; capture after uncertain transitions and for final evidence, not on a fixed screenshot quota. Stop Play Mode and visibly verify stable Edit Mode before final response. If Unity was not used, report not touched; do not open it for documentation/Git-only work. If closure fails, report blocked closure instead of completion.
2. **UI Implementer:** inspect target files, make one coherent change set, refresh assets only if required, then conduct focused visual verification. Never build. Do not regenerate scenes unless the task requires it.
3. **Git Closeout:** inspect status and diff; preserve unrelated existing changes. Reuse valid verification evidence; commit/push only when requested, with conventional commits and no AI attribution. Never create an empty commit. Confirm results without repeating unchanged tests.
4. **Architect:** start ASTRA low. Escalate to medium only after an explicit failed focused attempt or an unresolved cross-file dependency.

## Verification and Retry Gate
- Define acceptance checks before editing. For code follow active strict TDD; prefer affected tests. Mechanically specified test implementation uses Code Implementer; visual testing uses Unity Runner. Documentation-only work needs structural checks, not Unity tests.
- Record test command/filter, result and tested file state. Reuse evidence only while relevant code, assets, dependencies and environment remain unchanged. Broaden tests when integration risk justifies it.
- After a failed attempt, inspect the error and change the hypothesis before retrying. After two failed attempts on the same issue, escalate once with evidence or report the precise blocker; no blind retry loops. Safety cleanup still applies.
- Keep a compact handoff in the existing session summary: harness, requested/effective model and effort, checks, retries, changed paths and blockers. Include tokens/time only when measured; use unavailable otherwise. Do not use account-wide usage as task usage or invent savings.

## Output Contract
Report result, verification and unresolved follow-up briefly. Disclose model mismatch or unavailable dispatch when relevant. No claim of measured optimization until comparable completed tasks have usage evidence.

## Cost-Efficient Orchestrator Policy
- Start every task with the cheapest reasonable route; escalate only on concrete evidence.
- Routing: file inspection/config/reporting -> LUNA low; simple deterministic C# -> TERRA low; cross-file/debugging/architecture -> TERRA medium; Unity mechanical/visual -> ASTRA low.
- Permit at most one automatic escalation per subtask: LUNA low -> TERRA low, TERRA low -> TERRA medium, ASTRA low -> ASTRA medium. High requires an explicit recorded reason.
- Isolate failures by domain: C#/tests escalate TERRA; Unity interpretation escalates ASTRA; context/search failure escalates LUNA -> TERRA low.
- Use targeted tests before a single final suite; never repeat the full suite after each change.
- On escalation, pass only objective, current state, exact error, relevant paths, completed changes and constraints.
- Do not claim that Markdown routing changed the effective runtime model. Record requested and effective values separately.

### Execution Context
For each dispatched subtask, record: task_id, task_type, assigned_model, assigned_effort, selection_reason, escalation_count, escalation_reason, result.
If the runtime does not expose model selection, preserve external values as assigned_* and report MODEL_SELECTION_EXTERNAL.

- UNITY_WINDOW_GUARDIAN: use tools/Maximize-UnityWindow.ps1 around Sky; target the real Unity process/title with Win32 ShowWindow/SetForegroundWindow, never Sky activation or coordinates.
