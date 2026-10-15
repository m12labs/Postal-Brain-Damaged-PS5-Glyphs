using System.Text.Json;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

namespace PS5Glyphs;

/// <summary>
/// Restamps the gamepad slots of the game's TMP button atlases with PS5 symbols (see GlyphStamp).
/// InputStringManager emits "&lt;sprite=N&gt;", so swapping the atlas pixels covers every prompt at once.
/// </summary>
internal static class GlyphAtlas
{
    /// <summary>The game's sprite assets that hold controller icons (both share one atlas texture).</summary>
    internal static readonly string[] TargetAssets = { "SpriteAsset_Buttons", "SpriteAsset_Buttons_Text" };

    // sprite index (the N in <sprite=N>) -> button name, per asset; "*" applies to any target asset.
    private static Dictionary<string, Dictionary<int, string>>? _map;
    // Standalone UI sprite name (the menu legends' Input_Xbox_A...) -> button name, from the map's "sprites" section.
    private static readonly Dictionary<string, string> SpriteMap = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, Image32?> Icons = new(StringComparer.OrdinalIgnoreCase);
    // Original atlas instance id -> repainted replacement, so both assets and every material share one texture.
    private static readonly Dictionary<int, Texture2D> Replacements = new();

    internal static bool HasMap => LoadMap().Count > 0;

