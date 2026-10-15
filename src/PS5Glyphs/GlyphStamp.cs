using UnityEngine;

namespace PS5Glyphs;

/// <summary>
/// Turns one of the game's hand-drawn Xbox buttons into its PS5 counterpart: the button shape (outline, fill, shadow)
/// is kept and only the label inside it ("A", "LB", the View icon) is erased and replaced by a symbol overlay.
/// Works for any button size; the symbol is centred on where the old label was.
/// </summary>
internal static class GlyphStamp
{
    private const int AlphaSolid = 128, DarkLuma = 110;
    /// <summary>Overlays are drawn for the 70px slots of SpriteAsset_Buttons; other button sizes scale them.</summary>
    private const float OverlaySlot = 70f;

    internal static bool Apply(Image32 atlas, int rx, int ry, int w, int h, Image32 overlay)
    {
        if (w <= 0 || h <= 0) return false;
        float scale = Math.Min(w, h) / OverlaySlot;
        if (MathF.Abs(scale - 1f) > 0.01f) overlay = Resize(overlay, Math.Max(1, (int)MathF.Round(overlay.Width * scale)), Math.Max(1, (int)MathF.Round(overlay.Height * scale)));
        Color32 Px(int x, int y) => atlas.Pixels[(ry + y) * atlas.Width + rx + x];
        bool In(int x, int y) => x >= 0 && y >= 0 && x < w && y < h;

        // Transparent background reachable from the slot border.
        var outside = new bool[w, h];
        var queue = new Queue<(int, int)>();
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                if ((x == 0 || y == 0 || x == w - 1 || y == h - 1) && Px(x, y).a < AlphaSolid) { outside[x, y] = true; queue.Enqueue((x, y)); }
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            foreach (var (nx, ny) in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
                if (In(nx, ny) && !outside[nx, ny] && Px(nx, ny).a < AlphaSolid) { outside[nx, ny] = true; queue.Enqueue((nx, ny)); }
        }

        // Dark ink split into components: those touching the background are the outline/shadow, the rest is the label.
        var comp = new int[w, h];
        for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) comp[x, y] = -1;
        bool Dark(int x, int y) { var c = Px(x, y); return c.a >= AlphaSolid && (c.r * 299 + c.g * 587 + c.b * 114) / 1000 < DarkLuma; }
        var keep = new HashSet<int>();
        int count = 0;
        for (int sx = 0; sx < w; sx++)
            for (int sy = 0; sy < h; sy++)
            {
                if (comp[sx, sy] >= 0 || !Dark(sx, sy)) continue;
                comp[sx, sy] = count;
                queue.Enqueue((sx, sy));
                while (queue.Count > 0)
                {
                    var (x, y) = queue.Dequeue();
                    for (int i = -1; i <= 1; i++)
                        for (int j = -1; j <= 1; j++)
                        {
                            int nx = x + i, ny = y + j;
                            if ((i == 0 && j == 0) || !In(nx, ny)) continue;
                            if (outside[nx, ny]) keep.Add(count);
                            else if (comp[nx, ny] < 0 && Dark(nx, ny)) { comp[nx, ny] = count; queue.Enqueue((nx, ny)); }
                        }
                }
                count++;
            }

        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
        var label = new List<(int, int)>();
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                if (comp[x, y] >= 0 && !keep.Contains(comp[x, y]))
                {
                    label.Add((x, y));
                    minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
                }
        if (label.Count == 0) return false;

        // Whiten the label plus its anti-aliased fringe, never touching the outline.
        bool NearOutline(int x, int y)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (In(x + i, y + j) && comp[x + i, y + j] >= 0 && keep.Contains(comp[x + i, y + j])) return true;
            return false;
        }
        foreach (var (x, y) in label)
            for (int i = -2; i <= 2; i++)
                for (int j = -2; j <= 2; j++)
                {
                    int nx = x + i, ny = y + j;
                    if (!In(nx, ny) || outside[nx, ny] || NearOutline(nx, ny)) continue;
                    int idx = (ry + ny) * atlas.Width + rx + nx;
                    atlas.Pixels[idx] = new Color32(255, 255, 255, atlas.Pixels[idx].a);
                }

        // Centre the overlay where the label was.
        float cx = (minX + maxX + 1) / 2f, cy = (minY + maxY + 1) / 2f;
        int ox = (int)MathF.Round(cx - overlay.Width / 2f), oy = (int)MathF.Round(cy - overlay.Height / 2f);
        for (int oyi = 0; oyi < overlay.Height; oyi++)
            for (int oxi = 0; oxi < overlay.Width; oxi++)
            {
                int x = ox + oxi, y = oy + oyi;
                if (!In(x, y)) continue;
                var s = overlay.Pixels[oyi * overlay.Width + oxi];
                if (s.a == 0) continue;
                int idx = (ry + y) * atlas.Width + rx + x;
                var d = atlas.Pixels[idx];
                float sa = s.a / 255f, da = d.a / 255f, oa = sa + da * (1 - sa);
                byte Mix(byte sc, byte dc) => (byte)Math.Clamp((int)MathF.Round((sc * sa + dc * da * (1 - sa)) / oa), 0, 255);
                atlas.Pixels[idx] = new Color32(Mix(s.r, d.r), Mix(s.g, d.g), Mix(s.b, d.b), (byte)MathF.Round(oa * 255f));
            }
        return true;
    }

    /// <summary>Box/bilinear resample with premultiplied alpha, good for both shrinking and enlarging the overlays.</summary>
    private static Image32 Resize(Image32 src, int w, int h)
    {
        var dst = new Image32(w, h);
        float sx = (float)src.Width / w, sy = (float)src.Height / h;
        int taps = Math.Max(1, (int)MathF.Ceiling(MathF.Max(sx, sy)));
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float r = 0, g = 0, b = 0, a = 0;
                for (int j = 0; j < taps; j++)
                    for (int i = 0; i < taps; i++)
                    {
                        float u = (x + (i + 0.5f) / taps) * sx - 0.5f, v = (y + (j + 0.5f) / taps) * sy - 0.5f;
                        int x0 = (int)MathF.Floor(u), y0 = (int)MathF.Floor(v);
                        float fx = u - x0, fy = v - y0;
                        for (int k = 0; k < 4; k++)
                        {
                            int px = Math.Clamp(x0 + (k & 1), 0, src.Width - 1), py = Math.Clamp(y0 + (k >> 1), 0, src.Height - 1);
                            float wgt = ((k & 1) == 1 ? fx : 1 - fx) * ((k >> 1) == 1 ? fy : 1 - fy);
                            var c = src.Pixels[py * src.Width + px];
                            float al = c.a / 255f * wgt;
                            r += c.r * al; g += c.g * al; b += c.b * al; a += al;
                        }
                    }
                if (a <= 0) continue;
                dst.Pixels[y * w + x] = new Color32((byte)(r / a + .5f), (byte)(g / a + .5f), (byte)(b / a + .5f), (byte)Math.Min(255f, a / (taps * taps) * 255f + .5f));
            }
        return dst;
    }
}
