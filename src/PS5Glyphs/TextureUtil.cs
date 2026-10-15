using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace PS5Glyphs;

/// <summary>Managed RGBA image; all pixel work happens here so we cross into IL2CPP only to read and write whole textures.</summary>
internal sealed class Image32
{
    public readonly int Width, Height;
    public readonly Color32[] Pixels;
    public Image32(int width, int height, Color32[] pixels) { Width = width; Height = height; Pixels = pixels; }
    public Image32(int width, int height) : this(width, height, new Color32[width * height]) { }
}

internal static class TextureUtil
{
    internal static bool IsSrgb(Texture tex) => GraphicsFormatUtility.IsSRGBFormat(tex.graphicsFormat);

    /// <summary>Copies any texture (including non-readable, compressed atlases) through the GPU into managed pixels.</summary>
    internal static Image32 Read(Texture src)
    {
        int w = src.width, h = src.height;
        bool srgb = IsSrgb(src);
        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, srgb ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear);
        var previous = RenderTexture.active;
        Texture2D? tmp = null;
        try
        {
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;
            tmp = new Texture2D(w, h, TextureFormat.RGBA32, false, !srgb);
            tmp.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tmp.Apply();
            return new Image32(w, h, ToManaged(tmp.GetPixels32()));
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            if (tmp != null) UnityEngine.Object.Destroy(tmp);
        }
    }

    internal static Image32 LoadPng(string path)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!ImageConversion.LoadImage(tex, File.ReadAllBytes(path))) throw new InvalidDataException("Not a PNG/JPG: " + path);
            return new Image32(tex.width, tex.height, ToManaged(tex.GetPixels32()));
        }
        finally { UnityEngine.Object.Destroy(tex); }
    }

    internal static Texture2D ToTexture(Image32 image, string name, bool linear, Texture? like)
    {
        bool mips = like != null && like.mipmapCount > 1;
        var tex = new Texture2D(image.Width, image.Height, TextureFormat.RGBA32, mips, linear) { name = name };
        if (like != null) { tex.filterMode = like.filterMode; tex.wrapMode = like.wrapMode; tex.anisoLevel = like.anisoLevel; }
        tex.SetPixels32(image.Pixels);
        tex.Apply(mips, false);
        tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return tex;
    }

    internal static void SavePng(Image32 image, string path)
    {
        var tex = new Texture2D(image.Width, image.Height, TextureFormat.RGBA32, false);
        try
        {
            tex.SetPixels32(image.Pixels);
            tex.Apply();
            Il2CppStructArray<byte> png = ImageConversion.EncodeToPNG(tex);
            var bytes = new byte[png.Length];
            for (int i = 0; i < bytes.Length; i++) bytes[i] = png[i];
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, bytes);
        }
        finally { UnityEngine.Object.Destroy(tex); }
    }

    internal static Image32 Crop(Image32 src, int x, int y, int w, int h)
    {
        var dst = new Image32(w, h);
        for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                int sx = x + i, sy = y + j;
                if (sx >= 0 && sy >= 0 && sx < src.Width && sy < src.Height) dst.Pixels[j * w + i] = src.Pixels[sy * src.Width + sx];
            }
        return dst;
    }

    private static Color32[] ToManaged(Il2CppStructArray<Color32> source)
    {
        var result = new Color32[source.Length];
        for (int i = 0; i < result.Length; i++) result[i] = source[i];
        return result;
    }
}
