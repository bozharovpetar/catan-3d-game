# 01 — Rules (source of truth)

Every rule has an id `R-x.y`. Tests in `09-TESTING.md` reference these ids. When this document and any other document disagree, **this document wins**.

---

## R-1 Components

| Id | Rule |
|---|---|
| R-1.1 | **Tiles (19):** 4 Forest, 3 Clay Pit, 4 Flax Field, 4 Farmland, 3 Mine, 1 Ruins. |
| R-1.2 | **Number tokens (18):** 2, 3, 3, 4, 4, 5, 5, 6, 6, 8, 8, 9, 9, 10, 10, 11, 11, 12. Ruins gets no token. |
| R-1.3 | **Resource cards:** 19 of each of the 5 resources in the bank (95 total). The bank is finite (see R-5.3). |
| R-1.4 | **Charter deck (25):** 14 Warden, 5 Monument, 2 Road Crew, 2 Bounty, 2 Decree. Shuffled face down at game start. |
| R-1.5 | **Pieces per player:** 15 Paths, 5 Outposts, 4 Keeps. Pieces on the board never return to supply, **except** that upgrading an Outpost to a Keep returns the Outpost piece to its owner's supply. |
| R-1.6 | **Harbors (9):** 4 Generic (3:1), and one each of Timber, Clay, Fiber, Grain, Iron (2:1). |
| R-1.7 | **Awards:** Longest Path (+2 VP), Strongest Guard (+2 VP). Each held by at most one player. |
| R-1.8 | **Dice:** two six-sided dice. |
| R-1.9 | **Storm:** one piece; occupies exactly one tile at all times. |

## R-2 Board construction

| Id | Rule |
|---|---|
| R-2.1 | The island is a hexagonal arrangement of radius 2 (19 tiles), pointy-top hexes, axial coordinates (q, r) with q, r ∈ [-2, 2] and \|q + r\| ≤ 2. |
| R-2.2 | **Board modes.** `BalancedRandom` (default), `Random`, `Beginner`. |
| R-2.3 | **Random:** shuffle the 19 terrains onto the 19 tiles; then shuffle the 18 tokens onto the 18 non-Ruins tiles (in tile-id order). Constraint **C1**: no two tiles bearing 6 or 8 are adjacent (6–6, 6–8, 8–8 all forbidden). Re-shuffle the *tokens only* until C1 holds (max 10,000 attempts; if exhausted, re-shuffle terrains too and repeat). |
| R-2.4 | **BalancedRandom:** like Random, but must satisfy C1 plus: **C2** no two adjacent tiles have the same number; **C3** no two adjacent tiles have the same terrain; **C4** no corner touches tiles whose pip sum exceeds 11; **C5** Ruins is not the centre tile (0,0). Algorithm: repeat { shuffle terrains until C3 and C5 hold (max 5,000 tries per outer loop) ; then up to 2,000 token shuffles until C1, C2, C4 hold } until success. Must terminate within 200 ms for any seed (it does in practice; tests verify 1,000 seeds). |
| R-2.5 | **Beginner:** the fixed layout in the table below (it satisfies C1–C5). |
| R-2.6 | **Storm start:** the Storm starts on the Ruins tile. |
| R-2.7 | **Harbor slots** are fixed (table below). In Random/BalancedRandom the 9 harbor types are shuffled across the 9 slots. In Beginner they use the fixed types in the table. |
| R-2.8 | A harbor's two corners (the endpoints of its edge) are **harbor corners** of that type. No corner is in two harbors (slots are spaced ≥ 3 coastal edges apart). |

### Beginner layout (tile id = index in id order, see `03-CORE-API.md` §2)

| Tile id | (q, r) | Terrain | Number |
|---|---|---|---|
| 0 | (-2, 0) | Flax Field | 5 |
| 1 | (-2, 1) | Farmland | 8 |
| 2 | (-2, 2) | Mine | 10 |
| 3 | (-1, -1) | Forest | 6 |
| 4 | (-1, 0) | Mine | 3 |
| 5 | (-1, 1) | Forest | 11 |
| 6 | (-1, 2) | Clay Pit | 9 |
| 7 | (0, -2) | Clay Pit | 4 |
| 8 | (0, -1) | Flax Field | 10 |
| 9 | (0, 0) | Farmland | 12 |
| 10 | (0, 1) | Ruins | — |
| 11 | (0, 2) | Farmland | 8 |
| 12 | (1, -2) | Farmland | 3 |
| 13 | (1, -1) | Mine | 5 |
| 14 | (1, 0) | Flax Field | 2 |
| 15 | (1, 1) | Forest | 9 |
| 16 | (2, -2) | Flax Field | 6 |
| 17 | (2, -1) | Forest | 11 |
| 18 | (2, 0) | Clay Pit | 4 |

