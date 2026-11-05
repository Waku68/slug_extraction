using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SlugDesign
{
    /// <summary>
    /// 珍しい拾い物（spec 6章）。メニュー Slug/Nature/Props/Build Rare Finds で作り直す（Assets/Art/Nature/RareFinds）。
    /// 高い売り物で、使っても強くならない。どちらも 30 cm 台（ナメクジの目の高さの 2/3〜4/5、人なら約 1.1〜1.4 m）で、飲みこめない。
    /// Nov 4：灰色ラフで塩の魚は B（透ける塊）に決まった（A 突き出る・C 半分埋まるは Assets/_Old/Art/RareFinds）。ボトルシップは「レア物だからもっと凝って」で細部を足した。
    /// - BottleShip ボトルシップ：栓をした古いガラス瓶（長さ 32 cm・太さ 13 cm、少し緑）。首に麻ひもを2巻き、垂れた端。瓶の外にフジツボ、肩に海藻が1本かかる。
    ///   中は 3 本マストの帆船（長さ 15 cm）：茶色の船体に黒い帯と丸窓、船尾楼と船首楼、舵、横帆 2 枚ずつと帆桁、へさきの斜めの棒に三角の帆 2 枚、
    ///   張り綱、メインマストのてっぺんに赤い旗（鮮やかなのはここ1か所）。青い粘土の海に白い波がしら。浜に横倒しで少し転がり、底の側が砂に埋まる
    /// - SaltFish 塩に閉じこめられた海の魚：半透明の岩塩の塊（38×20×19 cm）の中に古代魚（長さ 32 cm、少し C の字に曲がる）が丸ごと透ける。
    ///   魚はシーラカンスのように鋼色の青に白い斑点、大きな目、えらぶた、太い柄のある対のヒレ、3 つに分かれた尾。塩の中に白いにごりの粒、上と角と根元に白い立方体の結晶
    /// ガラスと透ける塩は Slug/Toon の _Alpha（ふちほど濃い）、輪郭線と影はなし。かじれない物（ガラス・木・塩・フジツボ）はカクカク、魚・ヒレ・帆・ひも・海藻はなめらか。
    /// 三角形は手に持つ物の目安（1,500）より多い（珍しい物なので細部を優先）。撮った絵は Screenshots/Nature/rare_finds.png。
    /// </summary>
    public static class RareFindsBuilder
    {
        const string Dir = "Assets/Art/Nature/RareFinds";
        const int GLASS = 0, CORK = 1, TWINE = 2, HULL = 3, HULLDARK = 4, SAIL = 5, FLAG = 6, SEA = 7, FOAM = 8, SAND = 9,
                  BARN = 10, DARK = 11, WEED = 12, SALTCLEAR = 13, SALT = 14, FISH = 15, FIN = 16, SPECK = 17, EYERING = 18;
        static Material[] _m;

        [MenuItem("Slug/Nature/Props/Build Rare Finds")]
        public static void Build()
        {
            _m = PropKit.Mats(Dir, "Rare",
                new PropKit.Spec("Glass", "#B8D6CC", true),
                new PropKit.Spec("Cork", "#A88A62", false).Dent(0.4f, 120f),
                new PropKit.Spec("Twine", "#8C7A58", false).Felt(0.4f),
                new PropKit.Spec("Hull", "#7A5236", true).Mottle("#62402A", 0.4f, 60f),
                new PropKit.Spec("HullDark", "#4A3222", true),
                new PropKit.Spec("Sail", "#EDE4CC", false, 0f, true),
                new PropKit.Spec("Flag", "#C0453A", false, 0f, true),
                new PropKit.Spec("Sea", "#3E6E8E", false),
                new PropKit.Spec("Foam", "#E8EEF0", false),
                new PropKit.Spec("Sand", "#D6C8AA", false).Mottle("#BBAA88", 0.4f, 25f).Felt(0.35f),
                new PropKit.Spec("Barnacle", "#D8D0C0", true).Dent(0.3f, 150f),
                new PropKit.Spec("Dark", "#2E2A26", false),
                new PropKit.Spec("Weed", "#5E5A30", false, 0f, true).Mottle("#48441F", 0.45f, 9f),
                new PropKit.Spec("SaltClear", "#EEE6DE", true),
                new PropKit.Spec("Salt", "#F2EEE8", true).Dent(0.3f, 60f),
                new PropKit.Spec("Fish", "#3F5B7A", false).Dent(0.2f, 200f),
                new PropKit.Spec("Fin", "#34506A", false, 0f, true),
                new PropKit.Spec("Speck", "#E6EAEC", false),
                new PropKit.Spec("EyeRing", "#C8CCB8", false));
            Clear(_m[GLASS], 0.15f);
            Clear(_m[SALTCLEAR], 0.35f);
            var log = new StringBuilder("Rare finds built:");
            log.Append(' ').Append(BottleShip()).Append(' ').Append(SaltFish());
            AssetDatabase.SaveAssets();
            Debug.Log(log);
        }

        static NatureMeshKit Kit() => new NatureMeshKit(_m.Length);

        /// <summary>透ける物（ガラス、透ける塩）：ふちほど濃く、真ん中は透ける。輪郭線と影はなし（中の物を隠さない）。</summary>
        static void Clear(Material m, float alpha)
        {
            m.SetFloat("_Alpha", alpha);
            m.SetFloat("_AlphaEdge", 2f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetFloat("_OutlineWidth", 0f);
            m.SetShaderPassEnabled("ShadowCaster", false);
            EditorUtility.SetDirty(m);
        }

        // ───────── 小さな部品 ─────────

        /// <summary>輪（ひも・えらぶたのふち）：中心 c、軸 axis、輪の半径 rr、太さ r。</summary>
        static void Ring(NatureMeshKit kit, Vector3 c, Vector3 axis, float rr, float r, int sub, int rows = 20, int sides = 6)
        {
            axis.Normalize();
            var u = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            var w = Vector3.Cross(axis, u);
            PropKit.Surface(kit, (t, a) =>
            {
                float th = t * Mathf.PI * 2f;
                var rad = u * Mathf.Cos(th) + w * Mathf.Sin(th);
                return c + rad * (rr + r * Mathf.Cos(a)) - axis * (r * Mathf.Sin(a));
            }, rows, sides, sub);
        }

        /// <summary>三角の薄い板（三角の帆、旗）：厚み th の閉じた形。</summary>
        static void Sheet(NatureMeshKit kit, Vector3 a, Vector3 b, Vector3 c, float th, int sub)
        {
            var n = Vector3.Cross(b - a, c - a).normalized * (th * 0.5f);
            int i0 = kit.Vert(a + n, Vector2.zero), i1 = kit.Vert(b + n, Vector2.zero), i2 = kit.Vert(c + n, Vector2.zero);
            int j0 = kit.Vert(a - n, Vector2.zero), j1 = kit.Vert(b - n, Vector2.zero), j2 = kit.Vert(c - n, Vector2.zero);
            kit.Tri(sub, i0, i1, i2); kit.Tri(sub, j0, j2, j1);
            kit.Quad(sub, i0, j0, j1, i1); kit.Quad(sub, i1, j1, j2, i2); kit.Quad(sub, i2, j2, j0, i0);
        }

        /// <summary>曲がったひも・海藻：a → b → c の曲線（b に引かれる）、太さ r（flat で平たいリボン）。</summary>
        static void Strand(NatureMeshKit kit, Vector3 a, Vector3 b, Vector3 c, float r, float flat, int sub)
        {
            PropKit.Pipe(kit, t => (1 - t) * (1 - t) * a + 2 * (1 - t) * t * b + t * t * c,
                (t, ang) => r * (1f - flat + flat * Mathf.Abs(Mathf.Cos(ang))) * (1f - 0.4f * t), 12, 6, sub, sub, sub);
        }

        /// <summary>塩の小さな立方体の結晶（1.2〜2.5 cm）を c のまわり spread の中に n 個。</summary>
        static void Cubes(NatureMeshKit kit, Vector3 c, float spread, int n, int seed)
        {
            for (int i = 0; i < n; i++)
            {
                var at = c + new Vector3(PropKit.R(seed, i * 5, -spread, spread), PropKit.R(seed, i * 5 + 1, -0.01f, 0.01f), PropKit.R(seed, i * 5 + 2, -spread, spread) * 0.6f);
                float s = PropKit.R(seed, i * 5 + 3, 0.012f, 0.025f);
                var rot = Quaternion.Euler(PropKit.R(seed, i * 5 + 4, -30f, 30f), PropKit.R(seed, i * 7, 0f, 90f), PropKit.R(seed, i * 9, -30f, 30f));
                PropKit.Lump(kit, at, Vector3.one * s, 4, 8, SALT, seed + i, 0f, 1f, float.NaN, rot, 0f, 1f, null, null, 8f);
            }
        }

        // ───────── ボトルシップ ─────────

        static string BottleShip()
        {
            var kit = Kit();
            const float L = 0.32f, R = 0.065f, Rn = 0.022f;
            int from = kit.VertexCount;

            // bottle frame: base at -x, neck at +x, up +y. The ship first (opaque), then everything stuck on the glass, then the glass
            ShipInside(kit, R);
            float Radius(float t)
            {
                if (t < 0.05f) return R * (0.82f + 0.18f * Mathf.Sqrt(t / 0.05f));     // the rounded base
                if (t < 0.64f) return R;
                if (t < 0.8f) return Mathf.Lerp(R, Rn, Mathf.SmoothStep(0f, 1f, (t - 0.64f) / 0.16f));   // the shoulder
                return t > 0.96f ? Rn * 1.18f : Rn;                                     // the neck and its lip
            }
            PropKit.Stick(kit, new Vector3(L * 0.5f - 0.03f, 0f, 0f), new Vector3(L * 0.5f + 0.02f, 0f, 0f), Rn * 0.95f, Rn * 1.08f, 10, CORK);
            // twine: two turns round the neck, and the loose end hanging
            Ring(kit, new Vector3(L * 0.5f - 0.04f, 0f, 0f), Vector3.right, Rn * 1.05f, 0.0024f, TWINE);
            Ring(kit, new Vector3(L * 0.5f - 0.033f, 0f, 0f), Vector3.right, Rn * 1.05f, 0.0024f, TWINE);
            Strand(kit, new Vector3(L * 0.5f - 0.036f, -Rn, 0.004f), new Vector3(L * 0.5f - 0.03f, -R * 0.7f, 0.012f), new Vector3(L * 0.5f - 0.012f, -R * 0.95f, 0.02f), 0.0022f, 0f, TWINE);
            // barnacles crowd the base end (it lay longest in the sea)
            float[,] barn = { { -0.135f, 80f, 0.009f }, { -0.118f, 102f, 0.008f }, { -0.125f, 58f, 0.007f }, { -0.1f, 76f, 0.006f }, { -0.142f, 120f, 0.006f }, { 0.03f, 108f, 0.006f } };
            for (int i = 0; i < barn.GetLength(0); i++)
            {
                float th = barn[i, 1] * Mathf.Deg2Rad, s = barn[i, 2];
                var n = new Vector3(0f, Mathf.Sin(th), Mathf.Cos(th));
                var p = new Vector3(barn[i, 0], 0f, 0f) + n * (R - 0.001f);
                PropKit.Pipe(kit, t => p + n * (s * 0.9f * t), (t, _) => Mathf.Lerp(s, s * 0.45f, t), 2, 6, BARN, BARN, DARK);
            }
            PropKit.Pipe(kit, t => new Vector3(Mathf.Lerp(-L * 0.5f, L * 0.5f, t), 0f, 0f), (t, _) => Radius(t), 30, 18, GLASS, GLASS, -1, R * 0.1f);

            // rolled over a little and turned, the base end in the sand
            var q = Quaternion.Euler(0f, 20f, 0f) * Quaternion.AngleAxis(14f, Vector3.right);
            Vector3 W(Vector3 p) => q * p + Vector3.up * (R * 0.7f);
            kit.Transform(from, W);

            // a strand of seaweed caught over the shoulder, trailing into the sand
            var weedEnd = W(new Vector3(0.08f, 0f, R + 0.11f));
            weedEnd.y = 0.004f;
            Strand(kit, W(new Vector3(0.03f, R + 0.005f, -0.01f)), W(new Vector3(0.05f, R * 0.75f, R + 0.03f)), weedEnd, 0.0055f, 0.5f, WEED);
            // sand heaped against the base end and along one side
            var baseEnd = q * new Vector3(-L * 0.5f, 0f, 0f);
            PropKit.Lump(kit, new Vector3(baseEnd.x, 0f, baseEnd.z), new Vector3(0.1f, 0.045f, 0.11f), 7, 14, SAND, 11, 0.3f, 1.8f, 0f);
            var side = q * new Vector3(-0.04f, 0f, R * 1.05f);
            PropKit.Lump(kit, new Vector3(side.x, 0f, side.z), new Vector3(0.12f, 0.028f, 0.045f), 6, 12, SAND, 12, 0.3f, 1.8f, 0f, Quaternion.Euler(0f, 20f, 0f));
            return PropKit.Save(Dir, "BottleShip", kit, _m, 2);
        }

        /// <summary>瓶の中の帆船（長さ 15 cm、へさきが +x）。青い粘土の海に乗る。</summary>
        static void ShipInside(NatureMeshKit kit, float R)
        {
            // the putty sea on the bottle's floor, white caps along it and a bow wave
            float seaY = -R * 0.74f;
            PropKit.Lump(kit, new Vector3(-0.01f, seaY, 0f), new Vector3(0.115f, 0.02f, 0.042f), 6, 14, SEA, 3, 0.15f, 2.5f);
            for (int i = 0; i < 6; i++)
            {
                var at = new Vector3(PropKit.R(9, i, -0.1f, 0.09f), seaY + 0.016f, (i % 2 == 0 ? 1f : -1f) * PropKit.R(9, i + 20, 0.026f, 0.034f));
                PropKit.Lump(kit, at, new Vector3(0.009f, 0.004f, 0.005f), 3, 8, FOAM, 9 + i, 0.2f, 2f);
            }
            for (int s = -1; s <= 1; s += 2)
                PropKit.Lump(kit, new Vector3(0.068f, seaY + 0.017f, s * 0.016f), new Vector3(0.012f, 0.005f, 0.006f), 3, 8, FOAM, 15, 0.2f, 2f);

            // the hull: pointed at the bow, a flat deck, a dark band (the wale), a row of portholes each side
            float hy = -R * 0.46f, hl = 0.075f, hc = -0.005f, deck = hy + 0.012f;
            System.Func<Vector3, Vector3> bowSqueeze = p =>
            {
                float bow = Mathf.Max(0f, (p.x - hc) / hl);
                return new Vector3(p.x, Mathf.Min(p.y, deck), p.z * (1f - 0.7f * bow * bow));
            };
            PropKit.Lump(kit, new Vector3(hc, hy, 0f), new Vector3(hl, 0.017f, 0.021f), 8, 14, HULL, 4, 0f, 1f, float.NaN, null, 0f, 1f, null, bowSqueeze, 3f);
            PropKit.Lump(kit, new Vector3(hc, deck - 0.007f, 0f), new Vector3(hl * 1.01f, 0.0035f, 0.0218f), 3, 14, HULLDARK, 4, 0f, 1f, float.NaN, null, 0f, 1f, null, bowSqueeze, 3f);
            for (int i = 0; i < 4; i++)
            {
                float x = -0.045f + i * 0.022f, bow = Mathf.Max(0f, (x - hc) / hl), k = (x - hc) / hl;
                float zr = 0.021f * (1f - 0.7f * bow * bow) * Mathf.Sqrt(Mathf.Max(0f, 1f - k * k * k * k));
                for (int s = -1; s <= 1; s += 2)
                    kit.Ball(new Vector3(x, deck - 0.012f, s * zr), Vector3.one * 0.0026f, 3, DARK);
            }
            // the raised stern deck and the little fore deck, the rudder
            PropKit.Lump(kit, new Vector3(-0.062f, deck + 0.004f, 0f), new Vector3(0.016f, 0.007f, 0.016f), 4, 10, HULL, 6, 0f, 1f, float.NaN, null, 0f, 1f, null, null, 5f);
            PropKit.Lump(kit, new Vector3(-0.062f, deck + 0.011f, 0f), new Vector3(0.017f, 0.0015f, 0.0168f), 3, 10, HULLDARK, 6, 0f, 1f, float.NaN, null, 0f, 1f, null, null, 5f);
            PropKit.Lump(kit, new Vector3(0.046f, deck + 0.003f, 0f), new Vector3(0.011f, 0.004f, 0.011f), 4, 10, HULL, 7, 0f, 1f, float.NaN, null, 0f, 1f, null, null, 5f);
            PropKit.Lump(kit, new Vector3(-0.082f, hy - 0.002f, 0f), new Vector3(0.004f, 0.012f, 0.0016f), 3, 8, HULLDARK, 8, 0f, 1f, float.NaN, null, 0f, 1f, null, null, 5f);

            // three masts, two square sails each (bellied toward the bow) hung from yards
            float[] mx = { -0.045f, -0.008f, 0.028f }, mh = { 0.046f, 0.058f, 0.042f };
            for (int i = 0; i < 3; i++)
            {
                float foot = i == 0 ? deck + 0.012f : deck;
                PropKit.Stick(kit, new Vector3(mx[i], foot, 0f), new Vector3(mx[i], deck + mh[i], 0f), 0.0025f, 0.0017f, 6, HULLDARK);
                for (int s = 0; s < 2; s++)
                {
                    float sy = deck + mh[i] * (s == 0 ? 0.42f : 0.76f), hw = s == 0 ? 0.022f : 0.016f, hh = mh[i] * (s == 0 ? 0.17f : 0.12f);
                    PropKit.Lump(kit, new Vector3(mx[i] + 0.002f, sy, 0f), new Vector3(0.0025f, hh, hw), 4, 10, SAIL, 5 + i, 0f, 1f, float.NaN, null, 0f, 1f, null,
                        p => p + Vector3.right * (0.006f * (1f - Mathf.Pow(p.z / hw, 2f))), 6f);
                    PropKit.Stick(kit, new Vector3(mx[i], sy + hh, -hw * 1.15f), new Vector3(mx[i], sy + hh, hw * 1.15f), 0.0012f, 0.0012f, 5, HULLDARK);
                }
            }
            // the red pennant on the main mast (the one bright spot)
            float top = deck + mh[1];
            Sheet(kit, new Vector3(mx[1], top - 0.001f, 0f), new Vector3(mx[1], top - 0.009f, 0f), new Vector3(mx[1] - 0.02f, top - 0.004f, 0.002f), 0.0015f, FLAG);

            // the bowsprit, two triangular jibs to the fore mast, and the stays
            var tip = new Vector3(0.098f, deck + 0.014f, 0f);
            PropKit.Stick(kit, new Vector3(0.055f, deck - 0.002f, 0f), tip, 0.002f, 0.0012f, 6, HULLDARK);
            Sheet(kit, tip, new Vector3(mx[2] + 0.003f, deck + mh[2] * 0.95f, 0f), new Vector3(0.06f, deck + 0.004f, 0f), 0.002f, SAIL);
            Sheet(kit, Vector3.Lerp(new Vector3(0.055f, deck, 0f), tip, 0.6f), new Vector3(mx[2] + 0.003f, deck + mh[2] * 0.6f, 0f), new Vector3(0.052f, deck + 0.002f, 0f), 0.002f, SAIL);
            PropKit.Stick(kit, new Vector3(mx[1], top, 0f), new Vector3(mx[2], deck + mh[2], 0f), 0.0006f, 0.0006f, 4, DARK);
            PropKit.Stick(kit, new Vector3(mx[1], top, 0f), new Vector3(mx[0], deck + mh[0], 0f), 0.0006f, 0.0006f, 4, DARK);
            PropKit.Stick(kit, new Vector3(mx[0], deck + mh[0], 0f), new Vector3(-0.078f, deck + 0.012f, 0f), 0.0006f, 0.0006f, 4, DARK);
            PropKit.Stick(kit, new Vector3(mx[2], deck + mh[2], 0f), tip, 0.0006f, 0.0006f, 4, DARK);
        }

        // ───────── 塩の魚 ─────────

        static string SaltFish()
        {
            var kit = Kit();
            // the fish and the cloudy specks inside first (opaque), then the clear block round them
            Fish(kit, 0.32f, new Vector3(0f, 0.1f, 0f), Quaternion.Euler(0f, 0f, 3f));
            for (int i = 0; i < 26; i++)
            {
                var p = new Vector3(PropKit.R(51, i, -0.17f, 0.17f), PropKit.R(51, i + 40, 0.025f, 0.175f), PropKit.R(51, i + 80, -0.08f, 0.08f));
                kit.Ball(p, Vector3.one * PropKit.R(51, i + 120, 0.002f, 0.005f), 2, SALT);
            }
            PropKit.Lump(kit, new Vector3(0f, 0.1f, 0f), new Vector3(0.19f, 0.1f, 0.095f), 10, 18, SALTCLEAR, 31, 0.06f, 1.4f, 0f, null, 0f, 1f,
                         n => PropKit.Chips(n, 31, 0.06f), null, 4.5f);
            // white cubic crystals crusting the top, one corner and the foot
            Cubes(kit, new Vector3(0.08f, 0.197f, 0.02f), 0.06f, 5, 32);
            Cubes(kit, new Vector3(-0.15f, 0.17f, 0.05f), 0.03f, 3, 33);
            Cubes(kit, new Vector3(0f, 0.012f, 0.075f), 0.12f, 4, 34);
            return PropKit.Save(Dir, "SaltFish", kit, _m, 2);
        }

        /// <summary>
        /// 古代魚（シーラカンスのような）：大きな頭のずんぐりした体、鋼色の青に白い斑点、大きな目、えらぶた、
        /// 太い柄のある対のヒレ（小さな脚のよう）、3 つに分かれた尾（上・下と、真ん中の小さな房）。長さ len、鼻先が +x、背が +y。少し C の字に曲げて at に q で置く。
        /// </summary>
        static void Fish(NatureMeshKit kit, float len, Vector3 at, Quaternion q)
        {
            int from = kit.VertexCount;
            float R = len * 0.13f;
            // u runs nose (0) → tail base (1); a round the body, 0 = the +z side
            Vector3 Body(float u, float a)
            {
                float x = Mathf.Lerp(len * 0.5f, -len * 0.32f, u);
                float r = R * (0.22f + 0.78f * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.62f))), 0.75f));
                float s = Mathf.Sin(a);
                return new Vector3(x, s * r * (s < 0f ? 1.08f : 1f), Mathf.Cos(a) * r * 0.62f);   // flat sides, a heavier belly
            }
            PropKit.Surface(kit, (t, a) => Body(1f - t, a), 22, 14, FISH, FISH, FISH, R * 0.15f);   // built tail → nose so the faces point out
            // white specks, the mouth, the gill covers
            for (int i = 0; i < 22; i++)
                kit.Ball(Body(PropKit.R(61, i, 0.14f, 0.85f), PropKit.R(61, i + 30, 0f, Mathf.PI * 2f)), Vector3.one * PropKit.R(61, i + 60, 0.0028f, 0.0045f), 2, SPECK);
            PropKit.Lump(kit, new Vector3(len * 0.47f, -R * 0.22f, 0f), new Vector3(len * 0.035f, R * 0.05f, R * 0.42f), 3, 8, DARK, 3);
            for (int side = -1; side <= 1; side += 2)
            {
                float a0 = side > 0 ? 0f : Mathf.PI;
                PropKit.Pipe(kit, t => Body(0.24f, a0 + side * Mathf.Lerp(-1.05f, 1.05f, t)) + new Vector3(0f, 0f, side * 0.001f), (t, _) => 0.0022f, 8, 5, FIN);
                // big round eyes: a pale ring and a dark middle
                kit.Ball(new Vector3(len * 0.37f, R * 0.28f, side * R * 0.5f), new Vector3(R * 0.21f, R * 0.21f, R * 0.1f), 4, EYERING);
                kit.Ball(new Vector3(len * 0.372f, R * 0.28f, side * R * 0.57f), new Vector3(R * 0.12f, R * 0.12f, R * 0.06f), 3, DARK);
            }
            // the tail: an upper and a lower lobe, and the little middle tuft
            Fin(kit, len, new Vector3(-len * 0.3f, R * 0.22f, 0f), new Vector3(-0.87f, 0.5f, 0f), len * 0.2f, R * 0.95f);
            Fin(kit, len, new Vector3(-len * 0.3f, -R * 0.22f, 0f), new Vector3(-0.87f, -0.5f, 0f), len * 0.2f, R * 0.95f);
            Fin(kit, len, new Vector3(-len * 0.33f, 0f, 0f), Vector3.left, len * 0.15f, R * 0.45f);
            // the back fins and the fin under the tail
            Fin(kit, len, new Vector3(len * 0.1f, R * 0.9f, 0f), new Vector3(-0.5f, 0.87f, 0f), len * 0.12f, R * 0.75f);
            Fin(kit, len, new Vector3(-len * 0.12f, R * 0.72f, 0f), new Vector3(-0.7f, 0.7f, 0f), len * 0.11f, R * 0.55f);
            Fin(kit, len, new Vector3(-len * 0.12f, -R * 0.75f, 0f), new Vector3(-0.7f, -0.7f, 0f), len * 0.11f, R * 0.55f);
            // the fleshy paired fins: a round stalk, then the fan
            for (int side = -1; side <= 1; side += 2)
            {
                var pr = new Vector3(len * 0.22f, -R * 0.35f, side * R * 0.5f);
                var pv = new Vector3(-len * 0.02f, -R * 0.8f, side * R * 0.35f);
                kit.Ball(pr, Vector3.one * R * 0.2f, 3, FISH);
                kit.Ball(pv, Vector3.one * R * 0.18f, 3, FISH);
                Fin(kit, len, pr, new Vector3(-0.6f, -0.5f, side * 0.6f), len * 0.13f, R * 0.5f);
                Fin(kit, len, pv, new Vector3(-0.6f, -0.7f, side * 0.4f), len * 0.11f, R * 0.45f);
            }
            // bend the whole fish a little (a C, head and tail toward +z)
            kit.Transform(from, p =>
            {
                float k = p.x / (len * 0.5f);
                return at + q * (p + Vector3.forward * (len * 0.09f * k * k - len * 0.03f));
            });
        }

        /// <summary>ヒレ：根元 root から dir へ長さ length、幅 width の、平たい楕円の塊（厚みは魚の長さの 3%）。</summary>
        static void Fin(NatureMeshKit kit, float len, Vector3 root, Vector3 dir, float length, float width)
        {
            dir.Normalize();
            var rot = Quaternion.FromToRotation(Vector3.right, dir);
            PropKit.Lump(kit, root + dir * (length * 0.45f), new Vector3(length * 0.5f, width * 0.5f, len * 0.015f), 6, 12, FIN, 7, 0f, 1f, float.NaN, rot);
        }
    }
}
