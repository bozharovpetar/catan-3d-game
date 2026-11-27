# 11 — Charters (development cards): complete build guide

Charters are Hexhaven's development-card system. This document is the step-by-step implementation guide for it: data, engine logic, events, UI, animation, sound, AI and tests. The rules themselves are R-8 and R-4.2 in `01-RULES.md`; where this guide and the rules differ, the rules win.

Genre mapping, for readers who know the classic hex-trading board game (do **not** use the right-hand terms anywhere in the game, see `12-RULES-AUDIT.md`):

| Hexhaven | Classic equivalent | Count | Effect |
|---|---|---|---|
| Charter | Development card | 25 | Bought for 1 Fiber + 1 Grain + 1 Iron |
| Warden | Knight | 14 | Move the Storm, plunder 1 card; counts toward Strongest Guard |
| Monument | Victory point card | 5 | Hidden +1 VP |
| Road Crew | Road-building progress card | 2 | Place 2 free Paths |
| Bounty | Take-two-resources progress card | 2 | Take any 2 resources from the bank |
| Decree | Take-all-of-one-resource progress card | 2 | Every opponent gives you all of one named resource |
| Strongest Guard | Largest-army award | 1 | +2 VP, first to 3 Wardens played, taken only by exceeding |

---

## 1. Rules summary (normative ids)

| Topic | Rule |
|---|---|
| Deck | 25 cards, shuffled once at game start with the game RNG, never reshuffled or refilled (R-8.1, R-8.12) |
| Buying | Main phase only, after the roll; costs Fiber + Grain + Iron; any number per turn while the deck lasts (R-8.2) |
| Secrecy | Type known only to the owner; opponents see the count of unplayed Charters and the number of played Wardens (R-8.13, R-13) |
| When to play | On your own turn, **before the roll or after it**; at most **one** Charter per turn in total (R-4.2, R-8.3) |
| Not the turn bought | A Charter cannot be played on the turn it was bought; an older copy of the same type can (R-8.4) |
| Monument | Never played; counts +1 VP immediately and secretly, also on the turn bought; revealed at game end (R-8.6) |
| Warden | Move Storm to a different tile → plunder (R-7.3, R-7.4); no discards; return to the phase it was played in (R-8.5) |
| Road Crew | Up to 2 free Paths, forced while legal; needs ≥ 1 Path piece and ≥ 1 legal edge to be playable (R-8.7) |
| Bounty | Take 2 resources of your choice (same or different) from bank stock (R-8.8) |
| Decree | Name a resource; all opponents hand over all of it (R-8.9) |
| After play | Card leaves the game; Wardens stay counted (R-8.10) |
| Not tradeable | Charters can't be traded, given, stolen or discarded, and don't count toward the hand limit (R-8.11) |
| Strongest Guard | First to 3 played Wardens; transfers only when exceeded; +2 VP (R-11.4, R-11.5) |

## 2. Data model (already in the scaffold)

```csharp
public enum CharterType { Warden = 0, Monument = 1, RoadCrew = 2, Bounty = 3, Decree = 4 }
public sealed class CharterCard { public CharterType Type; public int BoughtOnTurn; }

// GameState
public List<CharterType> Deck;          // last element = top of the deck
public bool CharterPlayedThisTurn;      // reset only when a NEW turn begins
public int PlayedOtherCharters;         // Road Crew + Bounty + Decree played so far (invariant 2)
public Phase StormReturnPhase;          // TurnStart or Main — where a Warden returns
public int RoadCrewRemaining;           // free Paths still to place
public Phase RoadCrewReturnPhase;       // TurnStart or Main — where a Road Crew returns
public int StrongestGuardHolder;        // -1 or seat

// PlayerState
public List<CharterCard> Charters;      // unplayed cards in purchase order (Monuments included)
public int WardensPlayed;
```

### 2.1 Deck construction (in `GameEngine.NewGame`, after board generation)

```
deck = []
add 14 × Warden, 5 × Monument, 2 × RoadCrew, 2 × Bounty, 2 × Decree   // exactly this order
State.Rng.Shuffle(deck)                                                 // the ONLY shuffle, ever
State.Deck = deck                                                       // draw with Deck[Deck.Count-1]; RemoveAt(Deck.Count-1)
```

