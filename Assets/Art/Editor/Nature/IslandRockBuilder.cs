using System.Text;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 岩と土の小物（Nov 2 下書き、3 案ずつ）。メニュー Slug/Nature/Props/Build Rocks で作り直す（Assets/Art/Nature/Rocks）。
    /// 岩・塩・土はかじれないのでカクカク、苔だけなめらか。ナメクジの目の高さ 0.47 m は人の約 1/3.6。
    /// - Rock 岩：A ずんぐり丸岩 高さ 1.0 m（人なら 3.6 m）、上に苔。B 平たい板岩 高さ 0.45 m（ナメクジが乗れる）＋立てかかった小さい板。
    ///   C 二つに割れた岩 高さ 1.2 m、すき間 0.5 m（ナメクジがやっと通れる）
    /// - Talus 落石のすそ（崖の下）：A 扇に広がる石 16 個（遠くほど大きい）、幅 3 m・奥 2.5 m。B 大きな塊 5 個が積み重なる（0.5〜0.9 m）。
    ///   C 砂利の小山（幅 3 m・高さ 0.45 m）に小石 12 個
    /// - LavaRock 溶岩の岩：A ごつごつの塊 0.9 m（細かい割れ目）。B 縄のようなしわの平たい溶岩（幅 2.2 m・高さ 0.35 m）。C 溶岩の柱 1.6 m＋まわりに小石
    /// - SaltCrystal 塩の結晶の群れ（岩塩の山）：A 柱 7 本が扇に立つ（0.3〜1.0 m）。B 低くずんぐり 9 本が放射に寝る（0.3〜0.6 m）。C 灰色の岩から太い結晶 4 本
    /// - TermiteMound シロアリ塚（低い土地とサバナ）：A 大聖堂の形 3 m（主塔＋脇の塔 3 本）。B ずんぐりのドーム 2 m に煙突 4 本。C 細長い煙突 4 m、深いひだ
    /// 色：石 #A39C8E・暗い石 #7E786C（ケルンと同じ）、苔 #73804C、溶岩 #4E4844・赤い溶岩 #6E4E40、塩 #E9E3DA・桃色の塩 #E2C9C0、
    /// 土 #A57E5C・暗い土 #876448、砂利 #948B7C。当たり判定は形どおりのメッシュ。
    /// </summary>
    public static class IslandRockBuilder
    {
        const string Dir = "Assets/Art/Nature/Rocks";
        const int STONE = 0, DARK = 1, MOSS = 2, LAVA = 3, LAVARED = 4, SALT = 5, SALTPINK = 6, EARTH = 7, EARTHDARK = 8, GRAVEL = 9;
        static Material[] _m;

        [MenuItem("Slug/Nature/Props/Build Rocks")]
        public static void Build()
        {
            // stone: near-tone patches (a shade lighter, a shade darker, a pale lichen), soft blotches and fine speckles, a chiselled grain.
            // the colours stay close to the base, or it reads as camouflage
            _m = PropKit.Mats(Dir, "Rocks",
                new PropKit.Spec("Stone", "#A39C8E", true).Mottle("#88827A", 0.5f, 4f).Camo("#ADA799", "#928C80", "#B9B596", 5f, 0.6f).Dent(0.3f, 22f),
                new PropKit.Spec("StoneDark", "#7E786C", true).Mottle("#68635A", 0.5f, 4f).Camo("#88827A", "#6E695F", "#959276", 5f, 0.6f).Dent(0.3f, 22f),
                new PropKit.Spec("Moss", "#73804C", false).Mottle("#8E9C58", 0.7f, 14f).Felt(0.5f).Dent(0.5f, 60f),
                new PropKit.Spec("Lava", "#4E4844", true).Mottle("#3A3532", 0.6f, 9f).Camo("#584E48", "#403A36", "#5E5650", 6f, 0.6f).Dent(0.6f, 35f),
                new PropKit.Spec("LavaRed", "#6E4E40", true).Mottle("#553A30", 0.6f, 9f).Dent(0.6f, 35f),
                new PropKit.Spec("Salt", "#E9E3DA", true).Mottle("#D6CFC4", 0.5f, 10f),
                new PropKit.Spec("SaltPink", "#E2C9C0", true).Mottle("#CDB0A6", 0.5f, 10f),
                new PropKit.Spec("Earth", "#A57E5C", true).Mottle("#8A6648", 0.6f, 6f).Camo("#AE8866", "#967050", "#A88462", 4f, 0.6f).Dent(0.5f, 30f),
                new PropKit.Spec("EarthDark", "#876448", true).Mottle("#6E503A", 0.6f, 6f).Dent(0.5f, 30f),
                new PropKit.Spec("Gravel", "#948B7C", true).Mottle("#7A7266", 0.8f, 18f).Felt(0.4f).Dent(0.8f, 70f));
            var log = new StringBuilder("Rocks built:");
            for (int v = 0; v < 3; v++)
            {
                log.Append(' ').Append(Rock(v)).Append(' ').Append(Talus(v)).Append(' ').Append(LavaRock(v))
                   .Append(' ').Append(SaltCrystal(v)).Append(' ').Append(TermiteMound(v));
            }
            AssetDatabase.SaveAssets();
            Debug.Log(log);
        }

        static string Abc(int v) => ((char)('A' + v)).ToString();
        static NatureMeshKit Kit() => new NatureMeshKit(_m.Length);
        static string Save(string name, NatureMeshKit kit, int collider = 3) => PropKit.Save(Dir, name, kit, _m, collider);

        /// <summary>
        /// 岩の体：少し角ばった塊（box）に、地層の段・割れ目・欠けを刻む。のっぺりしないための形の細かさはここで決める。
        /// post で割る・ずらすこともできる。
        /// </summary>
        static void RockBody(NatureMeshKit kit, Vector3 c, Vector3 size, int sub, int seed, Quaternion? rot = null, float box = 3f,
                             float layers = 5f, float step = 0.035f, System.Func<Vector3, Vector3> post = null, int rows = 16, int sides = 24, float grow = 1f, float tFrom = 0f)
        {
            PropKit.Lump(kit, c, size, rows, sides, sub, seed, 0.16f, 1.3f, grow > 1f ? float.NaN : 0f, rot, tFrom, grow,
                n => PropKit.Strata(n, seed, layers, step) + PropKit.Cracks(n, seed + 1, 0.07f) + PropKit.Chips(n, seed + 2, 0.12f), post, box);
        }

        /// <summary>
        /// 苔：岩と同じ形を少しふくらませた皮を、上向きの所と割れ目のまわりだけに残す（ほかは岩の中へ沈めて隠す）。ふちはまばらに切れる。
        /// </summary>
        static void MossOn(NatureMeshKit kit, Vector3 c, Vector3 size, int seed, Quaternion? rot = null, float box = 3f, float layers = 5f, float step = 0.035f,
                           float cover = 0.45f, System.Func<Vector3, Vector3> post = null)
        {
            var q = rot ?? Quaternion.identity;
            var inv = Quaternion.Inverse(q);
            RockBody(kit, c, size, MOSS, seed, rot, box, layers, step, p =>
            {
                var n = Vector3.Scale(inv * (p - c), new Vector3(1f / size.x, 1f / size.y, 1f / size.z)).normalized;
                float k = n.y + 0.45f * PropKit.Noise(n * 3.5f, seed + 40) + 0.2f * PropKit.Noise(n * 9f, seed + 41);
                if (k < 1f - cover) p = c + (p - c) * 0.9f;
                return post != null ? post(p) : p;
            }, 12, 24, 1.035f, 0.3f);
        }

        static string Rock(int v)
        {
            var kit = Kit();
            if (v == 0)
            {
                // a rounded boulder, worn smooth on top, layered round its waist, moss over the crown
                var c = new Vector3(0f, 0.45f, 0f); var size = new Vector3(0.7f, 0.58f, 0.6f);
                RockBody(kit, c, size, STONE, 3, null, 2.6f, 4f, 0.04f);
                MossOn(kit, c, size, 3, null, 2.6f, 4f, 0.04f, 0.42f);
                Pebbles(kit, Vector3.zero, 0.85f, 9, 30);
            }
            else if (v == 1)
            {
                // a flat slab in thick beds (the slug can climb on), a thin slab leaning on it, moss in the seams
                var c = new Vector3(0f, 0.2f, 0f); var size = new Vector3(1.0f, 0.28f, 0.7f);
                var rot = Quaternion.Euler(0f, 0f, 3f);
                RockBody(kit, c, size, STONE, 7, rot, 4.5f, 2.5f, 0.07f);
                MossOn(kit, c, size, 7, rot, 4.5f, 2.5f, 0.07f, 0.3f);
                var lean = Quaternion.Euler(0f, 20f, -24f);
                RockBody(kit, new Vector3(1.02f, 0.32f, 0.35f), new Vector3(0.12f, 0.38f, 0.32f), DARK, 9, lean, 4f, 3f, 0.03f, null, 12, 16);
                Pebbles(kit, new Vector3(0.2f, 0f, 0f), 1.2f, 8, 31);
            }
            else
            {
                // split boulder: one round boulder cut flat down the middle, the halves leaning apart, a gap the slug can just squeeze through
                for (int side = -1; side <= 1; side += 2)
                {
                    int sd = side;
                    var lean = Quaternion.Euler(0f, 0f, -sd * 7f);
                    System.Func<Vector3, Vector3> cut = p =>
                    {
                        p.x = sd > 0 ? Mathf.Max(p.x, 0f) : Mathf.Min(p.x, 0f);
                        return lean * p + new Vector3(sd * 0.3f, 0f, 0f);
                    };
                    var c = new Vector3(0f, 0.55f, 0f); var size = new Vector3(1.0f, 0.7f, 0.85f);
                    RockBody(kit, c, size, sd < 0 ? STONE : DARK, 11, null, 3f, 5f, 0.035f, cut);
                    MossOn(kit, c, size, 11, null, 3f, 5f, 0.035f, sd < 0 ? 0.4f : 0.2f, p => cut(p) + (p.x * sd < 0.05f ? Vector3.right * (-sd * 0.03f) : Vector3.zero));
                }
                Pebbles(kit, Vector3.zero, 1.1f, 10, 32);
            }
            return Save($"Rock_{Abc(v)}", kit);
        }

        /// <summary>岩のすそに転がる小石（角ばって、半分埋まる）。</summary>
        static void Pebbles(NatureMeshKit kit, Vector3 c, float reach, int n, int seed)
        {
            for (int i = 0; i < n; i++)
            {
                float a = PropKit.R(seed, i, 0f, Mathf.PI * 2f), d = reach * PropKit.R(seed + 1, i, 0.85f, 1.15f), r = PropKit.R(seed + 2, i, 0.04f, 0.1f);
                var p = c + new Vector3(Mathf.Cos(a) * d, r * 0.25f, Mathf.Sin(a) * d * 0.85f);
                PropKit.Lump(kit, p, new Vector3(r, r * 0.65f, r * 0.85f), 4, 7, i % 3 == 0 ? DARK : STONE, seed * 10 + i, 0.25f, 1.6f, 0f,
                    Quaternion.Euler(0f, a * 57f, 0f), 0f, 1f, null, null, 3.5f);
            }
        }

        static string Talus(int v)
        {
            var kit = Kit();
            if (v == 0)
            {
                // a fan at the cliff foot (the cliff is along -Z): a wedge of gravel, angular blocks on it, the big ones rolled the farthest
                PropKit.Surface(kit, (t, a) =>
                {
                    float u = Mathf.Cos(a), w = Mathf.Sin(a);
                    float z = 2.7f * t, half = 0.5f + 0.75f * z;
                    float h = 0.14f * (1f - t) * (1f - t) * Mathf.Max(0f, w) + 0.012f;
                    return new Vector3(u * half * (1f + 0.15f * PropKit.Noise(new Vector3(z * 2f, a, 0f), 20)), w > 0f ? h : 0f, z - 0.1f);
                }, 10, 16, GRAVEL);
                for (int i = 0; i < 18; i++)
                {
                    float z = Mathf.Pow(PropKit.H(21, i), 0.8f) * 2.5f, x = (PropKit.H(22, i) - 0.5f) * (0.8f + 1.1f * z);
                    float r = Mathf.Lerp(0.1f, 0.42f, z / 2.5f) * PropKit.R(23, i, 0.6f, 1.2f);
                    var size = new Vector3(r, r * PropKit.R(24, i, 0.55f, 0.85f), r * PropKit.R(25, i, 0.75f, 1.1f));
                    RockBody(kit, new Vector3(x, size.y * 0.5f, z), size, PropKit.H(26, i) < 0.3f ? DARK : STONE, 30 + i,
                        Quaternion.Euler(PropKit.R(28, i, -15f, 15f), PropKit.R(27, i, 0f, 360f), PropKit.R(29, i, -15f, 15f)), 4f, 3f, 0.04f, null, 8, 12);
                }
            }
            else if (v == 1)
            {
                // a few big blocks fallen together, one resting across two others: square-edged, bedded, chipped at the corners
                var c = new[] { new Vector4(0f, 0.45f, 0f, 0.9f), new Vector4(0.95f, 0.3f, 0.3f, 0.6f), new Vector4(-0.8f, 0.3f, 0.4f, 0.55f),
                                new Vector4(0.3f, 1.0f, 0.15f, 0.5f), new Vector4(0.2f, 0.25f, 1.0f, 0.5f) };
                for (int i = 0; i < c.Length; i++)
                {
                    var rot = Quaternion.Euler(PropKit.R(41, i, -12f, 12f), PropKit.R(42, i, 0f, 360f), PropKit.R(43, i, -12f, 12f));
                    var at = new Vector3(c[i].x, c[i].y, c[i].z); var size = new Vector3(c[i].w, c[i].w * 0.62f, c[i].w * 0.8f);
                    RockBody(kit, at, size, i % 2 == 0 ? STONE : DARK, 40 + i, rot, 4.5f, 3f, 0.05f, null, 12, 16);
                    if (i == 0 || i == 2) MossOn(kit, at, size, 40 + i, rot, 4.5f, 3f, 0.05f, 0.3f);
                }
                Pebbles(kit, new Vector3(0.1f, 0f, 0.3f), 1.5f, 12, 44);
            }
            else
            {
                PropKit.Lump(kit, new Vector3(0f, -0.08f, 0f), new Vector3(1.5f, 0.55f, 1.15f), 8, 18, GRAVEL, 50, 0.12f, 1.3f, 0f, null, 0f, 1f,
                    n => 0.05f * PropKit.Noise(n * 8f, 51));
                for (int i = 0; i < 16; i++)
                {
                    float a = PropKit.R(51, i, 0f, Mathf.PI * 2f), d = Mathf.Sqrt(PropKit.H(52, i)) * 0.9f;
                    float x = Mathf.Cos(a) * d * 1.5f, z = Mathf.Sin(a) * d * 1.15f;
                    float y = -0.08f + 0.55f * Mathf.Sqrt(Mathf.Max(0f, 1f - d * d)) * 0.92f;
                    float r = PropKit.R(53, i, 0.07f, 0.2f);
                    RockBody(kit, new Vector3(x, y + r * 0.15f, z), new Vector3(r, r * 0.7f, r * 0.9f), i % 3 == 0 ? DARK : STONE, 60 + i,
                        Quaternion.Euler(PropKit.R(54, i, -20f, 20f), a * 57f, 0f), 4f, 2f, 0.04f, null, 6, 9, 1.0001f);
                }
            }
            return Save($"Talus_{Abc(v)}", kit);
        }

        static string LavaRock(int v)
        {
            var kit = Kit();
            // clinker: sharp, broken, full of small pits (pits = dents in the shader), with deep cracks between the blocks
            float Clinker(Vector3 n, int seed) => PropKit.Cracks(n, seed, 0.2f, 4f) + PropKit.Chips(n, seed + 1, 0.2f, 4f) + 0.08f * Mathf.Abs(PropKit.Noise(n * 9f, seed + 2));
            if (v == 0)
            {
                PropKit.Lump(kit, new Vector3(0f, 0.38f, 0f), new Vector3(0.62f, 0.5f, 0.55f), 16, 22, LAVA, 70, 0.3f, 2.2f, 0f, null, 0f, 1f, n => Clinker(n, 71), null, 3f);
                PropKit.Lump(kit, new Vector3(0f, 0.38f, 0f), new Vector3(0.62f, 0.5f, 0.55f), 8, 22, LAVARED, 70, 0.3f, 2.2f, float.NaN, null, 0.78f, 1.025f, n => Clinker(n, 71), null, 3f);
            }
            else if (v == 1)
            {
                // pahoehoe: a low slab whose top is folded into rope-like arcs, its edge pushed out into rounded toes
                var toes = new[] { PropKit.Dir(10f), PropKit.Dir(70f), PropKit.Dir(150f), PropKit.Dir(230f), PropKit.Dir(300f) };
                PropKit.Lump(kit, new Vector3(0f, 0.08f, 0f), new Vector3(1.1f, 0.27f, 0.8f), 16, 40, LAVA, 72, 0.12f, 1.2f, 0f, null, 0f, 1f, n =>
                {
                    float d = new Vector2(n.x + 1.3f, n.z * 1.2f).magnitude;
                    float ropes = 0.4f * Mathf.Sin(d * 11f) * Mathf.Max(0f, n.y);
                    float toe = 0f;
                    var flat = new Vector3(n.x, 0f, n.z);
                    foreach (var tdir in toes) toe += 0.45f * Mathf.Exp(-(flat - tdir).sqrMagnitude * 7f) * (1f - Mathf.Abs(n.y));
                    return ropes + toe + PropKit.Cracks(n, 73, 0.06f, 6f);
                });
            }
            else
            {
                PropKit.Pipe(kit, t => new Vector3(0.18f * t * t, 1.6f * t, 0.05f * t), (t, a) =>
                    Mathf.Lerp(0.48f, 0.13f, Mathf.Pow(t, 0.8f)) * (1f + 0.3f * PropKit.Fbm(new Vector3(Mathf.Cos(a) * 1.5f, t * 4f, Mathf.Sin(a) * 1.5f), 73))
                      * (1f - 0.12f * Mathf.Pow(1f - Mathf.Abs(PropKit.Noise(new Vector3(Mathf.Cos(a) * 3f, t * 9f, Mathf.Sin(a) * 3f), 79)), 6f)),
                    18, 16, LAVA, LAVA, LAVA, 0.06f);
                for (int i = 0; i < 4; i++)
                {
                    var d = PropKit.Dir(PropKit.R(74, i, 0f, 360f)) * PropKit.R(75, i, 0.6f, 0.9f);
                    float r = PropKit.R(76, i, 0.1f, 0.2f);
                    PropKit.Lump(kit, d + Vector3.up * r * 0.5f, new Vector3(r, r * 0.7f, r), 4, 6, i == 1 ? LAVARED : LAVA, 77 + i, 0.35f, 2.2f, 0f);
                }
            }
            return Save($"LavaRock_{Abc(v)}", kit);
        }

        static string SaltCrystal(int v)
        {
            var kit = Kit();
            if (v == 0)
            {
                PropKit.Lump(kit, new Vector3(0f, 0.05f, 0f), new Vector3(0.4f, 0.16f, 0.34f), 4, 8, SALT, 80, 0.2f, 1.5f, 0f);
                for (int i = 0; i < 7; i++)
                {
                    float tilt = i == 0 ? 4f : PropKit.R(81, i, 12f, 32f), yaw = 360f * i / 6f + PropKit.R(82, i, -15f, 15f);
                    var dir = Quaternion.AngleAxis(tilt, Vector3.Cross(Vector3.up, PropKit.Dir(yaw))) * Vector3.up;
                    float len = i == 0 ? 1.0f : PropKit.R(83, i, 0.3f, 0.75f), r = i == 0 ? 0.13f : PropKit.R(84, i, 0.06f, 0.1f);
                    PropKit.Crystal(kit, PropKit.Dir(yaw) * (i == 0 ? 0f : 0.14f) - Vector3.up * 0.05f, dir, len, r, 0.22f, i % 3 == 2 ? SALTPINK : SALT);
                }
            }
            else if (v == 1)
            {
                for (int i = 0; i < 9; i++)
                {
                    float tilt = PropKit.R(85, i, 40f, 70f), yaw = 360f * i / 9f + PropKit.R(86, i, -12f, 12f);
                    var dir = Quaternion.AngleAxis(tilt, Vector3.Cross(Vector3.up, PropKit.Dir(yaw))) * Vector3.up;
                    PropKit.Crystal(kit, PropKit.Dir(yaw) * 0.08f - Vector3.up * 0.06f, dir, PropKit.R(87, i, 0.3f, 0.6f), PropKit.R(88, i, 0.08f, 0.14f), 0.3f, i % 4 == 1 ? SALTPINK : SALT);
                }
                PropKit.Crystal(kit, -Vector3.up * 0.06f, Vector3.up, 0.42f, 0.13f, 0.3f, SALT);
            }
            else
            {
                PropKit.Lump(kit, new Vector3(0f, 0.22f, 0f), new Vector3(0.62f, 0.36f, 0.5f), 6, 9, DARK, 90, 0.2f, 1.4f, 0f);
                var at = new[] { new Vector3(-0.2f, 0.4f, 0.05f), new Vector3(0.2f, 0.42f, -0.08f), new Vector3(0.38f, 0.28f, 0.2f), new Vector3(-0.4f, 0.25f, -0.2f) };
                var dir = new[] { new Vector3(-0.2f, 1f, 0.1f), new Vector3(0.25f, 1f, -0.15f), new Vector3(0.9f, 0.7f, 0.3f), new Vector3(-0.8f, 0.6f, -0.4f) };
                float[] len = { 1.0f, 0.8f, 0.6f, 0.55f }, r = { 0.25f, 0.2f, 0.17f, 0.16f };
                for (int i = 0; i < 4; i++) PropKit.Crystal(kit, at[i] - dir[i].normalized * 0.1f, dir[i], len[i], r[i], 0.25f, i == 1 ? SALTPINK : SALT);
            }
            return Save($"SaltCrystal_{Abc(v)}", kit);
        }

        /// <summary>シロアリ塚の塔 1 本：根元 at から高さ h、根元の半径 r0 → 先 r1、ひだ flutes 本（深さ fluteDepth）、少し曲がる。</summary>
        static void Spire(NatureMeshKit kit, Vector3 at, float h, float r0, float r1, int flutes, float fluteDepth, Vector3 bend, int seed, int rows = 10, int sides = 14)
        {
            PropKit.Pipe(kit, t => at + Vector3.up * (h * t) + bend * (t * t), (t, a) =>
            {
                float body = Mathf.Lerp(r0, r1, Mathf.Pow(t, 0.75f));
                float flute = 1f + fluteDepth * Mathf.Cos(flutes * a + t * 1.4f);
                // built up in layers of mud: soft ledges round it, and dried cracks
                var q = new Vector3(Mathf.Cos(a) * 2f, t * h * 1.5f, Mathf.Sin(a) * 2f);
                float ledge = 0.04f * Mathf.Abs(Mathf.Sin(t * h * 9f + PropKit.Noise(q, seed + 3) * 2f));
                return body * flute * (1f + 0.12f * PropKit.Fbm(q, seed) + ledge - 0.08f * Mathf.Pow(1f - Mathf.Abs(PropKit.Noise(q * 2.5f, seed + 4)), 8f));
            }, rows * 2, sides + 6, EARTH, -1, EARTH, r1 * 0.8f);
            // a dark vent hole on the side (only one: two side by side read as a face)
            for (int i = 0; i < 1; i++)
            {
                float t = PropKit.R(seed, 50 + i, 0.25f, 0.6f), a = PropKit.R(seed, 52 + i, 0f, Mathf.PI * 2f);
                float body = Mathf.Lerp(r0, r1, Mathf.Pow(t, 0.75f));
                var c = at + Vector3.up * (h * t) + bend * (t * t) + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * body * 0.98f;
                kit.Ball(c, new Vector3(body * 0.16f, body * 0.22f, body * 0.16f), 4, LAVA);
            }
        }

        static string TermiteMound(int v)
        {
            var kit = Kit();
            if (v == 0)
            {
                PropKit.Lump(kit, new Vector3(0f, 0f, 0f), new Vector3(1.4f, 0.55f, 1.2f), 6, 14, EARTHDARK, 100, 0.2f, 1.5f, 0f);
                Spire(kit, new Vector3(0f, 0.2f, 0f), 2.8f, 0.85f, 0.14f, 5, 0.12f, new Vector3(0.12f, 0f, 0.05f), 101);
                Spire(kit, new Vector3(0.75f, 0.1f, 0.3f), 1.9f, 0.42f, 0.09f, 4, 0.12f, new Vector3(0.2f, 0f, 0.1f), 102, 8, 11);
                Spire(kit, new Vector3(-0.65f, 0.1f, 0.35f), 1.4f, 0.38f, 0.08f, 4, 0.12f, new Vector3(-0.15f, 0f, 0.05f), 103, 7, 11);
                Spire(kit, new Vector3(-0.1f, 0.1f, -0.75f), 1.15f, 0.34f, 0.08f, 4, 0.12f, new Vector3(0f, 0f, -0.15f), 104, 6, 10);
            }
            else if (v == 1)
            {
                PropKit.Lump(kit, new Vector3(0f, 0.35f, 0f), new Vector3(1.05f, 1.0f, 0.95f), 9, 16, EARTH, 105, 0.16f, 1.4f, 0f);
                PropKit.Lump(kit, new Vector3(0f, 0f, 0f), new Vector3(1.35f, 0.3f, 1.25f), 5, 16, EARTHDARK, 106, 0.18f, 1.6f, 0f);
                for (int i = 0; i < 4; i++)
                {
                    var d = PropKit.Dir(i * 90f + 20f) * PropKit.R(107, i, 0.15f, 0.5f);
                    float y = 0.35f + 1.0f * Mathf.Sqrt(Mathf.Max(0f, 1f - d.sqrMagnitude / 1.1f)) - 0.1f;
                    Spire(kit, d + Vector3.up * y, PropKit.R(108, i, 0.3f, 0.55f), 0.14f, 0.06f, 3, 0.1f, d * 0.2f, 109 + i, 4, 9);
                }
            }
            else
            {
                PropKit.Lump(kit, new Vector3(0f, 0f, 0f), new Vector3(1.0f, 0.45f, 0.95f), 6, 14, EARTHDARK, 110, 0.2f, 1.5f, 0f);
                Spire(kit, new Vector3(0f, 0.15f, 0f), 3.9f, 0.7f, 0.12f, 7, 0.2f, new Vector3(0.3f, 0f, -0.1f), 111, 14, 21);
            }
            return Save($"TermiteMound_{Abc(v)}", kit);
        }
    }
}
