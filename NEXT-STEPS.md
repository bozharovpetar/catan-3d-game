# NEXT STEPS — set up a machine and build Hexhaven end to end

**Audience:** an AI coding agent (and the human operating it) starting on a machine that has only this repository. Follow the steps in order. Each step says what to run, what you should see, and what to do if you don't.

**What exists today:** a complete specification (`docs/00`–`13`), verified starting code (`scaffold/`), a headless simulation harness and a compile checker (`tools/`). **What does not exist yet:** the game. No rules engine, AI, 3D, UI or audio has been implemented. This file is the plan for building all of it.

**Companion files:** `CLAUDE.md` holds the standing rules for every session. `docs/10-BUILD-ORDER.md` holds the milestone gates and the Definition of Done. This file is the detailed runbook that ties them together; where it disagrees with `docs/01-RULES.md` or `docs/10-BUILD-ORDER.md`, those win.

---

## Part A — What the human must do first (an agent cannot do these)

| # | Task | Why an agent can't |
|---|---|---|
| A1 | Install **Unity Hub** and sign in with a Unity account | interactive login, OS installer prompts |
| A2 | Make sure a Unity **Personal (or better) licence** is active for that account | licence agreement |
| A3 | Install **Git** and authenticate to GitHub so `git push` works for this repository | credentials |
| A4 | Install the agent (e.g. Claude Code) and, if available, enable the **Unity plugin / skills** for it | account-level setup |
| A5 | Decide where the repository lives. Any path works, including one with spaces; a short path such as `C:\dev\hexhaven` avoids Windows path-length problems inside `game/Library` | — |
| A6 | Keep the Unity Editor window available while the agent works on milestones M5–M12 (the agent drives it but cannot recover from modal dialogs it cannot see) | GUI |

Hardware: any desktop able to run Unity 6 (8 GB RAM minimum, 16 GB recommended, ~15 GB free disk for the Editor plus the project `Library`).

---

## Part B — Machine setup (agent runs these; human approves installs)

Run every command from the **repository root**. The `tools/*.sh` scripts are bash scripts. On Windows the most reliable way to run them is **WSL** (Ubuntu) with `mono-mcs` installed; Git Bash also works when `mcs` (Mono) or `dotnet` is on its PATH. Use PowerShell or any shell for `git` and `unity` commands.

### B1. Get the repository

```bash
git clone https://github.com/bozharovpetar/catan-3d-game.git hexhaven
```

```bash
cd hexhaven
```

Confirm the specification is complete. All of these files must exist:

```bash
ls docs/11-CHARTERS.md docs/12-RULES-AUDIT.md docs/13-STRINGS.md NEXT-STEPS.md .gitattributes
```

If any is missing, the branch you are on is behind. Fetch and merge the branch that has them, then re-check:

```bash
git fetch origin claude/charters-and-rules-audit
```

```bash
git merge origin/claude/charters-and-rules-audit
```

Create the working branch for the build (never build directly on `main`):

```bash
git checkout -b build/hexhaven
```

### B2. Headless toolchain (needed on every path)

The simulation harness and the compile checker need a C# compiler outside Unity.

| OS | Install |
|---|---|
| Windows (recommended: WSL) | In WSL Ubuntu: `sudo apt-get install -y mono-mcs mono-runtime unzip curl`. The repository can stay on the Windows drive (`/mnt/c/...`). |
| Windows (Git Bash) | Mono from mono-project.com (adds `mcs` and `mono`), **or** the .NET 8 SDK (`winget install Microsoft.DotNet.SDK.8`). Mono is required for `tools/compile-check.sh`; the simulation can use either. This route is untested — fall back to WSL if a script misbehaves. |
| macOS | `brew install mono` |
| Linux | `sudo apt-get install -y mono-mcs mono-runtime` |

Verify:

```bash
which mcs mono || which dotnet
```

Then prove the package works before changing anything:

```bash
CORE_DIR=scaffold/Assets/Hexhaven/Scripts/Core tools/sim/run-sim.sh --selftest board
```

