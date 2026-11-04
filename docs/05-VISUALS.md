# 05 — Visuals (3D world)

Art direction: **cosy low-poly diorama**. Flat-shaded meshes, soft warm sunlight, saturated but not neon colours, gentle motion everywhere (water, clouds, storm). Everything is procedural (`MeshFactory`, `TextureFactory`). World units: 1 = hex corner radius. Y is up. Tile top surface is at **y = 0.25**.

## 1. Materials (`MaterialLibrary`)

**Template materials (shader-variant safety):** runtime-created materials alone would let the build strip `_EMISSION`/`_SURFACE_TYPE_TRANSPARENT` variants, and adding URP/Lit to Always Included compiles every variant (very slow builds). So `ProjectSetup` generates template materials as assets in `Assets/Hexhaven/Resources/Hexhaven/Materials/` — `LitOpaque`, `LitTransparent`, `LitEmissive`, `Unlit`, `ParticleUnlit` — configured with the exact keywords/properties below (this is Editor-script-generated, not hand-written YAML). `MaterialLibrary` loads them with `Resources.Load<Material>` and clones them (`new Material(template)`), changing only colours. Never create materials from `Shader.Find` at runtime except as the Built-in fallback.

- `Lit(Color c)`: cached per colour. Shader: URP `Universal Render Pipeline/Lit` (Built-in fallback `Standard`). `_BaseColor`/`_Color` = c, `_Smoothness`/`_Glossiness` = 0.15, `_Metallic` = 0, `enableInstancing = true`.
- `LitEmissive(Color c, Color emission)`: as Lit + `_EMISSION` keyword, `_EmissionColor`.
- `Unlit(Color c)`: `Universal Render Pipeline/Unlit` (fallback `Unlit/Color`). Used for markers' inner glow and UI-ish world elements.
- `Particle(Color c)`: `Universal Render Pipeline/Particles/Unlit` (fallback `Particles/Standard Unlit`), alpha blended, soft round texture from `TextureFactory.SoftDot(64)`.
- `Transparent(Color c)`: clone of `LitTransparent` — URP Lit with `_Surface`=1, `_Blend`=0, `_SrcBlend`=5 (SrcAlpha), `_DstBlend`=10 (OneMinusSrcAlpha), `_ZWrite`=0, keyword `_SURFACE_TYPE_TRANSPARENT`, `SetOverrideTag("RenderType","Transparent")`, render queue 3000. Used for ghost previews (alpha 0.45) and clouds.
- `WorldText(Font f)`: material using the custom `Hexhaven/WorldText` shader (scaffold `Resources/Hexhaven/Shaders/WorldText.shader`) with `_MainTex = f.material.mainTexture`. The built-in `GUI/Text Shader` ignores depth (numbers would show through pieces and the Storm); WorldText is the same alpha-text shader with `ZTest LEqual`. Assign it to every `TextMesh` renderer.

## 2. Palette (`Palette.cs`, sRGB values)

| Name | Hex | Use |
|---|---|---|
| Ocean deep | `#1F5E8C` | ocean mesh |
| Ocean shallow | `#3E8FB8` | shallows ring |
| Foam | `#E8F4F8` | foam ring |
| Beach | `#EBD9A8` | island base |
| Sky top / horizon | `#7FB8E0` / `#D6ECF5` | camera background gradient (skybox: procedural gradient texture on a big inverted sphere) |
| Forest tile / tree / trunk | `#43854D` / `#21613A` / `#7A5231` |
| Clay Pit tile / pit / clay block | `#CC784D` / `#8C4A2E` / `#B85438` |
| Flax Field tile / stem / flower | `#99C280` / `#5E9A52` / `#6B95F2` |
| Farmland tile / stem / ear | `#EDCC6B` / `#CCA84D` / `#D99E2E` |
| Mine tile / rock / snow / entrance | `#85878F` / `#6F7280` / `#F2F2F7` / `#26201A` |
| Ruins tile / stone | `#DBC99E` / `#C7BDAD` |
| Token face / rim / text / hot text | `#FAF2DB` / `#B89B6E` / `#262019` / `#CC1F1A` |
| Wood (docks, masts) | `#80573A` |
| Storm cloud / rain / bolt | `#3F4250` / `#8DB3E6` / `#FFE45A` |
| Highlight (legal) / hover | player colour lightened 45 % / `#FFEB40` |
| Player 0 Crimson | `#DB3833` |
| Player 1 Azure | `#3375E6` |
| Player 2 Amber | `#F5AD1F` |
| Player 3 Violet | `#9452D1` |
| Resource UI colours | Timber `#38803F`, Clay `#CC6E42`, Fiber `#739EF2`, Grain `#F2CC52`, Iron `#959AA8` |