### Harbor slots

Side index `s` of a tile: side `s` lies between corner `s` and corner `s+1` (mod 6); its outward direction is at angle `60·s` degrees in world XZ (see `03-CORE-API.md` §2). The listed side of the listed tile is always a coastal edge.

| Slot | Tile (q, r) | Side | Beginner type |
|---|---|---|---|
| 0 | (2, 0) | 0 | Generic 3:1 |
| 1 | (2, -1) | 1 | Timber 2:1 |
| 2 | (1, -2) | 1 | Generic 3:1 |
| 3 | (0, -2) | 2 | Clay 2:1 |
| 4 | (-1, -1) | 3 | Generic 3:1 |
| 5 | (-2, 1) | 3 | Fiber 2:1 |
| 6 | (-2, 2) | 4 | Grain 2:1 |
| 7 | (-1, 2) | 5 | Generic 3:1 |
| 8 | (1, 1) | 5 | Iron 2:1 |

## R-3 Game setup

| Id | Rule |
|---|---|
| R-3.1 | **Seats** 0..N-1 (N = 3 or 4) are in fixed clockwise order. Turn order always proceeds seat+1 mod N. |
| R-3.2 | **Roll-off:** every player rolls 2d6 in seat order. The highest total becomes the **start player**. If several tie for highest, only the tied players re-roll (in seat order), repeating until one is highest. Humans press "Roll"; bots roll automatically. |
| R-3.3 | **Setup round 1:** starting with the start player and proceeding clockwise, each player places 1 Outpost then 1 Path. |
| R-3.4 | **Setup round 2:** starting with the *last* player of round 1 and proceeding counter-clockwise (so the start player places last), each player places 1 Outpost then 1 Path. (Snake order: for N=4 and start player S: S, S+1, S+2, S+3, S+3, S+2, S+1, S.) |
| R-3.5 | **Setup Outpost placement:** any empty corner satisfying the distance rule (R-6.3). No connection is required. Costs nothing. |
| R-3.6 | **Setup Path placement:** must be an empty edge touching the Outpost just placed by that player. Costs nothing. |
| R-3.7 | **Starting resources:** immediately after placing their **round-2** Outpost, the player takes from the bank 1 resource for each adjacent tile that produces (Ruins gives nothing; the Storm does **not** block this). |
| R-3.8 | After the last setup Path, the start player begins **turn 1** in phase TurnStart. |

## R-4 Turn structure

| Id | Rule |
|---|---|
| R-4.1 | A turn is: **TurnStart** → (optional: play one Charter) → **Roll** → resolve production or 7 → **Main** (trade, build, buy, play one Charter if none was played yet this turn, any order, any number of times except where limited) → **End turn**. |
| R-4.2 | At TurnStart the player may play **one Charter of any playable type** (Warden, Road Crew, Bounty or Decree) before rolling, subject to the one-Charter-per-turn limit (R-8.3). After it is fully resolved they still must roll. |
| R-4.3 | The player must roll before doing anything else except playing a Charter. Trading, building and buying are never allowed before the roll. |
| R-4.4 | In Main the player may, in any order and repeatedly: domestic trade (R-9), maritime trade (R-10), build (R-6), buy Charters (R-8.2), play at most one non-Monument Charter for the whole turn (R-8.3). |
| R-4.5 | The player ends the turn explicitly. There is no turn timer. |
| R-4.6 | Victory is checked per R-12. |
| R-4.7 | **Classic phases (option, default off):** the Main phase is played as *trade first, then build*. Domestic and maritime trades are allowed only until the player's first purchase of the turn (BuildPath, BuildOutpost, BuildKeep or BuyCharter); after that, trading is closed for the rest of the turn (`TradingClosed`). Playing a Charter never closes trading, and free Paths from a Road Crew do not count as a purchase. With the option off (default), trading and building may be freely mixed. |

## R-5 Production

