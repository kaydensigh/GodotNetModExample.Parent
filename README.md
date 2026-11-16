# Godot .NET mod example

A Godot C# project (the parent, or host) that loads another Godot C# project (the child, or mod) at run time:
the mod's assemblies and a `.pck` of its resources. Built and checked with Godot 4.7.2 and .NET 10.

Godot has no support for this. It works here because the projects are arranged around what Godot does today,
which the [rules](#rules) below set out. If you want a library that does it for you, see
[Modot](https://github.com/Carnagion/Modot).

# What works

| What | Works |
| ---- | ----- |
| Instantiating a C# script defined in a shared library | ✅ |
| Instantiating a scene after loading a `.pck` | ✅ |
| Instantiating a scene of the parent project from a script in the loaded `.pck` | ✅ |
| A mod class deriving from a class of the parent's own assembly, which the parent then uses as that class | ✅ |
| A scene with a node whose script is defined in a shared library, in the parent, in the mod and in the mod's own project | ✅ at run time |
| A shared scene referring to shared scripts | ✅ at run time |
| A mod with a library of its own | ✅ |
| `uid://` references between a mod's resources | ✅ with the mod's uid cache shipped beside its pack |
| Running the mod's project on its own, with the parent in it | ✅ |
| The editor's inspector for a script defined in a library | ❌ |
| Changing or unloading a mod while the parent runs | ❌ |

The editor knows the scripts of the project's own assembly and no others. A node with a library's script on it
shows none of that script's exported properties in the inspector, though values written in the `.tscn` are kept.
Registering the library from a `[Tool]` autoload makes the editor list them, but a value set that way did not
survive a save when tried. So a project's scenes should put only the project's own scripts on nodes, and make
anything else in code or by instancing its scene.

Godot cannot unmount a pack and the mod's assembly is not unloaded, so a mod changes when the parent restarts.

# Rules

1. **One class for each `res://` path, compiled once.** Godot finds a script's class by its `res://` path, which
   Godot's source generator works out from where the `.cs` file is under `GodotProjectDir`, and it refuses two
   classes with one path. A Godot project compiles every `.cs` file under it, so a library in a folder of the
   project (`Shared/`) has to be taken out of the project's compile, and has to set `GodotProjectDir` to the
   project's folder so that its classes get the paths the scenes use. See
   [Shared's csproj](https://github.com/kaydensigh/GodotNetModExample.Shared).
2. **Register every assembly but the project's.** Godot registers the scripts of the project's own assembly.
   Any other needs `ScriptManagerBridge.LookupScriptsInAssembly` called on it before a scene that uses its
   scripts is loaded. For a scene the project starts with, that means from an autoload's constructor, since
   Godot loads the main scene before any autoload enters the tree.
3. **Load a mod's assembly into the load context the host's assembly is in**, not the one GodotSharp is in. Run
   from the editor they differ, and a mod loaded into the wrong one gets copies of the host's libraries with
   types of their own. What the mod references that is not loaded yet comes from the mod's folder.
4. **A mod's resources live under `res://Mods/<Name>/`** in its own project, so they have the same paths when
   the host mounts its pack. The pack is mounted without replacing files, so a mod cannot change the host. That
   also keeps Godot from reading the pack's uids, so the mod ships its project's `uid_cache.bin` beside the pack
   and the host registers the entries under the mod's folder.
5. **The pack holds resources only.** With `dotnet/embed_build_outputs` on, an exported pack carries the whole
   .NET runtime.

[ModLoader](GodotNetModExample.Parent/Host/ModLoader.cs) is the host's side of rules 2 to 4.

# Repos

- GodotNetModExample.Parent (this repo) is the host. Its own assembly holds
  [ModBase](GodotNetModExample.Parent/Host/ModBase.cs), which a mod's entry scene derives from.
  `export-sdk.sh` builds what a mod's repository needs of it into `artifacts/sdk/`: its assemblies and its
  resources as one pack. A real host would publish that folder as a NuGet package.
- https://github.com/kaydensigh/GodotNetModExample.Child is the mod. It compiles against the host's SDK.
  `build-mod.sh` builds the folder the host loads: `mod.json`, the mod's assemblies without the host's, its pack
  and its uid cache. Its project's main scene, `Mods/<Name>/Dev/RunInHost.tscn`, runs the host from the SDK with
  the mod in it: the host's assembly is an ordinary reference there and its pack is mounted, so the mod's scenes
  and scripts are the project's own and nothing is exported to run it. Nothing under `Dev/` is shipped.
- https://github.com/kaydensigh/GodotNetModExample.Shared is a library of scripts and scenes, a git submodule of
  both. The host compiles it. The mod has it for its scenes and resources, so that the editor can show them, and
  takes its classes from the host's SDK.

# Building and running

With `GODOT_BIN_472` set to the Godot 4.7.2 .NET executable, and the two repositories side by side, each cloned
with its submodule (`git clone --recurse-submodules`):

```bash
# The host's SDK, which the mod compiles against
(cd GodotNetModExample.Parent && bash export-sdk.sh)

# The mod's own project, running the host with the mod in it. The import is needed once in a new clone: until
# Godot has scanned the project, it has no uid cache and warns of each uid:// reference in the mod's scenes.
cd GodotNetModExample.Child
dotnet build
"$GODOT_BIN_472" --headless --path GodotNetModExample.Child --import
"$GODOT_BIN_472" --path GodotNetModExample.Child

# The mod as the host loads it, built into the host's user://Mods
bash build-mod.sh "$APPDATA/Godot/app_userdata/GodotNetModExample.Parent/Mods/GodotNetModExample.Child"
cd ../GodotNetModExample.Parent
dotnet build
"$GODOT_BIN_472" --path GodotNetModExample.Parent
```

Either run takes `--headless -- --dump-tree`, which prints the scene tree and quits. An exported host loads the
mod the same way.

# What would be ideal

If Godot could refer to C# classes by class fullname instead of script path
(https://github.com/godotengine/godot/issues/15661), rules 1 and 2 would go, and the editor would know a
library's scripts (https://github.com/godotengine/godot/issues/75352). A proof of concept that registers the
scripts of referenced assemblies is open at https://github.com/godotengine/godot/pull/117452.

Other nice-to-haves would be to:
- Load `.pck`s into an arbitrary directory, or in some other way isolate them. https://github.com/godotengine/godot-proposals/issues/2689
- Load `.dll`s from `.pck`s.
