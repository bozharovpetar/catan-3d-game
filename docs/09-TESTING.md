# 09 — Testing & verification

Four layers. All must pass for Definition of Done (10-BUILD-ORDER).

| Layer | Where | Runs without Unity? | Command |
|---|---|---|---|
| A. Headless simulation | `tools/sim/` | **yes** (Mono `mcs` or .NET SDK) | `tools/sim/run-sim.sh 10000` |
| B. Compile check of Runtime/Editor code | `tools/compile-check.sh` | **yes** (reference DLLs from NuGet) | `tools/compile-check.sh` |
| C. EditMode unit tests (NUnit) | `game/Assets/Hexhaven/Tests/EditMode` | no | `unity test game --mode EditMode --report-format junit --output game/TestResults/editmode.xml` |
| D. PlayMode smoke + screenshots | `game/Assets/Hexhaven/Tests/PlayMode` | no | `unity test game --mode PlayMode --report-format junit --output game/TestResults/playmode.xml` |

EditMode tests reference only `Hexhaven.Core`, so the same test logic can also be compiled into the sim harness if NUnit is unavailable (keep tests free of Unity APIs).

## 1. Test helpers (EditMode, `TestUtil.cs`)
- `GameEngine NewGame(int seed = 1, int players = 4, BoardMode mode = Beginner, int target = 10)` — all seats bots=false so the test drives actions.
- `void FinishRollOff(GameEngine g)` — applies RollForOrder until phase is SetupOutpost.
- `void AutoSetup(GameEngine g)` — places setup outposts/paths using the first legal options (deterministic).
- `void Give(GameEngine g, int seat, ResourceBag bag)` — test-only: moves cards from bank to hand (public method `GameEngine.Debug_TransferFromBank`, compiled under `#if HEXHAVEN_TESTS || UNITY_INCLUDE_TESTS || UNITY_EDITOR || DEVELOPMENT_BUILD`; public because the test assembly is separate; never logged as actions). Similarly `Debug_SetPhase`, `Debug_SetDice(d1, d2)` (forces the next roll), `Debug_PlaceBuilding`, `Debug_PlacePath`, `Debug_GiveCharter(seat, type, boughtOnTurn)`, `Debug_SetStorm(tile)`.
- `ApplyOk(g, action)` asserts `Ok` and returns events; `ApplyFail(g, action, errorCode)` asserts failure code and **unchanged** `StateHasher.Hash`.

## 2. EditMode test catalogue (each a separate `[Test]`; ids are test names, e.g. `T101_BoardCounts`)

### Board (R-2)
| Id | Asserts |
|---|---|
| T101 | 19 tiles, 54 vertices, 72 edges, 9 harbors; vertices by tile count exactly {3 tiles: 24, 2 tiles: 12, 1 tile: 18}; edges with 2 tiles = 42, with 1 tile (coastal) = 30; every vertex has 2 or 3 neighbours |
| T102 | Tile id order matches the Beginner table (q, r) list |
| T103 | Beginner layout terrains/numbers exactly per table; harbor types per slot table |
| T104 | For 1,000 seeds, BalancedRandom satisfies C1–C5; Random satisfies C1; terrain multiset = R-1.1; token multiset = R-1.2; Ruins has number 0 |
| T105 | Generation deterministic: same seed → identical board; 2 different seeds → different (for ≥ 95 % of 200 pairs) |
| T106 | Harbor slot edges are coastal; harbor corners carry the harbor type; no corner has two harbors |
| T107 | Storm starts on Ruins |
| T108 | BalancedRandom generation < 200 ms worst case over 1,000 seeds |

### Setup (R-3)
| Id | Asserts |
|---|---|
| T201 | Roll-off: all 4 roll, unique max becomes StartPlayer (force dice with Debug_SetDice) |
| T202 | Roll-off tie: only tied players re-roll; others may not act (`NotYourTurn`) |
| T203 | Snake order for start player 2 in 4p: 2,3,0,1,1,0,3,2 |
| T204 | Setup outpost distance rule enforced (`DistanceRule`), occupied (`Occupied`) |
| T205 | Setup path must touch the just-placed outpost (`NotConnected`) |
| T206 | Round-1 outpost grants nothing; round-2 outpost grants 1 per adjacent producing tile, including tiles under the Storm |
| T207 | After setup: Phase TurnStart, Current = StartPlayer, TurnNumber 1; each player has 2 outposts, 2 paths, pieces 13/3/4 |
| T208 | Actions other than the expected one are `WrongPhase`; out-of-turn players `NotYourTurn` |