## 3. Engine implementation (file `GameEngine.Charters.cs`)

### 3.1 Shared helpers

```csharp
// index of the OLDEST playable card of that type, or -1
int FindPlayable(PlayerState p, CharterType type)
{
    for (int i = 0; i < p.Charters.Count; i++)
        if (p.Charters[i].Type == type && p.Charters[i].BoughtOnTurn < State.TurnNumber) return i;
    return -1;
}

// common validation for PlayWarden / PlayRoadCrew / PlayBounty / PlayDecree (after the common checks of 03 §6.1)
string ValidatePlay(int seat, CharterType type)
{
    if (State.Phase != Phase.TurnStart && State.Phase != Phase.Main) return Errors.WrongPhase;
    if (State.CharterPlayedThisTurn) return Errors.CharterAlreadyPlayed;
    if (FindPlayable(State.Players[seat], type) < 0) return Errors.NoPlayableCharter;
    return null;
}

// common effects: remove the card, set the flag, emit CharterPlayed
void ConsumeCharter(int seat, CharterType type, List<GameEvent> ev)
{
    var p = State.Players[seat];
    p.Charters.RemoveAt(FindPlayable(p, type));
    State.CharterPlayedThisTurn = true;
    if (type == CharterType.Warden) p.WardensPlayed++; else State.PlayedOtherCharters++;
    Emit(ev, EventType.CharterPlayed, player: seat, charter: type);     // public
}
```

`CanPlayCharter(seat, type)` (query used by UI and bots) returns true only if `seat == Current`, the phase is TurnStart or Main, no trade offer is open, `ValidatePlay` passes, and the type-specific precondition holds (Road Crew: `PathsLeft > 0` and `LegalPaths(seat, ignoreCost: true).Count > 0`; Bounty: `Bank.Total > 0`; Monument: always false).

`CanBuyCharter(seat)`: `seat == Current`, Phase Main, no open offer, `Deck.Count > 0`, `Hand.Contains(Costs.Charter)`.

### 3.2 BuyCharter

```
validate: Phase == Main → else WrongPhase; Deck.Count > 0 → else DeckEmpty; Hand ⊇ Costs.Charter → else CannotAfford
effects:
  Hand −= Costs.Charter; Bank += Costs.Charter; Stats.Spent += Costs.Charter; Stats.ChartersBought++
  type = Deck[last]; Deck.RemoveAt(last)
  Charters.Add(new CharterCard { Type = type, BoughtOnTurn = TurnNumber })
  if Config.ClassicPhases: TradingClosedThisTurn = true
  emit CharterBought { Player = seat, Charter = type, VisibleMask = 1 << seat }   // others get it Redacted
  victory check (a Monument may reach the target → immediate win, R-8.6)
```

### 3.3 PlayWarden

```
validate: ValidatePlay(seat, Warden)
effects:
  origin = Phase
  ConsumeCharter(seat, Warden)
  Strongest Guard: if WardensPlayed[seat] >= 3 and (holder == -1 or WardensPlayed[seat] > WardensPlayed[holder]):
        previous = holder; holder = seat; emit AwardChanged { Award = StrongestGuard, Player = seat, Other = previous, Value = WardensPlayed[seat] }
  victory check                      // the award may win the game before the Storm even moves
  if not GameOver: StormReturnPhase = origin; Phase = MoveStorm; emit PhaseChanged
then the normal MoveStorm → (ChooseVictim) flow runs (R-7.3, R-7.4, Friendly Storm R-7.7) and returns to StormReturnPhase.
No discards happen for a Warden. Returning to TurnStart does NOT start a new turn (no TurnStarted event, no flag reset).
```

### 3.4 PlayRoadCrew and PlaceFreePath

