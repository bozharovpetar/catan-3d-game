# 12 — Rules audit against the classic hex-trading board game

Purpose: prove that Hexhaven implements **every mechanic** of the classic base game (3–4 players, current official rulebook and FAQ) with nothing simplified, and record every place where Hexhaven intentionally differs.

This file and the mapping table at the top of `11-CHARTERS.md` are the **only** places in the repository allowed to mention the classic game's own terms; they appear purely as a cross-reference. They must never appear in `game/` (code, UI strings, assets), which the originality gate in `CLAUDE.md` enforces.

Status legend: **Covered** = already specified before this audit · **Fixed** = gap found by this audit and now specified · **Deviation** = intentional difference (original content rather than a missing mechanic).

## 1. Terminology map

| Classic term | Hexhaven term |
|---|---|
| Lumber / Brick / Wool / Grain / Ore | Timber / Clay / Fiber / Grain / Iron |
| Forest / Hills / Pasture / Fields / Mountains / Desert | Forest / Clay Pit / Flax Field / Farmland / Mine / Ruins |
| Road / Settlement / City | Path / Outpost / Keep |
| Robber | Storm |
| Development card | Charter |
| Knight / Victory point card / Road Building / Year of Plenty / Monopoly | Warden / Monument / Road Crew / Bounty / Decree |
| Largest Army / Longest Road | Strongest Guard / Longest Path |
| Harbor (3:1, 2:1) | Harbor (Generic, specific) |

## 2. Mechanic-by-mechanic audit

| # | Classic rule | Hexhaven rule | Status |
|---|---|---|---|
| 1 | 19 terrain hexes: 4/3/4/4/3 producing + 1 desert | R-1.1 | Covered |
| 2 | 18 number tokens 2–12 without 7; red 6/8 never adjacent | R-1.2, R-2.3 (C1) | Covered |
| 3 | 19 resource cards of each type; finite bank | R-1.3 | Covered |
| 4 | 25 development cards: 14 / 5 / 2 / 2 / 2 | R-1.4, 11-CHARTERS §2.1 | Covered |
| 5 | Pieces: 15 roads, 5 settlements, 4 cities; city returns the settlement | R-1.5, R-6.6 | Covered |
| 6 | 9 harbors: 4 generic, 5 specific | R-1.6, R-2.7 | Covered |
| 7 | Robber starts on the desert | R-2.6 | Covered |
| 8 | Highest roll starts; ties re-roll | R-3.2 | Covered |
| 9 | Setup: 2 rounds, second in reverse order; settlement + adjoining road each | R-3.3–R-3.6 | Covered |
| 10 | Starting resources from the second settlement | R-3.7 | Covered |
| 11 | Turn: roll → trade → build; a development card may be played at any time in the turn, **including before the roll** | R-4.1, R-4.2 | **Fixed** — previously only the Warden could be played before the roll; now any Charter can |
| 12 | Basic rule separates the trade phase from the build phase; mixing them is the commonly used advanced rule | R-4.7 `ClassicPhases` option (default: mixed) | **Fixed** — option added |
| 13 | Everyone collects on a roll; city yields 2 | R-5.1 | Covered |
| 14 | Bank shortage: nobody gets that resource unless only one player is owed | R-5.3 | Covered |
| 15 | Costs: road, settlement, city, development card | R-6.1 | Covered |
| 16 | Distance rule; settlements must connect to own road; roads connect to own network and cannot pass an opponent's building | R-6.3–R-6.5 | Covered |
| 17 | 7: no production; players with more than 7 cards discard half (rounded down), **their choice** | R-7.1, R-7.2 | Covered (the v0 prototype discarded at random; the spec does not) |
| 18 | Robber must move to a different hex (desert allowed); steal 1 random card from a chosen player there | R-7.3, R-7.4 | Covered (victim is chosen, not random) |
| 19 | Robber blocks the hex's production | R-7.5 | Covered |
| 20 | Buy a development card; hidden; deck never reshuffled | R-8.2, R-8.12, R-8.13 | **Fixed** — "never reshuffled / stays in hand / may inspect own cards" made explicit |
| 21 | One development card per turn; not on the turn bought; victory point cards exempt | R-8.3, R-8.4, R-8.6 | Covered |
| 22 | Knight: move robber and steal; counts for Largest Army | R-8.5 | Covered |
| 23 | Road Building: 2 free roads (1 if only one piece left) | R-8.7 | Covered; **Fixed** timing (before roll allowed) |
| 24 | Year of Plenty: any 2 resources from the bank | R-8.8 | Covered; **Fixed** timing |
| 25 | Monopoly: all players give all of one resource | R-8.9 | Covered; **Fixed** timing |
| 26 | Victory point cards stay hidden, count immediately, revealed to win | R-8.6, R-12 | Covered |
| 27 | Development cards cannot be traded or given away | R-8.11 | Covered; "given away" made explicit |
| 28 | Domestic trade only with the active player; no gifts; no like-for-like | R-9.1, R-9.2 | Covered |
| 29 | Offers, counter-offers, choosing among acceptors | R-9.3, R-9.4 | Covered |
| 30 | Maritime trade 4:1, 3:1, 2:1; bank must have the card | R-10.1, R-10.2 | Covered |
| 31 | A harbor can be used as soon as you have built on it | R-10.4 | **Fixed** — made explicit |
| 32 | Longest Road: ≥ 5 continuous segments, forks don't add, broken by opponents' buildings, ties keep the holder, set aside if tied after a break | R-11.1–R-11.3 | Covered |
| 33 | Largest Army: first to 3 knights; taken only by playing more | R-11.4 | Covered |
| 34 | 10 victory points, only on your own turn | R-12.1–R-12.3 | Covered (target configurable 8–15, default 10) |
| 35 | Hand sizes public, hand contents private | R-13 | Covered |
| 36 | Trading and building are not allowed before the roll | R-4.3 | Covered; wording tightened |

