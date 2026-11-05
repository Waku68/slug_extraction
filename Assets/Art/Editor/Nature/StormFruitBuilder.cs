using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 嵐の実（Nov 3 下書き、わくさんのペルソナが監督）。雨の森の大ガジュマルの下だけに、大雨が島時間 3 時間以上続いたあと 1〜2 個落ちる大物。
    /// 拾って抱えて帰る（バッグが大きく膨らむ）。ピッケルで 4 つに割れる（殻は拠点の「殻の箱」の材料）。
    /// 現実の元はホウガンノキ（砲丸の木。幹から直接、丸い実がぶら下がる）。メニュー Slug/Nature/Build Storm Fruit（Assets/Art/Nature/StormFruit）。
    /// ナメクジの目の高さ 0.47 m は人の約 1/3.6。
    /// - A 丸ごと：直径 45 cm・高さ 41 cm（ナメクジの目の高さくらい。人なら 1.6 m の玉）。硬い殻は亀の甲のような大きな板（5 段 × 10 枚、1 枚 14 cm）が
    ///   ふくらんでカクカク、板の間は暗い細い溝。縦に 4 本の割れ目は暗く深い溝（ここをピッケルで叩く）、てっぺんに折れた太い柄。
    ///   明るい色は割った中だけ（割れ目を明るい帯にすると、バスケットボールやかぼちゃに見えた）
    /// - B 割った 1/4：殻の厚み 7 cm（殻の箱の材料なので殻が主役）、切り口はくすんだ黄の果肉に暗い種が並ぶ。原点は底の真ん中（割れた位置のまま）
    /// 色：殻 #7A5E46・板の間の溝 #4E3A2A、割れ目 #2E241C、殻の切り口 #A07A52、柄 #5E5040、果肉 #CDB27A、種 #4E3A2A。
    /// 殻・柄・種はかじれない（カクカク）、果肉はなめらか。当たり判定は凸のメッシュ（拾う・叩く）。
    /// </summary>
    public static class StormFruitBuilder
    {
        const string Dir = "Assets/Art/Nature/StormFruit";
        const int SHELL = 0, LIP = 1, SEAM = 2, STALK = 3, FLESH = 4, SEED = 5, SHELLCUT = 6;
        const float R = 0.225f, Squash = 0.91f, Shell = 0.07f;
        const int Rows = 5, Scales = 10, Sub = 6;   // plates: rows × around; Sub = mesh steps across one plate
        static Material[] _m;

        [MenuItem("Slug/Nature/Build Storm Fruit")]
        public static void Build()
        {
            _m = PropKit.Mats(Dir, "StormFruit",
                new PropKit.Spec("Shell", "#7A5E46", true).Mottle("#6A5040", 0.4f, 8f).Dent(0.2f, 30f),
                new PropKit.Spec("Lip", "#4E3A2A", true),
                new PropKit.Spec("Seam", "#2E241C", true),
                new PropKit.Spec("Stalk", "#5E5040", true).Dent(0.3f, 30f),
                new PropKit.Spec("Flesh", "#CDB27A", false).Mottle("#BCA06A", 0.3f, 14f),
                new PropKit.Spec("Seed", "#4E3A2A", true),
                new PropKit.Spec("ShellCut", "#A07A52", true).Mottle("#8E6A46", 0.4f, 12f));
            var log = new System.Text.StringBuilder("Storm fruit built:");
            log.Append(' ').Append(Whole()).Append(' ').Append(Quarter());
            AssetDatabase.SaveAssets();
            Debug.Log(log);
        }

        /// <summary>割れ目（縦 4 本、a = 0, 90, 180, 270 度）からの近さ 0〜1。</summary>
        static float Seam(float a)
        {
            float d = Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 0f)) % 90f;
            d = Mathf.Min(d, 90f - d) * Mathf.Deg2Rad;
            return Mathf.Exp(-Mathf.Pow(d / 0.06f, 2f));
        }

        /// <summary>
        /// 殻の表の点：t は 0 底〜1 てっぺん、a は回り。亀の甲のような大きな板（段ごとに半分ずらす）が 1 枚ずつふくらみ、板の境は谷になる。
        /// 割れ目は深く凹む。inset だけ内へ（殻の内側）。groove は板の境の上か（暗く塗る。板の境はメッシュの 1 段の真ん中に来るようにずらしてある）。
        /// </summary>
        static Vector3 ShellPoint(float t, float a, float inset, out bool groove)
        {
            float phi = Mathf.PI * t, pole = Mathf.Sin(phi);
            float row = t * Rows + 0.5f / Sub, u = row - Mathf.Floor(row);
            int ri = Mathf.Min(Rows - 1, (int)row);
            float col = a / (Mathf.PI * 2f) * Scales + (ri % 2) * 0.5f + 0.5f / Sub, v = col - Mathf.Floor(col);
            float dome = 0.07f * Mathf.Sin(Mathf.PI * u) * Mathf.Sin(Mathf.PI * v) * pole;   // plates fade out at the poles
            groove = pole > 0.3f && (Mathf.Min(u, 1f - u) < 0.5f / Sub || Mathf.Min(v, 1f - v) < 0.5f / Sub);
            float r = R * (1f + dome) - 0.035f * Seam(a) * Mathf.Sqrt(pole) - inset;   // the four seams meet at the poles
            var n = new Vector3(Mathf.Cos(a) * Mathf.Sin(phi), -Mathf.Cos(phi) * Squash, Mathf.Sin(a) * Mathf.Sin(phi));
            return new Vector3(0f, R * Squash, 0f) + n * r;
        }

        static int ShellSub(float t, float a)
        {
            ShellPoint(t, a, 0f, out bool groove);
            return Seam(a) > 0.45f ? SEAM : groove ? LIP : SHELL;
        }

        /// <summary>殻の面を a0〜a1 の範囲で張る（outward が false なら内側を向く面）。</summary>
        static void ShellPatch(NatureMeshKit kit, float a0, float a1, int rows, int cols, float inset, bool outward)
        {
            int start = kit.VertexCount;
            for (int r = 0; r <= rows; r++)
                for (int c = 0; c <= cols; c++)
                {
                    float t = r / (float)rows, a = Mathf.Lerp(a0, a1, c / (float)cols);
                    kit.Vert(ShellPoint(t, a, inset, out _), new Vector2(c / (float)cols, t));
                }
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    int i0 = start + r * (cols + 1) + c, i1 = i0 + cols + 1;
                    float tm = (r + 0.5f) / rows, am = Mathf.Lerp(a0, a1, (c + 0.5f) / cols);
                    int sub = inset > 0f ? SHELL : ShellSub(tm, am);
                    if (outward) kit.Quad(sub, i0, i1, i1 + 1, i0 + 1);
                    else kit.Quad(sub, i0, i0 + 1, i1 + 1, i1);
                }
        }

        /// <summary>折れた太い柄（てっぺん）。</summary>
        static void Stalk(NatureMeshKit kit, Vector3 top)
        {
            PropKit.Pipe(kit, t => top + Vector3.up * (0.09f * t) + Vector3.right * (0.02f * t * t), (t, a) => Mathf.Lerp(0.04f, 0.03f, t) * (1f + 0.12f * Mathf.Sin(a * 5f)), 4, 10, STALK, -1, STALK, -0.01f);
        }

        // A: the whole fruit
        static string Whole()
        {
            var kit = new NatureMeshKit(_m.Length);
            ShellPatch(kit, 0f, Mathf.PI * 2f, Rows * Sub, Scales * Sub, 0f, true);
            Stalk(kit, new Vector3(0f, 2f * R * Squash - 0.02f, 0f));
            return PropKit.Save(Dir, "StormFruit_A", kit, _m, 2);
        }

        // B: one quarter, split along two seams: the shell outside, its thickness at the cut, pale flesh with a row of dark seeds
        static string Quarter()
        {
            var kit = new NatureMeshKit(_m.Length);
            float a0 = 0.02f, a1 = Mathf.PI * 0.5f - 0.02f;
            int rows = Rows * Sub, cols = Scales * Sub / 4;
            ShellPatch(kit, a0, a1, rows, cols, 0f, true);
            foreach (float a in new[] { a0, a1 })
            {
                // the cut face at this angle: flesh from the axis out to the inner shell, then the shell's own thickness
                var expected = a == a0 ? -new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)) : new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                int axisStart = kit.VertexCount;
                for (int r = 0; r <= rows; r++)
                {
                    float t = r / (float)rows;
                    var outer = ShellPoint(t, a, 0f, out _);
                    var inner = ShellPoint(t, a, Shell, out _);
                    kit.Vert(new Vector3(0f, inner.y, 0f), new Vector2(0f, t));
                    kit.Vert(inner, new Vector2(0.5f, t));
                    kit.Vert(inner, new Vector2(0.5f, t));
                    kit.Vert(outer, new Vector2(1f, t));
                }
                for (int r = 0; r < rows; r++)
                {
                    int b = axisStart + r * 4, n = b + 4;
                    Facing(kit, FLESH, b, n, n + 1, b + 1, expected);
                    Facing(kit, SHELLCUT, b + 2, n + 2, n + 3, b + 3, expected);
                }
                // seeds: a row of dark flat-sided seeds set into the flesh near the middle
                for (int k = 0; k < 5; k++)
                {
                    float t = Mathf.Lerp(0.25f, 0.75f, k / 4f);
                    var p = ShellPoint(t, a, Shell, out _);
                    // flat seeds lying in the cut face (thin along the face's normal), so they don't stick out sideways
                    var at = new Vector3(p.x * 0.45f, p.y, p.z * 0.45f) - expected * 0.001f;
                    var size = new Vector3(0.022f, 0.016f, 0.022f);
                    if (a == a0) size.z = 0.007f; else size.x = 0.007f;
                    kit.Ball(at, size, 4, SEED);
                }
            }
            return PropKit.Save(Dir, "StormFruit_B", kit, _m, 2);
        }

        /// <summary>四角を張る。面の向きが expected と逆なら裏返す。</summary>
        static void Facing(NatureMeshKit kit, int sub, int a, int b, int c, int d, Vector3 expected)
        {
            var n = Vector3.Cross(kit.Vertex(b) - kit.Vertex(a), kit.Vertex(c) - kit.Vertex(a));
            if (n.sqrMagnitude < 1e-12f) n = Vector3.Cross(kit.Vertex(c) - kit.Vertex(a), kit.Vertex(d) - kit.Vertex(a));
            if (Vector3.Dot(n, expected) >= 0f) kit.Quad(sub, a, b, c, d);
            else kit.Quad(sub, d, c, b, a);
        }
    }
}