```
validate PlayRoadCrew: ValidatePlay(seat, RoadCrew); PathsLeft > 0 and LegalPaths(seat, ignoreCost:true).Count > 0 → else NoLegalPlacement
effects:
  origin = Phase
  ConsumeCharter(seat, RoadCrew)
  RoadCrewRemaining = min(2, PathsLeft); RoadCrewReturnPhase = origin
  emit RoadCrewStarted { Player = seat, Value = RoadCrewRemaining }
  Phase = RoadCrew; emit PhaseChanged

validate PlaceFreePath: Phase == RoadCrew → else WrongPhase; edge in range → BadId; empty → Occupied; R-6.5 connection → NotConnected
effects:
  Edges[e].Owner = seat; PathsLeft--            // no cost, Stats.Spent unchanged, TradingClosedThisTurn unchanged
  emit Built { Player = seat, Piece = Path, Edge = e, Free = true }
  recompute awards (03 §8.2) → maybe AwardChanged(LongestPath)
  victory check → on win: RoadCrewRemaining = 0, GameOver, stop
  RoadCrewRemaining--
  if RoadCrewRemaining == 0 or PathsLeft == 0 or LegalPaths(seat, ignoreCost:true).Count == 0:
        RoadCrewRemaining = 0; Phase = RoadCrewReturnPhase; emit PhaseChanged
```
The second Path may use an edge that only became legal because of the first one. While in the RoadCrew phase every other action fails with `WrongPhase`. There is no "skip" action: placement is forced while legal.

### 3.5 PlayBounty

```
validate: ValidatePlay(seat, Bounty); Bank.Total > 0 → else BankEmpty;
          action.Get has no negatives, action.Get.Total == min(2, Bank.Total), Bank ⊇ action.Get → else InvalidBounty
effects:
  ConsumeCharter(seat, Bounty)
  Bank −= Get; Hand += Get; Stats.GainedFromCharters += Get
  emit BountyTaken { Player = seat, Bag = Get }          // public
  (phase unchanged: TurnStart stays TurnStart, Main stays Main)
```

### 3.6 PlayDecree

```
validate: ValidatePlay(seat, Decree)
effects:
  ConsumeCharter(seat, Decree)
  amounts = int[playerCount]; total = 0
  for each other seat o (ascending): n = Hand[o][r]; Hand[o][r] = 0; Hand[seat][r] += n; amounts[o] = n; total += n
        Stats(seat).GainedFromCharters[r] += n            // do not use the plunder stats here
  emit DecreeResolved { Player = seat, Resource = r, Amounts = amounts, Value = total }   // public, amounts included
  (phase unchanged)
```

### 3.7 Monuments and victory

`VictoryPoints(seat, includeHidden: true)` adds 1 per Monument in `Charters` (they are never removed). `includeHidden: false` (public VP) excludes them. When the victory check fires:

```
Winner = Current
emit MonumentsRevealed { Amounts[seat] = MonumentCount(seat) for every seat }
emit GameWon { Player = Winner, Value = VictoryPoints(Winner, true) }
Phase = GameOver
```

### 3.8 Turn-flag lifecycle (the most common source of bugs)

| Moment | `CharterPlayedThisTurn` | Notes |
|---|---|---|
| A new turn begins (EndTurn → next seat; after the last setup Path) | reset to false | also reset `TradingClosedThisTurn` |
| Any Play* action succeeds | set to true | |
| MoveStorm / ChooseVictim return to TurnStart after a pre-roll Warden | **unchanged (stays true)** | no `TurnStarted` event |
| RoadCrew phase returns to TurnStart | **unchanged (stays true)** | |
| RollDice | unchanged | a Charter played before the roll blocks any Charter after it |

### 3.9 Legality matrix

| Action | RollForOrder / Setup | TurnStart | Discard / MoveStorm / ChooseVictim | Main | Main with open offer | RoadCrew | Other player's turn |
|---|---|---|---|---|---|---|---|
| BuyCharter | ✗ WrongPhase | ✗ WrongPhase | ✗ WrongPhase | ✓ | ✗ TradePending | ✗ WrongPhase | ✗ NotYourTurn |
| PlayWarden / PlayRoadCrew / PlayBounty / PlayDecree | ✗ WrongPhase | ✓ | ✗ WrongPhase | ✓ | ✗ TradePending | ✗ WrongPhase | ✗ NotYourTurn |
| PlaceFreePath | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ |

## 4. Events and what each viewer sees