Expected last line: `board selftest: PASS (...)`.

```bash
tools/compile-check.sh scaffold/Assets/Hexhaven
```

Expected last line: `compile-check: PASS`. The first run downloads about 60 MB of reference DLLs from nuget.org into `tools/.refs` (git-ignored).

If a script fails with `bad interpreter` or `$'\r': command not found`, the checkout has Windows line endings: run `git add --renormalize . && git checkout -- tools` and retry.

### B3. Unity CLI, Editor and licence

Install the Unity CLI if `unity --version` fails.

Windows (PowerShell):

```bash
$env:UNITY_CLI_CHANNEL='beta'; irm https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.ps1 | iex
```

macOS / Linux:

```bash
curl -fsSL https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh | UNITY_CLI_CHANNEL=beta bash
```

Open a new shell, then check sign-in and licence (the human signs in if needed with `unity auth login`, and activates with `unity license activate`):

```bash
unity --version
```

```bash
unity auth status --format json
```

```bash
unity license status --format json
```

Install the Unity 6 LTS Editor if none is listed:

```bash
unity editors --installed --format json
```

```bash
unity install lts --yes --accept-eula
```

The project targets **Unity 6000.0 LTS**. Add build modules for other platforms only if you intend to ship them (`unity install-modules`).

### B4. Decide the path

| You have | Path | What it means |
|---|---|---|
| Unity CLI + Editor + licence | **Full** | Run every gate, including PlayMode tests, screenshots and the build |
| Unity MCP/connector tools but no CLI | **Full via connector** | Same gates, using the connector's equivalents of each command |
| No Unity at all | **Headless** | Build everything as text, pass the harness and compile-check gates, and list the Editor-side gates as *not run* in the final report |

Record the choice and the tool versions in `docs/DECISIONS.md` (create it).

---

## Part C — How to run the build as an agent

1. **Read first.** Read `CLAUDE.md`, then every file it lists in §1, in order, before writing code. That is about 30,000 words; do not skim `01-RULES.md`, `03-CORE-API.md` or `11-CHARTERS.md`.
2. **One milestone at a time.** Do not start a milestone until the previous gate passes. Never weaken a gate.
3. **Commit at every gate:** `git add -A && git commit -m "M<n>: <title>"`, then push the branch. A crashed or expired session then loses at most one milestone.
4. **Keep a progress log.** Maintain `docs/PROGRESS.md`: for each milestone the date, commit hash, gate output (numbers), and anything not verified. A fresh agent session resumes by reading `CLAUDE.md`, `docs/PROGRESS.md` and `docs/DECISIONS.md`.
5. **Long runs go in the background.** `tools/sim/run-sim.sh 10000` can take many minutes; run it detached and poll its log.
6. **Trust the harness over your reading.** If the harness reports an invariant violation or illegal action, reproduce with `tools/sim/run-sim.sh --one <seed> --verbose`, fix the engine or bot, and re-run the whole gate.
7. **Look at screenshots.** For M5, M6, M8 and M11 you must open the captured PNGs and compare them with `docs/09-TESTING.md` §5.
8. **Stop and ask the human** only for: licence/login prompts, a modal Editor dialog you cannot dismiss, a gate you believe is wrong (explain in `docs/DECISIONS.md` first), or pushing to a protected branch.

Kick-off prompt for a new agent session:

> Read CLAUDE.md and NEXT-STEPS.md. Check docs/PROGRESS.md for the last completed milestone, then continue with the next one in NEXT-STEPS.md Part D. Commit and push at every passing gate and update docs/PROGRESS.md. Don't stop until the Definition of Done in docs/10-BUILD-ORDER.md holds or you're blocked.

---

## Part D — Milestones, step by step

Each milestone lists: **Do** (ordered work), **Verify** (commands and expected output), **Commit**. Spec references are in parentheses.

### M0 — Unity project and scaffolding

**Do**
1. List templates and pick the Universal 3D (URP) one:
   ```bash
   unity templates list --editor lts --format json
   ```