Player roof/trim colour = player colour darkened 35 %. **Colour-blind emblems** (setting, default ON): each seat also has an emblem: 0 Circle, 1 Triangle, 2 Square, 3 Diamond — drawn as a small white flat mesh on piece roofs/path centres and next to names in the UI.

## 3. Scene layout & lighting

| Object | Spec |
|---|---|
| Sun | Directional, rotation (52°, −35°, 0), colour `#FFF5E0`, intensity 1.2, soft shadows, strength 0.65 |
| Fill | Directional, rotation (30°, 145°, 0), colour `#B8D4FF`, intensity 0.25, no shadows |
| Ambient | Trilight: sky `#B3C7E0`, equator `#8C949A`, ground `#59524D` |
| Fog | Linear, colour = sky horizon, start 30, end 70 |
| Post (URP Volume, global) | Bloom threshold 1.1 intensity 0.35; Tonemapping Neutral; Color Adjustments saturation +8, post exposure 0.1; Vignette 0.18 |
| Background | Gradient sky sphere radius 120, inverted normals, Unlit gradient texture (TextureFactory.VerticalGradient) |

## 4. Island construction (`BoardView.Build(state)`)

Hierarchy: `World/Board/{Ocean, Shallows, Foam, Beach, Tiles/Tile_<id>, Harbors/Harbor_<slot>, Tokens}`, `World/Pieces`, `World/Markers`, `World/Fx`. After building static content call `StaticBatchingUtility.Combine(staticRoot)` (tiles, beach, harbors — not tokens since they animate).

| Element | Mesh | Placement |
|---|---|---|
| Ocean | grid plane 90 × 90 units, 72 × 72 quads, animated (§8) | y = −0.18 |
| Shallows | flat-top hexagon prism radius 5.7, height 0.05 | y = −0.12, Ocean shallow |
| Foam | hexagon ring (outer 5.15, inner 4.95), height 0.02 | y = −0.04, slowly pulsing alpha via scale ±1 % |
| Beach | flat-top hexagon prism radius 4.95, height 0.22 (bevel: second prism radius 5.05 height 0.08 below) | y = −0.12 |
| Tile base | pointy-top hex prism radius 0.97, height 0.25, side colour = tile colour darkened 18 % (use 2 sub-meshes or separate side mesh) | at tile centre, y = 0 |
| Tile top variation | per tile, 5 tiny flat "grass tufts" (Box 0.06×0.02×0.06) of the tile colour lightened 12 %, scattered (avoid r < 0.33) | |

### 4.1 Terrain decorations (seeded per tile: `new Pcg32(seed*31 + tileId)` — visual only)
Scatter ring = radius 0.38–0.74 from tile centre (token sits at centre r < 0.33); min spacing 0.24 between items.

