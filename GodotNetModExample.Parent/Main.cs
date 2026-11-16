using System;
using System.Linq;
using Godot;
using GodotNetModExample.Parent.Host;
using GodotNetModExample.Shared;

public partial class Main : PanelContainer
{
    public override void _Ready()
    {
        // Before any scene that uses the shared library's scripts is loaded.
        SharedScripts.Register();

        var container = GetNode<Container>("Container");

        foreach (var mod in ModLoader.LoadAll())
        {
            string name = mod.Manifest.Name;
            GD.Print($"Mod {name}: {mod.Assembly.FullName}");

            // Instantiate the mod's entry scene, whose root is a class of the mod's that derives from the host's.
            var modScene = GD.Load<PackedScene>(mod.Manifest.EntryScene);
            var modSceneNode = modScene.Instantiate<ModBase>();
            modSceneNode.Name = "SceneInMod";
            modSceneNode.HostName = "GodotNetModExample.Parent";
            container.AddChild(modSceneNode);

            // Instantiate a class from the mod.
            var modClassType = mod.Assembly.GetType($"GodotNetModExample.Parent.Mods.{name}.GodotClassInMod");
            var modClassNode = (Control)Activator.CreateInstance(modClassType);
            modClassNode.Name = "GodotClassInMod";
            container.AddChild(modClassNode);
        }

        if (OS.GetCmdlineUserArgs().Contains("--dump-tree"))
            Callable.From(DumpTreeAndQuit).CallDeferred();
    }

    /// <summary>
    /// Prints every node with its class and any label's text, then quits: a run can be checked without a window.
    /// </summary>
    private void DumpTreeAndQuit()
    {
        Dump(this, 0);
        GetTree().Quit();

        static void Dump(Node node, int indent)
        {
            string text = node is Label label ? $" \"{label.Text}\"" : "";
            GD.Print($"{new string(' ', indent * 2)}{node.Name} [{node.GetType().FullName}]{text}");
            foreach (var child in node.GetChildren())
                Dump(child, indent + 1);
        }
    }
}
