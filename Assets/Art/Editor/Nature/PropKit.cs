using System;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 島の小物（岩・木・足元・浜・茂み・ヤシの残り・ナメクジの暮らしの跡）を数式で作るための部品（Nov 2）。NatureMeshKit に足していく。
    /// 面は「かじれるか」で分ける（docs/modeling/README.md 8章）：かじれない硬い物はカクカク、かじれるやわらかい物はなめらか。
    /// マテリアルごとに決めるので、1つの物の中で部品ごとに分けられる（苔の乗った岩なら、岩はカクカク・苔はなめらか）。
    /// </summary>
    public static class PropKit
    {
        /// <summary>
        /// マテリアル 1 つぶんの決まり：名前、色、カクカクか、風の揺れ（m）、両面か。
        /// 質感（Nov 2 わくさん「小さい物は質感を凝りたい」）はテクスチャを描かず、Slug/Toon の物の座標のノイズで付ける：
        /// Mottle（ぼんやりしたむらと細かい点）、Camo（ふちのくっきりした 3 色のまだら。地衣類・苔・しみ）、Dent（細かいでこぼこの陰）、Felt（細かい粒）。
        /// 大きさは m の物の座標なので、scale 4 なら約 25 cm のまだら。
        /// </summary>
        public struct Spec
        {
            public string name, hex;
            public bool facet, twoSided;
            public float wind;
            public float mottle, mottleScale, camo, camoScale, dent, dentFreq, felt;
            public string mottleHex, camo1, camo2, camo3;
            public Spec(string name, string hex, bool facet, float wind = 0f, bool twoSided = false) : this()
            { this.name = name; this.hex = hex; this.facet = facet; this.wind = wind; this.twoSided = twoSided; }

            public Spec Mottle(string color, float amount, float scale) { mottleHex = color; mottle = amount; mottleScale = scale; return this; }
            public Spec Camo(string c1, string c2, string c3, float scale, float amount = 1f) { camo1 = c1; camo2 = c2; camo3 = c3; camoScale = scale; camo = amount; return this; }
            public Spec Dent(float amp, float freq) { dent = amp; dentFreq = freq; return this; }
            public Spec Felt(float amount) { felt = amount; return this; }
        }

        public static Material[] Mats(string dir, string prefix, params Spec[] specs)
        {
            System.IO.Directory.CreateDirectory(dir);
            var m = new Material[specs.Length];
            for (int i = 0; i < specs.Length; i++)
            {
                var sp = specs[i];
                m[i] = NatureAssets.Mat($"{dir}/{prefix}_{sp.name}.mat", sp.hex, sp.twoSided, sp.wind);
                m[i].SetFloat("_Facet", sp.facet ? 1f : 0f);
                m[i].SetFloat("_MottleAmount", sp.mottle);
                if (sp.mottle > 0f) { m[i].SetColor("_MottleColor", Hex(sp.mottleHex)); m[i].SetFloat("_MottleScale", sp.mottleScale); }
                m[i].SetFloat("_CamoAmount", sp.camo);
                if (sp.camo > 0f)
                {
                    m[i].SetColor("_CamoColor1", Hex(sp.camo1)); m[i].SetColor("_CamoColor2", Hex(sp.camo2)); m[i].SetColor("_CamoColor3", Hex(sp.camo3));
                    m[i].SetFloat("_CamoScale", sp.camoScale);
                }
                m[i].SetFloat("_DentAmp", sp.dent);
                if (sp.dent > 0f) m[i].SetFloat("_DentFreq", sp.dentFreq);
                m[i].SetFloat("_FeltAmount", sp.felt);
            }
            return m;
        }

        public static Color Hex(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        /// <summary>メッシュとプレハブを保存する。collider：0 なし（見るだけ）、1 箱（見た目の箱を少し小さく）、2 凸のメッシュ、3 形どおりのメッシュ（動かない物）。</summary>
        public static string Save(string dir, string name, NatureMeshKit kit, Material[] mats, int collider)
        {
            var mesh = NatureAssets.SaveMesh(kit.ToMesh($"{name}_Mesh"), $"{dir}/{name}_Mesh.asset");
            NatureAssets.SavePrefab($"{dir}/{name}.prefab", mesh, mats, go =>
            {
                if (collider == 1)
                {
                    var b = mesh.bounds;
                    var c = go.AddComponent<BoxCollider>();
                    c.center = b.center; c.size = Vector3.Scale(b.size, new Vector3(0.85f, 0.95f, 0.85f));
                }
                else if (collider == 2)
                {
                    var c = go.AddComponent<MeshCollider>();
                    c.sharedMesh = mesh; c.convex = true;
                }
                else if (collider == 3) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            });
            return $"{name} {kit.TriangleCount}";
        }

        // ───────── 乱数とノイズ ─────────

        public static float H(int seed, int i) => NatureAssets.Hash(seed, i);
        public static float R(int seed, int i, float a, float b) => Mathf.Lerp(a, b, NatureAssets.Hash(seed, i));

        /// <summary>なめらかな 3D の値ノイズ（-1〜1）。</summary>
        public static float Noise(Vector3 p, int seed)
        {
            int x0 = Mathf.FloorToInt(p.x), y0 = Mathf.FloorToInt(p.y), z0 = Mathf.FloorToInt(p.z);
            float fx = p.x - x0, fy = p.y - y0, fz = p.z - z0;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy); fz = fz * fz * (3f - 2f * fz);
            float L(int x, int y, int z) => NatureAssets.Hash(seed, x * 73856093 ^ y * 19349663 ^ z * 83492791) * 2f - 1f;
            float a = Mathf.Lerp(L(x0, y0, z0), L(x0 + 1, y0, z0), fx), b = Mathf.Lerp(L(x0, y0 + 1, z0), L(x0 + 1, y0 + 1, z0), fx);
            float c = Mathf.Lerp(L(x0, y0, z0 + 1), L(x0 + 1, y0, z0 + 1), fx), d = Mathf.Lerp(L(x0, y0 + 1, z0 + 1), L(x0 + 1, y0 + 1, z0 + 1), fx);
            return Mathf.Lerp(Mathf.Lerp(a, b, fy), Mathf.Lerp(c, d, fy), fz);
        }

        /// <summary>大きいうねり＋半分の細かさを重ねたノイズ（-1〜1 くらい）。</summary>
        public static float Fbm(Vector3 p, int seed) => Noise(p, seed) * 0.7f + Noise(p * 2.1f + Vector3.one * 7.3f, seed + 1) * 0.3f;

        public static Vector3 Yaw(Vector3 v, float deg) => Quaternion.Euler(0f, deg, 0f) * v;
        public static Vector3 Dir(float yawDeg) => new Vector3(Mathf.Cos(yawDeg * Mathf.Deg2Rad), 0f, Mathf.Sin(yawDeg * Mathf.Deg2Rad));

        // ───────── 形 ─────────

        /// <summary>
        /// 面 P(t, a)：t は 0〜1 を rows 段、a は 0〜2π を sides 本。筒・玉・板のもと。capBottom / capTop は端のふたのマテリアル（-1 でなし）。
        /// ふたは端の輪の重心に 1 点を置く（lift だけ外へ出すと、丸いふたになる）。subAt を渡すと、段ごとにマテリアルを変える（t → マテリアル。幹の節の帯など）。
        /// subAtTA なら面ごと（t, a → マテリアル。樹皮の溝だけ暗く、など）。
        /// </summary>
        public static void Surface(NatureMeshKit kit, Func<float, float, Vector3> P, int rows, int sides, int sub, int capBottom = -1, int capTop = -1, float capLift = 0f,
                                   Func<float, int> subAt = null, Func<float, float, int> subAtTA = null)
        {
            int first = kit.VertexCount, prev = -1;
            for (int r = 0; r <= rows; r++)
            {
                float t = r / (float)rows;
                int start = kit.VertexCount;
                for (int i = 0; i < sides; i++) kit.Vert(P(t, Mathf.PI * 2f * i / sides), new Vector2(i / (float)sides, t));
                if (prev >= 0)
                {
                    float tm = (r - 0.5f) / rows;
                    int rs = subAt != null ? subAt(tm) : sub;
                    for (int i = 0; i < sides; i++)
                    {
                        int j = (i + 1) % sides;
                        kit.Quad(subAtTA != null ? subAtTA(tm, Mathf.PI * 2f * (i + 0.5f) / sides) : rs, prev + i, start + i, start + j, prev + j);
                    }
                }
                prev = start;
            }
            if (capBottom >= 0) Cap(kit, P, 0f, first, sides, capBottom, true, capLift);
            if (capTop >= 0) Cap(kit, P, 1f, prev, sides, capTop, false, capLift);
        }

        static void Cap(NatureMeshKit kit, Func<float, float, Vector3> P, float t, int ring, int sides, int sub, bool bottom, float lift)
        {
            Vector3 c = Vector3.zero;
            for (int i = 0; i < sides; i++) c += P(t, Mathf.PI * 2f * i / sides);
            c /= sides;
            Vector3 axis = (c - (P(bottom ? 0.05f : 0.95f, 0f) + P(bottom ? 0.05f : 0.95f, Mathf.PI)) * 0.5f).normalized;
            int ci = kit.Vert(c + axis * lift, new Vector2(0.5f, t));
            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                if (bottom) kit.Tri(sub, ci, ring + j, ring + i);
                else kit.Tri(sub, ci, ring + i, ring + j);
            }
        }

        /// <summary>
        /// 中心線 center(t) に沿った筒。半径は radius(t, a)（a で溝やこぶを付けられる）。
        /// 向きの基準は、ほぼ縦の筒なら前、横の筒なら上（筒の途中でねじれないように、筒ぜんたいで 1 つ）。
        /// </summary>
        public static void Pipe(NatureMeshKit kit, Func<float, Vector3> center, Func<float, float, float> radius, int rows, int sides, int sub,
                                int capBottom = -1, int capTop = -1, float capLift = 0f, Func<float, int> subAt = null, Func<float, float, int> subAtTA = null)
        {
            Vector3 whole = (center(1f) - center(0f)).normalized;
            Vector3 refUp = Mathf.Abs(Vector3.Dot(whole, Vector3.up)) > 0.7f ? Vector3.forward : Vector3.up;
            Surface(kit, (t, a) =>
            {
                Vector3 tan = center(Mathf.Min(1f, t + 0.01f)) - center(Mathf.Max(0f, t - 0.01f));
                tan = tan.sqrMagnitude < 1e-8f ? whole : tan.normalized;
                Vector3 side = Vector3.Cross(tan, refUp);
                if (side.sqrMagnitude < 1e-4f) side = Vector3.Cross(tan, Vector3.right);
                side.Normalize();
                Vector3 up = Vector3.Cross(side, tan);
                return center(t) + (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * radius(t, a);
            }, rows, sides, sub, capBottom, capTop, capLift, subAt, subAtTA);
        }

        /// <summary>まっすぐな棒（a から b、半径 r0 → r1）。両端にふた。</summary>
        public static void Stick(NatureMeshKit kit, Vector3 a, Vector3 b, float r0, float r1, int sides, int sub, int capSub = -2)
        {
            if (capSub == -2) capSub = sub;
            Pipe(kit, t => Vector3.Lerp(a, b, t), (t, _) => Mathf.Lerp(r0, r1, t), 1, sides, sub, capSub, capSub);
        }

        /// <summary>
        /// ごつごつした塊（岩・土・苔のかたまり）。c が真ん中、size が半径（縦横奥）、rough がでこぼこの割合、freq がでこぼこの細かさ。
        /// floorY より下は平らに切る（地面に座る。NaN なら切らない）。rot で回す。
        /// tFrom（0〜1）から上だけを作り、grow 倍に少しふくらませると、同じ塊の上にかぶさる皮になる（岩の上の苔・塩の吹き出し）。
        /// post で最後に点を動かす（平らに割る・ずらす）。box を 2 より大きくすると角ばる（3〜5 で角の丸い石の塊）。
        /// </summary>
        public static void Lump(NatureMeshKit kit, Vector3 c, Vector3 size, int rows, int sides, int sub, int seed,
                                float rough = 0.25f, float freq = 1.6f, float floorY = float.NaN, Quaternion? rot = null,
                                float tFrom = 0f, float grow = 1f, Func<Vector3, float> extra = null, Func<Vector3, Vector3> post = null, float box = 2f)
        {
            var q = rot ?? Quaternion.identity;
            Surface(kit, (t, a) =>
            {
                float phi = Mathf.PI * Mathf.Lerp(tFrom, 1f, t);
                var n = new Vector3(Mathf.Sin(phi) * Mathf.Cos(a), -Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(a));
                float k = 1f + rough * Fbm(n * freq + Vector3.one * (seed % 17), seed);
                if (extra != null) k += extra(n);
                var shape = box > 2.01f ? Boxy(n, box) : n;
                var p = c + q * Vector3.Scale(shape * (k * grow), size);
                if (post != null) p = post(p);
                if (!float.IsNaN(floorY) && p.y < floorY) p.y = floorY;
                return p;
            }, rows, sides, sub);
        }

        /// <summary>
        /// 平たい葉（落ち葉など）：根元 root から向き dir へ長さ len、片側の幅 w、先がめくれる curl（m）、ふちが反る cup（＋で皿）。
        /// roll（度）で軸のまわりに転がす（片方のふちが浮く）。
        /// </summary>
        public static void FlatLeaf(NatureMeshKit kit, Vector3 root, Vector3 dir, float len, float w, float curl, float cup, int segs, int sub, float roll = 0f, int ribSub = -1)
        {
            dir = new Vector3(dir.x, 0f, dir.z).normalized;
            Vector3 side = Quaternion.AngleAxis(roll, dir) * Vector3.Cross(Vector3.up, dir);
            Vector3 Path(float s) => root + dir * (len * s) + Vector3.up * (curl * s * s);
            // a fine saw edge along the leaf
            float Width(float s) => w * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Clamp01(s * 0.92f + 0.04f)), 0.8f) * (1f - 0.12f * Mathf.Abs(Mathf.Sin(s * Mathf.PI * 9f)));
            kit.Blade(Path, Width, segs * 2, Mathf.Max(0.004f, w * 0.05f), -cup, sub, side);
            if (ribSub < 0) return;
            // the midrib: a thin raised line down the middle, with a little stalk past the base
            var up = Vector3.Cross(dir, side).normalized;
            if (up.y < 0f) up = -up;
            Pipe(kit, s => Path(s * 1.08f - 0.08f) + up * (w * 0.06f), (s, a) => Mathf.Lerp(w * 0.07f, w * 0.015f, s), 5, 4, ribSub, ribSub, -1);
        }

        /// <summary>
        /// 枝分かれする枝（枯れ枝・茂みの骨）。start から dir へ len、太さ r。depth 段まで 2〜3 本に分かれる。
        /// tipAction は枝先ごとに呼ぶ（葉のかたまりを付けるのに使う）。
        /// </summary>
        public static void Branch(NatureMeshKit kit, Vector3 start, Vector3 dir, float len, float r, int depth, int seed, int sides, int sub,
                                  float spread = 35f, float droop = 0f, Action<Vector3, Vector3> tipAction = null)
        {
            dir.Normalize();
            Vector3 bendAxis = Vector3.Cross(dir, Vector3.up);
            if (bendAxis.sqrMagnitude < 1e-4f) bendAxis = Vector3.right;
            float kink = R(seed, 1, -18f, 18f);
            Vector3 end = start + (Quaternion.AngleAxis(kink, bendAxis.normalized) * dir) * len - Vector3.up * (droop * len);
            Vector3 c1 = Vector3.Lerp(start, end, 0.5f) + Vector3.Cross(dir, bendAxis).normalized * (len * R(seed, 2, -0.08f, 0.08f));
            Vector3 Center(float t) => (1 - t) * (1 - t) * start + 2 * (1 - t) * t * c1 + t * t * end;
            float rEnd = r * 0.62f;
            Pipe(kit, Center, (t, a) => Mathf.Lerp(r, rEnd, t), 3, sides, sub, -1, depth == 0 ? sub : -1);
            Vector3 endDir = (end - Center(0.9f)).normalized;
            if (depth == 0) { tipAction?.Invoke(end, endDir); return; }
            int kids = H(seed, 3) < 0.35f ? 3 : 2;
            float roll = R(seed, 4, 0f, 360f);
            for (int k = 0; k < kids; k++)
            {
                Vector3 perp = Quaternion.AngleAxis(roll + 360f * k / kids, endDir) * Vector3.Cross(endDir, Mathf.Abs(endDir.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
                Vector3 nd = Quaternion.AngleAxis(spread * R(seed, 10 + k, 0.7f, 1.2f), perp) * endDir;
                Branch(kit, end - endDir * (rEnd * 0.5f), nd, len * R(seed, 20 + k, 0.62f, 0.8f), rEnd, depth - 1, seed * 7 + k + 1, Mathf.Max(4, sides - 1), sub, spread, droop, tipAction);
            }
        }

        /// <summary>六角の結晶（塩）：根元 a から向き dir へ長さ len、太さ r、先は尖る（tip の割合）。</summary>
        public static void Crystal(NatureMeshKit kit, Vector3 a, Vector3 dir, float len, float r, float tip, int sub)
        {
            dir.Normalize();
            Vector3 b = a + dir * (len * (1f - tip));
            // growth steps: the faces step in and out a little every few centimetres (thin lines across the crystal)
            Pipe(kit, t => Vector3.LerpUnclamped(a, b, t), (t, _) => r * (1f - 0.045f * ((int)(t * 8f) % 2)), 8, 6, sub, sub, -1);
            // the point: a 6-sided pyramid on the top ring (verts so far: [bottom ring 6][top ring 6][bottom cap centre 1])
            int top = kit.VertexCount - 1 - 6;
            int apex = kit.Vert(a + dir * len, new Vector2(0.5f, 1f));
            for (int i = 0; i < 6; i++) kit.Tri(sub, apex, top + (i + 1) % 6, top + i);
        }

        // ───────── 質感の形（かたまりの extra に足す。n は単位球の向き、seed で変える） ─────────

        /// <summary>割れ目：細い溝がつながって走る（深さ depth、細かさ freq）。</summary>
        public static float Cracks(Vector3 n, int seed, float depth, float freq = 4.5f) =>
            -depth * Mathf.Pow(1f - Mathf.Abs(Noise(n * freq, seed)), 8f);

        /// <summary>地層の段：高さ（n.y）に沿って、うねる棚がいくつも重なる（layers 段、段の高さ step）。</summary>
        public static float Strata(Vector3 n, int seed, float layers, float step)
        {
            float h = n.y * layers + Noise(n * 1.5f, seed) * 0.8f;
            float f = h - Mathf.Floor(h);
            return step * (Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(f * 4f)) - 1f);
        }

        /// <summary>欠け：ところどころの面が平らにそがれる（強さ amount）。</summary>
        public static float Chips(Vector3 n, int seed, float amount, float freq = 3f)
        {
            float c = Noise(n * freq + Vector3.one * 3.3f, seed + 9);
            return -amount * Mathf.Max(0f, c - 0.25f);
        }

        /// <summary>
        /// 樹皮の板：縦の深い溝で割れた板が、ところどころ横に切れる（筒の t, a で）。count は回りの板の数、len は筒の長さ（m）。
        /// 戻り値は半径の倍率（1 ＋ 板の盛り上がり）。
        /// </summary>
        public static float BarkPlates(float t, float a, float len, int seed, float count = 14f, float depth = 0.08f)
        {
            float wob = Noise(new Vector3(t * len * 0.8f, 0f, 0f), seed) * 1.2f;
            float ridge = Mathf.Abs(Mathf.Sin((a * count + wob) * 0.5f));
            float breakAcross = Mathf.Abs(Noise(new Vector3(a * 2f, t * len * 3f, 0f), seed + 5));
            float plate = Mathf.Pow(ridge, 0.35f) * Mathf.SmoothStep(0.05f, 0.25f, breakAcross);
            return 1f + depth * (plate - 0.6f) + 0.03f * Noise(new Vector3(Mathf.Cos(a), t * len * 2f, Mathf.Sin(a)) * 2f, seed + 7);
        }

        /// <summary>BarkPlates の溝の所か（溝だけ暗いマテリアルにするのに使う）。</summary>
        public static bool BarkGroove(float t, float a, float len, int seed, float count = 14f)
        {
            float wob = Noise(new Vector3(t * len * 0.8f, 0f, 0f), seed) * 1.2f;
            float ridge = Mathf.Abs(Mathf.Sin((a * count + wob) * 0.5f));
            // only the long furrows, not the breaks across them (those read as brickwork)
            return ridge < 0.2f + 0.08f * Noise(new Vector3(a * 3f, t * len * 2f, 0f), seed + 6);
        }

        /// <summary>流木の木目：ねじれた細い筋（twist はねじれの回数）。</summary>
        public static float Grain(float t, float a, float len, int seed, float twist = 1f, float depth = 0.05f) =>
            1f + depth * (Mathf.Abs(Mathf.Sin(a * 9f + t * twist * Mathf.PI * 2f + Noise(new Vector3(t * len, a, 0f), seed) * 1.5f)) - 0.5f)
               - 0.06f * Mathf.Pow(Mathf.Max(0f, Noise(new Vector3(t * len * 1.5f, Mathf.Cos(a) * 2f, Mathf.Sin(a) * 2f), seed + 3)), 3f) * 8f * depth;

        /// <summary>角ばり：球の向き n を、四角に近い向きへ（p が大きいほど箱。2 で球のまま）。</summary>
        public static Vector3 Boxy(Vector3 n, float p)
        {
            float d = Mathf.Pow(Mathf.Pow(Mathf.Abs(n.x), p) + Mathf.Pow(Mathf.Abs(n.y), p) + Mathf.Pow(Mathf.Abs(n.z), p), 1f / p);
            return n / Mathf.Max(1e-4f, d);
        }

        // ───────── ケルン風の植物（docs/modeling/README.md 8章。手本は MassBushBuilder） ─────────

        /// <summary>
        /// ケルン風の塗り：段は 3 つでくっきり、陰は青みの緑、輪郭線は暗い色。細かいでこぼこ（_DentAmp）と粒（_FeltAmount）は消す
        /// （植物の塊の中は描き込まない）。葉・草・苔など、やわらかい植物のマテリアルに使う。
        /// </summary>
        public static void CairnPaint(Material m, string outline = "#2E3E2C", float shadeR = 0.5f, float shadeG = 0.62f, float shadeB = 0.74f)
        {
            m.SetColor("_ShadeColor", new Color(shadeR, shadeG, shadeB));
            m.SetColor("_OutlineColor", Hex(outline));
            m.SetFloat("_BandSoftness", 0.04f);
            m.SetFloat("_ToonSmooth", 0.05f);
            m.SetFloat("_DentAmp", 0f);
            m.SetFloat("_FeltAmount", 0f);
            UnityEditor.EditorUtility.SetDirty(m);
        }

        /// <summary>
        /// 葉の塊 1 つ（MassBush と同じ作り）：なめらかにつぶれた玉を塊の丸みで光らせ、上と外のふちからだけ丸い葉先を半分埋めて出す。
        /// c 中心、r 半径、floor より下は切る、tips は葉先の数、tipLen は葉先の長さ、h は風の重みを付けるときの高さ（揺れは上ほど大きい）。
        /// </summary>
        public static void LeafMass(NatureMeshKit kit, Vector3 c, Vector3 r, int seed, int massSub, int tipSub, int tips, float tipLen, float floor, float h)
        {
            var rot = Quaternion.Euler(0f, seed * 37f, 0f);
            int from = kit.VertexCount;
            Lump(kit, c, r, 10, 18, massSub, seed, 0.1f, 1.4f, floor, rot, 0f, 1f, n => 0.1f * Mathf.Abs(Noise(n * 6f, seed + 1)));
            kit.Paint(from, (p, uv) => new Color(Mathf.Clamp01(p.y / h) * 0.5f, 0.3f, H(seed, 3), 1f));
            kit.Mass(from, c, r, 1f);
            int tipFrom = kit.VertexCount;
            for (int i = 0; i < tips; i++)
            {
                float az = R(seed + 2, i, 0f, Mathf.PI * 2f), el = Mathf.Asin(R(seed + 3, i, -0.15f, 0.85f));
                var n = new Vector3(Mathf.Cos(el) * Mathf.Cos(az), Mathf.Sin(el), Mathf.Cos(el) * Mathf.Sin(az));
                var root = c + Vector3.Scale(n, r) * 0.82f;
                if (!float.IsNaN(floor) && root.y < floor + 0.04f) continue;
                var dir = (n * 0.7f + Vector3.up * 0.6f).normalized;
                float len = tipLen * R(seed + 4, i, 0.8f, 1.2f), wid = len * 0.42f;
                Func<float, Vector3> path = q => root + dir * (len * q) - Vector3.up * (0.2f * len * q * q);
                int bf = kit.VertexCount;
                kit.Blade(path, q => wid * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Min(1f, q * 0.9f + 0.1f))), 0.6f), 3, 0.006f, 0.15f, tipSub);
                int ii = i;
                kit.Paint(bf, (p, uv) => new Color(Mathf.Clamp01(p.y / h) * 0.5f + 0.1f, uv.y, H(seed + 5, ii), 1f));
            }
            kit.Mass(tipFrom, c, r, 0.8f);
        }

        /// <summary>
        /// ケルン風の草の株：根元の低い塊から、幅のある刃のような葉が上と外へ弓なりに出る。株ぜんたいを 1 つのドームとして光らせる
        /// （葉 1 本ずつの陰にしない）。at 根元、radius 株の半径、height 葉の高さ、blades 葉の数、lean は全体のなびき（m、向きつき）。
        /// </summary>
        public static void GrassTuft(NatureMeshKit kit, Vector3 at, float radius, float height, int blades, int seed, int massSub, int bladeSub,
                                     Vector3 lean = default, float width = 0.035f)
        {
            int from = kit.VertexCount;
            var baseR = new Vector3(radius * 0.75f, height * 0.07f, radius * 0.75f);
            Lump(kit, at, baseR, 5, 12, massSub, seed, 0.15f, 2f, at.y, null, 0f, 1f, n => 0.15f * Mathf.Abs(Noise(n * 5f, seed + 1)));
            kit.Paint(from, (p, uv) => new Color(0f, 0f, 0f, 1f));
            for (int i = 0; i < blades; i++)
            {
                float az = R(seed + 2, i, 0f, Mathf.PI * 2f), d = Mathf.Sqrt(H(seed + 3, i)) * radius * 0.8f;
                var outDir = new Vector3(Mathf.Cos(az), 0f, Mathf.Sin(az));
                var root = at + outDir * d + Vector3.up * (height * 0.02f);
                float len = height * R(seed + 4, i, 0.6f, 1.05f) * (1f - 0.3f * d / Mathf.Max(0.01f, radius));
                float arch = R(seed + 5, i, 0.25f, 0.6f) * (0.5f + d / Mathf.Max(0.01f, radius));
                Func<float, Vector3> path = q => root + Vector3.up * (len * q) + outDir * (arch * len * q * q) + lean * (q * q) - Vector3.up * (arch * 0.4f * len * q * q * q);
                int bf = kit.VertexCount;
                kit.Blade(path, q => width * Mathf.Pow(Mathf.Max(0f, 1f - q), 0.7f) * Mathf.Min(1f, 0.45f + q * 5f), 7, 0.004f, 0.12f, bladeSub);
                int ii = i;
                kit.Paint(bf, (p, uv) => new Color(uv.y, uv.y * 0.4f, H(seed + 6, ii), 1f));
            }
            // the whole tuft lights as one dome
            kit.Mass(from, at + Vector3.up * (height * 0.35f), new Vector3(radius * 1.6f + height * 0.3f, height * 0.75f, radius * 1.6f + height * 0.3f), 0.75f);
        }
    }
}
