# Claude Code Game Studios -- Game Studio Agent Architecture

Indie game development managed through 49 coordinated Claude Code subagents.
Each agent owns a specific domain, enforcing separation of concerns and quality.

## Technology Stack

- **Engine**: Unity 6.6 (6000.6.0f1) — upgraded from 6.3 LTS 2026-10-01
- **Language**: C#
- **Version Control**: Git with trunk-based development
- **Build System**: Unity Build Pipeline
- **Asset Pipeline**: Unity Asset Import Pipeline + Addressables

> **Note**: This project uses Unity. Use the `unity-specialist` and related
> Unity sub-specialist agents (`unity-ui-specialist`, `unity-shader-specialist`,
> `unity-addressables-specialist`, `unity-dots-specialist`).

## Project Structure

@.claude/docs/directory-structure.md

> **This repo's code root is `unity/StormChaserLoop3D/Assets/`**, not `Assets/` at
> the repo root. The framework resolves Unity's code root to `Assets/` and has no
> override key, so read every `Assets/...` path in skills and agent docs as
> `unity/StormChaserLoop3D/Assets/...` (tests: `unity/StormChaserLoop3D/Assets/Tests/`).

## Engine Version Reference

@docs/engine-reference/unity/VERSION.md

## Technical Preferences

`project.yaml` at the repo root is the primary config store — engine, naming,
performance, modes (pinned: `rigor: standard`, `automation: collaborative`).
Skills resolve it via `resolve_config` (see `.claude/docs/config-resolution.md`).

`technical-preferences.md` stays imported here because it carries project rules
`project.yaml` has no slot for (forbidden patterns, ADR log, Steam Deck budgets).

@.claude/docs/technical-preferences.md

## Coordination Rules

@.claude/docs/coordination-rules.md

## Collaboration Protocol

**User-driven collaboration, not autonomous execution.**
Every task follows: **Question -> Options -> Decision -> Draft -> Approval**

- Agents MUST ask "May I write this to [filepath]?" before using Write/Edit tools
- Agents MUST show drafts or summaries before requesting approval
- Multi-file changes require explicit approval for the full changeset
- No commits without user instruction

See `docs/COLLABORATIVE-DESIGN-PRINCIPLE.md` for full protocol and examples.

> **First session?** If the project has no engine configured and no game concept,
> run `/start` to begin the guided onboarding flow.

## Coding Standards

@.claude/docs/coding-standards.md

## Context Management

Read `.claude/docs/context-management.md` on demand — it is a reference, not
session context. Two of its conventions are load-bearing and cited by name
elsewhere in the repo, so they are restated here rather than lost:

- **`production/session-state/active.md` is the session checkpoint.** The file is
  the memory, not the conversation. Read it first after any compaction, crash, or
  `/clear`.
- **Helpers in `.claude/scripts/` emit observations, never verdicts.** A script
  that scores or judges will eventually contradict a mode or override it cannot
  see. (Cited by `artifact-check.sh` and `adr-dep-graph.sh`.)
