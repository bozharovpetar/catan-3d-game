# Hexhaven — agent instructions (read this first)

You are building **Hexhaven**, a complete, polished 3D hex-island trading/building strategy game in Unity 6 (URP), from the specification in `docs/`. The spec is deliberately exhaustive: **do not invent rules, simplify mechanics, or skip features**. If something is genuinely unspecified, choose the simplest option consistent with the rest of the spec, write it down in `docs/DECISIONS.md` (create it), and continue.

**Starting on a new machine or resuming the build?** `NEXT-STEPS.md` is the step-by-step runbook: machine setup, then every milestone with commands and expected results. `docs/PROGRESS.md` (once it exists) records how far the build has got.

## 1. Read in this order (all of it, before writing code)
1. `docs/00-OVERVIEW.md` — scope, glossary (use these exact terms), tech targets.
2. `docs/01-RULES.md` — **the gameplay source of truth**.
3. `docs/02-ARCHITECTURE.md` — project creation, assemblies, layering, RNG, runtime flow, what the scaffold provides.
4. `docs/03-CORE-API.md` — normative API (names/signatures must match; the harness and tests depend on them).
5. `docs/04-AI.md`, `docs/05-VISUALS.md`, `docs/06-UI.md`, `docs/07-AUDIO.md`, `docs/08-PERSISTENCE.md`.
6. `docs/09-TESTING.md` — the test catalogue and verification layers.
7. `docs/10-BUILD-ORDER.md` — milestones, gates, Definition of Done. **Follow it in order.**
8. `docs/11-CHARTERS.md` — step-by-step build guide for the Charter (development card) system; implement it in M2 alongside the engine.
9. `docs/12-RULES-AUDIT.md` — the completeness checklist against the classic genre rules (classic terms may appear only in the cross-reference tables of docs 11 and 12, never under `game/`).
10. `docs/13-STRINGS.md` — every user-facing string, verbatim.

## 2. Non-negotiable rules
1. **Originality.** Never use the name, terminology, card names, art or trade dress of any existing commercial board game. Use only the glossary terms (Outpost, Keep, Path, Storm, Charter, Warden, Monument, Road Crew, Bounty, Decree, Longest Path, Strongest Guard, Timber/Clay/Fiber/Grain/Iron …). Gate: `grep -rniwE "catan|settlers?|robber|knights?|lumber|wool|ore|brick|sheep|monopoly|year of plenty|road building|largest army|longest road|development cards?" game/Assets/Hexhaven` must print nothing.
2. **Never hand-write Unity YAML** (`.unity`, `.prefab`, `.asset`, `.mat`). Scenes/assets are created by Editor scripts (`Hexhaven ▸ Setup Project`). Text assets you may author directly: `.cs`, `.asmdef`, `.uxml`, `.uss`, `.tss`, `Packages/manifest.json`, `.md`.
3. **Zero imported binary assets.** Meshes, textures, sounds, music are generated in code.
4. **C# 7.2 syntax everywhere** (compile-check uses Mono `mcs`). Core has no `UnityEngine` references, no `System.Random`, no static mutable state; all randomness via `GameState.Rng` (`Pcg32`, copy verbatim).
5. **All state changes go through `GameEngine.Apply`.** UI/View/Bots never mutate `GameState`.
6. **Gates are not negotiable.** Never weaken a test, threshold or invariant to make it pass. If you believe a gate is wrong, stop, explain why in `docs/DECISIONS.md`, and pick the interpretation that best matches `01-RULES.md`.
7. **Look at your screenshots.** Visual milestones require capturing screenshots and actually viewing them (Read the PNG files) against the checklist in `09-TESTING.md §5`. Fix what you see.
8. Commit after every passing gate: `git add -A && git commit -m "M<n>: <title>"` (run `git init -b main` first if the folder is not yet a repository). Never commit `game/Library`, `game/Temp`, `game/Logs`, `game/obj`, `game/Builds`, `.sim-build`, `.compile-check`, `tools/.refs` (see `.gitignore`).

## 3. Environment detection (do this first, then choose a path)
```bash
which unity && unity --version          # Unity CLI present?
unity editors --installed --format json # a 6000.0.x editor installed?
unity status --format json              # a live, connected Editor? (state "ready")
which mcs mono || which dotnet          # headless toolchain for sim + compile-check
```
- **Full path (Unity CLI + Editor available):** follow `02 §2` to create `game/`, then drive the Editor live (see §4). Run every gate.
- **Unity MCP / connector tools available instead of the CLI:** use them for the same operations (create project, run menu items, run tests, capture screenshots, read console). Prefer them over editing files when they exist.
- **Headless path (no Unity at all):** still build everything. Create `game/Assets/Hexhaven/**` and `game/Packages/manifest.json` as text, verify with `tools/compile-check.sh` and `tools/sim/run-sim.sh`, and in the final report list precisely which Editor-side gates the user must run (`Hexhaven ▸ Setup Project`, Test Runner EditMode + PlayMode, screenshots, build). Do not claim those passed.
- If `mcs` is missing: `apt-get install -y mono-mcs mono-runtime` (or use the .NET 8 SDK — `run-sim.sh` supports both).

## 4. Command cheat sheet
```bash
# Headless verification (any time)
CORE_DIR=scaffold/Assets/Hexhaven/Scripts/Core tools/sim/run-sim.sh --selftest board   # before M0
tools/sim/run-sim.sh --selftest board            # M1 gate (after copying the scaffold into game/)
tools/sim/run-sim.sh --random-legal 2000         # M2 gate
tools/sim/run-sim.sh 10000                       # M3 gate (exit code 0 required)
tools/sim/run-sim.sh --one 42 --verbose          # debug a single game
tools/compile-check.sh                           # type-check Core/Runtime/Editor/Tests (after M0)

# Unity CLI (see the unity-cli skill for details)
unity projects create "game" --path . --editor-version lts --template <urp-template-id>
unity status
unity command eval --caller plugin --skill unity-cli 'UnityEditor.EditorApplication.ExecuteMenuItem("Hexhaven/Setup Project");'
unity test game --mode EditMode --report-format junit --output game/TestResults/editmode.xml
unity test game --mode PlayMode --report-format junit --output game/TestResults/playmode.xml
unity build game --target <StandaloneLinux64|StandaloneWindows64|StandaloneOSX> --execute-method Hexhaven.EditorTools.BuildScript.BuildAll
```
If the Editor is stuck in Safe Mode (compile errors), `unity status` can't connect: fix the compile errors (use `tools/compile-check.sh` to find them) and restart the Editor.

## 5. Working method
- Implement Core (M1–M4) completely and get the harness green **before** touching visuals; the harness catches most bugs in minutes.
- Write the EditMode tests from `09 §2` alongside the engine (they double as executable rules).
- When the harness reports a failing seed, reproduce with `--one <seed> --verbose`, fix, re-run the full gate.
- Keep `docs/DECISIONS.md` for any interpretation you had to make.
- Performance: no per-frame allocations in steady state; check with the F3 debug overlay.

## 6. Final report (to the user)
- What was built (feature checklist mapped to milestones).
- Gate results with numbers: sim summary (games, finish %, mean turns, win rates, illegal/invariant counts), EditMode/PlayMode pass counts, compile-check, perf numbers.
- Screenshots (title, setup, mid-game, trade dialog, discard dialog, game over) attached/sent.
- Anything not verified and exactly how the user can verify it.
- Decisions made (`docs/DECISIONS.md` summary).