| Terrain | Content |
|---|---|
| Forest | 6–7 trees. Tree = trunk (5-gon prism r 0.035 h 0.10) + 2 stacked cones (6-gon pyramids: r 0.14 h 0.26 at y+0.08; r 0.10 h 0.22 at y+0.22), random scale 0.8–1.3, random yaw. 1 in 4 trees uses the lighter green `#2F7A45`. |
| Clay Pit | 3 pits (7-gon prism r 0.16 h 0.02, darker) each with a clay-block stack: 3 boxes 0.12×0.05×0.06 stacked with yaw offsets; plus 1 small wheelbarrow (box 0.12×0.05×0.08 + 1 wheel cylinder r 0.03). |
| Flax Field | rows (3 rows rotated by a random multiple of 60°) of stems (box 0.04×0.10×0.04) topped with flower boxes 0.07×0.04×0.07 in flower blue; spacing 0.18; skip points within r < 0.36 or > 0.78. |
| Farmland | same row layout, stems box 0.05×0.14×0.05, ears = 4-gon pyramid r 0.05 h 0.09 in ear colour; plus a small barn (box 0.22×0.14×0.16 red-brown `#A3472E`, roof prism) at one ring point. |
| Mine | 3 peaks (5-gon pyramid r 0.26 h 0.5, scale 0.8–1.3) with snow caps (5-gon pyramid r 0.09 h 0.17 at 66 % height), mine entrance box (0.12×0.10×0.06, dark) facing outward at 70 % of the first peak's offset, 2 mine carts (box 0.08×0.05×0.06 iron grey). |
| Ruins | 5 broken pillars (8-gon prism r 0.06, h 0.08–0.38), 1 fallen slab (box 0.35×0.05×0.18), 1 arch (two pillars h 0.3 + lintel box 0.3×0.06×0.08). |

### 4.2 Number tokens (`TokenView`)
- Disc: 20-gon prism r 0.27, h 0.05, face colour; rim: 20-gon prism r 0.29, h 0.04, rim colour, under it.
- Number: `TextMesh`, `LegacyRuntime.ttf`, fontSize 64, characterSize 0.05, bold, anchor MiddleCenter, colour hot (6/8) or normal, positioned at y = 0.25 + 0.06 + 0.005, **lying flat** (rotation X = 90°) and yawed to face the camera's current yaw snapped to the nearest 60° (updated when the camera yaw crosses a sector boundary) so it is always upright for the viewer.
- Pips: row of N small dark dots (4-gon flat prisms r 0.018) under the number, spacing 0.04 (hot colour for 6/8).
- Roll highlight: when its number is rolled, token rises 0.12, scales to 1.25 and its rim turns emissive gold for 1.4 s (ease out back), then settles; if under the Storm it instead flashes a grey "X" (two crossed boxes) for 1 s.

### 4.3 Harbors (`HarborView`)
- Dock: box 0.42 × 0.06 × 0.30 (wood) at the slot's marker position, y = 0.08, yaw facing outward.
- Two piers: boxes 0.06 × 0.04 × length from dock to each harbor corner at y = 0.11 (start at dock edge, end 0.12 before the corner).
- Mast: 5-gon prism r 0.015 h 0.42 on the dock; flag: box 0.02 × 0.12 × 0.16 in resource colour (Generic: white) with a small wave animation (yaw ±8° sine, 0.7 Hz).
- Label: TextMesh billboard above (y + 0.65): "3:1" or "2:1" + newline + resource name, white with dark outline (render twice offset black at z+0.002 to fake outline).
- A small crate stack (2 boxes) in the resource colour for specific harbors.

## 5. Pieces (`PieceFactory`)

All pieces have their pivot at the base centre. Body = player colour; roof/trim = darker. Emblem decal on roof (§2).

| Piece | Composition | Placement |
|---|---|---|
| Path | box 0.11 × 0.09 × 0.62 + a thinner darker top strip box 0.06 × 0.01 × 0.6 | edge midpoint, y = 0.25, yaw along edge |
| Outpost | walls box 0.22 × 0.16 × 0.18; gabled roof (triangular prism) 0.26 × 0.24 × 0.13 on top; door box 0.05 × 0.08 × 0.01 dark on the front | vertex, y = 0.25, yaw = 30° + 60° × (vertexId % 2) |
| Keep | hall box 0.34 × 0.16 × 0.22 + roof 0.36 × 0.26 × 0.12; tower 8-gon prism r 0.09 h 0.38 at x + 0.14; spire 8-gon pyramid r 0.11 h 0.18; banner box 0.01 × 0.06 × 0.08 at the spire top | vertex |
| Ghost preview | same mesh in `Transparent(player colour, α 0.45)` shown at the hovered legal spot while in a placement mode | |

