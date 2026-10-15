using HarmonyLib;
using Hyperstrange.PBD;

namespace PS5Glyphs;

/// <summary>
/// When InputStringManager has no icon for an element it returns Rewired's element name ("Right Shoulder").
/// This only touches the joystick path, so keyboard keys named A/B/X/Y are never renamed.
/// </summary>
[HarmonyPatch]
internal static class FallbackNames
{
    private static readonly Dictionary<string, string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        ["A"] = "Cross", ["B"] = "Circle", ["X"] = "Square", ["Y"] = "Triangle",
        ["Action Bottom Row 1"] = "Cross", ["Action Bottom Row 2"] = "Circle",
        ["Action Top Row 1"] = "Square", ["Action Top Row 2"] = "Triangle",
        ["Left Shoulder"] = "L1", ["Left Bumper"] = "L1", ["LB"] = "L1", ["Left Shoulder 1"] = "L1",
        ["Right Shoulder"] = "R1", ["Right Bumper"] = "R1", ["RB"] = "R1", ["Right Shoulder 1"] = "R1",
        ["Left Trigger"] = "L2", ["LT"] = "L2", ["Left Shoulder 2"] = "L2",
        ["Right Trigger"] = "R2", ["RT"] = "R2", ["Right Shoulder 2"] = "R2",
        ["Back"] = "Create", ["View"] = "Create", ["Select"] = "Create", ["Center 1"] = "Create",
        ["Start"] = "Options", ["Menu"] = "Options", ["Center 2"] = "Options",
        ["Guide"] = "PS", ["Xbox"] = "PS", ["Center 3"] = "PS",
        ["Left Stick Button"] = "L3", ["Left Stick Click"] = "L3", ["LS"] = "L3", ["Left Thumb"] = "L3",
        ["Right Stick Button"] = "R3", ["Right Stick Click"] = "R3", ["RS"] = "R3", ["Right Thumb"] = "R3",
    };

    [HarmonyPatch(typeof(InputStringManager), nameof(InputStringManager.GetJoystickIconString))]
    [HarmonyPostfix]
    private static void Rename(ref string __result)
    {
        if (string.IsNullOrEmpty(__result) || __result.Contains("<sprite", StringComparison.Ordinal)) return;
        if (Names.TryGetValue(__result.Trim(), out var ps)) __result = ps;
    }
}
