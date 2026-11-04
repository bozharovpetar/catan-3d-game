#!/usr/bin/env bash
# Type-checks all Hexhaven assemblies with Mono mcs against reference DLLs (no Unity needed).
# Usage: tools/compile-check.sh [assets_dir]   (default game/Assets/Hexhaven; use scaffold/Assets/Hexhaven before M0)
# Constraints this implies (see docs/02-ARCHITECTURE.md §8): C# 7.2 syntax everywhere; Unity APIs must exist in 2021.3
# (guard Unity-6-only APIs with #if UNITY_6000_0_OR_NEWER and keep a fallback); Input System / URP APIs limited to tools/stubs.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
ASSETS="${1:-$ROOT/game/Assets/Hexhaven}"
REFS="$ROOT/tools/.refs"
OUT="$ROOT/.compile-check"
[ -d "$REFS/modules" ] || "$ROOT/tools/fetch-unity-refs.sh"
command -v mcs >/dev/null || { echo "Need Mono mcs: apt-get install -y mono-mcs mono-runtime"; exit 2; }
mkdir -p "$OUT"
MODS=$(ls "$REFS"/modules/lib/net45/UnityEngine.*Module.dll | sed 's/^/-r:/' | tr '\n' ' ')
EDITOR_DLL="$REFS/sdk/lib/UnityEditor.dll"
NUNIT=$(find "$REFS/nunit" -path '*net45*' -name nunit.framework.dll | head -1)
DEFS="HEXHAVEN_URP;HEXHAVEN_INPUTSYSTEM;ENABLE_INPUT_SYSTEM;UNITY_2022_2_OR_NEWER;UNITY_2023_1_OR_NEWER;UNITY_6000_0_OR_NEWER;UNITY_STANDALONE"
MCS="mcs -langversion:7.2 -target:library -nowarn:1591,0649,0414 -warnaserror+"
files () { find "$1" -name '*.cs' 2>/dev/null | sort; }
step () { echo "== $1"; }

step "Core (no engine refs)"
$MCS -define:"UNITY_EDITOR" -out:"$OUT/Hexhaven.Core.dll" $(files "$ASSETS/Scripts/Core")

step "Stubs (Input System, URP/Core RP, TestRunner)"
$MCS -nowarn:0067 -out:"$OUT/Stubs.dll" $MODS "$ROOT"/tools/stubs/*.cs

RUNTIME_FILES=$(files "$ASSETS/Scripts/Runtime")
if [ -n "$RUNTIME_FILES" ]; then
  step "Runtime"
  $MCS -define:"$DEFS" -out:"$OUT/Hexhaven.Runtime.dll" $MODS -r:"$OUT/Hexhaven.Core.dll" -r:"$OUT/Stubs.dll" $RUNTIME_FILES
  step "Runtime (fallback: Built-in pipeline + legacy input)"
  $MCS -define:"UNITY_2022_2_OR_NEWER;UNITY_2023_1_OR_NEWER;UNITY_6000_0_OR_NEWER;ENABLE_LEGACY_INPUT_MANAGER" -out:"$OUT/Hexhaven.Runtime.fallback.dll" $MODS -r:"$OUT/Hexhaven.Core.dll" $RUNTIME_FILES
  step "Runtime (Development build defines)"
  $MCS -define:"$DEFS;DEVELOPMENT_BUILD" -out:"$OUT/Hexhaven.Runtime.dev.dll" $MODS -r:"$OUT/Hexhaven.Core.dll" -r:"$OUT/Stubs.dll" $RUNTIME_FILES
fi
EDITOR_FILES=$(files "$ASSETS/Scripts/Editor")
if [ -n "$EDITOR_FILES" ]; then
  step "Editor"
  $MCS -define:"$DEFS;UNITY_EDITOR" -nowarn:1701,1702 -out:"$OUT/Hexhaven.Editor.dll" $MODS -r:"$REFS/modules/lib/net45/UnityEngine.dll" -r:"$EDITOR_DLL" -r:"$OUT/Hexhaven.Core.dll" -r:"$OUT/Hexhaven.Runtime.dll" -r:"$OUT/Stubs.dll" $EDITOR_FILES
fi
EDIT_TESTS=$(files "$ASSETS/Tests/EditMode")
if [ -n "$EDIT_TESTS" ]; then
  step "Tests (EditMode)"
  $MCS -define:"UNITY_INCLUDE_TESTS;UNITY_EDITOR" -out:"$OUT/Tests.EditMode.dll" $MODS -r:"$NUNIT" -r:"$OUT/Stubs.dll" -r:"$OUT/Hexhaven.Core.dll" $EDIT_TESTS
fi
PLAY_TESTS=$(files "$ASSETS/Tests/PlayMode")
if [ -n "$PLAY_TESTS" ]; then
  step "Tests (PlayMode)"
  $MCS -define:"$DEFS;UNITY_INCLUDE_TESTS" -out:"$OUT/Tests.PlayMode.dll" $MODS -r:"$NUNIT" -r:"$OUT/Stubs.dll" -r:"$OUT/Hexhaven.Core.dll" -r:"$OUT/Hexhaven.Runtime.dll" $PLAY_TESTS
fi
echo "compile-check: PASS"
