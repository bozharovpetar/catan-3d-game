# 03 — Core API (Hexhaven.Core)

Everything here is **normative**: names, signatures and semantics must match exactly, because `tools/sim/Sim.cs`, the tests and the Runtime layer are written against them. You may add private helpers and extra public members, but must not rename or remove anything listed.

Namespace for everything below: `Hexhaven.Core` (AI types: `Hexhaven.Core.AI`). C# 7.2 only.

---

## 1. Primitives

```csharp
public enum Resource { Timber = 0, Clay = 1, Fiber = 2, Grain = 3, Iron = 4 }
public enum Terrain { Forest = 0, ClayPit = 1, FlaxField = 2, Farmland = 3, Mine = 4, Ruins = 5 }
public enum BuildingType { None = 0, Outpost = 1, Keep = 2 }
public enum PieceType { Path = 0, Outpost = 1, Keep = 2 }
public enum HarborType { None = 0, Generic = 1, Timber = 2, Clay = 3, Fiber = 4, Grain = 5, Iron = 6 }
public enum CharterType { Warden = 0, Monument = 1, RoadCrew = 2, Bounty = 3, Decree = 4 }
public enum Phase { RollForOrder, SetupOutpost, SetupPath, TurnStart, Discard, MoveStorm, ChooseVictim, Main, RoadCrew, GameOver }
public enum BoardMode { BalancedRandom = 0, Random = 1, Beginner = 2 }
public enum BotDifficulty { Easy = 0, Normal = 1, Hard = 2 }
public enum TradeResponse { Pending = 0, Accepted = 1, Declined = 2, Countered = 3 }
public enum AwardType { LongestPath = 0, StrongestGuard = 1 }
```

```csharp
public static class ResourceInfo
{
    public const int Count = 5;
    public static readonly Resource[] All;                 // Timber..Iron in enum order
    public static string Name(Resource r);                 // "Timber", "Clay", "Fiber", "Grain", "Iron"
    public static char Letter(Resource r);                 // 'T','C','F','G','I'
}

public static class TerrainInfo
{
    /// <summary>Resource produced by a terrain; false for Ruins.</summary>
    public static bool TryGetResource(Terrain t, out Resource r);
    public static string Name(Terrain t);                  // "Forest","Clay Pit","Flax Field","Farmland","Mine","Ruins"
}

public static class HarborInfo
{
    public static bool IsSpecific(HarborType h);           // Timber..Iron
    public static Resource ToResource(HarborType h);       // throws for None/Generic
    public static HarborType FromResource(Resource r);
    public static string Label(HarborType h);              // "3:1" or "2:1 Timber" etc.
}
```

### 1.1 `ResourceBag` (struct, value semantics)

```csharp
public struct ResourceBag : System.IEquatable<ResourceBag>
{
    public int Timber, Clay, Fiber, Grain, Iron;
    public int this[Resource r] { get; set; }
    public int this[int i] { get; set; }                   // 0..4
    public int Total { get; }
    public bool IsEmpty { get; }                           // Total == 0 and none negative
    public bool AnyNegative { get; }
    public static readonly ResourceBag Zero;
    public static ResourceBag Of(int timber, int clay, int fiber, int grain, int iron);
    public static ResourceBag Single(Resource r, int count = 1);
    public bool Contains(ResourceBag other);               // every component >= other's
    public bool SharesResourceWith(ResourceBag other);     // some r with this[r] > 0 && other[r] > 0
    public int DistinctTypes { get; }
    public static ResourceBag operator +(ResourceBag a, ResourceBag b);
    public static ResourceBag operator -(ResourceBag a, ResourceBag b);
    public static bool operator ==(ResourceBag a, ResourceBag b);
    public static bool operator !=(ResourceBag a, ResourceBag b);
    public string ToCodec();                               // "t,c,f,g,i" e.g. "1,0,2,0,3"
    public static ResourceBag FromCodec(string s);
    public override string ToString();                     // human: "1 Timber, 2 Fiber" or "nothing"
}

public static class Costs
{
    public static readonly ResourceBag Path    = ResourceBag.Of(1, 1, 0, 0, 0);
    public static readonly ResourceBag Outpost = ResourceBag.Of(1, 1, 1, 1, 0);
    public static readonly ResourceBag Keep    = ResourceBag.Of(0, 0, 0, 2, 3);
    public static readonly ResourceBag Charter = ResourceBag.Of(0, 0, 1, 1, 1);
}

public static class Limits
{
    public const int BankPerResource = 19, Paths = 15, Outposts = 5, Keeps = 4;
    public const int HandLimit = 7, LongestPathMin = 5, StrongestGuardMin = 3, AwardPoints = 2;
    public const int Wardens = 14, Monuments = 5, RoadCrews = 2, Bounties = 2, Decrees = 2; // 25
}
```

