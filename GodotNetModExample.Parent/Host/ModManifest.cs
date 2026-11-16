using System.IO;
using System.Text.Json;

namespace GodotNetModExample.Parent.Host;

/// <summary>
/// A mod's <c>mod.json</c>, which sits beside its assemblies and pack and says what they are.
/// </summary>
/// <param name="Name">The mod's name. Its resources are under <c>res://Mods/&lt;Name&gt;/</c>.</param>
/// <param name="Assembly">The file name of the assembly that holds the mod's scripts.</param>
/// <param name="Pack">The file name of the mod's resource pack.</param>
/// <param name="EntryScene">The res:// path of the scene the host shows, whose root is a <see cref="ModBase"/>.</param>
public sealed record ModManifest(string Name, string Assembly, string Pack, string EntryScene)
{
    public const string FileName = "mod.json";

    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static ModManifest Read(string modDir) =>
        JsonSerializer.Deserialize<ModManifest>(File.ReadAllText(Path.Combine(modDir, FileName)), Options)
        ?? throw new InvalidDataException($"{modDir}: {FileName} is empty.");
}
