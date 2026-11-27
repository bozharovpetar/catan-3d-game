# 13 — Strings (all user-facing text)

Every string below goes in `Runtime/App/Strings.cs` as `public static readonly string` fields or small format methods. Use them verbatim. `{0}`, `{1}` are `string.Format` placeholders. Player names are wrapped in `<color=#RRGGBB>` rich text by the caller.

## 1. Error messages (`Strings.Errors[code]`, shown as error toasts)

| Error code | Message |
|---|---|
| GameOver | The game is over. |
| NotYourTurn | It isn't your turn. |
| WrongPhase | You can't do that right now. |
| BadId | That spot doesn't exist. |
| Occupied | Something is already built there. |
| DistanceRule | Too close to another building — leave one corner free in between. |
| NotConnected | That isn't connected to your Paths. |
| NoPiecesLeft | You have none of those pieces left. |
| CannotAfford | You don't have the resources for that. |
| NotYourOutpost | You need one of your own Outposts there. |
| SameTile | The Storm must move to a different tile. |
| StormProtected | Friendly Storm: that tile is protected. |
| InvalidVictim | You can't plunder that player. |
| InvalidDiscard | Choose exactly the number of cards you must discard. |
| DeckEmpty | The Charter deck is empty. |
| CharterAlreadyPlayed | You already played a Charter this turn. |
| NoPlayableCharter | You have no playable Charter of that kind (cards bought this turn must wait). |
| NoLegalPlacement | There is nowhere to place a Path. |
| BankEmpty | The bank doesn't have that. |
| InvalidBounty | Choose two resources the bank has in stock. |
| InvalidTrade | That offer isn't valid: both sides must give something different. |
| TradePending | Finish or cancel your trade offer first. |
| NoSuchTrade | That offer is no longer open. |
| PartnerNotReady | That player hasn't accepted. |
| PartnerCannotPay | That player can no longer pay. |
| TradingClosed | Classic phases: trading is closed once you start building. |

## 2. Turn prompts (`tb-prompt`)

| Situation | Text |
|---|---|
| RollForOrder (you) | Roll to decide who goes first |
| RollForOrder tie | Tie! {0} roll again |
| SetupOutpost round 1 / 2 | Place your first Outpost / Place your second Outpost |
| SetupPath | Place a Path next to your Outpost |
| TurnStart, no playable Charter | Roll the dice |
| TurnStart, playable Charter | Roll the dice (or play a Charter first) |
| Discard (you) | Storm surge! Discard {0} cards |
| Discard (waiting) | Waiting for {0} to discard… |
| MoveStorm | Move the Storm to a new tile |
| ChooseVictim | Choose who to plunder |
| Main | Build, trade, or end your turn |
| Main, Classic phases, trading closed | Build or end your turn (trading is closed) |
| Main, offer open | Waiting for answers to your offer… |
| RoadCrew | Road Crew: place a free Path ({0} left) |
| Bot acting | {0} is thinking… |
| Spectating | Spectating |

## 3. Placement hints (`placement-hint`)

| Mode | Text |
|---|---|
| Path | Click a glowing edge to build a Path — Esc to cancel |
| Outpost | Click a glowing corner to build an Outpost — Esc to cancel |
| Keep | Click one of your Outposts to raise a Keep — Esc to cancel |
| Storm | Click a tile to move the Storm |
| Free Path | Click a glowing edge — Road Crew Paths are free |

## 4. Disabled-button reasons (tooltips)

| Button | Reasons (first that applies) |
|---|---|
| Roll | It isn't your turn · Finish the current step first |
| Path | Roll first · Need {0} (first missing resource) · No Path pieces left · No legal edge · Finish your trade offer first |
| Outpost | Roll first · Need {0} · No Outpost pieces left · No legal corner — extend your Paths · Finish your trade offer first |
| Keep | Roll first · Need {0} · No Keep pieces left · You have no Outpost to upgrade · Finish your trade offer first |
| Buy Charter | Roll first · Need {0} · The Charter deck is empty · Finish your trade offer first |
| Trade | Roll first · Classic phases: trading is closed once you start building · You already have an open offer |
| End turn | Roll first · Finish placing your Road Crew Paths · Move the Storm first · Waiting for discards |
| Play (Charter) | see 11-CHARTERS §5.2 |
| Propose (trade dialog) | Offer at least one card · Ask for at least one card · You can't give and ask for the same resource · Choose at least one player · You don't have those cards |
| Harbor trade | You need {0} {1} (rate {0}:1) · The bank is out of {0} · Pick a different resource to receive |
| Accept (incoming offer) | You don't have the cards they ask for |

