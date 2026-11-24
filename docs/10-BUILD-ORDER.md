# 10 — Build order, gates, Definition of Done

Work strictly in this order. **Do not start a milestone until the previous gate passes.** Commit after every gate (`git commit -m "M<n>: <title>"`). If a gate cannot be run in your environment (no Unity), say so explicitly in your final report and list exactly which checks remain for the user.

| # | Milestone | Deliverables | Gate (all must pass) |
|---|---|---|---|
| M0 | Project & scaffolding | `game/` Unity 6 URP project (02 §2), packages, asmdefs (02 §3.1), copy `scaffold/Assets/Hexhaven` into `game/Assets/Hexhaven`, `ProjectSetup.cs` (scaffold: Setup Project, Play Main Scene, Open Test Runner) + `HexhavenMenu.cs` (Capture Screenshots, Replay Save…, Debug ▸ Spawn Demo Pieces) | Editor opens without compile errors (`unity status` ready / console clean); `Hexhaven ▸ Setup Project` creates `Main.unity`; `tools/compile-check.sh` passes |
| M1 | Core primitives & board | `Pcg32`, `ResourceBag`, enums, `HexMath`, `Board.CreateGeometry`, `BoardGenerator`, `BeginnerLayout` | T101–T108 pass (or the equivalent sim-harness `--selftest board`) |
| M2 | Rules engine | Full `GameEngine` per 03 (all actions, phases, events, errors, awards, victory), `GameInvariants`, `StateHasher`, `Debug_*` helpers | T201–T901 (including T612–T624 from `11-CHARTERS.md` §7 and T709–T712) and T904–T907 pass; the 11-CHARTERS §8 acceptance checklist holds; T902/T903 run with the random-legal driver (bot-driven versions move to M3); `tools/sim/run-sim.sh --random-legal 2000` shows 0 invariant violations, 0 exceptions, replay and save round-trips match |
| M3 | AI | `BotBrain`, `BotEvaluator`, `CardTracker`, `BotParams`, `BotTrade` per 04 | T902/T903 (bot-driven), T1001–T1003; `tools/sim/run-sim.sh 10000` exits 0 (all 04 §6 gates). Run it in the background (`nohup tools/sim/run-sim.sh 10000 > sim.log 2>&1 &`), expect 5–40 min; iterate with 1,000-game runs first |
| M4 | Persistence | `SaveCodec`, `GameAction` codec, `SaveStore`, `SettingsStore` | T903–T905 pass; sim save round-trips pass |
| M5 | 3D board | `MaterialLibrary`, `Palette`, `MeshFactory`, `TextureFactory`, `BoardView` (island, tiles, decorations, tokens, harbors), `OceanView`, `AmbientView`, `CameraRig`, lighting, post FX | In Play mode the title preview island renders (screenshot reviewed per 09 §5 items 1–3, 9); no pink materials; ≥ 60 fps |
| M6 | Pieces, storm, dice, markers, picking | `PieceFactory`, `StormView`, `DiceView`, `Markers`, `Picker`, `Tweens`, `ResourceFlyout`, `VfxFactory` | A scripted demo (Editor menu `Hexhaven ▸ Debug ▸ Spawn Demo Pieces`) shows every piece type for every colour, storm, dice roll animation, flyouts; screenshot reviewed |
| M7 | Session & event playback | `GameSession`, `EventPlayer`, `BotDriver`, `InputShim`, `HexhavenApp` composition | 4-CPU game plays to completion visually at Normal speed with correct animations; P02 passes in its M7 form (asserts `Engine.State.Phase == GameOver`; the Game Over screen assertion is added in M8) |
| M8 | UI | Root/Theme/Tokens/Common, Title, New Game, Load, HUD, all dialogs (06 §4), curtain, toasts, tooltips, log, settings, rules, pause, game over | P01, P03, P04 pass; every element name from 06 exists (add an EditMode-less PlayMode test `P08_UiElementNames` that loads each uxml and asserts all mandatory names resolve) |
| M9 | Audio | `SfxSynth`, `MusicSynth`, `AudioService`, event mapping | Every SfxId renders non-silent clip (peak > 0.1) within 150 ms total; music loop length = 53.33 s ± 1 sample-block; mapping from 07 §4 wired |
| M10 | Save/Load UX | autosave, Continue, slots, load screen, corrupted-save handling, replay viewer | P05 passes; manual check of corrupted file |
| M11 | Polish | camera follow, reduce-motion, colour-blind emblems, confirm end turn, debug overlay (F3) with force-roll/give-charter/give-resources, screenshot key (F12), all tooltips and disabled reasons | P06 screenshots captured and reviewed (09 §5 all boxes); P07 perf passes |
| M12 | Builds & docs | Standalone builds for the host OS via `unity build game --target <host target> --execute-method Hexhaven.EditorTools.BuildScript.BuildAll`; `game/README.md` (how to play, controls, QA script 09 §6) | Built player launched windowed with `-logFile build-smoke.log -hexhaven-autoquit 10` (HexhavenApp quits after 10 s when that argument is present) shows the title screen and the log contains no exceptions; final report written |

## Definition of Done (all true)

1. Every rule R-1 … R-14 implemented; T-tests for each pass; every row of `12-RULES-AUDIT.md` §2 is satisfied; all user-facing text matches `13-STRINGS.md`.
2. `tools/sim/run-sim.sh 10000` exits 0: 0 illegal actions, 0 invariant violations, replay/save hashes match, AI quality gates met.
3. `tools/compile-check.sh` passes; Unity console has 0 errors and 0 warnings from `Hexhaven.*` assemblies.
4. EditMode and PlayMode test suites green (JUnit reports saved as `game/TestResults/editmode.xml` and `game/TestResults/playmode.xml`).
5. All 06 UI screens/dialogs exist with mandatory element names, keyboard shortcuts work, every disabled action explains why.
6. Screenshots reviewed (09 §5) at 1920×1080 and 1280×720; no visual defects remain.
7. Performance gates (P07) met.
8. Saves round-trip; autosave/continue works; corrupted saves are rejected gracefully.
9. No imported binary assets: `git ls-files game/Assets | grep -vE '\.(cs|asmdef|uxml|uss|tss|shader|meta|unity|mat|asset|md|json)$'` prints nothing, and every `.mat`/`.asset`/`.unity` under `game/Assets/Hexhaven` was generated by `ProjectSetup` (others come from the URP template).
10. No use of any existing commercial board game's name or terminology (grep the repo for forbidden words listed in CLAUDE.md → 0 hits).
11. Final report to the user: what was built, test/sim results with numbers, screenshots attached, any checks that could not be run and why.
