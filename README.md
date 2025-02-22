TLDR: This does not work. If you want Godot C# mods, use [Modot](https://github.com/Carnagion/Modot).

# Status

Note: I don't really know what I'm doing here. If you're an expert in this area I'd welcome any suggestions.

I started this intending for it to be an example of how to get `LoadResourcePack()` to work with C# projects.
It is now more a demonstration of what doesn't work. In short: it works if you don't need to reference C# scripts across project boundaries.

| What | Works |
| ---- | ----- |
| Instantiating a C# script defined in a shared library | ✅ |
| Instantiating a scene after loading a `.pck` | ✅ |
| Instantiating a scene of the parent project from a script in the loaded `.pck` | ✅ |
| [Assigning a C# script defined in a shared library to a node](https://github.com/godotengine/godot/issues/75352) | ❌ |
| Locally defining a no-op subclass of a shared library class so you can assign it to a node | ✅ |
| Having shared scene resources refer to shared scripts | I couldn't figure out how to do this |
| Sharing non-C# resources as submodules | ✅ |
| Sharing C# scripts as submodules | Only if they have different paths AND different namespaces |

# Original goal (ideal dev flow)

Develop components of Godot games in as similar way to developing a regular Godot game with C# scripts. I.e.:
- Design scenes in the editor.
- Attach C# scripts.
- Run just the component to test it.

Later, load the component into a parent project and have things just work.

# Repos

- GodotNetModExample.Parent (this repo) is the parent project.
- https://github.com/kaydensigh/GodotNetModExample.Child is the component (aka mod), loaded by the parent at run-time.
- https://github.com/kaydensigh/GodotNetModExample.Shared is a design-time shared component used by both (or multiple children projects).

# Hacks

- `export.sh` runs the Godot export process and then pulls out the `.dll`s used by `Child` and puts them all in one directory.
- `Parent` loads all the `.dll`s before loading the `.pck`.
- We use a convention that components put all their resources into a path called `Mods/<ComponentName>/` so that when
  they're loaded into `Parent` they don't collide with parent resources or other components.
- `Shared` is included in both `Parent` and `Child` as a git submodule.
  - It is also a C# shared library.
  - The project defines a conditional compilation variable `NESTED_GODOT_LIB`.
  - Godot scripts have a preprocessor `#if NESTED_GODOT_LIB` so they can define different classes (with the same name) inside the component vs in the shared library.
    - These scripts are compiled by both the child project and the shared project.

# Why NESTED_GODOT_LIB

The goal was to let a shared scene with corresponding script be used in both `Parent` and `Child`.
The scene would refer to the script as `res://Shared/SceneInShared.cs`, and this would resolve correctly
in either project (since `res://Shared/` is a git submodule to `Shared`).

This causes the script to be compiled twice, by `Child` and `Shared`. If we exclude the script from `Child`, then Godot cannot
find it when instantiating the scene because it does not register classes unless the source file is included.

`NESTED_GODOT_LIB` allows us to compile the script in both projects, but with different code.
We make it so that the one in `Child` is a no-op subclass of the one in `Shared`.
They have to be the same class name (in different namespaces) because Godot requires the script and class names to match.

We do the same in `Parent` so now there are 3 classes called `SceneInShared` (one in each project), and 1 script at `res://Shared/SceneInShared.cs`.

This fails when we try to load it in `Parent` because `ScriptManagerBridge.LookupScriptsInAssembly()` does not allow different classes to have the same script path.

# What would be ideal

If Godot could refer to C# classes by class fullname instead of script path (https://github.com/godotengine/godot/issues/15661), it would solve a lot of the problems here.
You could then put all shared scripts in a separate C# project (https://github.com/godotengine/godot/issues/75352). You'd still need to share non-C# resources somehow, but git submodules seems sufficient.

Other nice-to-haves would be to:
- Load `.pck`s into an arbitrary directory, or in some other way isolate them. https://github.com/godotengine/godot-proposals/issues/2689
- Load `.dll`s from `.pck`s.