2. Create the project in `game/`:
   ```bash
   unity projects create "game" --path . --editor-version lts --template <urp-3d-template-id>
   ```
3. Copy the scaffold into the project (Git Bash / macOS / Linux):
   ```bash
   cp -R scaffold/Assets/Hexhaven game/Assets/
   ```
   PowerShell equivalent: `Copy-Item scaffold\Assets\Hexhaven game\Assets\ -Recurse`
4. Open `game/Packages/manifest.json` and confirm it contains `com.unity.render-pipelines.universal`, `com.unity.inputsystem` and `com.unity.test-framework`. Add any that are missing (use the `unity-package-management` skill; never guess version numbers).
5. Add the Pipeline package so the CLI can drive the Editor live:
   ```bash
   unity pipeline install
   ```
6. Open the project and wait until the Editor reports ready:
   ```bash
   unity open game
   ```
   ```bash
   unity status --format json
   ```
7. Run project setup (creates `Assets/Hexhaven/Scenes/Main.unity`, template materials, settings — `docs/02-ARCHITECTURE.md` §2.2):
   ```bash
   unity command eval --caller plugin --skill unity-cli 'UnityEditor.EditorApplication.ExecuteMenuItem("Hexhaven/Setup Project");'
   ```
8. Write the remaining Editor scaffolding named in the M0 row of `docs/10-BUILD-ORDER.md`: `HexhavenMenu.cs` with *Capture Screenshots*, *Replay Save…* and *Debug ▸ Spawn Demo Pieces* (the last two may be stubs that log "not implemented" until M6/M10).

**Verify**
- Unity console: 0 errors, 0 warnings from `Hexhaven.*` assemblies.
- `game/Assets/Hexhaven/Scenes/Main.unity` and `game/Assets/Hexhaven/Resources/Hexhaven/Materials/*.mat` exist (generated, never hand-written).
- `tools/compile-check.sh` → `compile-check: PASS`.
- `tools/sim/run-sim.sh --selftest board` → `PASS` (now reading `game/Assets/Hexhaven/Scripts/Core`).

**Headless path:** create `game/Assets/Hexhaven` by copying the scaffold and write `game/Packages/manifest.json` by hand with the three packages above; steps 5–7 are deferred to the human and must be listed in the final report.

**Commit:** `M0: project and scaffolding`

### M1 — Core primitives and board

The scaffold already implements this milestone. Your job is to add its tests.

**Do**
1. Add EditMode tests T101–T108 (`docs/09-TESTING.md` §2, Board) in `game/Assets/Hexhaven/Tests/EditMode/`. Three already exist in `BoardTests.cs`.
2. Do not modify `Pcg32.cs` or the id-assignment code in `Board.cs` (saves depend on them).

**Verify**
```bash
tools/sim/run-sim.sh --selftest board
```
```bash
unity test game --mode EditMode --report-format junit --output game/TestResults/editmode.xml
```
All board tests pass.

**Commit:** `M1: core primitives and board tests`

### M2 — Rules engine (the largest and most important milestone)

Implement every `throw new NotImplementedException("M2")` in `Scripts/Core/Rules/GameEngine.cs`, split into partial files as that file's header suggests.