| Id | Rule |
|---|---|
| R-5.1 | When the dice total is not 7, every tile with that number **and not under the Storm** produces: each Outpost on its corners earns 1 of the tile's resource, each Keep earns 2. All players (not only the roller) collect. |
| R-5.2 | Production is computed for all tiles and players first, then paid out resource by resource. |
| R-5.3 | **Bank shortage:** for each resource separately, if the total owed to all players exceeds the bank's remaining stock of that resource: if exactly one player is owed that resource, that player receives the entire remaining stock; if two or more players are owed it, **nobody** receives any of that resource this roll. Other resources are unaffected. |
| R-5.4 | Players do not choose; production is automatic. |

## R-6 Building

| Id | Rule |
|---|---|
| R-6.1 | **Costs:** Path = 1 Timber + 1 Clay. Outpost = 1 Timber + 1 Clay + 1 Fiber + 1 Grain. Keep = 2 Grain + 3 Iron. Charter = 1 Fiber + 1 Grain + 1 Iron. Spent cards return to the bank. |
| R-6.2 | You can only build if you have the piece in your supply (R-1.5). |
| R-6.3 | **Distance rule:** an Outpost may only be placed on an empty corner whose every neighbouring corner (connected by one edge) is empty. Applies in setup and in play. |
| R-6.4 | **Outpost connection (normal play):** the corner must be an endpoint of at least one of your own Paths. |
| R-6.5 | **Path placement (normal play):** the edge must be empty and at least one endpoint corner must be either (a) occupied by your own Outpost/Keep, or (b) unoccupied by any building and an endpoint of another of your Paths. You may not extend a Path *through* a corner occupied by an opponent's building. |
| R-6.6 | **Keep:** replaces one of your Outposts (same corner). Returns the Outpost to your supply, uses one Keep from supply. |
| R-6.7 | Building is only allowed in Main (and setup per R-3, and free Paths per R-8.7, which may happen before the roll). |
| R-6.8 | Paths may be built on coastal edges and harbor edges normally. |

## R-7 Rolling a 7 — the Storm

| Id | Rule |
|---|---|
| R-7.1 | On a 7 nobody produces. |
| R-7.2 | **Discard:** every player (including the roller) holding **more than 7** resource cards must return ⌊cards / 2⌋ resource cards of their choice to the bank. Charters are not counted and never discarded. All affected players discard "simultaneously": the game waits until every affected player has chosen. Humans choose in the Discard dialog; in hot-seat, affected humans choose one at a time behind the privacy curtain (seat order starting from the roller). |
| R-7.3 | **Move the Storm:** after discards, the roller must move the Storm to a **different** tile (Ruins is allowed). |
| R-7.4 | **Plunder:** the roller then chooses one opponent who has an Outpost/Keep on a corner of the Storm's new tile **and** at least 1 resource card, and takes 1 random resource card from them. If exactly one such opponent exists, they are chosen automatically. If none exist, nothing is stolen. The stolen resource type is revealed only to the thief and the victim. |
| R-7.5 | A tile under the Storm produces nothing while the Storm stays there (R-5.1). |
| R-7.6 | After the plunder (or if none), play continues in Main. |
| R-7.7 | **Friendly Storm (option, default off):** the Storm may not be moved to a tile that has any building belonging to an **opponent of the mover** whose **public** VP ≤ 2, unless every tile other than the current one is excluded, in which case any tile other than the current one is allowed. Applies to 7s and Wardens. |

## R-8 Charters

