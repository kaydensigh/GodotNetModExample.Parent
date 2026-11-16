#!/usr/bin/env bash
# Builds what a mod's repository needs of the host, into artifacts/sdk/:
#   - the host's assemblies, which a mod compiles against and, to run the host from its own project, loads
#   - the host's resources as one pack, which a mod's project mounts to run the host
# A real host would publish this folder as a NuGet package.
set -euo pipefail

cd "$(dirname "$0")"
GODOT="${GODOT_BIN_472:?Set GODOT_BIN_472 to the Godot 4.7.2 .NET executable}"
PROJECT_DIR="GodotNetModExample.Parent"
NAME="GodotNetModExample.Parent"
OUT="artifacts/sdk"

mkdir -p "$OUT"

# ExportDebug is the host as an exported game has it: no editor API, unlike Debug.
dotnet build "$PROJECT_DIR/$NAME.csproj" -c ExportDebug -v q --nologo
BIN="$PROJECT_DIR/.godot/mono/temp/bin/ExportDebug"
for file in "$BIN"/GodotNetModExample.*.dll "$BIN"/GodotNetModExample.*.pdb; do
    cp "$file" "$OUT/"
done

# The pack holds resources only. With embed_build_outputs off in the preset, the .NET publish Godot runs for
# every export lands beside the pack, in a data_* folder nothing here needs.
PACK_DIR="$PROJECT_DIR/.godot/sdk-pack"
mkdir -p "$PACK_DIR"
"$GODOT" --headless --path "$PROJECT_DIR" --export-pack "Windows Desktop" ".godot/sdk-pack/$NAME.pck"
cp "$PACK_DIR/$NAME.pck" "$OUT/"

ls -l "$OUT"