### 1.2 `HexMath`

```csharp
public static class HexMath
{
    public const double Sqrt3 = 1.7320508075688772;
    public const double HexSize = 1.0;                     // centre-to-corner, world units
    /// Axial (q,r) pointy-top → world XZ:  x = √3·(q + r/2),  z = −1.5·r
    public static void AxialToWorld(int q, int r, out double x, out double z);
    /// Corner i (0..5) of a tile is at angle (60·i − 30)° from the centre: (x + cos a, z + sin a)
    public static void Corner(double cx, double cz, int i, out double x, out double z);
    /// Neighbour across side s (side s spans corner s → s+1, outward angle 60·s°):
    public static readonly int[] SideDq = { 1, 1, 0, -1, -1, 0 };
    public static readonly int[] SideDr = { 0, -1, -1, 0, 1, 1 };
    public static int Pips(int number);                    // 0 for 0/7/out of range; else 6 − |7 − n|
}
```

## 2. Board and id conventions (exact — save files depend on them)

```csharp
public sealed class Tile
{
    public int Id, Q, R;
    public Terrain Terrain;
    public int Number;                       // 0 for Ruins
    public double X, Z;                      // world centre
    public readonly int[] Corners = new int[6];   // vertex id of corner i
    public readonly int[] Sides = new int[6];     // edge id of side s (corner s → corner s+1)
    public readonly List<int> Neighbors;     // tile ids, ordered by side index (only existing ones)
    public bool TryGetResource(out Resource r);
}

public sealed class Vertex
{
    public int Id;
    public double X, Z;
    public readonly List<int> Tiles;         // tile ids in discovery order
    public readonly List<int> Edges;         // edge ids in creation order
    public readonly List<int> Neighbors;     // vertex ids, parallel to Edges (Neighbors[k] is the other end of Edges[k])
    public HarborType Harbor = HarborType.None;
    public BuildingType Building = BuildingType.None;
    public int Owner = -1;
}

public sealed class Edge
{
    public int Id, A, B;                     // vertex ids, A = corner s, B = corner s+1 of the tile that created it
    public readonly List<int> Tiles;         // 1 (coastal) or 2
    public int Owner = -1;
    public int HarborSlot = -1;
    public int Other(int vertex);
}

public sealed class Harbor
{
    public int Slot, TileId, Side, EdgeId;
    public HarborType Type;
    public double X, Z;                      // marker position: edge midpoint + 0.45·outward
    public double OutX, OutZ;                // unit outward vector = (cos 60s°, sin 60s°)
}

public sealed class Board
{
    public readonly List<Tile> Tiles;        // 19
    public readonly List<Vertex> Vertices;   // 54
    public readonly List<Edge> Edges;        // 72
    public readonly List<Harbor> Harbors;    // 9, by slot
    public int RuinsTile { get; }
    public Tile TileAt(int q, int r);        // null if off-board
    public int EdgeBetween(int vA, int vB);  // -1 if none
    public static Board CreateGeometry();    // ids + adjacency + harbor slots, no terrain/numbers/types yet
}

public static class BoardGenerator
{
    /// Fills terrains, numbers and harbor types per 01-RULES R-2 using rng. Beginner consumes no RNG.
    public static Board Generate(BoardMode mode, Pcg32 rng);
    public static bool SatisfiesC1(Board b); public static bool SatisfiesC2(Board b);
    public static bool SatisfiesC3(Board b); public static bool SatisfiesC4(Board b); public static bool SatisfiesC5(Board b);
}
```

**Id assignment algorithm (must be exactly this):**
1. Tiles: for q = −2..2 ascending, for r = max(−2, −q−2)..min(2, −q+2) ascending → id = running index. (Yields the order in the Beginner table of 01-RULES.)
2. Vertices: for each tile in id order, for corner i = 0..5: compute position; key = (round(x·1000), round(z·1000)); if new, assign next vertex id. Append tile id to `Vertex.Tiles`.
3. Edges: for each tile in id order, for side s = 0..5: endpoints a = Corners[s], b = Corners[(s+1)%6]; key = (min, max); if new, create edge with `A = a, B = b`, next id; append edge id to both vertices' `Edges` and the opposite vertex to `Neighbors`. Append tile id to `Edge.Tiles`; set `Tile.Sides[s]`.
4. Tile neighbours: for s = 0..5, if TileAt(q + SideDq[s], r + SideDr[s]) exists, append its id.
5. Harbors: the 9 slots of 01-RULES (slot order), `EdgeId = TileAt(q,r).Sides[side]`; set `Edge.HarborSlot`; after types are assigned set `Vertex.Harbor` on both endpoints.

Resulting counts: 19 / 54 / 72 / 9 (tested).