Placement animation: drop from y + 0.6 with ease-in (0.18 s), squash (scale Y 0.8, XZ 1.1) for 0.06 s, settle with ease-out-back (0.2 s), dust puff VFX, `sfx_build`. Keep upgrade: Outpost sinks (0.15 s) then Keep rises from below (0.3 s, ease-out-back).

## 6. Storm (`StormView`)
- Cloud: 7 spheres (Unity sphere primitive, colliders removed) in cloud colour, radii 0.18–0.30, arranged in a ring of r 0.25 + one on top; whole cloud rotates 20°/s and bobs ±0.06 at 0.35 Hz, hovering at y = 1.35 over the tile centre.
- Rain: particle system (stretched billboards, colour rain α 0.6, rate 45/s, lifetime 0.55 s, speed 2.2 downward, emitter: circle r 0.3 at cloud base).
- Shadow: dark transparent disc (r 0.6, α 0.25) on the tile surface.
- Move animation: arc (height +0.8) to the new tile over 0.9 s (ease in-out), then lightning: bolt mesh (3 zig-zag boxes, emissive bolt colour) visible 0.12 s, point light flash (intensity 0 → 6 → 0 over 0.25 s, colour bolt), `sfx_thunder`.
- Blocked tile: tile base material swapped to a 25 % darkened variant while the Storm is on it.

## 7. Dice (`DiceView`)
- Two cubes 0.34 units, rounded look via a bevelled mesh (cube with chamfered edges: 24-vertex chamfer box, chamfer 0.04), white `#F8F6EE`, pips as dark circles in a 3×2 atlas texture generated by `TextureFactory.DiceAtlas(256)` (faces 1–6, standard opposite faces sum to 7).
- Throw: dice appear at the camera-relative front-right of the island (world point 1.2 units above board plane, projected at screen (0.72, 0.30)), tumble along a scripted arc for 0.9 s with random spin, then rotate (slerp, 0.25 s) to the orientation showing the rolled face up, bounce twice (heights 0.15, 0.05), hold 1.0 s, then shrink away (0.25 s). Total ≈ 2.2 s at normal speed; at Fast speed ×0.4; Instant: skip 3D dice, UI only.
- Roll-off rolls use the same dice, tinted by the rolling player's colour (pips in player colour).
- `sfx_dice` timed to the bounces.

## 8. Ocean & ambient (`OceanView`, `AmbientView`)
- Ocean vertices: `y = 0.05·sin(0.55x + 1.1t) + 0.035·sin(0.8z + 1.4t) + 0.02·sin(1.7(x+z) + 2.3t)`; recompute in a reused `Vector3[]` each frame and call `mesh.SetVertices` + `RecalculateNormals` every 2nd frame (no allocations).
- Clouds: 6 low-poly clouds (3–5 spheres each, white, α 0.9 transparent) drifting at y = 7–9, radius 14–20 from centre, angular speed 0.6–1.2°/s.
- Boats: 2 small sailboats (hull: 4-gon prism scaled, sail: triangle prism white) slowly circling at radius 7.5 and 9.0, bobbing with the waves.
- Birds: 3 "V" shapes (two thin boxes) circling at y = 4, radius 5–8, wing flap via scale Y sine.
- All ambient motion uses unscaled time and pauses when Settings → Reduce motion is on (static).

## 9. Placement markers & picking (`Markers`, `Picker`)
- Legal corner marker: 12-gon disc r 0.10 h 0.04 in highlight colour + inner Unlit white disc r 0.05; bobbing y + 0.03…0.06 at 2.5 Hz. Over an existing Outpost (Keep mode) lift by 0.42.
- Legal edge marker: box 0.07 × 0.04 × 0.50 along the edge, same colours.
- Legal tile marker (Storm move): hex ring (outer 0.62, inner 0.52) height 0.03 at tile surface + 0.02, Storm-coloured; tiles with plunder candidates also show small candidate-colour pips around the ring.
- Hover: marker scales ×1.4 and turns hover colour; ghost preview piece shown; cursor tooltip shows the action and cost.
- **Picking:** never use colliders. Ray from camera through mouse → intersect plane y = 0.25 → nearest candidate: corners within 0.33, edge midpoints within 0.33, tile centres within 0.9. In Keep mode, also test the plane y = 0.45 (piece height) and take the closer hit.
- Hover info when no mode active: tile tooltip ("Forest — Timber on 8 · 5 pips"; "Storm: no harvest"), harbor tooltip, piece tooltip ("Azure Keep").