## 5. Toasts

| Event | Text | Style |
|---|---|---|
| Autosave written (first time each session only) | Autosaved | info |
| Manual save | Saved to slot {0} | success |
| Trade completed (viewer involved) | Trade completed with {0} | success |
| Offer declined by everyone | Nobody accepted your offer | info |
| Award gained (viewer) | You now hold the {0}! (+2 points) | success |
| Award lost (viewer) | {0} took the {1} from you | error |
| Shortage | The bank ran short of {0} — nobody harvests {0} this roll | info |
| Plundered (viewer is victim) | {0} plundered 1 {1} from you | error |
| Debug session | Debug session — saving disabled | info |
| Corrupted save | This save appears to be corrupted | error |

## 6. Confirm dialogs

| Use | Title | Body | Buttons |
|---|---|---|---|
| End turn while a build is affordable | End your turn? | You can still afford: {0}. | End turn / Keep playing |
| Quit to title | Leave this game? | Your progress is autosaved. | Leave / Stay |
| Overwrite save slot | Overwrite slot {0}? | {1} | Overwrite / Cancel |
| Delete save | Delete this save? | This cannot be undone. | Delete / Cancel |

## 7. In-game rules reference (`Strings.RulesSections`, shown in the Rules overlay)

**Goal** — Be the first to reach {target} victory points on your own turn. Outposts are worth 1, Keeps 2, each award 2, and each Monument 1 (kept secret).

**Your turn** — 1. You may play one Charter. 2. Roll the dice: every tile showing that number gives its resource to each Outpost (1 card) and Keep (2 cards) on its corners, for all players. 3. Trade, build and buy in any order. 4. End your turn. You may play only one Charter per turn, before or after the roll, and never one you bought this turn.

**Building** — Path: 1 Timber + 1 Clay, on an edge connected to your network. Outpost: 1 Timber + 1 Clay + 1 Fiber + 1 Grain, on a corner touching one of your Paths with no building on any neighbouring corner. Keep: 2 Grain + 3 Iron, replaces one of your Outposts. Charter: 1 Fiber + 1 Grain + 1 Iron. You have 15 Paths, 5 Outposts and 4 Keeps.

**The Storm** — When a 7 is rolled nobody harvests. Everyone holding more than 7 resource cards discards half (rounded down) of their choice. The roller moves the Storm to another tile and plunders one random card from a player with a building there. A tile under the Storm produces nothing.

**Charters** — Warden: move the Storm and plunder. Monument: +1 secret victory point. Road Crew: place 2 free Paths. Bounty: take any 2 resources from the bank. Decree: name a resource; every opponent gives you all of theirs. Charters stay hidden until played and can't be traded.

**Trading** — On your turn, after rolling, you may offer trades to other players; they can accept, decline or counter. You can also trade with the bank at 4:1, or 3:1 with a Generic harbor, or 2:1 for the matching resource with a specific harbor. A harbor works once you have a building on one of its two corners.

**Awards** — Longest Path: the first player with a continuous Path of 5 or more segments; someone else takes it only by building a longer one. An opponent's building breaks a Path. Strongest Guard: the first player to play 3 Wardens; someone else takes it only by playing more. Each award is worth 2 points.

**Bank limits** — The bank holds 19 cards of each resource. If it can't pay everyone owed a resource on a roll, nobody gets that resource — unless only one player is owed, who takes what is left.

**Options** — Classic phases: finish trading before you start building each turn. Friendly Storm: the Storm can't hit players with 2 or fewer points.

## 8. Miscellaneous labels

| Key | Text |
|---|---|
| Seat type options | Human · CPU Easy · CPU Normal · CPU Hard |
| Board options | Balanced random · Random · Beginner |
| CPU speed options | Slow · Normal · Fast · Instant |
| Hot-seat note | Hot-seat: hands are hidden between turns. |
| Curtain | Pass to {0} / I'm {0} — show my hand |
| Game over | {0} wins! / {0} points in {1} rounds |
| Harbor labels | 3:1 / 2:1 {0} |
| Tile tooltip | {0} — {1} on {2} · {3} pips / Storm: no harvest |
| Piece tooltip | {0}'s Outpost / {0}'s Keep / {0}'s Path |