**RNG consumption order in `NewGame`:** (1) `BoardGenerator.Generate` (terrain shuffles, token shuffles, then harbor-type shuffle); (2) charter deck shuffle; (3) roll-off dice as players roll; (4) game dice / plunder picks as they happen. Initial deck order before shuffle: 14 Warden, 5 Monument, 2 RoadCrew, 2 Bounty, 2 Decree. Harbor types before shuffle: Generic×4, Timber, Clay, Fiber, Grain, Iron. Terrains before shuffle: the R-1.1 order. Tokens before shuffle: the R-1.2 order.

## 3. Configuration

```csharp
public sealed class PlayerConfig
{
    public string Name;
    public bool IsBot;
    public BotDifficulty Difficulty = BotDifficulty.Normal;   // ignored for humans
}

public sealed class GameConfig
{
    public int Seed;
    public BoardMode BoardMode = BoardMode.BalancedRandom;
    public int VictoryTarget = 10;                            // 8..15
    public bool FriendlyStorm = false;
    public List<PlayerConfig> Players = new List<PlayerConfig>();   // 3 or 4; seat = index
    /// seat 0 human named "You", others bots "Bryn","Corra","Dain" (Normal)
    public static GameConfig Default(int seed, int playerCount = 4);
    public string Validate();                                 // null if ok, else message
    public GameConfig Clone();
}
```

## 4. State

```csharp
public sealed class CharterCard
{
    public CharterType Type;
    public int BoughtOnTurn;                 // GameState.TurnNumber at purchase
}

public sealed class PlayerState
{
    public int Seat;
    public string Name;
    public bool IsBot;
    public BotDifficulty Difficulty;
    public ResourceBag Hand;
    public readonly List<CharterCard> Charters;      // unplayed, in purchase order (includes Monuments)
    public int WardensPlayed;
    public int PathsLeft = 15, OutpostsLeft = 5, KeepsLeft = 4;
    public int PathLength;                            // cached longest trail (R-11.1)
    public int PendingDiscard;                        // > 0 only during Discard phase
    public int RollOffTotal;                          // last roll-off total, 0 if not rolled in current round
    public PlayerStats Stats;
}

public sealed class PlayerStats
{
    public ResourceBag Produced;             // from production + starting resources
    public ResourceBag Spent;                // building/charters
    public ResourceBag TradedAway, TradedFor;// domestic + maritime
    public ResourceBag LostToDiscard, LostToPlunder, GainedFromPlunder, GainedFromCharters;
    public int TradesCompleted, PlundersMade, TimesPlundered, ChartersBought;
}

public sealed class TradeOffer
{
    public int Id;
    public int Proposer;
    public ResourceBag Give;                 // proposer gives
    public ResourceBag Get;                  // proposer receives
    public int TargetMask;                   // bit i set = seat i addressed
    public readonly TradeResponse[] Responses;      // per seat (non-targets stay Declined)
    public readonly ResourceBag[] CounterGive;      // per seat; counter terms, PROPOSER perspective
    public readonly ResourceBag[] CounterGet;
    public bool IsTarget(int seat);
}

public sealed class GameState
{
    public GameConfig Config;
    public Board Board;
    public PlayerState[] Players;
    public ResourceBag Bank;                         // starts 19 each
    public List<CharterType> Deck;                   // draw from the END (last element = top)
    public Pcg32 Rng;

    public Phase Phase;
    public int Current;                              // seat whose turn/setup step it is (roll-off: next roller)
    public int StartPlayer = -1;
    public int TurnNumber;                           // 0 during roll-off/setup; 1 = first normal turn
    public int Die1, Die2;                           // last game roll (0 before first)
    public int StormTile;
    public int LongestPathHolder = -1, StrongestGuardHolder = -1;
    public int Winner = -1;

    public List<int> RollOffQueue;                   // seats still to roll in this roll-off round
    public int[] SetupOrder;                         // length 2N, snake order from StartPlayer
    public int SetupIndex;                           // index into SetupOrder
    public int LastSetupOutpost = -1;

    public Phase StormReturnPhase = Phase.Main;      // where MoveStorm/ChooseVictim return (TurnStart for pre-roll Warden)
    public bool CharterPlayedThisTurn;
    public int PlayedOtherCharters;                  // Road Crew + Bounty + Decree played (for invariant 2)
    public int RoadCrewRemaining;                    // free paths left in RoadCrew phase
    public TradeOffer ActiveTrade;                   // null when none
    public int NextTradeId = 1;

    public readonly List<GameAction> ActionLog;      // every successfully applied action
    public int EventSeq;                             // last event sequence number
    public readonly int[] DiceHistogram = new int[13];   // index = total (2..12), game rolls only

    public PlayerState CurrentPlayer { get; }
    public int PlayerCount { get; }
}
```

## 5. Phase machine

`GameEngine.AwaitingInput()` returns the seats that must act now, in the order a single-threaded driver should ask them.

