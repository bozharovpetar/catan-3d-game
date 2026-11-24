#!/usr/bin/env bash
# Type-checks all Hexhaven assemblies with Mono mcs against reference DLLs (no Unity needed).
# Usage: tools/compile-check.sh [assets_dir]   (default game/Assets/Hexhaven; use scaffold/Assets/Hexhaven before M0)
# Constraints this implies (see docs/02-ARCHITECTURE.md §8): C# 7.2 syntax everywhere; Unity APIs must exist in 2021.3
# (guard Unity-6-only APIs with #if UNITY_6000_0_OR_NEWER and keep a fallback); Input System / URP APIs limited to tools/stubs.
# Safe for paths that contain spaces.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
ASSETS="${1:-$ROOT/game/Assets/Hexhaven}"
REFS="$ROOT/tools/.refs"
OUT="$ROOT/.compile-check"
[ -d "$REFS/modules" ] || "$ROOT/tools/fetch-unity-refs.sh"
command -v mcs >/dev/null || { echo "Need Mono mcs: apt-get install -y mono-mcs mono-runtime"; exit 2; }
mkdir -p "$OUT"

MODS=()
for dll in "$REFS"/modules/lib/net45/UnityEngine.*Module.dll; do MODS+=("-r:$dll"); done
EDITOR_DLL="$REFS/sdk/lib/UnityEditor.dll"
NUNIT="$(find "$REFS/nunit" -path '*net45*' -name nunit.framework.dll | head -1)"
DEFS="HEXHAVEN_URP;HEXHAVEN_INPUTSYSTEM;ENABLE_INPUT_SYSTEM;UNITY_2022_2_OR_NEWER;UNITY_2023_1_OR_NEWER;UNITY_6000_0_OR_NEWER;UNITY_STANDALONE"
FALLBACK_DEFS="UNITY_2022_2_OR_NEWER;UNITY_2023_1_OR_NEWER;UNITY_6000_0_OR_NEWER;ENABLE_LEGACY_INPUT_MANAGER"
MCS=(mcs -langversion:7.2 -target:library -nowarn:1591,0649,0414 -warnaserror+)

# collect(dir) -> fills the global array FILES with the .cs files under dir (sorted)
collect () {
  FILES=()
  [ -d "$1" ] || return 0
  while IFS= read -r f; do FILES+=("$f"); done < <(find "$1" -name '*.cs' | sort)
}
step () { echo "== $1"; }

step "Core (no engine refs)"
collect "$ASSETS/Scripts/Core"
[ ${#FILES[@]} -gt 0 ] || { echo "No Core sources under $ASSETS/Scripts/Core"; exit 2; }
"${MCS[@]}" -define:"UNITY_EDITOR" -out:"$OUT/Hexhaven.Core.dll" "${FILES[@]}"

step "Stubs (Input System, URP/Core RP, TestRunner)"
collect "$ROOT/tools/stubs"
"${MCS[@]}" -nowarn:0067 -out:"$OUT/Stubs.dll" "${MODS[@]}" "${FILES[@]}"

collect "$ASSETS/Scripts/Runtime"
if [ ${#FILES[@]} -gt 0 ]; then
  step "Runtime"
  "${MCS[@]}" -define:"$DEFS" -out:"$OUT/Hexhaven.Runtime.dll" "${MODS[@]}" -r:"$OUT/Hexhaven.Core.dll" -r:"$OUT/Stubs.dll" "${FILES[@]}"
  step "Runtime (fallback: Built-in pipeline + legacy input)"
  "${MCS[@]}" -define:"$FALLBACK_DEFS" -out:"$OUT/Hexhaven.Runtime.fallback.dll" "${MODS[@]}" -r:"$OUT/Hexhaven.Core.dll" "${FILES[@]}"
  step "Runtime (Development build defines)"
  "${MCS[@]}" -define:"$DEFS;DEVELOPMENT_BUILD" -out:"$OUT/Hexhaven.Runtime.dev.dll" "${MODS[@]}" -r:"$OUT/Hexhaven.Core.dll" -r:"$OUT/Stubs.dll" "${FILES[@]}"
fi

collect "$ASSETS/Scripts/Editor"
if [ ${#FILES[@]} -gt 0 ]; then
  step "Editor"
  "${MCS[@]}" -define:"$DEFS;UNITY_EDITOR" -nowarn:1701,1702 -out:"$OUT/Hexhaven.Editor.dll" "${MODS[@]}" \
    -r:"$REFS/modules/lib/net45/UnityEngine.dll" -r:"$EDITOR_DLL" -r:"$OUT/Hexhaven.Core.dll" -r:"$OUT/Hexhaven.Runtime.dll" -r:"$OUT/Stubs.dll" "${FILES[@]}"
fi

collect "$ASSETS/Tests/EditMode"
if [ ${#FILES[@]} -gt 0 ]; then
  step "Tests (EditMode)"
  "${MCS[@]}" -define:"UNITY_INCLUDE_TESTS;UNITY_EDITOR" -out:"$OUT/Tests.EditMode.dll" "${MODS[@]}" -r:"$NUNIT" -r:"$OUT/Stubs.dll" -r:"$OUT/Hexhaven.Core.dll" "${FILES[@]}"
fi

collect "$ASSETS/Tests/PlayMode"
if [ ${#FILES[@]} -gt 0 ]; then
  step "Tests (PlayMode)"
  "${MCS[@]}" -define:"$DEFS;UNITY_INCLUDE_TESTS" -out:"$OUT/Tests.PlayMode.dll" "${MODS[@]}" -r:"$NUNIT" -r:"$OUT/Stubs.dll" -r:"$OUT/Hexhaven.Core.dll" -r:"$OUT/Hexhaven.Runtime.dll" "${FILES[@]}"
fi
echo "compile-check: PASS"