| Event | Buyer / player | Opponents | Log line (06 §6) |
|---|---|---|---|
| `CharterBought` | card type, card flies into their Charter hand | redacted: a face-down card flies to that player's panel, their Charter count +1 | "**You** bought a Charter: Warden." / "**Bryn** bought a Charter." |
| `CharterPlayed` | public | public | "**Bryn** played a Warden." |
| `AwardChanged(StrongestGuard)` | public | public | "**Bryn** now holds the Strongest Guard (3 Wardens)." |
| `RoadCrewStarted`, `Built{Free}` | public | public | "**Bryn**'s Road Crew laid a Path." |
| `BountyTaken` | public | public | "**Bryn** took 1 Grain and 1 Iron from the bank." |
| `DecreeResolved` | public | public | "Decree: **Bryn** took 5 Grain (You 2, Corra 3)." |
| `MonumentsRevealed` | public at game end | public | "Monuments revealed: Bryn 2, You 1." |

`CardTracker` (04 §4): `CharterBought` subtracts the cost from the buyer's estimate; `BountyTaken` and `DecreeResolved` are exact.

## 5. UI

### 5.1 Buying
- `btn-charter-buy` (key 4) in the action panel shows the cost chips (Fiber, Grain, Iron) and the deck count badge. Enabled iff `CanBuyCharter`. Disabled reasons: "Roll first", "Need 1 Iron" (names the first missing resource), "The Charter deck is empty", "Finish your trade offer first".
- On success: a card back flies from the bank panel's deck (`bank-deck`) to the `btn-charters` button (0.5 s), flips to show its face for 1.2 s in a small centred preview (only for the buyer, and only when the buyer is the current viewer), then tucks into the button; `sfx_card`. The `btn-charters` label updates ("Charters (3)").

### 5.2 Charter hand (`Dialogs/Charters.uxml`, key C)
- One card per unplayed Charter in purchase order: illustration (`TextureFactory.CharterArt`), title, rules text (06 §10).
- A card bought this turn shows a ribbon "Bought this turn — playable next turn".
- Monuments show "+1 VP (secret)" and no Play button.
- `btn-play` per playable card; enabled iff `CanPlayCharter(viewer, type)`. Disabled reasons, in priority order: "Not your turn" · "Finish your trade offer first" · "You already played a Charter this turn" · "Bought this turn" · "No legal Path to place" (Road Crew) · "No Path pieces left" (Road Crew) · "The bank is empty" (Bounty).
- Footer: "Wardens played: n" and, if holding it, the Strongest Guard badge.
- At TurnStart the turn prompt reads "Roll the dice (or play a Charter first)" and `btn-charters` pulses gently if any Charter is playable.

### 5.3 Play flows
| Card | Flow after pressing Play |
|---|---|
| Warden | dialog closes → card-play flourish (card rises centre-screen, 0.6 s, `sfx_card`, ring VFX) → Storm tile markers appear → click a tile → (victim dialog if ≥ 2 candidates) → flyout of the plundered card → back to TurnStart or Main |
| Road Crew | dialog closes → flourish → edge markers, prompt "Road Crew: place a free Path (2 left)" → click → Path drops in → prompt "(1 left)" → click → back to the previous phase. Esc does **not** cancel (the card is already played). |
| Bounty | Resource picker in "pick 2" mode opens **before** the action is submitted; Confirm submits `PlayBounty(seat, bag)`; Cancel closes the picker and the card is **not** consumed. Buttons for resources with bank stock 0 are disabled; a resource with stock 1 can be picked once. |
| Decree | Resource picker in "pick 1" mode ("you hold n" under each); Confirm submits `PlayDecree(seat, r)`; Cancel keeps the card. Afterwards flyouts travel from each opponent's panel to the player's panel. |

### 5.4 Opponent's view and scoreboard
- Player card shows `pc-N-charters` (unplayed count, card-back icon) and `pc-N-wardens` (shield icon + number); `pc-N-sg` badge for the Strongest Guard holder.
- A bot's play shows the same flourish with the card face (plays are public).
- Hot-seat: the Charter hand is private, so it is hidden behind the pass-device curtain like the resource hand.

### 5.5 Game over
`MonumentsRevealed` animates each player's Monuments flipping face-up on their scoreboard card (0.3 s each, `sfx_card`) before the Game Over screen; the VP table has a "Monuments" column.