### Turn & production (R-4, R-5)
| Id | Asserts |
|---|---|
| T301 | Cannot build/trade/end before rolling (`WrongPhase`) |
| T302 | Production: outpost +1, keep +2, all players collect, not tile under Storm (`ProductionBlocked` emitted) |
| T303 | Shortage, two players owed Grain > stock → nobody gets Grain; other resources still paid |
| T304 | Shortage, one player owed 3 Grain, stock 2 → receives 2 (`ShortagePartial` Value 2) |
| T305 | DiceHistogram increments; Die1/Die2 stored |
| T306 | EndTurn passes to next seat, TurnNumber increments, CharterPlayedThisTurn resets |

### Storm (R-7)
| Id | Asserts |
|---|---|
| T401 | Roll 7: players with 8 cards discard 4, with 7 discard 0, with 9 discard 4; discard waits for all |
| T402 | Discard wrong count → `InvalidDiscard`; cards not held → `CannotAfford` |
| T403 | MoveStorm to the same tile → `SameTile` |
| T404 | Single candidate → auto plunder; zero → `NoPlunder`; two → ChooseVictim phase |
| T405 | ChooseVictim invalid target (no building there / no cards / self) → `InvalidVictim` |
| T406 | Plunder takes exactly 1 card, conserves totals, event private to thief and victim (`RedactFor` other seat hides Resource) |
| T407 | Friendly Storm: tiles touching players with ≤ 2 public VP rejected (`StormProtected`), fallback when all excluded |
| T408 | After storm resolution phase returns to Main (7) or TurnStart (pre-roll Warden) |

### Building (R-6)
| Id | Asserts |
|---|---|
| T501 | Path legality: connected via own building; via own path through empty corner; NOT through opponent building corner (`NotConnected`) |
| T502 | Outpost needs own adjacent path (`NotConnected`), distance rule, supply, cost |
| T503 | Keep: only on own outpost (`NotYourOutpost`), returns outpost to supply, KeepsLeft decrements |
| T504 | Piece exhaustion: 15th path ok, 16th `NoPiecesLeft`; 5 outposts limit; 4 keeps limit |
| T505 | Costs deducted exactly and returned to bank (conservation) |
| T506 | After Keep upgrade with 0 outposts left, an outpost can be built again (FAQ 16) |

### Charters (R-8)
| Id | Asserts |
|---|---|
| T601 | Deck has 25 cards with exact type counts; buy pops the top (last) card; `DeckEmpty` after 25 |
| T602 | Cannot play a charter bought this turn (`NoPlayableCharter`); can play an older copy of same type |
| T603 | One charter per turn (`CharterAlreadyPlayed`), Monuments excluded and never playable |
| T604 | Warden before roll → MoveStorm → back to TurnStart, must still roll; Road Crew / Bounty / Decree are equally playable before the roll (R-4.2) |
| T605 | Warden in Main → back to Main; WardensPlayed++ |
| T606 | Strongest Guard at 3 Wardens; transfer only when exceeded; AwardChanged events |
| T607 | Road Crew: places 2 free paths; with 1 path piece places 1; illegal when no legal edge (`NoLegalPlacement`); no other actions during RoadCrew |
| T608 | Bounty: takes 2 (same type allowed); bank stock limits; exactly 1 when bank total 1; `BankEmpty` when 0; `InvalidBounty` for wrong totals |
| T609 | Decree: collects all of named resource from every opponent; amounts per seat in event |
| T610 | Monument counts toward VP immediately (hidden: public VP excludes it) and can win on purchase turn |
| T611 | Charter purchase event private (RedactFor hides type) |
| T612–T624 | Charter timing, flag lifecycle, Road Crew return phase, deck determinism, Strongest Guard sequence, redaction — full list in `11-CHARTERS.md` §7 |