**Do, in this order** (each sub-step compiles and gets its tests before the next)
1. **Skeleton:** `NewGame` (config validation, board generation, deck build + shuffle per `11-CHARTERS.md` §2.1, bank 19 each, players, `RollOffQueue`), `TakeInitialEvents`, `AwaitingInput`, event emission helper (assigns `Seq`), action log, common validation (`03-CORE-API.md` §6.1), `Clone`, `Replay`.
2. **Roll-off and setup** (R-3): `RollForOrder`, snake order, `PlaceSetupOutpost`, `PlaceSetupPath`, starting resources. Tests T201–T208.
3. **Turn flow and production** (R-4, R-5): new-turn handling, `RollDice`, production with bank shortage, `EndTurn`. Tests T301–T306.
4. **Storm** (R-7): discard phase, `MoveStorm`, plunder candidates, `ChooseVictim`, Friendly Storm. Tests T401–T408.
5. **Building** (R-6): `BuildPath`, `BuildOutpost`, `BuildKeep`, piece limits, queries `LegalPaths`/`LegalOutposts`/`LegalKeeps`. Tests T501–T506.
6. **Awards and victory** (R-11, R-12, `03` §8.1–8.2): path-length search, award recomputation, Strongest Guard, victory check after every current-player action and at new-turn start. Tests T801–T806.
7. **Charters** — follow `docs/11-CHARTERS.md` §3 literally: `BuyCharter`, `PlayWarden`, `PlayRoadCrew`/`PlaceFreePath`, `PlayBounty`, `PlayDecree`, Monuments, the flag lifecycle table (§3.8). Tests T601–T624.
8. **Trading** (R-9, R-10, R-4.7): `MaritimeTrade`, `ProposeTrade`, `RespondTrade`, `ConfirmTrade`, `CancelTrade`, auto-cancel on `EndTurn`, Classic phases. Tests T701–T712.
9. **Invariants and debug hooks:** `GameInvariants.Check` (`03` §10, all ten), the `Debug_*` methods. Tests T901, T904–T907.

**Verify**
```bash
tools/sim/run-sim.sh --random-legal 2000
```
Expected: `random-legal: games 2000, ... → PASS` (0 invariant violations, 0 exceptions, replay and save round-trips match).
```bash
unity test game --mode EditMode --report-format junit --output game/TestResults/editmode.xml
```
Every T2xx–T9xx test listed for M2 in `docs/10-BUILD-ORDER.md` passes. Then tick every box of the `11-CHARTERS.md` §8 checklist.

**Commit:** `M2: rules engine`

### M3 — AI

**Do** (`docs/04-AI.md`)
1. `CardTracker` and `BotMemory.Observe` (§4).
2. `BotEvaluator`: resource weights, production profile, site value, hand utility, turns-to-afford (§2).
3. `BotBrain.Decide` for every phase (§3.1–§3.11), including trade responses, discards, Storm choice, Charter use (pre-roll rule in §3.4), and the Classic phases ordering note.
4. Difficulty parameters come from `BotParams.For` (already implemented).

**Verify**
Iterate with small runs first:
```bash
tools/sim/run-sim.sh 1000
```
Then the gate, in the background:
```bash
nohup tools/sim/run-sim.sh 10000 > sim.log 2>&1 &
```
`sim.log` must end with `RESULT: PASS`: 0 illegal actions, 0 invariant violations, ≥ 99.5 % of games finished, mean length 60–110 turns, win-rate and seat-bias gates, Decide mean < 2 ms and p99.9 < 30 ms. Tests T902, T903 (bot-driven), T1001–T1003 pass. Copy the summary block from `sim.log` into `docs/PROGRESS.md`.

If a quality gate fails, tune the bot (never the gate). Typical causes: bots hoarding instead of building, never completing trades, never buying Charters.

**Commit:** `M3: AI`

### M4 — Persistence

**Do** (`docs/08-PERSISTENCE.md`): `SaveCodec` already exists; add `SaveStore` (atomic writes, autosave debounce, three slots) and `SettingsStore` (PlayerPrefs JSON) in Runtime.

**Verify:** T903–T905 pass; the harness reports 0 save mismatches.

**Commit:** `M4: persistence`

### M5 — 3D board

**Do** (`docs/05-VISUALS.md` §1–§4, §8, §12)
1. `Palette`, `MaterialLibrary` (clone the generated template materials; `WorldText` for all `TextMesh`), `TextureFactory`.
2. Extend `MeshFactory` as needed (ring, chamfer box, grid).
3. `BoardView.Build`: ocean, shallows, foam, beach, tiles, terrain decorations, number tokens with pips, harbors; static batching.
4. `OceanView`, `AmbientView`, `CameraRig`, lighting, fog, sky sphere, post-processing volume (camera needs `renderPostProcessing = true`).
5. `HexhavenApp` creates all of this and shows the orbiting title preview.
6. Implement `ScreenshotTool.Capture(name, width, height)` now (`docs/09-TESTING.md` P06) — you need it for every visual gate.

