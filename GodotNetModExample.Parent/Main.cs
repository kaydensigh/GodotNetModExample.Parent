using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Godot;
using NuGet.ProjectModel;

public partial class Main : PanelContainer
{
    public override void _Ready()
    {
        var modName = "GodotNetModExample.Child";
        var modAssembly = LoadMod(modName);

        var container = GetNode<Container>("Container");

        // Instantiate a scene from the mod.
        var ModResourcePath = $"res://Mods/{modName}/";
        var modScene = GD.Load<PackedScene>(ModResourcePath + "SceneInMod.tscn");
        var modSceneNode = modScene.Instantiate<Control>();
        modSceneNode.Name = "SceneInMod";
        container.AddChild(modSceneNode);

        // Instantiate a class from the mod.
        var modClassType = modAssembly.GetType($"{ModNamespace(modName)}.GodotClassInMod");
        var modClassNode = (Control)Activator.CreateInstance(modClassType);
        modClassNode.Name = "GodotClassInMod";
        container.AddChild(modClassNode);
    }

    private string ModPath(string modName) => $"user://Mods/{modName}/";
    private string ModNamespace(string modName) => $"GodotNetModExample.Parent.Mods.{modName}";

    private Assembly LoadMod(string modName)
    {
        var modPath = ModPath(modName);
        var modPathAbs = ProjectSettings.GlobalizePath(modPath);

        // This is what works in 4.3.
        // See https://github.com/godotengine/godot/issues/75352#issuecomment-2481814309
        var context = AssemblyLoadContext.GetLoadContext(typeof(Godot.Bridge.ScriptManagerBridge).Assembly);
        var assembly = context.LoadFromAssemblyPath(modPathAbs.PathJoin($"{modName}.dll"));
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(assembly);

        GD.Print($"Loaded {assembly.FullName}: ");

        // Load dependencies.
        var assets = LoadModAssetsJson(modName);
        var existingAssemblies = context.Assemblies.Select(_ => _.FullName.Split(',', 2)[0]);
        List<string> librariesToLoad = [];
        foreach (var l in assets.Targets.First().Libraries)
        {
            if (l.RuntimeAssemblies.Count > 0 && !existingAssemblies.Contains(l.Name))
            {
                var dllPath = modPathAbs.PathJoin($"{l.Name}.dll");
                if (File.Exists(dllPath))
                {
                    librariesToLoad.Add(dllPath);
                    GD.Print($"- {l.Name} {l.Version}");
                }
            }
        }
        GD.Print();
        if (librariesToLoad.Count > 0)
        {
            foreach (var dllPath in librariesToLoad)
            {
                context.LoadFromAssemblyPath(dllPath);
            }
        }

        GD.Print("Types:");
        foreach (var s in assembly.ExportedTypes)
        {
            if (!s.IsNested) GD.Print(s.Name);
        }
        GD.Print();

        // Don't replace files since we treat this as a mod not a patch.
        var loaded = ProjectSettings.LoadResourcePack(modPath.PathJoin($"{modName}.pck"), replaceFiles: false);
        GD.Print($"{(loaded ? "Loaded" : "Failed to load")} {modName}.pck");

        return assembly;
    }

    private LockFile LoadModAssetsJson(string modName)
    {
        var modPathAbs = ProjectSettings.GlobalizePath(ModPath(modName));
        var content = File.ReadAllText(modPathAbs.PathJoin("project.assets.json"));
        var lockFileFormat = new LockFileFormat();
        return lockFileFormat.Parse(content, "In Memory");
    }

    private void LsDir(string dir, int indent = 0)
    {
        foreach (var subdir in DirAccess.GetDirectoriesAt(dir))
        {
            GD.Print($"{new string(' ', indent)}{subdir}:");
            LsDir(dir.PathJoin(subdir), indent + 2);
        }
        foreach (var file in DirAccess.GetFilesAt(dir))
        {
            GD.Print($"{new string(' ', indent)}{file}");
        }
    }
}