| Id | Rule |
|---|---|
| R-8.1 | The deck holds 25 cards (R-1.4), shuffled at game start with the game RNG. |
| R-8.2 | **Buy:** in Main, pay 1 Fiber + 1 Grain + 1 Iron, take the top card into your hand (hidden from opponents; they only see your Charter count). Cannot buy if the deck is empty. Multiple buys per turn allowed. |
| R-8.3 | **One play per turn:** a player may play at most **one** Charter per turn (Warden, Road Crew, Bounty or Decree), at any point of their own turn: before rolling (TurnStart) or after (Main). Monuments are never "played" and do not count toward this limit. Charters can never be played during another player's turn, during setup, or while a Discard / MoveStorm / ChooseVictim / RoadCrew step or a trade offer is pending. |
| R-8.4 | **Not the turn you bought it:** a Charter cannot be played during the same turn it was bought. (If you hold another, older copy of the same type you may play that one.) |
| R-8.5 | **Warden:** may be played at TurnStart (before rolling) or in Main. Move the Storm per R-7.3 and plunder per R-7.4 (no discards). The card stays face up in front of the player (counts toward Strongest Guard). After resolution, return to the phase the card was played in (TurnStart → still must roll; Main → Main). |
| R-8.6 | **Monument:** worth 1 VP, hidden from opponents until the game ends (or until revealed to win). It is never played. It counts toward the owner's total VP immediately, including the turn it was bought (a Monument bought this turn can win the game). |
| R-8.7 | **Road Crew:** may be played at TurnStart or in Main. The player places up to 2 Paths at no cost, following R-6.5 and R-6.2. Playable only if the player has ≥ 1 Path in supply and ≥ 1 legal edge. They must place the first Path; after placing one, if they have a Path in supply and a legal edge remains (the first Path may have created new legal edges), they must place the second, otherwise the effect ends. While placing, no other action is allowed. Afterwards play returns to the phase the card was played in (TurnStart → still must roll). |
| R-8.8 | **Bounty:** may be played at TurnStart or in Main. Choose any 2 resources (may be the same) and take them from the bank; they may be used immediately. Only resources in stock may be chosen; if the bank has only 1 card total available the player takes that one; if the bank is totally empty the card cannot be played. |
| R-8.9 | **Decree:** may be played at TurnStart or in Main. Name one resource; every opponent gives you all resource cards of that type they hold (possibly zero). |
| R-8.10 | Wardens, Road Crews, Bounties and Decrees, once played, are out of the game (Wardens remain displayed for counting). |
| R-8.11 | Charters cannot be traded, given away or stolen; Charters are not discarded on a 7 and do not count toward the 7-card hand limit. |
| R-8.12 | The deck is never reshuffled or refilled. When it is empty no more Charters can be bought. Unplayed Charters stay in hand until played or until the game ends. |
| R-8.13 | A player may look at their own Charters at any time. Opponents see only how many unplayed Charters a player holds and how many Wardens they have played. |

## R-9 Domestic trade (between players)

| Id | Rule |
|---|---|
| R-9.1 | Only the **current player** may trade with other players, only during Main, and only with players they choose as targets. Non-current players never trade with each other. |
| R-9.2 | An **offer** specifies what the proposer gives (`Give`) and receives (`Get`). Both must contain ≥ 1 card (no gifts), and no resource may appear on both sides. The proposer must hold `Give` at proposal time. |
| R-9.3 | An offer is addressed to one or more opponents (targets). Each target responds **Accept**, **Decline**, or **Counter** (proposing alternative `Give'/Get'` terms that also satisfy R-9.2 and that the countering player can pay). A target may accept only if they hold the requested cards. |
| R-9.4 | The proposer may at any time **confirm** the trade with any one target who has Accepted (original terms) or Countered (counter terms, which the proposer must be able to pay), or **cancel** the offer. Only one exchange happens per offer. |
| R-9.5 | At confirmation both sides must still hold the cards; otherwise the confirmation is rejected. |
| R-9.6 | While an offer is open, the proposer may not take other actions except Confirm or Cancel. Opening a new offer requires the previous one to be closed. |
| R-9.7 | Any number of offers per turn (bots self-limit, see `04-AI.md`). |
| R-9.8 | Ending the turn while an offer is open first cancels the offer, then ends the turn. |

## R-10 Maritime trade (with the bank)

| Id | Rule |
|---|---|
| R-10.1 | In Main, the current player may give the bank N identical resource cards for 1 resource card of a **different** type, where N is: 2 if the player has a building on a 2:1 harbor corner of that resource; else 3 if on any Generic harbor corner; else 4. |
| R-10.2 | The bank must have the requested card in stock. |
| R-10.3 | Multiple maritime trades per turn are allowed; each action trades exactly one bundle. |
| R-10.4 | A harbor's rate applies as soon as the player has a building on one of its corners, including later in the same turn it was built (subject to R-4.7 when Classic phases is on). |

## R-11 Awards

| Id | Rule |
|---|---|
| R-11.1 | **Path length** = the number of edges in the longest simple trail (no edge reused; corners may be revisited, forks allowed—it is the longest trail through the player's Path graph) of the player's Paths, where the trail may not pass **through** a corner occupied by an opponent's building (it may end there). |
| R-11.2 | **Longest Path:** the first player to reach a path length ≥ 5 takes the award. Another player takes it only by exceeding the holder's length (ties keep the holder). |
| R-11.3 | Recompute all players' lengths after every Path or Outpost placement (an opponent's Outpost can break a Path). If the holder's length drops: if the holder is still (possibly tied) the longest with ≥ 5, they keep it; otherwise, if exactly one player has the longest length ≥ 5, that player takes it; otherwise (tie among non-holders, or no one ≥ 5) the award is set aside (nobody holds it) until a single player has the longest length ≥ 5. |
| R-11.4 | **Strongest Guard:** the first player to have played ≥ 3 Wardens takes it. Another player takes it only by exceeding the holder's count. |
| R-11.5 | Each award is worth 2 VP to its holder and counts toward victory immediately. |

