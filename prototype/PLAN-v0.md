# Hexhaven: game plan

Hexhaven is an original 3D hex-island strategy game for 3–4 players, built in Unity. It uses the genre's familiar mechanics: dice-driven resource production, building on hex corners and edges, trading, and a race to 10 points. All names, art and rules details are its own.

## 1. Rules (as implemented)

| Concept | Hexhaven |
|---|---|
| Board | 19 hex tiles (radius 2) on a beach, ringed by ocean |
| Terrain → resource | Forest → **Timber**, Clay Pit → **Clay**, Flax Field → **Fiber**, Farmland → **Grain**, Mine → **Iron**, Ruins → nothing |
| Number tokens | 2–12 (no 7); 6s and 8s are never adjacent |
| Pieces per player | 15 Paths, 5 Outposts, 4 Keeps |
| Path | 1 Timber + 1 Clay; must connect to your network |
| Outpost (1 VP) | Timber + Clay + Fiber + Grain; on a corner, no building on any adjacent corner, connected by your path |
| Keep (2 VP) | 2 Grain + 3 Iron; upgrades one of your outposts and produces double |
| Setup | Snake draft (1-2-3-4-4-3-2-1): outpost + adjacent path; your second outpost pays out its neighbouring tiles |
| Roll of 7 | Anyone holding more than 7 cards loses half (chosen at random). The roller moves the **Storm**, which blocks that tile's harvest, and plunders 1 random card from an opponent on that tile |
| Harbors | 9 coastal docks: 4 generic (3:1) and 5 specialised (2:1). The default bank rate is 4:1 |
| Longest Path | +2 VP for the longest continuous path of 5 or more. Ties keep the current holder, and opponents' buildings break paths |
| Victory | 10 VP on your own turn |

## 2. Architecture

```
Assets/Hexhaven/
  Scripts/Core/      Pure C# rules engine. No UnityEngine, so it can be unit-tested and simulated headless
    ResourceTypes.cs   resources, terrain, costs, ResourceBag
    Board.cs           hex geometry, corner/edge graph, terrain and number shuffle, harbor placement
    Game.cs            turn/phase state machine, legality checks, production, storm, trading, longest path, scoring
    Bot.cs             heuristic AI, one action per Step() so turns animate
  Scripts/Game/      Unity layer
    HexhavenGame.cs    MonoBehaviour entry point: camera/light setup, bot pacing, human input, auto-bootstrap
    HexhavenGame.Hud.cs IMGUI HUD: start menu, scoreboard, dice, hand, build/trade actions, log, game over
    BoardView.cs       builds the 3D island (procedural low-poly meshes), pieces, storm, placement markers, picking
    MeshFactory.cs     flat-shaded prism/pyramid/box/roof meshes generated at runtime
    Palette.cs         colours plus a material cache that picks a URP or Built-in shader automatically
    CameraRig.cs       orbit/zoom/pan camera
    InputShim.cs       works with either the legacy Input Manager or the Input System package
  Editor/HexhavenMenu.cs  "Hexhaven > Create Game Scene / Play" menu items
```

Design choices:
- **No imported assets.** All geometry is generated at runtime, so the folder drops into any project.
- **Rules are separate from presentation.** The view polls `Game.Version` and only syncs when something changed.
- **Picking without colliders.** The mouse ray is intersected with the board plane, then snapped to the nearest legal corner, edge or tile.
- **Pipeline- and input-agnostic.** It runs in Built-in or URP, with legacy input or the new Input System.

## 3. Milestones

1. ✅ Rules engine plus AI, verified by 2,000 headless all-bot games: 1,999 finished, averaging about 24 rounds, with seat win rates of 23–27%, no invariant violations, and the distance rule checked.
2. ✅ Procedural 3D board: terrain dioramas, number tokens, harbors, storm cloud.
3. ✅ Pieces, placement markers, hover, pop-in animation.
4. ✅ HUD: menu, scoreboard, hand, build/trade, dice, log, tooltip, game over.
5. ✅ Hot-seat: any mix of human and CPU seats.
6. ⏭ Next: player-to-player trade offers, choosing your own discards and steal target, progress (development) cards, sound and particles, a save/load system, online multiplayer (see the `setup-multiplayer-services` Unity skill).