| Phase | AwaitingInput | Legal actions | Transition |
|---|---|---|---|
| RollForOrder | `[RollOffQueue[0]]` | `RollForOrder` | After the queue empties: unique max → `StartPlayer`, build `SetupOrder`, `Current = SetupOrder[0]`, → SetupOutpost. Tie → queue = tied seats ascending, reset their `RollOffTotal`, emit `RollOffTie`. |
| SetupOutpost | `[Current]` | `PlaceSetupOutpost` | → SetupPath (`LastSetupOutpost = v`). Round-2 outposts grant starting resources (R-3.7). |
| SetupPath | `[Current]` | `PlaceSetupPath` | `SetupIndex++`; if `< 2N` → `Current = SetupOrder[SetupIndex]`, SetupOutpost; else `TurnNumber = 1`, `Current = StartPlayer`, → TurnStart. |
| TurnStart | `[Current]` | `RollDice`, `PlayWarden` | **Beginning a new turn** (only via EndTurn → next seat, or after the final setup Path — never when MoveStorm/ChooseVictim returns to TurnStart after a pre-roll Warden): reset `CharterPlayedThisTurn = false`, reset bot per-turn counters, emit `TurnStarted`, **victory check** (R-12.3). RollDice: 7 → set `PendingDiscard`; any > 0 → Discard else MoveStorm; `StormReturnPhase = Main`. Non-7 → produce → Main. PlayWarden → `StormReturnPhase = TurnStart`, → MoveStorm. |
| Discard | seats with `PendingDiscard > 0`, ordered starting at `Current` going clockwise | `Discard` | When all zero → MoveStorm. |
| MoveStorm | `[Current]` | `MoveStorm` | Candidates (R-7.4): 0 → emit `NoPlunder`, → `StormReturnPhase`; 1 → auto plunder, → `StormReturnPhase`; ≥2 → ChooseVictim. |
| ChooseVictim | `[Current]` | `ChooseVictim` | Plunder → `StormReturnPhase`. |
| Main | if `ActiveTrade != null`: targets with `Pending` (clockwise from Current+1), then `Current`; else `[Current]` | all Main actions (§6); with an open trade the proposer may only `ConfirmTrade` / `CancelTrade` / `EndTurn` (auto-cancels), targets only `RespondTrade` | `EndTurn` → `TurnNumber++`, `Current = next`, → TurnStart. `PlayRoadCrew` → RoadCrew. `PlayWarden` → `StormReturnPhase = Main`, MoveStorm. |
| RoadCrew | `[Current]` | `PlaceFreePath` | After each placement `RoadCrewRemaining--`; when 0, or no Path in supply, or no legal edge → set `RoadCrewRemaining = 0`, → Main. On victory during RoadCrew also set it to 0. (If no legal edge exists at play time, `PlayRoadCrew` is illegal.) |
| GameOver | `[]` | none | terminal |

Victory check (R-12.2) runs after **every** successful action whose actor is `Current` (and when a new turn begins). When it triggers: `Winner = Current`, emit `MonumentsRevealed` then `GameWon`, `Phase = GameOver`.

## 6. Actions

```csharp
public enum ActionType
{
    RollForOrder, PlaceSetupOutpost, PlaceSetupPath, RollDice, Discard, MoveStorm, ChooseVictim,
    BuildPath, BuildOutpost, BuildKeep, BuyCharter,
    PlayWarden, PlayRoadCrew, PlayBounty, PlayDecree, PlaceFreePath,
    MaritimeTrade, ProposeTrade, RespondTrade, ConfirmTrade, CancelTrade,
    EndTurn
}

public sealed class GameAction
{
    public ActionType Type;
    public int Player;                        // acting seat
    public int Vertex = -1, Edge = -1, Tile = -1, Target = -1, TradeId = -1, TargetMask = 0;
    public Resource Resource, Resource2;
    public ResourceBag Give, Get;
    public TradeResponse Response;

    public static GameAction RollForOrder(int p);
    public static GameAction PlaceSetupOutpost(int p, int vertex);
    public static GameAction PlaceSetupPath(int p, int edge);
    public static GameAction RollDice(int p);
    public static GameAction Discard(int p, ResourceBag cards);              // uses Give
    public static GameAction MoveStorm(int p, int tile);
    public static GameAction ChooseVictim(int p, int target);
    public static GameAction BuildPath(int p, int edge);
    public static GameAction BuildOutpost(int p, int vertex);
    public static GameAction BuildKeep(int p, int vertex);
    public static GameAction BuyCharter(int p);
    public static GameAction PlayWarden(int p);
    public static GameAction PlayRoadCrew(int p);
    public static GameAction PlayBounty(int p, ResourceBag take);           // uses Get
    public static GameAction PlayDecree(int p, Resource r);
    public static GameAction PlaceFreePath(int p, int edge);
    public static GameAction MaritimeTrade(int p, Resource give, Resource get);
    public static GameAction ProposeTrade(int p, ResourceBag give, ResourceBag get, int targetMask);
    public static GameAction RespondTrade(int p, int tradeId, TradeResponse response, ResourceBag counterGive, ResourceBag counterGet); // counter terms in PROPOSER perspective; ignored unless Countered
    public static GameAction ConfirmTrade(int p, int tradeId, int partner);  // uses Target
    public static GameAction CancelTrade(int p, int tradeId);
    public static GameAction EndTurn(int p);

    public string ToCodec();                  // see 08-PERSISTENCE
    public static GameAction FromCodec(string line);
    public GameAction Clone();
}
```

