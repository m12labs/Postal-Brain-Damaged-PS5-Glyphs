using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PS5Glyphs;

/// <summary>Watches for the button sprite assets (they load with the first menu scene) and swaps them.</summary>
public sealed class GlyphBehaviour : MonoBehaviour
{
    public GlyphBehaviour(IntPtr pointer) : base(pointer) { }

    private readonly HashSet<int> _handled = new();
    private readonly HashSet<int> _dumped = new();
    private bool _inputDumped, _failed;
    private float _nextScan;
    private int _scene = int.MinValue;
    private bool _retarget;
    private float _refreshAgainAt;

    private static bool Dumping => Plugin.DumpAtlas.Value;

    public void Update()
    {
        if (_failed) return;
        try
        {
            int scene = SceneManager.GetActiveScene().handle;
            if (scene != _scene) { _scene = scene; _nextScan = 0; _retarget = true; }
            if (Time.unscaledTime >= _nextScan)
            {
                _nextScan = Time.unscaledTime + 1f;
                Scan();
            }
        }
        catch (Exception ex)
        {
            _failed = true;
            Plugin.Logger.LogError("PS5 Glyphs stopped after an error: " + ex);
        }
    }

    private void Scan()
    {
        foreach (var asset in Resources.FindObjectsOfTypeAll<TMP_SpriteAsset>())
        {
            if (asset == null || !GlyphAtlas.TargetAssets.Contains(asset.name)) continue;
            int id = asset.GetInstanceID();
            if (Dumping && _dumped.Add(id)) AtlasDump.SpriteAsset(asset);
            if (Plugin.Enabled.Value && !_handled.Contains(id) && GlyphAtlas.HasMap && GlyphAtlas.Apply(asset))
            {
                _handled.Add(id);
                _retarget = true;
                _refreshAgainAt = Time.unscaledTime + 3f; // the main menu may still be assembling its legend
            }
        }
        if (Dumping && !_inputDumped) _inputDumped = AtlasDump.InputTables();
        if (_refreshAgainAt > 0 && Time.unscaledTime >= _refreshAgainAt) { _refreshAgainAt = 0; _retarget = true; }
        if (_retarget && _handled.Count > 0)
        {
            _retarget = false;
            int n = GlyphAtlas.RetargetMaterials();
            var (text, images) = GlyphAtlas.RefreshUi();
            if (n + text + images > 0) Plugin.Logger.LogInfo($"PS5 atlas applied to on-screen UI: {n} material(s), {text} text sprite mesh(es), {images} image(s).");
        }
    }
}
