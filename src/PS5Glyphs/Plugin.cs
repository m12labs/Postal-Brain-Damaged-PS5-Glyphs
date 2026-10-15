using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace PS5Glyphs;

[BepInPlugin(Guid, "PS5 Glyphs", Version)]
public sealed class Plugin : BasePlugin
{
    internal const string Guid = "pbd.ps5glyphs";
    internal const string Version = "1.0.1";
    internal static ManualLogSource Logger = null!;
    internal static ConfigEntry<bool> Enabled = null!;
    internal static ConfigEntry<bool> DumpAtlas = null!;
    internal static ConfigEntry<bool> RenameFallbackText = null!;
    /// <summary>Folder holding glyphs\*.png and glyph-map.json, next to the plugin DLL.</summary>
    internal static string AssetDir = "";
    /// <summary>Where DumpAtlas writes: BepInEx\PS5Glyphs.</summary>
    internal static string OutputDir = "";

    public override void Load()
    {
        Logger = Log;
        Enabled = Config.Bind("General", "Enabled", true, "Replace the Xbox controller glyphs with PS5 glyphs.");
        RenameFallbackText = Config.Bind("General", "RenameFallbackText", true,
            "When the game prints a button name instead of an icon, use PlayStation names (Cross, R1, Options...).");
        DumpAtlas = Config.Bind("Debug", "DumpAtlas", false,
            "Write the original icon atlas, glyph rects and controller icon tables to BepInEx\\PS5Glyphs\\dump.");
        AssetDir = Path.GetDirectoryName(typeof(Plugin).Assembly.Location)!;
        OutputDir = Path.Combine(Paths.BepInExRootPath, "PS5Glyphs");
        var harmony = new Harmony(Guid);
        if (Enabled.Value) harmony.PatchAll(typeof(LegendSprites));
        if (Enabled.Value && RenameFallbackText.Value) harmony.PatchAll(typeof(FallbackNames));
        AddComponent<GlyphBehaviour>();
        Log.LogInfo($"PS5 Glyphs {Version} loaded (enabled={Enabled.Value}, dump={DumpAtlas.Value}).");
    }
}