## R-12 Victory

| Id | Rule |
|---|---|
| R-12.1 | VP = 1 per Outpost + 2 per Keep + 2 per held award + 1 per Monument held. Victory target default 10 (configurable 8–15). |
| R-12.2 | A player wins **only during their own turn**, the moment their total VP (including hidden Monuments) reaches the target. Check after every action the current player performs and at their TurnStart. |
| R-12.3 | If a player reaches the target outside their own turn (e.g., an award transfers to them while another player is acting), they win at the start of their next turn (TurnStart check). |
| R-12.4 | On victory the game ends immediately; all Monuments are revealed; the game-over screen shows the breakdown. |

## R-13 Information visibility

| Id | Rule |
|---|---|
| R-13.1 | Public: board, pieces, Storm, every player's resource-card **count**, Charter-hand **count**, played Wardens, awards, public VP, dice, bank stock, deck size, all trades (terms), discards' counts and types, Decree/Bounty results, maritime trades. |
| R-13.2 | Private: the exact contents of each hand (resource types) and the types of unplayed Charters (owner only). Stolen card type: thief and victim only. Bought Charter type: buyer only. |
| R-13.3 | Bots may only use public information plus their own hand, and may track opponents' hands by observing public events (card counting is allowed, like a human with perfect memory). |

## R-14 Edge-case FAQ (normative)

1. **7 rolled when nobody can be robbed:** discards still happen, Storm still must move, no plunder.
2. **Storm moved to a tile where only the roller has buildings:** no plunder.
3. **Warden at TurnStart then rolling a 7:** both resolve fully (Storm moves twice).
3a. **Any Charter before the roll:** Road Crew, Bounty and Decree may also be played before rolling (R-4.2). A Road Crew that completes the Longest Path and reaches the victory target wins before the dice are rolled.
3b. **Charter played before the roll, then another after:** not allowed — one per turn in total (R-8.3).
4. **Warden while the Storm is the only legal move restriction (Friendly Storm):** apply R-7.7 fallback.
5. **Discard with exactly 7 cards:** no discard. With 9 cards: discard 4.
6. **Buying a Monument and reaching the target:** immediate win (R-8.6, R-12.2).
7. **Two Charters bought this turn, an old Warden held:** may play the old Warden; the new cards wait.
8. **Road Crew with 1 Path piece left:** places 1.
9. **Road Crew placing Paths that create Longest Path:** award resolves after each placement; may trigger victory immediately.
10. **Bank empty of Grain, player rolls an 8 producing only Grain for two players:** nobody gets Grain (R-5.3). One player only: they get the remaining stock (possibly 0).
11. **Maritime trade for a resource the bank lacks:** not allowed.
12. **Bounty when bank has 0 of everything:** not playable. When it has exactly 1 card total: take that 1.
13. **Decree naming a resource nobody has:** legal, gains 0.
14. **Building an Outpost that splits an opponent's Path:** recompute Longest Path per R-11.3; the builder may gain the award if they become uniquely longest; holder may lose it.
15. **Keep upgrade with 0 Keeps left:** not allowed even with resources.
16. **Outpost with 0 Outposts left but a Keep upgrade frees one:** after upgrading you may build an Outpost with the returned piece.
17. **Trade where either side can no longer pay at confirmation:** the confirmation is rejected and nothing changes (the offer stays open). In normal play this cannot happen because hands cannot change while an offer is open.
18. **End turn with an open offer:** the offer is cancelled, then the turn ends (R-9.8).
19. **Path length counts through your own buildings:** yes. Through an opponent's: no (trail breaks there).
20. **Award ties at the moment of first reaching threshold simultaneously:** cannot happen for Strongest Guard (one play per action). For Longest Path, if an Outpost placement breaks the holder and two others tie ≥ 5, set aside (R-11.3).
21. **Setup: two corners adjacent to the Storm's tile (Ruins):** placement is legal; Ruins produce nothing anyway.
22. **Discarding is chosen by the player, not random.** Bots choose by their utility (04-AI).
23. **A player with 0 cards is never a plunder candidate.**
24. **Rolling at TurnStart is mandatory:** the turn cannot end before rolling.
25. **Victory check at TurnStart** happens before anything else in the turn (R-12.3).