### Trade (R-9, R-10)
| Id | Asserts |
|---|---|
| T701 | Propose validation: empty sides, shared resource, no targets, targeting self, lacking cards → `InvalidTrade`/`CannotAfford` |
| T702 | While offer open: proposer can't build (`TradePending`); non-targets can't respond (`NotYourTurn`) |
| T703 | Accept → Confirm exchanges exactly; offer closed; stats updated |
| T704 | Counter → Confirm uses counter terms; proposer lacking counter Give → `CannotAfford` and no change |
| T705 | Confirm with a partner who no longer has the cards → partner set Declined, `PartnerCannotPay`, offer stays open |
| T706 | Cancel closes; EndTurn auto-cancels an open offer |
| T707 | Maritime rates 4/3/2 by harbor ownership (outpost or keep on harbor corner); bank stock check (`BankEmpty`); same resource (`InvalidTrade`) |
| T708 | Non-current players cannot propose (`NotYourTurn`) |
| T709 | Classic phases on: MaritimeTrade and ProposeTrade succeed before the first purchase and fail with `TradingClosed` after BuildPath / BuildOutpost / BuildKeep / BuyCharter |
| T710 | Classic phases on: playing a Charter and placing free Road Crew Paths do not close trading; the flag resets on the next turn |
| T711 | Classic phases off (default): trading after building succeeds; `TradingClosedThisTurn` is never true |
| T712 | Harbor rate applies in the same turn the Outpost on the harbor corner is built (R-10.4) |

