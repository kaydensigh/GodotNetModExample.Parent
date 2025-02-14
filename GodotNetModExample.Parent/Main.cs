using System.Reflection;
using System;
using Godot;
using System.IO;
using System.Runtime.Loader;

public partial class Main : VBoxContainer
{
    public override void _Ready()
    {
        const string ModPath = "user://Mods/GodotNetModExample.Child/GodotNetModExample.Child";
        const string ModResourcePath = "res://Mods/GodotNetModExample.Child/";

        // https://github.com/godotengine/godot/issues/75352#issuecomment-2481814309
        var context = AssemblyLoadContext.GetLoadContext(typeof(Godot.Bridge.ScriptManagerBridge).Assembly);
        var assembly = context.LoadFromAssemblyPath(ProjectSettings.GlobalizePath(ModPath + ".dll"));
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(assembly);

        var loaded = ProjectSettings.LoadResourcePack(ModPath + ".pck", replaceFiles: false);
        GD.Print($"loaded {loaded}");

        foreach (var s in assembly.ExportedTypes)
            GD.Print(s);

        var modScene = GD.Load<PackedScene>(ModResourcePath + "SceneInMod.tscn");
        var modSceneNode = modScene.Instantiate<Control>();
        modSceneNode.Name = "SceneInMod_Main";
        AddChild(modSceneNode);

        var modClassType = assembly.GetType("GodotNetModExample.Parent.Mods.GodotNetModExample.Child.GodotClassInMod");
        var modClassNode = (Control)Activator.CreateInstance(modClassType);
        modClassNode.Name = "GodotClassInMod_Main";
        AddChild(modClassNode);
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