## 10. Resource flyouts (`ResourceFlyout`)
For each `Produced`, `StartingResources`, `BountyTaken`, `DecreeResolved`, `Plundered` (visible), `TradeCompleted`, `MaritimeTraded` event: spawn small 3D resource icons and fly them.
- Icons: Timber = 2 stacked logs (6-gon prisms r 0.035 h 0.16 lying), Clay = clay block (box 0.12×0.06×0.07), Fiber = bundle (3 thin cylinders + band), Grain = sheaf (4-gon pyramid + band), Iron = ingot (trapezoid prism). Scale 1.4 during flight.
- Path: from the tile surface (or from player panel anchor for trades) rise 0.4 over 0.2 s, then fly along a quadratic Bézier to the world point 6 units in front of the camera under the receiving player's UI panel anchor (`UiRoot.GetPlayerAnchorScreenPos(seat)`, converted with `Camera.ScreenToWorldPoint`) over 0.55 s, scale down to 0.3, then despawn and pulse the UI counter (`sfx_collect`, pitch rises per stacked card).
- Stagger multiple icons by 0.07 s. For hidden plunders seen by a third party: grey "?" card icon.
- Pool icons (max 40 active).

## 11. VFX (`VfxFactory`) — built in code (ParticleSystem)

| Effect | Spec |
|---|---|
| Dust puff | burst 14, lifetime 0.4–0.6, speed 0.4–0.9, size 0.05–0.12, colour beach→transparent, gravity −0.2 (rises), sphere emitter r 0.1 |
| Award sparkle | burst 40, gold `#FFD34D`, lifetime 0.8, speed 1–2, size 0.04, emitted at the midpoint of the new holder's longest trail (Longest Path) or above their most recent building (Strongest Guard), plus `sfx_award` |
| Charter play | ring burst (circle emitter r 0.4, 30 particles) in player colour at the board centre facing camera |
| Plunder | 8 small grey sparks from victim's building to thief's nearest building |
| Victory confetti | 300 particles, 4 player colours + gold, lifetime 3 s, from 2 emitters at screen corners projected 5 units ahead of camera, gravity 0.6 |
| Storm rain | §6 |

## 12. Camera (`CameraRig`)
- Orbit around target (default (0, 0, 0.3)), yaw default 0°, pitch 56°, distance 11.5, FOV 42°.
- Controls: right- or middle-drag orbit (0.25°/px yaw, 0.2°/px pitch, pitch clamp 25°–85°), scroll zoom (×0.88 per notch, 5.5–20), WASD/arrows pan (speed 0.6 × distance per s, target clamp radius 5), Q/E yaw ±90°/s, **H** home (animate back to defaults), double-click on a tile focuses it (target → tile, distance 8).
- Smoothing: exponential, k = 1 − e^(−12·dt). Settings: sensitivity 0.5–2.0, invert Y.
- Edge-of-screen pan: off by default (setting).
- Title screen mode: auto-orbit 6°/s at pitch 40°, distance 13.
- Event focus (setting "Follow action", default ON): when a bot builds or moves the Storm, smoothly pan target 35 % toward the event position for 1.2 s, then return.
- Victory: slow orbit around the winner's largest building.

## 13. Performance rules
- Static island content batched; pieces use instanced materials.
- No `GetComponent` in Update loops; no LINQ in per-frame code; ocean uses preallocated arrays.
- Target ≤ 250 draw calls, ≤ 300k triangles total, ≤ 16 ms frame on Iris Xe at 1080p. Verify with the Frame Debugger and the `DebugOverlay` (F3 toggles: fps, frame ms, and `Unity.Profiling.ProfilerRecorder` counters "Draw Calls Count", "Triangles Count" (Render category) and "GC Allocated In Frame" (Memory category)).
