using Godot;

namespace GodotNetModExample.Parent.Host;

/// <summary>
/// The root of a mod's entry scene: what the host knows a mod by. It lives in the host's own assembly, which a
/// mod compiles against and which is already loaded when the mod is.
/// </summary>
public abstract partial class ModBase : PanelContainer
{
    /// <summary>Set by the host before the mod's scene enters the tree.</summary>
    public string HostName { get; set; } = "(no host)";
}