**Verify:** enter Play mode, capture 1920×1080 and 1280×720 screenshots, open them, and check `docs/09-TESTING.md` §5 items 1–3 and 9 (island centred, tokens readable, harbors correct, no pink materials). Frame rate ≥ 60.

**Commit:** `M5: 3D board`

### M6 — Pieces, Storm, dice, markers, picking

**Do** (`05` §5–§7, §9–§11): `PieceFactory` (Path, Outpost, Keep, ghost, emblems), `StormView`, `DiceView`, `Markers`, `Picker` (plane-intersection picking, no colliders), `Tweens`, `ResourceFlyout`, `VfxFactory`, and the *Spawn Demo Pieces* debug menu.

**Verify:** run the demo menu, capture screenshots, confirm every piece type in all four colours, the Storm with rain, a dice roll animation and resource flyouts.

**Commit:** `M6: pieces, storm, dice, picking`

### M7 — Session and event playback

**Do** (`docs/02-ARCHITECTURE.md` §9): `GameSession`, `DisplayState` (advanced per event, resynced when the queue empties), `EventPlayer` (one playback routine per event type, speed scaling), `BotDriver` (pacing from `04` §5; never asks a proposer to act while responses are pending), placement modes via `InputShim`.

**Verify:** a four-CPU game plays to the end at Normal speed with correct animations; PlayMode test P02 in its M7 form passes (`Engine.State.Phase == GameOver`, no errors logged).

**Commit:** `M7: session and playback`

### M8 — UI

**Do** (`docs/06-UI.md`, text from `docs/13-STRINGS.md`)
1. `Root.uxml`, `Tokens.uss`, `Common.uss`, `UiRoot` (runtime `PanelSettings`, pointer-over-UI test).
2. Screens: Title, New Game (including the Classic phases and Friendly Storm toggles), Load, HUD, Game Over.
3. Dialogs: Discard, Victim, Resource picker, Charters (flows in `11-CHARTERS.md` §5), Trade, Offer status, Incoming offer, Confirm, Pause, Settings, Rules, and the pass-device curtain.
4. Log formatting with per-viewer redaction, toasts, tooltips, keyboard shortcuts.

**Verify:** P01, P03, P04 and P08 (every mandatory element name resolves) pass; P02 now also asserts the Game Over screen. Capture and review trade, discard and HUD screenshots at both resolutions.

**Commit:** `M8: UI`

### M9 — Audio

**Do** (`docs/07-AUDIO.md`): `SfxSynth` (all effects in §2), `MusicSynth` (53.33 s loop, rendered over several frames), `AudioService`, the event-to-sound mapping in §4.

**Verify:** every effect renders a non-silent clip (peak > 0.1), all clips render in < 150 ms total, the music loop has the specified length.

**Commit:** `M9: audio`

### M10 — Save / load experience

**Do:** autosave after every action, Continue on the title screen, three manual slots, the Load screen, corrupted-save handling, session rebuild on load (re-apply the action log and feed bot memories), the replay viewer menu.

**Verify:** P05 passes; a hand-edited save file is rejected with the corrupted-save message.

**Commit:** `M10: save and load`

### M11 — Polish

**Do:** camera follow, reduce-motion, colour-blind emblems, confirm-end-turn, debug overlay (F3: fps, draw calls, force roll, give Charter, give resources — any use disables saving for that session), screenshot key (F12), every tooltip and disabled reason from `docs/13-STRINGS.md` §4.

**Verify:** P06 captures the full screenshot set at 1920×1080 and 1280×720; tick **every** box in `docs/09-TESTING.md` §5. P07 performance: median frame < 16.7 ms, draw calls < 250, 0 B GC per frame in steady state.

**Commit:** `M11: polish`

### M12 — Builds and hand-over

