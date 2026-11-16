using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Godot;
using Godot.Bridge;

namespace GodotNetModExample.Parent.Host;

/// <summary>
/// Loads mods into the running host. A mod is a folder holding a <see cref="ModManifest"/>, the mod's assemblies,
/// its resource pack and a copy of its project's uid cache, as the mod's own repository builds it.
/// </summary>
public static class ModLoader
{
    /// <summary>A mod the host can show.</summary>
    public sealed record Mod(ModManifest Manifest, Assembly Assembly);

    public const string UidCacheFileName = "uid_cache.bin";

    /// <summary>
    /// Mods that are part of the running project already, so that nothing has to be loaded for them. A mod's own
    /// project adds itself here when it runs the host for development.
    /// </summary>
    public static List<Mod> Preloaded { get; } = [];

    /// <summary>The preloaded mods, then every mod under <paramref name="modsDir"/>, loaded.</summary>
    public static List<Mod> LoadAll(string modsDir = "user://Mods")
    {
        var mods = new List<Mod>(Preloaded);
        string dir = ProjectSettings.GlobalizePath(modsDir);
        if (!Directory.Exists(dir))
            return mods;

        foreach (string modDir in Directory.GetDirectories(dir).Order())
        {
            if (!File.Exists(Path.Combine(modDir, ModManifest.FileName)))
                continue;
            try
            {
                mods.Add(Load(modDir));
            }
            catch (Exception e)
            {
                // One broken mod must not stop the host or the other mods.
                GD.PushError($"Mod at {modDir} was not loaded: {e}");
            }
        }
        return mods;
    }

    private static Mod Load(string modDir)
    {
        var manifest = ModManifest.Read(modDir);

        // The mod's assembly goes into the load context the host's own assembly is in, so that its references to
        // the host, to the host's libraries and to GodotSharp are the assemblies already loaded there, never
        // copies with types of their own. Run from the editor that is Godot's context for the project; exported,
        // the default one.
        var context = AssemblyLoadContext.GetLoadContext(typeof(ModLoader).Assembly)!;

        // A reference of the mod's that nothing has loaded yet is one of the mod's own libraries, in its folder.
        // The context asks only for those: an assembly it already holds is used without asking, so a copy of a
        // host assembly left in a mod's folder is never loaded.
        context.Resolving += (ctx, name) =>
        {
            string path = Path.Combine(modDir, name.Name + ".dll");
            return File.Exists(path) ? ctx.LoadFromAssemblyPath(path) : null;
        };

        var assembly = context.LoadFromAssemblyPath(Path.Combine(modDir, manifest.Assembly));

        // Godot registers the scripts of the project's own assembly only. Until a mod's are registered, its scenes
        // load without them.
        ScriptManagerBridge.LookupScriptsInAssembly(assembly);

        // Never replacing files: a mod adds to the host and cannot change it.
        string pack = Path.Combine(modDir, manifest.Pack);
        if (!ProjectSettings.LoadResourcePack(pack, replaceFiles: false))
            throw new IOException($"Godot could not load {pack}.");

        RegisterUids(Path.Combine(modDir, UidCacheFileName), $"res://Mods/{manifest.Name}/");

        return new Mod(manifest, assembly);
    }

    /// <summary>
    /// Registers the uid of each of a mod's resources. Godot reads a pack's uids only when the pack replaces
    /// files, since every project keeps them at the one path, <c>res://.godot/uid_cache.bin</c>, where the host's
    /// already is. So a mod ships a copy of its project's cache beside the pack, which is read here: a count,
    /// then for each resource its id, the length of its path and the path.
    /// </summary>
    private static void RegisterUids(string uidCachePath, string modRoot)
    {
        if (!File.Exists(uidCachePath))
            return;

        using var reader = new BinaryReader(File.OpenRead(uidCachePath));
        uint count = reader.ReadUInt32();
        for (uint i = 0; i < count; i++)
        {
            long id = reader.ReadInt64();
            string path = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));

            // The mod's project also knows the uids of the host resources it holds copies of. Those are the host's.
            if (!path.StartsWith(modRoot, StringComparison.Ordinal))
                continue;

            if (ResourceUid.HasId(id))
                ResourceUid.SetId(id, path);
            else
                ResourceUid.AddId(id, path);
        }
    }
}