## 3. Intentional deviations (original content, not missing mechanics)

| Topic | Classic | Hexhaven | Why |
|---|---|---|---|
| Names, art, card titles, layout | proprietary | original (00 §1) | Hexhaven is its own game |
| Number-token placement | lettered tokens laid in a fixed spiral | seeded random with constraints C1–C5 (BalancedRandom), C1 only (Random), or the original fixed Beginner layout | avoids copying a specific layout; gives more variety with a fairness guarantee |
| Harbor positions | printed on the sea frame | Hexhaven's own 9 fixed slots (R-2.7) with shuffled types | same mechanic, original geometry |
| Beginner pre-placed starting pieces | recommended fixed starting positions | not included; Beginner mode fixes only the board | setup placement is part of the game and bots/humans handle it |
| Friendly Storm, victory target 8–15 | not in the base rules | optional settings, default off / 10 | common house options; defaults match the classic game |
| 5–6 players, expansions | separate products | out of scope (00 §3) | — |

## 4. Non-rules gaps found in the package and closed in this revision

| Gap | Resolution |
|---|---|
| No dedicated build guide for the development-card system (only rules + scattered API rows) | `11-CHARTERS.md`: data model, deck construction, per-card engine pseudocode, flag lifecycle, legality matrix, events/visibility, UI flows, AI summary, 13 new tests (T612–T624), acceptance checklist |
| Road Crew had no way to return to TurnStart | `GameState.RoadCrewReturnPhase` (scaffold, hasher, API) |
| No state for trade-then-build | `GameConfig.ClassicPhases`, `GameState.TradingClosedThisTurn`, `Errors.TradingClosed`, save `classic=` field, harness coverage |
| User-facing texts were left to the implementer (error messages, disabled reasons, prompts, in-game rules reference) | `13-STRINGS.md` |
| No tests for the new timing and option | T612–T624 (11 §7), T709–T711 (09 §2) |
| Tool scripts lost their executable bit when committed from Windows | restored in git (`chmod +x`, mode 100755) |

## 5. How to re-run this audit
1. Read the current official base-game rulebook and FAQ.
2. For every numbered rule and FAQ answer, find the R-id that implements it and a T-test that asserts it; add a row to §2.
3. Any rule without both is a gap: add the rule to `01-RULES.md`, the API to `03-CORE-API.md`, a test to `09-TESTING.md`, and a row here marked **Fixed**.
