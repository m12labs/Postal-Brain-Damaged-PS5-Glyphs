using HarmonyLib;
using Hyperstrange.PBD.UI.Menus;
using UnityEngine.UI;

namespace PS5Glyphs;

/// <summary>
/// Menu legends ("A Confirm", "B Back") use standalone Input_Xbox_* sprites, not the TMP atlas.
/// Swap them each time a legend is shown, which also covers menus instantiated after the last scene scan.
/// </summary>
[HarmonyPatch]
internal static class LegendSprites
{
    [HarmonyPatch(typeof(MenuLegendComponent), nameof(MenuLegendComponent.Show))]
    [HarmonyPostfix]
    private static void Shown(MenuLegendComponent __instance)
    {
        try
        {
            int swapped = 0;
            foreach (var image in __instance.GetComponentsInChildren<Image>(true))
                if (image != null && GlyphAtlas.SwapImage(image)) swapped++;
            if (swapped > 0) Plugin.Logger.LogInfo($"Menu legend: {swapped} button sprite(s) switched to PS5.");
        }
        catch (Exception ex) { Plugin.Logger.LogError("Legend sprite swap failed: " + ex); }
    }
}
