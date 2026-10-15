using System.Text.Json;
using Hyperstrange.PBD;
using Rewired;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using InputManager = Hyperstrange.PBD.InputManager;

namespace PS5Glyphs;

/// <summary>
/// Writes what's needed to build glyph-map.json: the original atlas, one crop per sprite index,
/// and the game's own controller-element -> sprite index tables.
/// </summary>
internal static class AtlasDump
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private static string Dir => Path.Combine(Plugin.OutputDir, "dump");

    internal static void SpriteAsset(TMP_SpriteAsset asset)
    {
        var sheet = asset.spriteSheet;
        if (sheet == null) return;
        var atlas = TextureUtil.Read(sheet);
        TextureUtil.SavePng(atlas, Path.Combine(Dir, $"{asset.name}.atlas.png"));
        var sprites = new List<object>();
        var characters = asset.spriteCharacterTable;
        for (int i = 0; i < characters.Count; i++)
        {
            var ch = characters[i];
            var glyph = ch.glyph;
            if (glyph == null) { sprites.Add(new { index = i, ch.name, ch.unicode, missingGlyph = true }); continue; }
            var r = glyph.glyphRect;
            sprites.Add(new { index = i, ch.name, ch.unicode, glyphIndex = ch.glyphIndex, x = r.x, y = r.y, w = r.width, h = r.height });
            if (r.width > 0 && r.height > 0)
                TextureUtil.SavePng(TextureUtil.Crop(atlas, r.x, r.y, r.width, r.height), Path.Combine(Dir, asset.name, $"{i:D2}.png"));
        }
        File.WriteAllText(Path.Combine(Dir, $"{asset.name}.json"), JsonSerializer.Serialize(new
        {
            asset = asset.name, texture = sheet.name, sheet.width, sheet.height,
            format = sheet.graphicsFormat.ToString(), srgb = TextureUtil.IsSrgb(sheet), mipmaps = sheet.mipmapCount,
            material = asset.material?.name, shader = asset.material?.shader?.name, sprites
        }, Json));
        Plugin.Logger.LogInfo($"Dumped {asset.name}: {characters.Count} sprites, atlas {sheet.name} {sheet.width}x{sheet.height} {sheet.graphicsFormat}.");
    }

    /// <summary>Returns false while the game's input singletons aren't up yet.</summary>
    internal static bool InputTables()
    {
        var ism = Object.FindObjectOfType<InputStringManager>();
        if (ism == null || !ReInput.isReady) return false;
        var result = new Dictionary<string, object?>
        {
            ["joystickIcons"] = Icons(ism._joystickControllerInputIcons),
            ["mouseIcons"] = Icons(ism._mouseControllerInputIcons),
            ["joystickEnabled"] = SafeGet(() => InputManager.JoystickEnabled),
            ["joysticks"] = SafeGet(() => ReInput.controllers.GetJoysticks().Select(j => new { j.name, j.hardwareName, j.hardwareTypeGuid, type = j.type.ToString() }).ToList()),
        };
        var maps = new Dictionary<string, ControllerMap?>
        {
            ["settings.GameplayJoystickMap"] = SafeGet(() => SettingsManager.ControlsSettings?.GameplayJoystickMap),
        };
        var input = Object.FindObjectOfType<InputManager>();
        if (input != null)
        {
            maps["InputManager._gameplayJoystickMap"] = input._gameplayJoystickMap;
            maps["InputManager._menuJoystickMap"] = input._menuJoystickMap;
            maps["InputManager._gameplayMouseMap"] = input._gameplayMouseMap;
        }
        foreach (var (key, map) in maps) result[key] = map == null ? null : Elements(ism, map);
        Directory.CreateDirectory(Dir);
        File.WriteAllText(Path.Combine(Dir, "inputicons.json"), JsonSerializer.Serialize(result, Json));
        Plugin.Logger.LogInfo("Dumped controller icon tables to " + Path.Combine(Dir, "inputicons.json"));
        return true;
    }

    private static List<object> Icons(Il2CppSystem.Collections.Generic.List<InputStringManager.ControllerInputIcon>? list)
    {
        var rows = new List<object>();
        if (list == null) return rows;
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            rows.Add(new { e.InputId, e.IconId, axisRange = e.AxisRange.ToString() });
        }
        return rows;
    }

    private static List<object> Elements(InputStringManager ism, ControllerMap map)
    {
        var rows = new List<object>();
        foreach (var aem in map.GetElementMaps())
        {
            if (aem == null) continue;
            string? action = SafeGet(() => ReInput.mapping.GetAction(aem.actionId)?.name);
            string? joyIcon = SafeGet(() => ism.GetJoystickIconString(aem));
            string? mouseIcon = SafeGet(() => ism.GetMouseIconString(aem));
            rows.Add(new
            {
                action, aem.actionId, aem.elementIdentifierId, aem.elementIdentifierName,
                elementType = aem.elementType.ToString(), axisRange = aem.axisRange.ToString(), contribution = aem.axisContribution.ToString(),
                joystickIconString = joyIcon, mouseIconString = mouseIcon,
            });
        }
        return rows;
    }

    private static T? SafeGet<T>(Func<T> get)
    {
        try { return get(); }
        catch (Exception ex) { Plugin.Logger.LogDebug("Dump field unavailable: " + ex.Message); return default; }
    }
}