    internal static Dictionary<string, Dictionary<int, string>> LoadMap()
    {
        if (_map != null) return _map;
        _map = new(StringComparer.OrdinalIgnoreCase);
        var path = Path.Combine(Plugin.AssetDir, "glyph-map.json");
        if (!File.Exists(path)) { Plugin.Logger.LogWarning("glyph-map.json not found next to the plugin; atlas left unchanged."); return _map; }
        var raw = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(path),
            new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true })!;
        foreach (var (asset, entries) in raw)
            if (asset == "sprites") foreach (var (sprite, button) in entries) SpriteMap[sprite] = button;
            else _map[asset] = entries.Where(e => int.TryParse(e.Key, out _)).ToDictionary(e => int.Parse(e.Key), e => e.Value);
        return _map;
    }

    private static Dictionary<int, string>? MapFor(string assetName)
    {
        var map = LoadMap();
        return map.TryGetValue(assetName, out var m) ? m : map.TryGetValue("*", out var any) ? any : null;
    }

    private static Image32? Icon(string button)
    {
        if (Icons.TryGetValue(button, out var cached)) return cached;
        var path = Path.Combine(Plugin.AssetDir, "glyphs", button + ".png");
        Image32? icon = null;
        if (File.Exists(path)) icon = TextureUtil.LoadPng(path);
        else Plugin.Logger.LogWarning($"Missing glyph image glyphs\\{button}.png; that slot keeps the Xbox icon.");
        Icons[button] = icon;
        return icon;
    }

    /// <summary>Repaints the asset's atlas (once per original texture) and points the asset at the result.</summary>
    internal static bool Apply(TMP_SpriteAsset asset)
    {
        var original = asset.spriteSheet;
        if (original == null) return false;
        if (original.name.EndsWith("_PS5", StringComparison.Ordinal)) return true;
        int id = original.GetInstanceID();
        if (!Replacements.TryGetValue(id, out var replacement))
        {
            var map = MapFor(asset.name);
            if (map == null || map.Count == 0) return false;
            var atlas = TextureUtil.Read(original);
            int painted = 0;
            var characters = asset.spriteCharacterTable;
            foreach (var (index, button) in map)
            {
                if (index < 0 || index >= characters.Count) { Plugin.Logger.LogWarning($"{asset.name}: sprite {index} is out of range."); continue; }
                var glyph = characters[index].glyph;
                var icon = Icon(button);
                if (glyph == null || icon == null) continue;
                var r = glyph.glyphRect;
                if (GlyphStamp.Apply(atlas, r.x, r.y, r.width, r.height, icon)) painted++;
                else Plugin.Logger.LogWarning($"{asset.name}: sprite {index} has no label to replace; left as is.");
            }
            replacement = TextureUtil.ToTexture(atlas, original.name + "_PS5", !TextureUtil.IsSrgb(original), original);
            Replacements[id] = replacement;
            Plugin.Logger.LogInfo($"Repainted {painted} PS5 glyphs into {original.name} ({original.width}x{original.height}).");
        }
        asset.spriteSheet = replacement;
        if (asset.material != null) asset.material.SetTexture(ShaderUtilities.ID_MainTex, replacement);
        Plugin.Logger.LogInfo($"{asset.name} now uses PS5 glyphs.");
        return true;
    }

    /// <summary>TMP clones sprite materials for masking; repoint any clone made before the swap.</summary>
    internal static int RetargetMaterials()
    {
        if (Replacements.Count == 0) return 0;
        int changed = 0;
        foreach (var material in Resources.FindObjectsOfTypeAll<Material>())
        {
            if (material == null || !material.HasProperty(ShaderUtilities.ID_MainTex)) continue;
            var tex = material.GetTexture(ShaderUtilities.ID_MainTex);
            if (tex != null && Replacements.TryGetValue(tex.GetInstanceID(), out var replacement))
            {
                material.SetTexture(ShaderUtilities.ID_MainTex, replacement);
                changed++;
            }
        }
        return changed;
    }

    /// <summary>
    /// UI already on screen (the main menu legend) keeps the texture its CanvasRenderer was given when it was built,
    /// so redraw it; and swap any UI Image whose sprite is cut from the original atlas.
    /// </summary>
    internal static (int Text, int Images) RefreshUi()
    {
        int text = 0, images = 0;
        foreach (var sub in Resources.FindObjectsOfTypeAll<TMP_SubMeshUI>())
        {
            if (sub == null || sub.sharedMaterial == null) continue;
            var tex = sub.sharedMaterial.GetTexture(ShaderUtilities.ID_MainTex);
            if (tex == null || !IsReplacement(tex)) continue;
            sub.SetMaterialDirty();
            text++;
        }
        foreach (var image in Resources.FindObjectsOfTypeAll<Image>())
        {
            if (image == null) continue;
            if (SwapImage(image)) images++;
        }
        return (text, images);
    }

    /// <summary>Points an Image at the PS5 version of its sprite, if there is one. Returns true when it changed.</summary>
    internal static bool SwapImage(Image image)
    {
        var sprite = image.sprite;
        if (sprite == null) return false;
        var ps5 = SpriteFor(sprite);
        if (ps5 == null || ps5.GetInstanceID() == sprite.GetInstanceID()) return false;
        image.sprite = ps5;
        return true;
    }

    // Original sprite instance id -> PS5 sprite, or null when the sprite needs no change.
    private static readonly Dictionary<int, Sprite?> Sprites = new();
    private static readonly HashSet<int> Made = new();

    private static Sprite? SpriteFor(Sprite original)
    {
        LoadMap();
        int id = original.GetInstanceID();
        if (Made.Contains(id)) return null;
        if (Sprites.TryGetValue(id, out var cached)) return cached;
        Sprite? result = null;
        var tex = original.texture;
        if (tex != null && Replacements.TryGetValue(tex.GetInstanceID(), out var replacement))
            result = Rebuild(original, replacement, original.textureRect);
        else if (tex != null && SpriteMap.TryGetValue(original.name, out var button) && Icon(button) is { } icon)
        {
            var r = original.textureRect;
            int x = (int)r.x, y = (int)r.y, w = (int)r.width, h = (int)r.height;
            var pixels = TextureUtil.Crop(TextureUtil.Read(tex), x, y, w, h);
            if (Plugin.DumpAtlas.Value) TextureUtil.SavePng(pixels, Path.Combine(Plugin.OutputDir, "dump", "sprites", original.name + ".png"));
            if (GlyphStamp.Apply(pixels, 0, 0, w, h, icon))
            {
                if (Plugin.DumpAtlas.Value) TextureUtil.SavePng(pixels, Path.Combine(Plugin.OutputDir, "dump", "sprites", original.name + ".ps5.png"));
                var texture = TextureUtil.ToTexture(pixels, original.name + "_PS5", !TextureUtil.IsSrgb(tex), tex);
                result = Rebuild(original, texture, new Rect(0, 0, w, h));
                Plugin.Logger.LogInfo($"Restamped UI sprite {original.name} ({w}x{h}) as {button}.");
            }
            else Plugin.Logger.LogWarning($"UI sprite {original.name} has no label to replace; left as is.");
        }
        if (result != null) Made.Add(result.GetInstanceID());
        return Sprites[id] = result;
    }

    /// <summary>Same sprite geometry (pivot, border, pixels per unit) on a new texture region.</summary>
    private static Sprite Rebuild(Sprite original, Texture2D texture, Rect rect)
    {
        var offset = original.textureRectOffset;
        var pivot = new Vector2((original.pivot.x - offset.x) / rect.width, (original.pivot.y - offset.y) / rect.height);
        var sprite = Sprite.Create(texture, rect, pivot, original.pixelsPerUnit, 0, SpriteMeshType.FullRect, original.border);
        sprite.name = original.name;
        sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return sprite;
    }

    private static bool IsReplacement(Texture tex)
    {
        int id = tex.GetInstanceID();
        foreach (var replacement in Replacements.Values) if (replacement.GetInstanceID() == id) return true;
        return false;
    }
}
