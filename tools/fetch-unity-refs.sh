#!/usr/bin/env bash
# Downloads reference assemblies used by compile-check.sh into tools/.refs (≈ 60 MB). Idempotent.
#   - UnityEngine.Modules 2021.3.33 (UnityEngine.*Module.dll, incl. UIElements)   https://www.nuget.org/packages/UnityEngine.Modules
#   - Unity3D.SDK 2021.1.14.1 (UnityEditor.dll)                                     https://www.nuget.org/packages/Unity3D.SDK
#   - NUnit 3.13.3 (nunit.framework.dll)
# These are API-reference builds only (no engine); they let agents type-check Runtime/Editor/Test code without Unity installed.
set -euo pipefail
DIR="$(cd "$(dirname "$0")" && pwd)/.refs"
mkdir -p "$DIR"
fetch () { # id version subdir
  local id="$1" ver="$2" sub="$3"
  if [ -d "$DIR/$sub" ]; then return; fi
  echo "fetching $id $ver"
  curl -fsSL "https://api.nuget.org/v3-flatcontainer/$id/$ver/$id.$ver.nupkg" -o "$DIR/$sub.nupkg"
  mkdir -p "$DIR/$sub" && (cd "$DIR/$sub" && unzip -oq "../$sub.nupkg") && rm -f "$DIR/$sub.nupkg"
}
fetch unityengine.modules 2021.3.33 modules
fetch unity3d.sdk 2021.1.14.1 sdk
fetch nunit 3.13.3 nunit
echo "refs ready in $DIR"
