using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 森の木 4 種（Nov 3 下書き、1 種 1 案。わくさんのペルソナが監督）。メニュー Slug/Nature/Build Forest Trees で作り直す（Assets/Art/Nature/ForestTree）。
    /// ケルン風（docs/modeling/README.md 8章、手本は MassBushBuilder）：葉は大きな塊で見せ、陰は塊の丸み、陰は青み、段 3 つ。
    /// 幹と枝はかじれないのでカクカク、葉はなめらかで風に揺れる。葉の塊は高い所だけ（森の中は幹の柱の広間で、天井の塊のすき間から光が落ちる）。
    /// 4 種で幹の太さを変える（シルエットで見分ける）。ナメクジの目の高さ 0.47 m は人の約 1/3.6。
    /// - A 板根の木（雨の森の主木、いちばん多い）：高さ 8.3 m（ナメクジの 18 倍、人なら 30 m）、幹の直径 1.15 m（高さの 1/7）。
    ///   板根 5 枚：幹から 2 m 張り出す厚い板（厚み 0.5 m）で、上から見るとうねる。背は 45° の坂（這い上がれる。上は見張り台）。板根の間は「部屋」。
    ///   頭上 5.6〜8.3 m に葉の塊 6 つ（幅 7.5 m）、つる 2 本
    /// - B 段の木（モモタマナ風。浜・低い土地・花の草原のふち）：高さ 6 m、幹の直径 0.55 m。水平な枝が 3 段、段ごとに大きな平たい葉の板が 1〜2 枚
    ///   （下の段ほど広い。小さい皿を並べると刈り込んだ庭木に見える）。乾季は葉がくすんだ赤茶（ForestTree_B_Dry。形は同じでマテリアルだけ替える）
    /// - C 傘の木（アカシア風。砂漠の端・赤い台地）：高さ 4.9 m、幹（直径 0.62 m）が 1.5 m で 3 本に分かれ、上に平たい傘（幅 6.5 m、下は平ら）
    /// - D ねじれ木（オヒア風。黒い溶岩原で最初に生える木）：高さ 5 m 前後、細い幹（直径 0.34 m）が 1.5 m で 3 本に分かれてジグザグに曲がり、
    ///   根が溶岩をつかむ。まばらな小さい塊 6〜9 つは白っぽい灰緑、赤い小さな花の点（いちばん鮮やかな赤はヒバナに譲る）
    /// 色：葉 A #5E7A44・B #6E8448（乾季 #9A5E44）・C #8A9258・D #8A9478、葉先は一段明るい。樹皮 A #A69C88・B #9C8C76・C #9A8A72・D #94897A（溝は一段暗い）、
    /// つる #6A6A44、花 #A8483A。当たり判定は幹と板根（と傘の木の太い枝）だけのメッシュ（ForestTree_X_Col。板根の坂は這い上がれる）。葉には当たらない。
    /// </summary>
    public static class ForestTreeBuilder
    {
        const string Dir = "Assets/Art/Nature/ForestTree";
        const int BARK = 0, GROOVE = 1, LEAF = 2, TIP = 3, VINE = 4, FLOWER = 5;

        [MenuItem("Slug/Nature/Build Forest Trees")]
        public static void Build()
        {
            var log = new StringBuilder("Forest trees built:");
            log.Append(' ').Append(Buttress()).Append(' ').Append(Tiered()).Append(' ').Append(Umbrella()).Append(' ').Append(Twisted());
            AssetDatabase.SaveAssets();
            Debug.Log(log);
        }

        static Material[] Mats(string id, string bark, string groove, string leaf, string tip)
        {
            var m = PropKit.Mats(Dir, $"ForestTree_{id}",
                new PropKit.Spec("Bark", bark, true).Mottle(groove, 0.35f, 4f).Dent(0.25f, 14f),
                new PropKit.Spec("Groove", groove, true),
                new PropKit.Spec("Leaf", leaf, false, 0.1f).Mottle(tip, 0.2f, 1.2f),
                new PropKit.Spec("Tip", tip, false, 0.12f),
                new PropKit.Spec("Vine", "#6A6A44", false, 0.03f),
                new PropKit.Spec("Flower", "#A8483A", false, 0.1f));
            foreach (int i in new[] { LEAF, TIP, VINE, FLOWER }) PropKit.CairnPaint(m[i]);
            return m;
        }

        static (Mesh mesh, Mesh col) SaveMeshes(string name, NatureMeshKit kit, NatureMeshKit col) =>
            (NatureAssets.SaveMesh(kit.ToMesh($"{name}_Mesh"), $"{Dir}/{name}_Mesh.asset"),
             NatureAssets.SaveMesh(col.ToMesh($"{name}_Col"), $"{Dir}/{name}_Col.asset"));

        static void SavePrefab(string name, (Mesh mesh, Mesh col) meshes, Material[] mats) =>
            NatureAssets.SavePrefab($"{Dir}/{name}.prefab", meshes.mesh, mats, go => go.AddComponent<MeshCollider>().sharedMesh = meshes.col);

        /// <summary>
        /// 幹：中心線 path(u)、太さ radius(u, a)（u は 0 根元〜1 てっぺん）。段は根元ほど細かい（u = t²。板根の坂と根元の張りがなめらかに）。
        /// 樹皮は溝だけ暗い色（isGroove を渡せばその溝、なければ縦の板の溝。groove が -1 なら塗り分けない：当たり判定のメッシュ用）。
        /// </summary>
        static void Trunk(NatureMeshKit kit, Func<float, Vector3> path, Func<float, float, float> radius, int rows, int sides, int bark, int groove,
                          float len, int seed, Func<float, float, bool> isGroove = null)
        {
            Func<float, float, int> paint = null;
            if (groove >= 0)
                paint = (t, a) => (isGroove != null ? isGroove(t * t, a) : PropKit.BarkGroove(t * t, a, len, seed, 12f)) ? groove : bark;
            PropKit.Pipe(kit, t => path(t * t), (t, a) => radius(t * t, a), rows, sides, bark, bark, bark, 0.05f, null, paint);
        }

        /// <summary>枝：a から b へ、c に引っぱられて曲がる筒（太さ r0 → r1）。先は葉の塊の中に隠れる。</summary>
        static void Limb(NatureMeshKit kit, Vector3 a, Vector3 c, Vector3 b, float r0, float r1, int sides, int sub) =>
            PropKit.Pipe(kit, t => (1 - t) * (1 - t) * a + 2 * (1 - t) * t * c + t * t * b, (t, ang) => Mathf.Lerp(r0, r1, t), 6, sides, sub, -1, sub);

        /// <summary>
        /// 葉の塊を並べ（PropKit.LeafMass。葉先は塊の大きさ × tipScale）、最後に木の頭ぜんたいを 1 つのドームとして光らせる（塊の陰をそろえる）。
        /// floor より下は平らに切る（傘の木の平らな下）。h は風の重みの高さ。塊 k の種は seed + k * 7（花の点の揺れをそろえるのに使う）。
        /// </summary>
        static void Crown(NatureMeshKit kit, List<(Vector3 c, Vector3 r)> masses, int seed, int tipsFirst, int tips, float tipScale, float floor, float h,
                          Vector3 domeC, Vector3 domeR, float domeAmount)
        {
            int from = kit.VertexCount;
            for (int k = 0; k < masses.Count; k++)
            {
                var (c, r) = masses[k];
                PropKit.LeafMass(kit, c, r, seed + k * 7, LEAF, TIP, k == 0 ? tipsFirst : tips, tipScale * r.x, floor, h);
            }
            kit.Mass(from, domeC, domeR, domeAmount);
        }

        /// <summary>
        /// 森の木の頭（Nov 3 直し。わくさん「頭が玉の寄せ集め」）：玉を並べず、大きな 1 つの塊に、ゆるい出っぱりとへこみをノイズで付ける。
        /// 陰は塊ぜんたいの丸み、輪郭からだけ丸い葉先が出る（PropKit.LeafMass と同じ葉先）。floor より下は平らに切る。h は風の重みの高さ。
        /// </summary>
        static void CrownMass(NatureMeshKit kit, Vector3 c, Vector3 r, int seed, int tips, float tipLen, float floor, float h, float lobes = 0.22f)
        {
            var rot = Quaternion.Euler(0f, seed * 37f, 0f);
            Func<Vector3, float> shape = n => lobes * PropKit.Noise(n * 2.2f + Vector3.one * (seed % 13), seed) + 0.07f * Mathf.Abs(PropKit.Noise(n * 7f, seed + 1));
            int from = kit.VertexCount;
            PropKit.Lump(kit, c, r, 16, 32, LEAF, seed, 0f, 1f, floor, rot, 0f, 1f, shape);
            kit.Paint(from, (p, uv) => new Color(Mathf.Clamp01(p.y / h) * 0.5f, 0.3f, PropKit.H(seed, 3), 1f));
            kit.Mass(from, c, r, 0.85f);
            int tipFrom = kit.VertexCount;
            for (int i = 0; i < tips; i++)
            {
                float az = PropKit.R(seed + 2, i, 0f, Mathf.PI * 2f), el = Mathf.Asin(PropKit.R(seed + 3, i, -0.1f, 0.9f));
                var n = new Vector3(Mathf.Cos(el) * Mathf.Cos(az), Mathf.Sin(el), Mathf.Cos(el) * Mathf.Sin(az));
                var root = c + rot * Vector3.Scale(n * (1f + shape(n)), r) * 0.9f;
                if (!float.IsNaN(floor) && root.y < floor + 0.05f) continue;
                var wn = rot * n;
                var dir = (wn * 0.7f + Vector3.up * 0.6f).normalized;
                float len = tipLen * PropKit.R(seed + 4, i, 0.8f, 1.2f), wid = len * 0.42f;
                Func<float, Vector3> path = q => root + dir * (len * q) - Vector3.up * (0.2f * len * q * q);
                int bf = kit.VertexCount;
                kit.Blade(path, q => wid * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Min(1f, q * 0.9f + 0.1f))), 0.6f), 3, 0.006f, 0.15f, TIP);
                int ii = i;
                kit.Paint(bf, (p, uv) => new Color(Mathf.Clamp01(p.y / h) * 0.5f + 0.1f, uv.y, PropKit.H(seed + 5, ii), 1f));
            }
            kit.Mass(tipFrom, c, r, 0.8f);
        }

        // ───────── A 板根の木（雨の森の主木） ─────────

        static string Buttress()
        {
            var m = Mats("A", "#A69C88", "#7E7462", "#5E7A44", "#7A9450");
            var kit = new NatureMeshKit(m.Length);
            var col = new NatureMeshKit(1);
            const int seed = 1100;
            const float H = 5.6f, R0 = 0.42f, Fin = 2.2f, FinH = 2.4f, FinW = 0.24f;
            // a slimmer trunk (0.84 m, a tenth of the height) with a real bend in it
            Func<float, Vector3> path = u => new Vector3(0.35f * Mathf.Sin(u * 2.2f), H * u - 0.05f, 0.2f * Mathf.Sin(u * 3.1f + 1f));
            Func<float, float, float> radius = (u, a) =>
            {
                float y = H * u, r = R0 * (1f - 0.35f * u) * PropKit.BarkPlates(u, a, H, seed, 12f, 0.04f) * (1f + 0.12f * Mathf.Exp(-y / 0.3f));
                // buttresses: five thin tall plates, steep against the trunk (2.4 m up) and sweeping out low to 2.2 m (a concave fin,
                // climbable where it is low), snaking a little seen from above. Nothing fills between them: the gaps are rooms, not a skirt
                float ext = Fin * Mathf.Pow(Mathf.Max(0f, 1f - y / FinH), 1.8f);
                if (ext <= 0f) return r;
                float g = 0f;
                for (int k = 0; k < 5; k++)
                {
                    float ak = k * 72f + 17f + 12f * Mathf.Sin(ext / Fin * Mathf.PI * 1.5f + k);
                    float d = Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, ak)) * Mathf.Deg2Rad;
                    float half = FinW * 0.5f / (R0 + ext * 0.5f);
                    g = Mathf.Max(g, Mathf.Exp(-Mathf.Pow(d / half, 4f)));
                }
                return r + ext * g;
            };
            Trunk(kit, path, radius, 40, 160, BARK, GROOVE, H, seed);
            Trunk(col, path, radius, 24, 120, 0, -1, H, seed);

            // the ceiling: one broad lobed crown (not a pile of balls) and one smaller lobe off to the side at another height
            Vector3 top = path(1f);
            var mainC = top + new Vector3(-0.1f, 1.45f, 0.1f);
            var mainR = new Vector3(3.5f, 1.35f, 3.2f);
            Limb(kit, top - Vector3.up * 0.4f, top + Vector3.up * 0.5f, mainC - Vector3.up * 0.6f, 0.26f, 0.12f, 10, BARK);
            for (int i = 0; i < 5; i++)
            {
                var d = PropKit.Dir(i * 72f + PropKit.R(seed + 5, i, -14f, 14f));
                var a0 = path(PropKit.R(seed + 9, i, 0.82f, 0.95f));
                var end = top + d * PropKit.R(seed + 7, i, 1.8f, 2.4f) + Vector3.up * PropKit.R(seed + 8, i, 0.7f, 1.0f);
                Limb(kit, a0, a0 + Vector3.up * 0.8f + d * 0.4f, end, 0.2f, 0.07f, 9, BARK);
                // a thin side branch under the crown, so looking up reads as branches holding a ceiling
                PropKit.Branch(kit, Vector3.Lerp(a0, end, 0.55f), (d + Vector3.up * 0.2f).normalized, 1.1f, 0.05f, 1, seed + 20 + i, 6, BARK, 30f);
            }
            CrownMass(kit, mainC, mainR, seed + 30, 70, 0.4f, float.NaN, 8.3f);
            CrownMass(kit, top + new Vector3(2.6f, 0.75f, -1.6f), new Vector3(1.7f, 0.85f, 1.5f), seed + 40, 28, 0.32f, float.NaN, 8.3f);

            // two lianas from the underside of the crown: one down to the ground, one ending in the air
            for (int i = 0; i < 2; i++)
            {
                var d = PropKit.Dir(40f + i * 150f);
                var hang = mainC + d * (mainR.x * 0.62f) - Vector3.up * (mainR.y * 0.55f);
                var foot = new Vector3(hang.x * 0.8f, i == 0 ? -0.05f : 1.3f, hang.z * 0.8f);
                var sway = Vector3.Cross(Vector3.up, foot - hang).normalized;
                var outward = new Vector3(hang.x, 0f, hang.z).normalized;
                PropKit.Pipe(kit, t => Vector3.Lerp(hang, foot, t) + outward * (0.45f * Mathf.Sin(Mathf.PI * t)) + sway * (0.12f * Mathf.Sin(t * 9f + i)),
                    (t, a) => Mathf.Lerp(0.035f, i == 0 ? 0.05f : 0.025f, t), 18, 6, VINE, -1, VINE);
            }
            var meshes = SaveMeshes("ForestTree_A", kit, col);
            SavePrefab("ForestTree_A", meshes, m);
            return $"ForestTree_A {kit.TriangleCount}";
        }

        // ───────── B 段の木（モモタマナ風） ─────────

        static string Tiered()
        {
            var m = Mats("B", "#9C8C76", "#7A6A56", "#6E8448", "#8A9E58");
            var kit = new NatureMeshKit(m.Length);
            var col = new NatureMeshKit(1);
            const int seed = 1200;
            const float H = 5.2f, R0 = 0.2f;
            // a slim leaning trunk (0.4 m)
            Func<float, Vector3> path = u => new Vector3(0.35f * Mathf.Sin(u * 1.8f), H * u - 0.05f, 0.15f * Mathf.Sin(u * 2.6f + 0.5f));
            Func<float, float, float> radius = (u, a) => R0 * (1f - 0.45f * u) * PropKit.BarkPlates(u, a, H, seed, 10f, 0.06f) * (1f + 0.5f * Mathf.Exp(-H * u / 0.3f));
            Trunk(kit, path, radius, 24, 32, BARK, GROOVE, H, seed);
            Trunk(col, path, radius, 10, 14, 0, -1, H, seed);

            // level branches at uneven heights spiralling round the trunk, each holding one flat lobed sheet of leaves of its own size:
            // layered like the real tree, but nothing repeats (even tiers read as a cake or a clipped garden pine)
            var branches = new[] { (y: 1.9f, yaw: 20f, len: 2.0f, mr: 1.25f), (y: 2.5f, yaw: 160f, len: 1.6f, mr: 1.05f), (y: 3.0f, yaw: 265f, len: 1.5f, mr: 1.0f),
                                   (y: 3.7f, yaw: 85f, len: 1.15f, mr: 0.85f), (y: 4.3f, yaw: 205f, len: 0.85f, mr: 0.75f) };
            for (int i = 0; i < branches.Length; i++)
            {
                var br = branches[i];
                var d = PropKit.Dir(br.yaw);
                var a0 = path(br.y / H);
                var b = a0 + d * br.len + Vector3.up * PropKit.R(seed + 2, i, 0.1f, 0.35f);
                Limb(kit, a0, a0 + d * (br.len * 0.6f) - Vector3.up * PropKit.R(seed + 3, i, 0f, 0.15f), b, R0 * (0.6f - 0.07f * i), 0.04f, 7, BARK);
                CrownMass(kit, b + Vector3.up * 0.1f - d * (br.mr * 0.25f), new Vector3(br.mr * 1.15f, br.mr * 0.3f, br.mr), seed + 30 + i * 10, 26, 0.24f, float.NaN, 6f, 0.3f);
            }
            CrownMass(kit, path(1f) + Vector3.up * 0.15f, new Vector3(0.8f, 0.38f, 0.72f), seed + 200, 18, 0.22f, float.NaN, 6f, 0.3f);

            // the dry season: the same tree with dull red-brown leaves
            var dry = PropKit.Mats(Dir, "ForestTree_B",
                new PropKit.Spec("LeafDry", "#9A5E44", false, 0.1f).Mottle("#A87050", 0.2f, 1.2f),
                new PropKit.Spec("TipDry", "#A87050", false, 0.12f));
            foreach (var x in dry) PropKit.CairnPaint(x, "#3E2E24", 0.62f, 0.56f, 0.7f);
            var dryMats = (Material[])m.Clone();
            dryMats[LEAF] = dry[0];
            dryMats[TIP] = dry[1];
            var meshes = SaveMeshes("ForestTree_B", kit, col);
            SavePrefab("ForestTree_B", meshes, m);
            SavePrefab("ForestTree_B_Dry", meshes, dryMats);
            return $"ForestTree_B {kit.TriangleCount}";
        }

        // ───────── C 傘の木（アカシア風） ─────────

        static string Umbrella()
        {
            var m = Mats("C", "#9A8A72", "#76664F", "#8A9258", "#9EA466");
            var kit = new NatureMeshKit(m.Length);
            var col = new NatureMeshKit(1);
            const int seed = 1300;
            const float H1 = 1.6f, R0 = 0.31f;
            Func<float, Vector3> path = u => new Vector3(0.05f * Mathf.Sin(u * 2f), H1 * u - 0.05f, 0f);
            Func<float, float, float> radius = (u, a) => R0 * (1f - 0.15f * u) * PropKit.BarkPlates(u, a, H1, seed, 9f, 0.1f) * (1f + 0.45f * Mathf.Exp(-H1 * u / 0.25f));
            Trunk(kit, path, radius, 14, 36, BARK, GROOVE, H1, seed);
            Trunk(col, path, radius, 8, 14, 0, -1, H1, seed);

            // three limbs fork out of the short trunk and climb to just under the umbrella, then split into twigs that hold it up
            var top = path(1f);
            for (int i = 0; i < 3; i++)
            {
                var d = PropKit.Dir(i * 120f + 25f + PropKit.R(seed + 2, i, -15f, 15f));
                var a0 = top - Vector3.up * 0.25f;
                var b = a0 + d * PropKit.R(seed + 3, i, 1.3f, 1.7f) + Vector3.up * PropKit.R(seed + 4, i, 1.9f, 2.3f);
                var c = a0 + d * 0.3f + Vector3.up * 1.2f;
                Limb(kit, a0, c, b, 0.2f, 0.1f, 8, BARK);
                Limb(col, a0, c, b, 0.2f, 0.1f, 6, 0);
                for (int k = 0; k < 2; k++)
                    PropKit.Branch(kit, b, (PropKit.Yaw(d, k == 0 ? -35f : 35f) * 0.8f + Vector3.up).normalized, 0.9f, 0.07f, 1, seed + 10 + i * 2 + k, 6, BARK, 30f);
            }

            // the umbrella: one wide flat mass and six lobes round its rim (a scalloped outline), cut flat underneath
            var masses = new List<(Vector3 c, Vector3 r)> { (new Vector3(0f, 4.45f, 0f), new Vector3(2.3f, 0.42f, 2.2f)) };
            for (int i = 0; i < 6; i++)
            {
                var d = PropKit.Dir(i * 60f + PropKit.R(seed + 6, i, -10f, 10f));
                float rr = PropKit.R(seed + 7, i, 0.95f, 1.25f);
                masses.Add((d * PropKit.R(seed + 8, i, 1.9f, 2.2f) + Vector3.up * 4.35f, new Vector3(rr, 0.34f, rr)));
            }
            Crown(kit, masses, seed + 30, 30, 16, 0.14f, 4.12f, 5f, new Vector3(0f, 4.4f, 0f), new Vector3(3.2f, 0.7f, 3.2f), 0.5f);
            var meshes = SaveMeshes("ForestTree_C", kit, col);
            SavePrefab("ForestTree_C", meshes, m);
            return $"ForestTree_C {kit.TriangleCount}";
        }

        // ───────── D ねじれ木（オヒア風） ─────────

        static string Twisted()
        {
            var m = Mats("D", "#94897A", "#6E6458", "#8A9478", "#A0A88E");
            var kit = new NatureMeshKit(m.Length);
            var col = new NatureMeshKit(1);
            const int seed = 1400;
            const float H = 1.5f, R0 = 0.17f;
            // a short trunk, three ridges twisting round it, flaring where it grips the rock
            Func<float, Vector3> path = u => new Vector3(0.12f * Mathf.Sin(u * 2f), H * u - 0.04f, 0.06f * Mathf.Sin(u * 3f));
            Func<float, float, float> radius = (u, a) => R0 * (1f - 0.15f * u) * (1f + 0.16f * Mathf.Cos(3f * a - 5f * u)) * (1f + 0.7f * Mathf.Exp(-H * u / 0.22f));
            Trunk(kit, path, radius, 16, 24, BARK, GROOVE, H, seed, (u, a) => Mathf.Cos(3f * a - 5f * u) < -0.55f);
            Trunk(col, path, radius, 8, 10, 0, -1, H, seed);

            // roots crawling over the lava: they hump out of the flare and dive into the ground
            for (int i = 0; i < 5; i++)
            {
                var d = PropKit.Dir(i * 72f + PropKit.R(seed + 2, i, -20f, 20f));
                var side = PropKit.Yaw(d, 90f);
                float len = PropKit.R(seed + 3, i, 0.8f, 1.4f), r = PropKit.R(seed + 4, i, 0.06f, 0.09f);
                var at = path(0f) + d * (R0 * 0.8f) + Vector3.up * 0.12f;
                int ii = i;
                PropKit.Pipe(kit, t => at + d * (len * t) + Vector3.up * (0.08f * Mathf.Sin(Mathf.PI * t) - 0.16f * t * t) + side * (0.15f * Mathf.Sin(t * 5f + ii)),
                    (t, a) => r * Mathf.Lerp(1f, 0.15f, t), 8, 6, BARK, -1, BARK);
            }

            // the trunk splits at 1.5 m into three stems that zigzag up, kinking twice, then fork into twigs with a small mass on each end
            var top = path(1f);
            var tips = new List<Vector3>();
            for (int i = 0; i < 3; i++)
            {
                var d = PropKit.Dir(i * 120f + 30f + PropKit.R(seed + 5, i, -20f, 20f));
                var side = PropKit.Yaw(d, 90f);
                float hh = PropKit.R(seed + 6, i, 1.6f, 2.2f);
                var p0 = top - Vector3.up * 0.15f;
                var p1 = p0 + d * 0.35f + side * 0.25f + Vector3.up * (hh * 0.35f);
                var p2 = p1 + d * 0.3f - side * 0.4f + Vector3.up * (hh * 0.35f);
                var p3 = p2 + d * 0.35f + side * 0.2f + Vector3.up * (hh * 0.3f);
                var pts = new[] { p0, p1, p2, p3 };
                Func<float, Vector3> zig = t => { float x = Mathf.Clamp01(t) * 3f; int s = Mathf.Min(2, (int)x); return Vector3.Lerp(pts[s], pts[s + 1], x - s); };
                PropKit.Pipe(kit, zig, (t, a) => Mathf.Lerp(0.11f, 0.06f, t), 12, 7, BARK, -1, -1);
                PropKit.Branch(kit, p3, ((p3 - p2).normalized + Vector3.up * 0.3f).normalized, PropKit.R(seed + 9, i, 0.5f, 0.7f), 0.06f, 1, seed + 7 + i, 5, BARK, 40f, 0.05f, (p, dir) => tips.Add(p));
            }
            var masses = new List<(Vector3 c, Vector3 r)>();
            Vector3 mid = Vector3.zero;
            for (int i = 0; i < tips.Count; i++)
            {
                float rr = PropKit.R(seed + 8, i, 0.38f, 0.52f);
                masses.Add((tips[i] + Vector3.up * (rr * 0.3f), new Vector3(rr, rr * 0.7f, rr)));
                mid += masses[i].c / tips.Count;
            }
            Crown(kit, masses, seed + 30, 18, 12, 0.24f, float.NaN, 5f, mid, new Vector3(1.8f, 0.9f, 1.8f), 0.3f);

            // small red flower dots on the lit tops of the masses (they sway with their mass)
            for (int k = 0; k < masses.Count; k++)
            {
                var (c, r) = masses[k];
                int ks = seed + 30 + k * 7;
                for (int b = 0; b < 2; b++)
                {
                    float az = PropKit.R(ks + 50, b, 0f, Mathf.PI * 2f), el = PropKit.R(ks + 51, b, 0.4f, 1.2f);
                    var n = new Vector3(Mathf.Cos(el) * Mathf.Cos(az), Mathf.Sin(el), Mathf.Cos(el) * Mathf.Sin(az));
                    int from = kit.VertexCount;
                    kit.Ball(c + Vector3.Scale(n, r), Vector3.one * 0.055f, 4, FLOWER);
                    kit.Paint(from, (p, uv) => new Color(Mathf.Clamp01(p.y / 5f) * 0.5f, 0.3f, PropKit.H(ks, 3), 1f));
                }
            }
            var meshes = SaveMeshes("ForestTree_D", kit, col);
            SavePrefab("ForestTree_D", meshes, m);
            return $"ForestTree_D {kit.TriangleCount}";
        }
    }
}