**Do**
1. `BuildScript.BuildAll` in the Editor assembly.
2. Build for the host OS:
   ```bash
   unity build game --target <StandaloneWindows64|StandaloneOSX|StandaloneLinux64> --execute-method Hexhaven.EditorTools.BuildScript.BuildAll
   ```
3. `game/README.md`: how to play, controls, the manual QA script (`docs/09-TESTING.md` §6).

**Verify:** launch the built player with `-logFile build-smoke.log -hexhaven-autoquit 10`; the title screen appears and the log has no exceptions. Run the originality gate from `CLAUDE.md` §2.1 (must print nothing) and walk the Definition of Done in `docs/10-BUILD-ORDER.md` item by item.

**Commit:** `M12: builds and docs`, then open a pull request from `build/hexhaven` to `main`.

---

## Part E — Final report (agent to human)

Use the structure in `CLAUDE.md` §6: features by milestone; gate numbers (simulation summary, EditMode/PlayMode pass counts, compile-check, performance); the screenshots; anything not verified and exactly how to verify it; the contents of `docs/DECISIONS.md`.

---

## Part F — Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `tools/*.sh: Permission denied` | executable bit lost on checkout | `chmod +x tools/*.sh tools/sim/*.sh` (or run with `bash tools/...`) |
| `$'\r': command not found` in a script | CRLF line endings | `git add --renormalize . && git checkout -- tools`; `.gitattributes` prevents recurrence |
| `Need Mono mcs` | Mono not installed, or not visible to the shell | install per B2 (the simulation alone can use the .NET 8 SDK); on Windows run the scripts in WSL |
| compile-check cannot download references | nuget.org blocked | allow `api.nuget.org`, or skip compile-check on the Full path (the Unity console is then the compile authority) |
| `unity status` shows no instance / commands time out | Editor closed, or in **Safe Mode** because of compile errors | `unity pipeline list` to confirm; fix errors found by `tools/compile-check.sh`; restart the Editor |
| `unity command eval` is unknown | Pipeline package missing | `unity pipeline install`, reopen the project |
| Pink materials | template materials not generated or wrong pipeline | run `Hexhaven ▸ Setup Project` again; check `Resources/Hexhaven/Materials` |
| Number tokens show through pieces | `TextMesh` still using the default text shader | assign the `Hexhaven/WorldText` material (05 §1) |
| Post-processing has no effect | camera lacks `renderPostProcessing` | set it on `UniversalAdditionalCameraData` (02 §9) |
| Simulation stalls or mean game length is out of range | bot logic, not the gate | inspect with `--one <seed> --verbose`; see M3 notes |
| Replay or save hash mismatch | engine used randomness outside `GameState.Rng`, iterated a dictionary, or a state field is missing from `StateHasher` | remove the nondeterminism; add the field to the hasher **and** `03` §9 together |
| Game Over never appears in a CPU game | victory check not run at new-turn start or after free Paths | see `03` §5 and `11-CHARTERS.md` §3.4 |
| Save rejected as corrupted after using the debug overlay | expected: debug sessions are not replayable | saving must be disabled once a debug command is used (09 §6) |
| Push rejected | branch protection or missing credentials | human fixes access; never force-push `main` |

---

## Part G — Quick reference

| Purpose | Command |
|---|---|
| Board self-test | `tools/sim/run-sim.sh --selftest board` |
| Engine gate (M2) | `tools/sim/run-sim.sh --random-legal 2000` |
| AI gate (M3) | `tools/sim/run-sim.sh 10000` |
| Debug one game | `tools/sim/run-sim.sh --one <seed> --verbose` |
| Type-check without Unity | `tools/compile-check.sh` |
| EditMode tests | `unity test game --mode EditMode --report-format junit --output game/TestResults/editmode.xml` |
| PlayMode tests | `unity test game --mode PlayMode --report-format junit --output game/TestResults/playmode.xml` |
| Run a menu item | `unity command eval --caller plugin --skill unity-cli 'UnityEditor.EditorApplication.ExecuteMenuItem("Hexhaven/Setup Project");'` |
| Originality gate | see `CLAUDE.md` §2.1 (must print nothing) |
