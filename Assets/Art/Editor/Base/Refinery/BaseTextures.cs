using System;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 拠点の本気の小物のテクスチャ（Nov 4 わくさん「シンプルなテクスチャはアリ」）。絵は描かず、くり返せるノイズで作る。
    /// どれも 1 枚＝1 m 四方（UV は m）。木目は U の向き。木・石・臼の石は凹凸の法線マップも作る。
    /// 色は参考の絵（夕方の工房、暖かい木と白っぽい石灰岩）から。
    /// </summary>
    public static class BaseTextures
    {
        public const int Size = 512;

        /// <summary>くり返せる値ノイズ（-1〜1）：格子を period でくり返す。</summary>
        static float PNoise(float x, float y, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            float L(int ix, int iy)
            {
                ix = ((ix % period) + period) % period; iy = ((iy % period) + period) % period;
                return NatureAssets.Hash(seed, ix * 7919 + iy * 104729) * 2f - 1f;
            }
            return Mathf.Lerp(Mathf.Lerp(L(x0, y0), L(x0 + 1, y0), fx), Mathf.Lerp(L(x0, y0 + 1), L(x0 + 1, y0 + 1), fx), fy);
        }

        /// <summary>くり返せる fbm。u, v は 0〜1、px, py は一番粗い格子の数（横・縦でちがってよい＝木目のように伸ばせる）。</summary>
        static float Fbm(float u, float v, int px, int py, int oct, int seed)
        {
            float s = 0f, amp = 0.5f, norm = 0f;
            for (int o = 0; o < oct; o++)
            {
                int k = 1 << o;
                // 横と縦で周期がちがうので、縦は別のノイズで伸ばす
                s += amp * PNoiseXY(u * px * k, v * py * k, px * k, py * k, seed + o * 13);
                norm += amp; amp *= 0.5f;
            }
            return s / norm;
        }

        static float PNoiseXY(float x, float y, int periodX, int periodY, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            float L(int ix, int iy)
            {
                ix = ((ix % periodX) + periodX) % periodX; iy = ((iy % periodY) + periodY) % periodY;
                return NatureAssets.Hash(seed, ix * 7919 + iy * 104729) * 2f - 1f;
            }
            return Mathf.Lerp(Mathf.Lerp(L(x0, y0), L(x0 + 1, y0), fx), Mathf.Lerp(L(x0, y0 + 1), L(x0 + 1, y0 + 1), fx), fy);
        }

        static Color Hex(string h) => PropKit.Hex(h);

        /// <summary>色と高さを作って保存する。bump が 0 より大きいと法線マップも（name_N.png）。</summary>
        public static (Texture2D albedo, Texture2D normal) Make(string dir, string name, Func<float, float, (Color col, float h)> f, float bump)
        {
            System.IO.Directory.CreateDirectory(dir);
            var col = new Color[Size * Size];
            var h = new float[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    var r = f((x + 0.5f) / Size, (y + 0.5f) / Size);
                    col[y * Size + x] = r.col; h[y * Size + x] = r.h;
                }
            var albedo = Save(dir + "/" + name + ".png", col, false);
            Texture2D normal = null;
            if (bump > 0f)
            {
                var nc = new Color[Size * Size];
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                    {
                        float hl = h[y * Size + (x + Size - 1) % Size], hr = h[y * Size + (x + 1) % Size];
                        float hd = h[((y + Size - 1) % Size) * Size + x], hu = h[((y + 1) % Size) * Size + x];
                        var n = new Vector3((hl - hr) * bump, (hd - hu) * bump, 1f).normalized;
                        nc[y * Size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                    }
                normal = Save(dir + "/" + name + "_N.png", nc, true);
            }
            return (albedo, normal);
        }

        static Texture2D Save(string path, Color[] px, bool isNormal)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            imp.sRGBTexture = !isNormal;
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.mipmapEnabled = true;
            imp.anisoLevel = 4;
            imp.maxTextureSize = Size;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ───────── 素材ごと ─────────

        /// <summary>木：U に沿った細い年輪の筋、ゆるいうねり、ところどころの濃いしみ。</summary>
        public static (Color, float) Wood(float u, float v)
        {
            float warp = Fbm(u, v, 2, 6, 3, 11) * 0.06f;
            float lines = Mathf.Sin((v + warp) * Mathf.PI * 2f * 48f);
            float fine = Fbm(u, v, 8, 96, 2, 12);
            float blot = Fbm(u, v, 3, 3, 3, 13);
            float t = Mathf.Clamp01(0.55f + 0.25f * lines + 0.25f * fine + 0.3f * blot);
            var c = Color.Lerp(Hex("#6E4527"), Hex("#B07A48"), t);
            c = Color.Lerp(c, Hex("#5A3820"), Mathf.Clamp01(-blot * 1.5f - 0.2f));
            return (c, 0.5f * lines + 0.6f * fine);
        }

        /// <summary>石灰岩：白っぽいむら、黄土のしみ、細い割れ、小さな穴。</summary>
        public static (Color, float) Limestone(float u, float v)
        {
            float big = Fbm(u, v, 4, 4, 4, 21);
            float mid = Fbm(u, v, 16, 16, 3, 22);
            float crack = 1f - Mathf.Abs(Fbm(u, v, 6, 6, 3, 23));
            crack = Mathf.Pow(Mathf.Clamp01(crack), 18f);
            float pit = Mathf.Clamp01((Fbm(u, v, 48, 48, 1, 24) - 0.55f) * 6f);
            var c = Color.Lerp(Hex("#B9A88C"), Hex("#E2D5BC"), Mathf.Clamp01(0.55f + 0.5f * big + 0.25f * mid));
            c = Color.Lerp(c, Hex("#A8865A"), Mathf.Clamp01(Fbm(u, v, 3, 3, 3, 25) * 1.4f - 0.2f) * 0.5f);
            c = Color.Lerp(c, Hex("#6E604C"), Mathf.Max(crack, pit) * 0.7f);
            return (c, 0.7f * big + 0.35f * mid - 0.8f * crack - 0.4f * pit);
        }

        /// <summary>臼の石：灰色の地に黒と白の細かい粒。</summary>
        public static (Color, float) Granite(float u, float v)
        {
            float g = Fbm(u, v, 6, 6, 3, 31);
            float dark = Mathf.Clamp01((Fbm(u, v, 96, 96, 1, 32) - 0.45f) * 5f);
            float light = Mathf.Clamp01((Fbm(u, v, 128, 128, 1, 33) - 0.5f) * 5f);
            var c = Color.Lerp(Hex("#8E8A82"), Hex("#B3AEA4"), 0.5f + 0.5f * g);
            c = Color.Lerp(c, Hex("#45423E"), dark * 0.8f);
            c = Color.Lerp(c, Hex("#E4E0D8"), light * 0.6f);
            return (c, 0.5f * g - 0.4f * dark + 0.2f * light);
        }

        /// <summary>素焼き：クリーム色の地にゆるいむらと、焼きの濃い所。</summary>
        public static (Color, float) Clay(float u, float v)
        {
            float g = Fbm(u, v, 5, 5, 4, 41);
            var c = Color.Lerp(Hex("#C8B08A"), Hex("#E8DAC0"), 0.55f + 0.5f * g);
            c = Color.Lerp(c, Hex("#A27E58"), Mathf.Clamp01(Fbm(u, v, 3, 3, 2, 42) - 0.35f) * 0.8f);
            return (c, g);
        }

        /// <summary>塩：白に青みのかすかなむらと、きらりとした粒。</summary>
        public static (Color, float) Salt(float u, float v)
        {
            float g = Fbm(u, v, 8, 8, 3, 51);
            float spark = Mathf.Clamp01((Fbm(u, v, 160, 160, 1, 52) - 0.55f) * 6f);
            var c = Color.Lerp(Hex("#D9DDDC"), Hex("#F7F6F0"), 0.6f + 0.4f * g);
            c = Color.Lerp(c, Color.white, spark);
            return (c, g * 0.5f + spark * 0.3f);
        }

        /// <summary>縄：ななめに撚った 3 本の筋。</summary>
        public static (Color, float) Rope(float u, float v)
        {
            float s = Mathf.Repeat(u * 40f + v * 40f, 1f);
            float strand = Mathf.Sin(s * Mathf.PI);
            float fib = Fbm(u, v, 64, 64, 1, 61);
            var c = Color.Lerp(Hex("#6A5233"), Hex("#C3A273"), Mathf.Clamp01(strand * 0.9f + 0.15f * fib));
            return (c, strand);
        }

        /// <summary>鉄：黒っぽい地に赤茶のさび。</summary>
        public static (Color, float) Iron(float u, float v)
        {
            float g = Fbm(u, v, 6, 6, 4, 71);
            var c = Color.Lerp(Hex("#2B2725"), Hex("#4A403A"), 0.5f + 0.5f * g);
            c = Color.Lerp(c, Hex("#7A4A2A"), Mathf.Clamp01(Fbm(u, v, 10, 10, 3, 72) - 0.2f) * 0.7f);
            return (c, g);
        }

        /// <summary>光の筋：横はまん中が明るく両端へ消え、縦は上が強く下へ薄れる（加算で重ねる）。</summary>
        public static (Color, float) Beam(float u, float v)
        {
            float across = Mathf.Pow(Mathf.Sin(Mathf.PI * u), 2f);
            float along = Mathf.SmoothStep(0f, 1f, v) * (0.75f + 0.25f * Fbm(u, v, 6, 2, 2, 81));
            float k = across * along;
            return (new Color(k, k * 0.8f, k * 0.55f, 1f), 0f);
        }
    }
}