## 6. AI (summary; full logic in 04-AI)

| Decision | Easy | Normal | Hard |
|---|---|---|---|
| Buy | when affordable and nothing to build | 04 §3.8 rule 9 | rule 9 + when Strongest Guard is contestable or Fiber/Grain/Iron are surplus |
| Warden before the roll | when the Storm sits on an own tile | 04 §3.4 | 04 §3.4 + to hit a leader within 2 VP of winning |
| Road Crew / Bounty / Decree before the roll | never | only if it wins the game immediately | only if it wins the game immediately (after the roll the bot knows more, so waiting is otherwise better) |
| Road Crew / Bounty / Decree after the roll | random | 04 §3.8 rules 5, 6, 8 | same with CardTracker estimates |
| Monument | nothing to do (automatic) | — | — |

Bots never play a Charter bought this turn and never attempt a second Charter in a turn (both guarded by `CanPlayCharter`).

## 7. Tests (add to the catalogue in 09 §2; ids continue the T6xx block)

| Id | Asserts |
|---|---|
| T601–T611 | as listed in 09 §2 |
| T612 | Road Crew played at TurnStart: phase → RoadCrew → back to **TurnStart**; RollDice then legal; `CharterPlayedThisTurn` still true so a Warden afterwards fails with `CharterAlreadyPlayed` |
| T613 | Bounty and Decree played at TurnStart resolve and stay in TurnStart; build/trade/buy still `WrongPhase` until the roll |
| T614 | Road Crew at TurnStart that reaches the victory target via Longest Path ends the game before any roll (FAQ 3a) |
| T615 | Second free Path may use an edge made legal by the first; with only one legal edge overall the effect ends after 1 and the phase returns |
| T616 | Returning to TurnStart after a pre-roll Warden emits no `TurnStarted` and does not reset `CharterPlayedThisTurn` |
| T617 | BuyCharter before the roll → `WrongPhase`; with an open offer → `TradePending`; on another player's turn → `NotYourTurn` |
| T618 | Deck order: after `NewGame(seed)` buying all 25 yields exactly 14/5/2/2/2 and the same sequence for the same seed |
| T619 | Charters are untouched by a 7: a player with 9 resource cards and 4 Charters discards 4 resource cards and keeps 4 Charters |
| T620 | Warden play that gives Strongest Guard and reaches the target wins immediately (no MoveStorm phase) |
| T621 | Strongest Guard: A plays 3 (takes it), B plays 3 (A keeps), B plays a 4th (B takes), A plays a 4th (B keeps) |
| T622 | Decree with ClassicPhases on does not close trading; BuyCharter does |
| T623 | `CanPlayCharter` agrees with `Validate` for every type in every phase over 200 random states |
| T624 | Redaction: `CharterBought.RedactFor(other)` has `Redacted == true`; `CharterPlayed` is never redacted |

The random-legal harness already tries every Play* action before the roll (see `Candidates` in `tools/sim/Sim.cs`).

## 8. Acceptance checklist for the Charter system

- [ ] Deck = 14/5/2/2/2, shuffled once with the game RNG, never refilled; purchases stop when empty.
- [ ] Buy only in Main; costs returned to the bank; type hidden from opponents.
- [ ] Any Charter may be played before the roll or after it; exactly one per turn in total; never one bought this turn.
- [ ] Warden: Storm must move to a different tile; auto-plunder with one candidate; choice with several; nothing with none; returns to the phase it came from.
- [ ] Road Crew: 2 free Paths (or 1 with one piece / one legal edge), forced, returns to the phase it came from, can win before the roll.
- [ ] Bounty: exactly `min(2, bank total)` cards, limited by stock, cancel in the picker does not consume the card.
- [ ] Decree: all opponents, all cards of the named resource, amounts public.
- [ ] Monument: +1 hidden VP immediately, never playable, revealed at the end, can win on the purchase turn.
- [ ] Strongest Guard: first to 3, exceeded-only transfer, +2 VP, can win the game.
- [ ] All UI disabled-reasons appear; hot-seat privacy holds; log lines redact what the viewer may not know.
- [ ] T601–T624 pass; harness shows Decree and Road Crew each played in ≥ 20 % of games.