### Awards & victory (R-11, R-12)
| Id | Asserts |
|---|---|
| T801 | Path length: straight 5 → 5; fork (Y shape 3+2+2) → 5 (best trail 3+2); loop of 6 → 6; trail through own outpost continues; an opponent outpost on the 4th corner of a 7-edge trail splits it into 3 and 4 → length 4 |
| T802 | Longest Path first reach 5; tie does not transfer; exceeding transfers |
| T803 | Breaking the holder: holder still tied-longest keeps; unique other takes; tie among others → set aside (-1); later unique → takes |
| T804 | Victory reached via build on own turn ends game immediately (`GameWon`, `MonumentsRevealed`, Phase GameOver, further actions `GameOver`) |
| T805 | Player reaching target off-turn (award transfer caused by another's outpost) wins at their TurnStart, not before |
| T806 | Configurable target (8 and 15) respected |

### Determinism & persistence
| Id | Asserts |
|---|---|
| T901 | `Pcg32(42)` first 5 `NextUInt()` values are stable (record them once from your implementation and assert — guards accidental algorithm edits) and `NextInt` is unbiased in a 600k-sample chi-square test for bound 6 (p > 0.001) |
| T902 | 50 full bot games: `Replay(config, ActionLog)` hash equals original hash |
| T903 | `SaveCodec.Write` → `Read` round trip equals hash for 50 games at random cut points (mid-trade, mid-discard, RoadCrew phase) |
| T904 | Corrupted save (edited action) → Read returns null with error |
| T905 | `GameAction.ToCodec/FromCodec` round-trip for every ActionType with all fields |
| T906 | `GameEngine.Clone()` independence: mutating the clone does not change the original hash |
| T907 | `Validate` is pure: hash unchanged after validating 1,000 random actions |

### AI
| Id | Asserts |
|---|---|
| T1001 | Over 1,000 games (4 bots, mixed difficulty), `Decide` never returns an action failing Validate |
| T1002 | Bots respond to trades, discard correct counts, choose valid victims |
| T1003 | Decide performance: mean < 2 ms, p99.9 < 30 ms over 200 games (first 1,000 decisions excluded) |

## 3. Simulation harness (Layer A)

`tools/sim/Sim.cs` (provided) builds against the Core sources only. It:
1. Runs N games with rotating configurations: player counts 3/4, difficulty mixes (all Normal; 1 Hard + 3 Easy; 1 Normal + 3 Easy; 1 Hard + 3 Normal), board modes (all three), Friendly Storm on in 10 %, targets 10 (90 %), 8 and 12 (5 % each).
2. Drives every awaiting seat with `BotBrain.Decide`, feeding every event to every bot's `BotMemory`.
3. After every action: `GameInvariants.Check` must return empty; `Validate` must have accepted the bot's action.
4. At game end: replay and compare hashes (every 10th game), save/load round-trip (every 25th game).
5. Reports: games finished, mean/median turns, stalls (> 400 turns), win rate per seat and per difficulty mix, mean/max Decide time, illegal actions (must be 0), invariant violations (must be 0), award/charter usage stats (sanity: Longest Path awarded in 40–90 % of games, Strongest Guard 10–60 %, Decree played in ≥ 20 %, Road Crew ≥ 20 %, domestic trades completed ≥ 1 per game on average when Normal/Hard present).
6. Exit code 0 only if every hard gate in 04-AI §6 and this section passes.

## 4. PlayMode tests (Layer D)
| Id | Test |
|---|---|
| P01 | Load `Main.unity`; title screen visible; no errors/exceptions logged for 3 s (`LogAssert.NoUnexpectedReceived`) |
| P02 | Start a 4-CPU game at Instant speed via `GameSession.Start`; runs to GameOver within 120 s real time; no errors; Game Over screen appears |
| P03 | Start 1 human + 3 CPU (Normal speed ×4 timescale); script the human through setup by submitting actions for the first legal spots; verify pieces exist in the scene hierarchy (`World/Pieces` child count) and HUD labels update (resource card labels equal Display hand) |
| P04 | Hot-seat 2 humans: curtain appears on turn change and hides the hand bar |
| P05 | Save mid-game, return to title, Continue → same hash, board rebuilt (piece counts match state) |
| P06 | Screenshots via `ScreenshotTool.Capture(name, width, height)` — renders the main camera into a `RenderTexture` of the requested size with the UI panel's `PanelSettings.targetTexture` pointed at a second RenderTexture of the same size, composites UI over world, `ReadPixels` → `EncodeToPNG` (works in batch mode, unlike `ScreenCapture`). Saved to `game/Screenshots/<w>x<h>/`: title, newgame, setup, midgame (after 6 rounds of CPU play), trade (trade dialog open), discard (forced 7 with 9 cards), gameover — each at **1920×1080 and 1280×720**. After capture restore `targetTexture = null`. |
| P07 | Performance: during a CPU game at Normal speed sample 300 frames; median frame time < 16.7 ms on the test machine, draw calls (ProfilerRecorder) < 250, GC alloc per frame median 0 B |

## 5. Screenshot review (agent self-check, manual)
After P06, open each screenshot (the agent must actually look at the images) and verify:
- [ ] Island is centred, fully visible, not clipped by HUD panels at 1920×1080 and at 1280×720.
- [ ] Every number token readable and upright; 6/8 red; pips visible.
- [ ] Harbors readable (rate + resource), docks touch the coast, piers lead to the correct corners.
- [ ] Pieces in correct player colours with emblems; paths sit exactly on edges; outposts on corners.
- [ ] Storm cloud above the correct tile with rain; tile darkened.
- [ ] HUD: no overlapping panels, no truncated text, costs visible, disabled buttons dimmed.
- [ ] Trade/discard dialogs centred, legible, buttons enabled/disabled correctly.
- [ ] Game over table correct totals; dice histogram drawn.
- [ ] No pink (missing shader) materials anywhere.
Fix and re-capture until every box is checked.

## 6. Manual QA script (for the human, included in README)
1. New game, Beginner board, you + 3 CPU Normal. Complete setup; confirm starting resources.
2. Play 5 turns: build a path, outpost, buy a charter, maritime trade, propose a domestic trade and get a response.
3. In a **separate** game, force-test 7 using the debug overlay `F3 → Force next roll: 7` (Development only): discard dialog, storm move, victim choice. Any debug command marks the session *debug*: autosave and manual saving are disabled for it (toast "Debug session — saving disabled") because `Debug_*` changes are not in the action log.
4. Play a Warden before rolling; Road Crew; Bounty; Decree (debug: `Give charter` buttons in debug overlay).
5. In a normal (non-debug) game, save to slot 1, quit to title, load slot 1, confirm identical state.
6. Hot-seat game with 2 humans: confirm curtain privacy.
7. Play to victory at Fast speed; check game-over stats.
