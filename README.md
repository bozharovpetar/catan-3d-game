# Hexhaven — build-ready specification package

Hexhaven is an original 3D hex-island trading and building strategy game for 3–4 players (hot-seat humans and CPU opponents), built in Unity 6 with URP. This repository holds everything an AI coding agent needs to build the complete game in a single pass.

| Path | What it is |
|---|---|
| `CLAUDE.md` / `AGENTS.md` | Agent entry point: reading order, rules, environment detection, commands, final report |
| `docs/00…10` | The specification: overview, full rulebook, architecture, Core API, AI, visuals, UI, audio, persistence, testing, build order and Definition of Done |
| `scaffold/` | Verified starting code: complete data model, board generator, RNG, codecs, hashing, input shim, mesh factory, project setup, asmdefs, plus engine and AI signatures to implement |
| `tools/sim/` | Headless simulation harness, the executable spec, run with `tools/sim/run-sim.sh` |
| `tools/compile-check.sh` | Type-checks all Unity code without Unity (Mono plus reference DLLs fetched from NuGet) |
| `prototype/` | The earlier throwaway prototype, for reference only |

## Hand it to an agent

> Read CLAUDE.md and build Hexhaven exactly as specified, following docs/10-BUILD-ORDER.md milestone by milestone. Don't stop until the Definition of Done holds or you're blocked.

## Check the package yourself

These two commands need Mono or the .NET 8 SDK:

```bash
CORE_DIR=scaffold/Assets/Hexhaven/Scripts/Core tools/sim/run-sim.sh --selftest board
```

```bash
tools/compile-check.sh scaffold/Assets/Hexhaven
```
