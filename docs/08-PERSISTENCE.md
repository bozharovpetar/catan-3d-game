# 08 — Persistence

## 1. Principle
A save = **config + action log + state hash**. Loading replays the log through `GameEngine.Replay`, which is exact because the engine is deterministic. No Unity objects are serialised. Bot memories (`CardTracker`) are rebuilt by re-applying the action log to a fresh engine and feeding every resulting event (redacted per bot seat) — see 02 §9 "Loading".

## 2. File format (`SaveCodec`, Core) — UTF-8 text, `\n` line endings, invariant culture

```
HEXHAVEN-SAVE 1
meta savedAtUtc=2026-11-04T19:12:00Z round=7 label=Round%207 vp=6,5,4,3
config seed=48213 board=BalancedRandom target=10 friendly=0
player seat=0 name=You bot=0 diff=Normal
player seat=1 name=Bryn bot=1 diff=Normal
player seat=2 name=Corra bot=1 diff=Hard
player seat=3 name=Dain bot=1 diff=Easy
a RollForOrder p=0
a RollForOrder p=1
...
a BuildPath p=2 e=41
a ProposeTrade p=0 give=0,0,0,0,1 get=0,0,0,1,0 mask=14
a RespondTrade p=1 trade=3 resp=Countered give=0,0,0,0,2 get=0,0,0,1,0
a ConfirmTrade p=0 trade=3 target=1
a MaritimeTrade p=0 r=Timber r2=Clay
a PlayBounty p=3 get=0,0,0,2,0
a PlayDecree p=1 r=Grain
...
hash 9f3a6c01d2e4b587
```

- `vp` = public VP per seat (buildings + held awards, no Monuments) for the Load screen summary; informational only.
- Values are URL-encoded (`%20`, `%3D`, `%25`) where they could contain spaces/`=`/`%` (names, label).
- `GameAction.ToCodec()` writes `a <Type>` followed by only the non-default fields in this fixed order: `p v e t target trade mask r r2 give get resp`. Field keys: `p`=Player, `v`=Vertex, `e`=Edge, `t`=Tile, `target`, `trade`=TradeId, `mask`=TargetMask, `r`/`r2`=Resource/Resource2 (names), `give`/`get`=bag codec, `resp`=TradeResponse name. Defaults omitted: −1 ids, 0 mask, Zero bags; `r`/`r2` written only for MaritimeTrade (r, r2) and PlayDecree (r).
- `FromCodec` must accept fields in any order and ignore unknown keys (forward compatibility).
- `hash` = `StateHasher.Hash` of the state after replay, 16 lowercase hex digits. On load, mismatch → refuse to load, show "This save appears to be corrupted" and log details.

```csharp
public static class SaveCodec
{
    public static string Write(GameState state, string label, string savedAtUtcIso);
    /// Parses and replays. Returns null and sets error on failure (bad header, unknown version, illegal action, hash mismatch).
    public static GameEngine Read(string text, out string error, out SaveMeta meta);
}
public sealed class SaveMeta { public string SavedAtUtc, Label; public int Round; public int[] PublicVp; public GameConfig Config; }
```

## 3. Storage (`SaveStore`, Runtime)
- Directory: `Application.persistentDataPath/saves/`.
- Files: `autosave.hxs`, `slot1.hxs`, `slot2.hxs`, `slot3.hxs`.
- **Autosave** after every successfully applied action (debounced: write at most once per 0.5 s and always on TurnEnded, application pause/quit, and before returning to title). Write atomically: write `*.tmp` then `File.Replace`/move.
- Autosave is deleted when a game ends (GameOver). Finished games are not resumable and cannot be saved; the Game Over screen offers only rematch / new setup / title.
- Load screen lists files whose header parses; corrupt files show "Unreadable save" with a Delete button.

## 4. Settings
`SettingsStore` serialises a `[Serializable] class Settings` with `JsonUtility` to `PlayerPrefs` key `hexhaven.settings.v1`; `PlayerPrefs.Save()` on change commit. Missing/invalid → defaults (06 §7).

## 5. Replay viewer (debug, Editor menu `Hexhaven ▸ Replay Save…`)
Opens a save, replays it in Play mode at chosen speed using the normal EventPlayer (useful for bug reports). Required because it is the main debugging tool for the agent and for QA.
