# 07 — Audio (procedural)

No audio files. `SfxSynth` renders every effect once at startup into `AudioClip`s (`AudioClip.Create(name, samples, 1, 44100, false)` + `SetData`), cached in a dictionary. `AudioService` plays them through a pool of 12 `AudioSource`s (2D, `spatialBlend = 0`) with per-play pitch/volume variation, and one looping music source. Volumes: master × (sfx | music) from settings. Rendering all clips must take < 150 ms total.

## 1. Synthesis building blocks (all in `SfxSynth`, deterministic, `Pcg32` noise seeded per clip)
- `Osc(type, freq, t)`: sine, triangle, square (band-limited is not required; soften with a one-pole lowpass), saw.
- `Noise()`: white noise in [−1, 1].
- `Env(t, a, d, s, r, dur)`: linear ADSR.
- `Lowpass(x, cutoffHz)`: one-pole IIR `y += α(x − y)`, α = 1 − e^(−2π·fc/44100).
- `PitchSweep(f0, f1, t, dur)`: exponential interpolation.
- Final normalisation to peak 0.8; 3 ms fade in/out to avoid clicks.

## 2. Effects

| Id | Duration | Recipe | Used for |
|---|---|---|---|
| `sfx_click` | 0.05 s | sine 1,400 Hz, env a 0.002 d 0.04 | UI button |
| `sfx_hover` | 0.03 s | sine 2,000 Hz, very quiet (0.25) | UI hover (rate-limited 20/s) |
| `sfx_dice` | 0.45 s | 6 noise bursts (each 25 ms, lowpass 3 kHz, decaying amplitude 1→0.3) at random offsets 0–0.4 s + woody tick (triangle 600 Hz, 15 ms) per burst | dice bounces |
| `sfx_build_path` | 0.18 s | noise lowpass 900 Hz env d 0.15 + sine 180→120 Hz | Path placed |
| `sfx_build_outpost` | 0.3 s | sine 220→140 Hz thump (d 0.2) + triangle 660 Hz ping (d 0.25, vol 0.3) | Outpost |
| `sfx_build_keep` | 0.5 s | outpost recipe ×2 layered at +0.12 s with 330 Hz bell (sine + 2.76× partial, d 0.45) | Keep |
| `sfx_collect` | 0.12 s | triangle 880 Hz (pitch param per stack: ×1.06^n), env d 0.1 | resource arrives at panel |
| `sfx_coin` | 0.25 s | square 1,320 Hz 40 ms then 1,760 Hz 180 ms, lowpass 5 kHz | trade completed / maritime |
| `sfx_card` | 0.15 s | noise bandpassed (lowpass 6 kHz minus lowpass 1.5 kHz) swish, env a 0.02 d 0.12 | charter bought / played |
| `sfx_thunder` | 1.6 s | noise lowpass 400 Hz with amplitude: crack (first 80 ms full, highpassed) + rumble (slow random amplitude walk, d 1.4) | Storm lands |
| `sfx_wind` | 1.0 s | noise lowpass 700→300 Hz sweep, env a 0.3 d 0.6 | Storm moving |
| `sfx_plunder` | 0.35 s | saw 300→150 Hz lowpass 1.2 kHz + card swish | plunder |
| `sfx_discard` | 0.3 s | 3 quick card swishes | discard |
| `sfx_turn` | 0.6 s | two-note chime: sine 660 Hz (0–0.3 s) then 880 Hz (0.15–0.6 s), each with 2× partial at 0.2 | your turn starts (human only) |
| `sfx_award` | 0.9 s | arpeggio triangle 523, 659, 784, 1047 Hz, 0.12 s apart, each d 0.4 | award gained |
| `sfx_error` | 0.2 s | square 220 Hz 0.08 s, then 165 Hz 0.1 s, lowpass 1.5 kHz, vol 0.5 | illegal action |
| `sfx_win` | 2.5 s | fanfare: triangle+square mix melody C5 E5 G5 C6 (0.25 s each) then sustained C major chord (C5 E5 G5) 1.2 s with tremolo 6 Hz | victory |
| `sfx_offer` | 0.4 s | sine 988 Hz + 1,319 Hz dyad, env d 0.35 | incoming trade offer (human) |

Variation on play: pitch × U(0.96, 1.04), volume × U(0.9, 1.0) (not for UI/fanfare).

## 3. Music (`MusicSynth`)
A seamless ambient loop of exactly 16 bars (53.33 s) rendered lazily in a coroutine spread over ~20 frames after the title screen appears (no startup hitch):
- Tempo 72 BPM, 4/4, key D major. Chord progression (2 bars each): D – Bm – G – A, played twice = 16 bars = 16 × 4 × 60/72 s = 53.33 s.
- Pad: per chord, 3 sine voices (root, third, fifth, octave 3–4) with slow attack 1.5 s / release 1.5 s, gentle chorus (second voice detuned +4 cents), lowpass 2.5 kHz.
- Plucks: pentatonic (D E F# A B) random melody per bar seeded with constant seed 7, triangle wave, env a 0.005 d 0.6, one note per beat with 40 % rests, octave 5, volume 0.25, simple feedback delay (375 ms, 0.3 feedback, 0.25 mix).
- Soft "ocean" bed: noise lowpass 500 Hz with amplitude modulated by 0.1 Hz sine (waves), volume 0.08.
- Mixed and normalised to peak 0.6, mono (1 channel), 44.1 kHz.
- Crossfade-free loop: render 1 extra bar and fold its tail into the start (add the delay/release tail of the last bar onto the first samples).
- Title screen and game use the same loop; volume ducks to 40 % for 1.5 s during `sfx_win`.

## 4. Event → sound mapping (played by `EventPlayer` in sync with animations)

| Event | Sound |
|---|---|
| DiceRolled / RollOffRoll | `sfx_dice` at throw start |
| Produced | `sfx_collect` per arriving icon |
| ShortageNoPayout / ShortagePartial | `sfx_error` (quiet 0.4) |
| Discarded | `sfx_discard` |
| StormMoved | `sfx_wind` at start, `sfx_thunder` on landing |
| Plundered | `sfx_plunder` |
| Built | path / outpost / keep sounds at landing |
| CharterBought / CharterPlayed | `sfx_card` |
| BountyTaken / DecreeResolved | `sfx_coin` |
| MaritimeTraded / TradeCompleted | `sfx_coin` |
| TradeProposed (to a human target) | `sfx_offer` |
| AwardChanged (holder ≠ −1) | `sfx_award` |
| TurnStarted (a human's turn) | `sfx_turn` |
| GameWon | `sfx_win` |
| Illegal UI attempt | `sfx_error` |
| UI buttons | `sfx_click` (registered globally via a `ClickEvent` callback on the root, `TrickleDown`) |
