#!/usr/bin/env bash
# Builds the Core sources + Sim.cs and runs the harness. Usage:
#   tools/sim/run-sim.sh [args...]            e.g.  10000 | --random-legal 2000 | --selftest board | --one 42 --verbose
# Env: CORE_DIR (default: game/Assets/Hexhaven/Scripts/Core; use scaffold/Assets/Hexhaven/Scripts/Core before M0)
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
CORE_DIR="${CORE_DIR:-$ROOT/game/Assets/Hexhaven/Scripts/Core}"
OUT="${SIM_OUT:-$ROOT/.sim-build}"
mkdir -p "$OUT"
SOURCES=(); while IFS= read -r f; do SOURCES+=("$f"); done < <(find "$CORE_DIR" -name "*.cs" | sort)
[ ${#SOURCES[@]} -gt 0 ] || { echo "No Core sources under $CORE_DIR"; exit 2; }

if command -v mcs >/dev/null 2>&1; then
  mcs -langversion:7.2 -optimize+ -define:HEXHAVEN_TESTS -nowarn:1591 -out:"$OUT/Sim.exe" "$ROOT/tools/sim/Sim.cs" "${SOURCES[@]}"
  exec mono "$OUT/Sim.exe" "$@"
elif command -v dotnet >/dev/null 2>&1; then
  # On Git Bash / MSYS, dotnet needs Windows-style paths inside the project file.
  P_ROOT="$ROOT"; P_CORE="$CORE_DIR"
  if command -v cygpath >/dev/null 2>&1; then P_ROOT="$(cygpath -m "$ROOT")"; P_CORE="$(cygpath -m "$(cd "$CORE_DIR" && pwd)")"; fi
  cat > "$OUT/Sim.csproj" <<PROJ
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>7.2</LangVersion>
    <DefineConstants>HEXHAVEN_TESTS</DefineConstants><Nullable>disable</Nullable><ImplicitUsings>disable</ImplicitUsings>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems><Optimize>true</Optimize>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$P_ROOT/tools/sim/Sim.cs" />
    <Compile Include="$P_CORE/**/*.cs" />
  </ItemGroup>
</Project>
PROJ
  dotnet run -c Release --project "$OUT/Sim.csproj" -- "$@"
else
  echo "Need Mono (apt-get install mono-mcs mono-runtime) or the .NET 8 SDK."; exit 2
fi