### 6.1 Validation and effects (check in this order; first failure wins)

Common checks for every action: `GameOver` → `Errors.GameOver`; `Player` not in `AwaitingInput()` → `Errors.NotYourTurn`; action type not legal in current phase → `Errors.WrongPhase`; while `ActiveTrade != null`, the proposer may only Confirm/Cancel/**EndTurn** (EndTurn auto-cancels the offer) and others only Respond → `Errors.TradePending`.

| Action | Specific validation (error code) | Effects & events (in order) |
|---|---|---|
| RollForOrder | — | roll 2 dice → `RollOffTotal`; `RollOffRoll`; pop queue; resolve per §5 (`RollOffTie` or `StartPlayerChosen` + `PhaseChanged`). |
| PlaceSetupOutpost | vertex in range (`BadId`); empty (`Occupied`); distance rule (`DistanceRule`) | place Outpost, `OutpostsLeft--`; `Built{Free=true}`; if `SetupIndex >= N` grant starting resources from bank → `StartingResources`; recompute paths/awards; `PhaseChanged(SetupPath)`. |
| PlaceSetupPath | edge in range (`BadId`); empty (`Occupied`); touches `LastSetupOutpost` (`NotConnected`) | place, `PathsLeft--`; `Built{Free=true}`; recompute awards; advance per §5. |
| RollDice | — | dice; `DiceHistogram[total]++`; `DiceRolled`; 7 → for each seat with Hand.Total > 7: `PendingDiscard = Total/2`, `DiscardRequired`; phase change. Else production: for each tile with the number: under Storm → `ProductionBlocked`; else accumulate owed per (seat, resource, tile). Apply R-5.3 per resource: full payout → one `Produced{Player,Tile,Resource,Value}` per (seat,tile) contribution; none → `ShortageNoPayout{Resource}`; single owed player short → `ShortagePartial{Player,Resource,Value=paid}` (pay remaining stock, emit no `Produced` for that resource). → Main. |
| Discard | `Give.Total == PendingDiscard` and no negatives (`InvalidDiscard`); `Hand.Contains(Give)` (`CannotAfford`) | hand −, bank +; `PendingDiscard = 0`; `Discarded`; if none pending → MoveStorm. |
| MoveStorm | tile in range (`BadId`); ≠ StormTile (`SameTile`); Friendly Storm restriction (`StormProtected`) | `StormMoved{Tile=to, Value=from}`; candidates per R-7.4 → auto/none/ChooseVictim (§5). Auto plunder emits `Plundered`. |
| ChooseVictim | target is a candidate (`InvalidVictim`) | take random card (`Rng.NextInt(total)` over hand in enum order) → `Plundered{Player,Other,Resource}` visible to thief+victim; → StormReturnPhase. |
| BuildPath | edge range; empty (`Occupied`); `PathsLeft > 0` (`NoPiecesLeft`); afford (`CannotAfford`); R-6.5 (`NotConnected`) | pay to bank; place; `Built`; recompute awards (may emit `AwardChanged`); victory check. |
| BuildOutpost | vertex range; empty (`Occupied`); distance (`DistanceRule`); `OutpostsLeft > 0` (`NoPiecesLeft`); afford; R-6.4 (`NotConnected`) | pay; place; `Built`; recompute awards (can break others' paths); victory check. |
| BuildKeep | own Outpost there (`NotYourOutpost`); `KeepsLeft > 0` (`NoPiecesLeft`); afford | pay; Keep; `KeepsLeft--`, `OutpostsLeft++`; `Built{Piece=Keep}`; victory check. |
| BuyCharter | deck non-empty (`DeckEmpty`); afford | pay; pop top; add `CharterCard{BoughtOnTurn=TurnNumber}`; `ChartersBought++`; `CharterBought{Charter}` visible to buyer only (others see count); victory check (Monument). |
| PlayWarden | phase TurnStart or Main; `!CharterPlayedThisTurn` (`CharterAlreadyPlayed`); has a Warden with `BoughtOnTurn < TurnNumber` (`NoPlayableCharter`) | remove oldest eligible Warden; `WardensPlayed++`; `CharterPlayedThisTurn = true`; `CharterPlayed`; recompute Strongest Guard (`AwardChanged`); `StormReturnPhase = phase`; → MoveStorm; victory check. |
| PlayRoadCrew | Main; once-per-turn; playable card; `PathsLeft > 0` and ≥ 1 legal free edge (`NoLegalPlacement`) | remove card; flag; `CharterPlayed`; `RoadCrewRemaining = min(2, PathsLeft)`; `RoadCrewStarted`; → RoadCrew. |
| PlaceFreePath | phase RoadCrew; edge legal per R-6.5 (`NotConnected`/`Occupied`) | place free; `Built{Free=true}`; awards; victory check; decrement; maybe → Main. |
| PlayBounty | Main; once-per-turn; playable; bank total > 0 (`BankEmpty`); `Get.Total == min(2, Bank.Total)` and `Bank.Contains(Get)` and no negatives (`InvalidBounty`) | remove card; flag; `CharterPlayed`; bank → hand; `BountyTaken{Bag}`; victory check (no VP change, harmless). |
| PlayDecree | Main; once-per-turn; playable | remove card; flag; `CharterPlayed`; take all of `Resource` from every opponent → `DecreeResolved{Amounts[seat], Value=total}`. |
| MaritimeTrade | Main; `Resource != Resource2` (`InvalidTrade`); `Hand[give] >= rate` (`CannotAfford`); `Bank[get] >= 1` (`BankEmpty`) | hand −rate give, +1 get; bank inverse; `MaritimeTraded{Bag=gave,Bag2=got}`. |
| ProposeTrade | Main; no open trade (`TradePending`); `Give`, `Get` non-empty, non-negative, not sharing a resource (`InvalidTrade`); `TargetMask` ⊆ opponents and ≠ 0 (`InvalidTrade`); `Hand.Contains(Give)` (`CannotAfford`) | create offer (Id = NextTradeId++), responses Pending for targets, Declined otherwise; `TradeProposed`. |
| RespondTrade | open trade with that id (`NoSuchTrade`); responder is a Pending target (`NotYourTurn`); Accept requires responder hand ⊇ offer.Get (`CannotAfford`); Counter requires valid terms per R-9.2 (`InvalidTrade`) and responder hand ⊇ counterGet (`CannotAfford`) | set response (store counter terms); `TradeResponded`. |
| ConfirmTrade | proposer only; trade id (`NoSuchTrade`); partner Accepted or Countered (`PartnerNotReady`); terms = original or counter; proposer hand ⊇ terms.Give (`CannotAfford`); partner hand ⊇ terms.Get (`PartnerCannotPay`) — both checked in `Validate`, failures change nothing (unreachable in normal play because hands cannot change while an offer is open; kept as a safety check) | exchange; stats; `TradeCompleted`; `ActiveTrade = null`. |
| CancelTrade | proposer; trade id | `TradeCancelled`; `ActiveTrade = null`. |
| EndTurn | Main; if `ActiveTrade != null` it is auto-cancelled first (emit `TradeCancelled`) | `TurnEnded`; `TurnNumber++`; `Current = (Current+1) % N`; enter TurnStart (emits `TurnStarted`, victory check). |

```csharp
public static class Errors
{
    public const string GameOver = "GameOver", NotYourTurn = "NotYourTurn", WrongPhase = "WrongPhase",
        BadId = "BadId", Occupied = "Occupied", DistanceRule = "DistanceRule", NotConnected = "NotConnected",
        NoPiecesLeft = "NoPiecesLeft", CannotAfford = "CannotAfford", NotYourOutpost = "NotYourOutpost",
        SameTile = "SameTile", StormProtected = "StormProtected", InvalidVictim = "InvalidVictim",
        InvalidDiscard = "InvalidDiscard", DeckEmpty = "DeckEmpty", CharterAlreadyPlayed = "CharterAlreadyPlayed",
        NoPlayableCharter = "NoPlayableCharter", NoLegalPlacement = "NoLegalPlacement", BankEmpty = "BankEmpty",
        InvalidBounty = "InvalidBounty", InvalidTrade = "InvalidTrade", TradePending = "TradePending",
        NoSuchTrade = "NoSuchTrade", PartnerNotReady = "PartnerNotReady", PartnerCannotPay = "PartnerCannotPay";
}

public sealed class ApplyResult
{
    public bool Ok;
    public string Error;                       // null when fully successful
    public List<GameEvent> Events;             // empty on failure
    public static ApplyResult Fail(string error);
}
```

## 7. Events

```csharp
public enum EventType
{
    GameStarted, RollOffRoll, RollOffTie, StartPlayerChosen, PhaseChanged, TurnStarted, TurnEnded,
    DiceRolled, Produced, ProductionBlocked, ShortageNoPayout, ShortagePartial, StartingResources,
    DiscardRequired, Discarded, StormMoved, Plundered, NoPlunder,
    Built, CharterBought, CharterPlayed, BountyTaken, DecreeResolved, RoadCrewStarted,
    MaritimeTraded, TradeProposed, TradeResponded, TradeCompleted, TradeCancelled,
    AwardChanged, MonumentsRevealed, GameWon
}

public sealed class GameEvent
{
    public int Seq;                            // strictly increasing per game
    public EventType Type;
    public int Player = -1, Other = -1, Tile = -1, Vertex = -1, Edge = -1;
    public int Die1, Die2, Value, Mask;
    public ResourceBag Bag, Bag2;
    public Resource Resource;
    public CharterType Charter;
    public AwardType Award;
    public PieceType Piece;
    public Phase Phase;
    public TradeResponse Response;
    public bool Free;
    public int[] Amounts;                      // per seat when relevant
    public int VisibleMask = -1;               // -1 = public; else bitmask of seats that see private fields
    public bool IsVisibleTo(int seat);
    /// Copy with private fields (Resource for Plundered, Charter for CharterBought) blanked for non-entitled viewers; HiddenValue flag set.
    public GameEvent RedactFor(int seat);
    public bool Redacted;
}
```

| EventType | Fields used |
|---|---|
| GameStarted | Value = player count |
| RollOffRoll | Player, Die1, Die2 |
| RollOffTie | Mask = tied seats |
| StartPlayerChosen | Player |
| PhaseChanged | Phase, Player (= Current) |
| TurnStarted | Player, Value = TurnNumber |
| TurnEnded | Player |
| DiceRolled | Player, Die1, Die2 |
| Produced | Player, Tile, Resource, Value (amount from that tile) |
| ProductionBlocked | Tile |
| ShortageNoPayout | Resource |
| ShortagePartial | Player, Resource, Value (amount actually paid) |
| StartingResources | Player, Bag |
| DiscardRequired | Player, Value (count) |
| Discarded | Player, Bag |
| StormMoved | Player, Tile (to), Value (from) |
| Plundered | Player (thief), Other (victim), Resource (**private**: VisibleMask = thief\|victim) |
| NoPlunder | Player |
| Built | Player, Piece, Vertex or Edge, Free |
| CharterBought | Player, Charter (**private**: VisibleMask = buyer) |
| CharterPlayed | Player, Charter |
| BountyTaken | Player, Bag |
| DecreeResolved | Player, Resource, Amounts[seat] taken, Value total |
| RoadCrewStarted | Player, Value (free paths) |
| MaritimeTraded | Player, Bag (gave), Bag2 (got) |
| TradeProposed | Player, Value (trade id), Bag (give), Bag2 (get), Mask (targets) |
| TradeResponded | Player (responder), Value (trade id), Response, Bag/Bag2 (counter terms, proposer perspective) |
| TradeCompleted | Player (proposer), Other (partner), Bag (proposer gave), Bag2 (proposer got), Value (trade id) |
| TradeCancelled | Player, Value (trade id) |
| AwardChanged | Award, Player (new holder or −1), Other (previous holder or −1), Value (length or wardens) |
| MonumentsRevealed | Amounts[seat] = Monuments held |
| GameWon | Player, Value (total VP) |

## 8. Engine

```csharp
public sealed partial class GameEngine
{
    public GameState State { get; }

    public static GameEngine NewGame(GameConfig config);          // validates config (throws ArgumentException), generates board, shuffles deck, Phase = RollForOrder, RollOffQueue = all seats, emits GameStarted then PhaseChanged(RollForOrder) (retrievable via TakeInitialEvents)
    public List<GameEvent> TakeInitialEvents();
    public static GameEngine Replay(GameConfig config, IList<GameAction> actions);   // throws InvalidOperationException if any action fails
    public GameEngine Clone();                                    // deep copy (AI lookahead); clone does not share mutable state

    public List<int> AwaitingInput();
    public string Validate(GameAction a);                         // null if legal (pure; no mutation)
    public ApplyResult Apply(GameAction a);

    // ---- queries (pure) ----
    public int VictoryPoints(int seat, bool includeHidden);       // includeHidden=false → public VP
    public int MonumentCount(int seat);
    public int MaritimeRate(int seat, Resource give);             // 2/3/4
    public bool HasHarbor(int seat, HarborType type);
    public bool CanAfford(int seat, ResourceBag cost);
    public bool IsOpenSite(int vertex);                           // empty + distance rule
    public List<int> LegalSetupOutposts();
    public List<int> LegalSetupPaths();
    public List<int> LegalPaths(int seat, bool ignoreCost);       // R-6.5 + supply (+ cost unless ignored)
    public List<int> LegalOutposts(int seat, bool ignoreCost);
    public List<int> LegalKeeps(int seat, bool ignoreCost);
    public List<int> LegalStormTiles(int seat);                   // incl. Friendly Storm
    public List<int> PlunderCandidates(int seat, int tile);       // ascending seat order
    public bool CanPlayCharter(int seat, CharterType type);       // full legality incl. phase
    public bool CanBuyCharter(int seat);
    public int ComputePathLength(int seat);                       // R-11.1 from scratch
    public int TotalProductionPips(int seat);                     // Σ pips × (1 or 2) over owned buildings, Storm ignored
}
```

### 8.1 Path length algorithm (R-11.1)

```
best = 0
for each edge e owned by seat (ascending id):
    mark e used
    best = max(best, walk(seat, e.B, 1), walk(seat, e.A, 1))
    unmark e
walk(seat, v, len):
    if v has building owned by another seat: return len      // cannot continue through
    r = len
    for each edge f in Vertex[v].Edges with Owner == seat and not used:
        mark f; r = max(r, walk(seat, f.Other(v), len + 1)); unmark f
    return r
```
Max 15 edges → exhaustive search is fine (<1 ms).

### 8.2 Award recomputation (after every Path/Outpost placement, both setup and play)

```
for each seat: PathLength = ComputePathLength(seat)
best = max PathLength
h = LongestPathHolder
if h >= 0 and PathLength[h] >= 5 and PathLength[h] == best: keep (no event)
else:
    leaders = seats with PathLength == best and best >= 5
    newHolder = leaders.Count == 1 ? leaders[0] : -1
    if newHolder != h: LongestPathHolder = newHolder; emit AwardChanged(LongestPath, newHolder, h, best)
```
Note the first-reach rule (R-11.2) is implied: a non-holder only takes the award by being uniquely ≥ best. Strongest Guard after each PlayWarden: if `WardensPlayed[p] >= 3` and (`holder == -1` or `WardensPlayed[p] > WardensPlayed[holder]`) → transfer + event.

## 9. State hash (`StateHasher.Hash(GameState) : ulong`)

FNV-1a 64-bit (offset 14695981039346656037, prime 1099511628211) over the UTF-8 bytes of this canonical dump (`\n` line endings, invariant culture):

```
phase=<Phase> current=<int> turn=<int> storm=<int> dice=<d1>,<d2> lp=<int> sg=<int> winner=<int> start=<int>
flow=<StormReturnPhase>,<CharterPlayedThisTurn 0|1>,<RoadCrewRemaining>,<SetupIndex>,<LastSetupOutpost>,<NextTradeId>,<PlayedOtherCharters> rolloff=<RollOffQueue seats separated by ';'>
bank=<codec>
deck=<comma-separated CharterType ints, bottom→top>
rng=<State>,<Inc>
p<seat> hand=<codec> charters=<type:boughtTurn;...> wardens=<n> left=<paths>,<outposts>,<keeps> len=<n> pend=<n> roll=<RollOffTotal>
... one line per seat
v=<for each vertex: owner*3+building, comma-separated>          (−1 owner → write "-")
e=<for each edge: owner, comma-separated>                         ("-" for none)
trade=<id,proposer,give,get,mask,responses...> or trade=none
```

## 10. Invariants (`GameInvariants.Check(GameState) : List<string>`; empty = OK)

1. For each resource: `Bank[r] + Σ Hand[r] == 19`; no negative counts anywhere.
2. `Deck.Count + Σ Charters.Count + Σ WardensPlayed + PlayedOtherCharters == 25`, and per type the counts never exceed R-1.4.
3. Per seat: pieces on board + left == 15 / 5 / 4 (outposts on board counted as Outpost buildings; keeps as Keep).
4. Distance rule holds for every building.
5. Every owned edge shares a vertex with an own building or another own edge, unless that vertex holds an opponent building (opponents may legally cut a network, so global connectivity is *not* an invariant; connection is verified at placement time by tests).
6. `PathLength[s] == ComputePathLength(s)` for all seats. Longest Path: if holder ≥ 0 then `PathLength[holder] >= 5` and `PathLength[holder] >= PathLength[s]` for all s; if holder == −1 then there is no *unique* seat with the maximum length ≥ 5. Strongest Guard: if holder ≥ 0 then `WardensPlayed[holder] >= 3` and ≥ everyone else's; if −1 then nobody has ≥ 3.
7. `Phase` consistency: `PendingDiscard > 0` only in Discard; `ActiveTrade` only in Main; `RoadCrewRemaining > 0` only in RoadCrew; `Winner >= 0` iff GameOver.
8. `VictoryPoints(seat, true) < target` for every non-winner during their own turn after each action (otherwise victory check failed).
9. `StormTile` in range; Storm tile ≠ previous when a `StormMoved` occurred (checked in tests).
10. In phases MoveStorm and ChooseVictim every `PendingDiscard == 0` (a hand may legitimately still exceed 7 after discarding half).
