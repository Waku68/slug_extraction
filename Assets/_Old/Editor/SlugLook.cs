using System.Collections.Generic;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>One eye highlight: position on the eye (unit ellipsoid coords) and size relative to the eye.</summary>
    public struct Glint
    {
        public Vector3 unit; public float rel;
        public Glint(float x, float y, float z, float rel) { unit = new Vector3(x, y, z); this.rel = rel; }
    }

    /// <summary>Eye variant. The body shape never changes; only the eyes are swapped.</summary>
    public class EyeStyle
    {
        public string key, label;
        public float scale = 1f;            // relative to the v1 eye size
        public bool dot;                    // bead eye: small and round, no highlight
        public float dotRadius = 0.42f;     // dot radius as a fraction of the v1 eye width/height average
        public Glint[] glints = new Glint[0];
        public bool lid;                    // sleepy eyelid
        public float lidCut = -0.1f;        // lid edge height in unit-eye coordinates (-1 bottom ... 1 top)
        public float lidTiltDeg = 5f;       // outer corner droops by this much
        public Color eyeColor = new Color(0.075f, 0.065f, 0.07f);

        public static readonly EyeStyle Subdued = new EyeStyle
        {
            key = "subdued", label = "控えめ", scale = 0.82f,
            glints = new[] { new Glint(0.30f, 0.34f, 0.89f, 0.17f) }
        };
        public static readonly EyeStyle Dot = new EyeStyle { key = "dot", label = "点目", dot = true };
        public static readonly EyeStyle Sleepy = new EyeStyle
        {
            key = "sleepy", label = "眠たそう", scale = 0.85f, lid = true,
            glints = new[] { new Glint(0.28f, -0.22f, 0.93f, 0.14f) }
        };
    }

    /// <summary>Natural "camouflage" colour scheme. Everything else is derived so the look stays consistent.</summary>
    public class Palette
    {
        public string key, label;
        public Color body, shade, belly, pattern;
        public float patternAlpha = 0.4f;
        public float outlineDarkness = 0.42f;   // outline = body colour darkened to this value factor
        public Color cheek;

        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        public static Color Darken(Color c, float v)
        {
            Color.RGBToHSV(c, out float h, out float s, out float val);
            return Color.HSVToRGB(h, s * 0.9f, val * v);
        }

        public Color Appendage => Color.Lerp(body, belly, 0.45f);

        /// <summary>Moss / lichen: olive greens, the colour of a slug on a wet mossy log.</summary>
        public static readonly Palette Moss = new Palette
        {
            key = "moss", label = "苔系",
            body = Hex("#6E7A4C"), shade = Hex("#5E6A5C"), belly = Hex("#A4AA82"), pattern = Hex("#4C5A36"),
            cheek = Hex("#B0856A")
        };
        /// <summary>Earth / bark: warm greyish browns.</summary>
        public static readonly Palette Earth = new Palette
        {
            key = "earth", label = "土系",
            body = Hex("#7C5F49"), shade = Hex("#675A5E"), belly = Hex("#B19B7E"), pattern = Hex("#563F30"),
            cheek = Hex("#B58A72")
        };
        /// <summary>Ash grey: the pale grey of a leopard slug / wet stone.</summary>
        public static readonly Palette Gray = new Palette
        {
            key = "gray", label = "灰系",
            body = Hex("#878A84"), shade = Hex("#5E6670"), belly = Hex("#BAB9AE"), pattern = Hex("#64686A"),
            cheek = Hex("#B58F85")
        };
    }

    /// <summary>A look = palette (null keeps the v1 pop colours, i.e. the "rare colours") + eye style.</summary>
    public class SlugLook
    {
        public string key;
        public Palette palette;
        public EyeStyle eye;
        public SlugLook(string key, Palette p, EyeStyle e) { this.key = key; palette = p; eye = e; }

        public static List<SlugLook> All() => new List<SlugLook>
        {
            new SlugLook("moss_subdued", Palette.Moss, EyeStyle.Subdued),
            new SlugLook("earth_subdued", Palette.Earth, EyeStyle.Subdued),
            new SlugLook("gray_subdued", Palette.Gray, EyeStyle.Subdued),
            new SlugLook("moss_dot", Palette.Moss, EyeStyle.Dot),
            new SlugLook("moss_sleepy", Palette.Moss, EyeStyle.Sleepy),
            new SlugLook("pop_subdued", null, EyeStyle.Subdued),   // rare colours (the old pop look), new eyes
        };

        public static SlugLook Get(string key) => All().Find(l => l.key == key);
    }

    public static class PaletteApply
    {
        public static void Body(Material m, Palette p)
        {
            m.SetColor("_BaseColor", p.body);
            m.SetColor("_ShadeColor", p.shade);
            m.SetColor("_BellyColor", p.belly);

            float type = m.GetFloat("_PatternType");
            var pc = p.pattern;
            if (type > 3.5f)
            {   // sparkle (gummy sugar / salt crystal): keep it, but quiet
                pc = Color.Lerp(p.belly, Color.white, 0.5f);
                pc.a = 1f;
                m.SetFloat("_SparkleStrength", 0.55f);
            }
            else
            {
                pc.a = type > 2.5f ? p.patternAlpha * 0.8f : p.patternAlpha;   // stripes are the faintest
            }
            m.SetColor("_PatternColor", pc);

            m.SetColor("_OutlineColor", Palette.Darken(p.body, p.outlineDarkness));
            if (m.GetFloat("_OutlineWidth") > 0f) m.SetFloat("_OutlineWidth", 0.0065f);
            m.SetColor("_RimColor", Color.Lerp(p.body, Color.white, 0.45f));
            m.SetFloat("_RimAmount", m.GetFloat("_RimAmount") * 0.6f);
            m.SetColor("_StitchColor", Palette.Darken(p.body, 0.55f));
        }

        public static void Appendage(Material m, Palette p)
        {
            var c = p.Appendage;
            m.SetColor("_BaseColor", c);
            m.SetColor("_OutlineColor", Palette.Darken(c, p.outlineDarkness));
        }
    }
}
