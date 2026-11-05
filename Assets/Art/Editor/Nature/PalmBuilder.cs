using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 立っているヤシ（Nov 3 作り直し：わくさん「ずんぐりじゃない方のヤシがいい」→ Oct 20 の 3 案の A 曲がり・B まっすぐ高い。形は PalmShape）。
    /// メニュー Slug/Nature/Build Palm で Palm_A・Palm_B のメッシュ・マテリアル・プレハブを作り直す（Assets/Art/Nature/Palm）。
    /// 前のずんぐり（Palm_Smooth・Palm_Facet）はもう作らない（ファイルは残っている。消すのはわくさん）。
    ///
    /// 幹は硬いのでカクカク、葉・鞘はなめらかでケルン風（README 8章：葉を大きな色の面として、樹冠ぜんたいを 1 つの塊の丸みで光らせる）。
    /// 葉 10 枚（A）・9 枚（B）と、枯れて垂れた葉 2 枚、本物の大きさの実 9 個。
    /// 色：幹 #B0A28A、節の溝 #7A6C5A、葉 #788A50、枯れ葉 #B89D70、鞘 #8A8A55、実 #6A5A3A。
    /// 当たり判定は幹のカプセル。
    /// </summary>
    public static class PalmBuilder
    {
        const string Dir = "Assets/Art/Nature/Palm";
        const int TRUNK = 0, RING = 1, LEAF = 2, DEAD = 3, SHEATH = 4, NUT = 5;
        static Material[] _m;

        [MenuItem("Slug/Nature/Build Palm")]
        public static void Build()
        {
            _m = PropKit.Mats(Dir, "Palm",
                new PropKit.Spec("Trunk", "#B0A28A", true).Mottle("#9C8E78", 0.3f, 3f),
                new PropKit.Spec("TrunkRing", "#7A6C5A", true),
                new PropKit.Spec("Leaf", "#788A50", false, 0.06f, true).Mottle("#869856", 0.25f, 1.5f),
                new PropKit.Spec("DeadLeaf", "#B89D70", false, 0.03f, true),
                new PropKit.Spec("Crown", "#8A8A55", false),
                new PropKit.Spec("Nut", "#6A5A3A", true));
            PropKit.CairnPaint(_m[LEAF]);
            PropKit.CairnPaint(_m[SHEATH]);
            PropKit.CairnPaint(_m[DEAD], "#4A3E2A");
            string a = Make("Palm_A", PalmShape.A), b = Make("Palm_B", PalmShape.B);
            AssetDatabase.SaveAssets();
            Debug.Log($"Palm built: {a} / {b}");
        }

        static string Make(string name, PalmShape.Palm p)
        {
            var kit = new NatureMeshKit(_m.Length);
            PalmShape.Trunk(kit, p, -0.04f, 1f, TRUNK, RING, -1, -1, 0f, p.seed);
            Vector3 crown = PalmShape.Crown(kit, p, TRUNK, SHEATH);

            // the fronds: out and up, the tips drooping; the whole crown lights as one mass (Cairn)
            int frondFrom = kit.VertexCount;
            float y0 = PropKit.R(p.seed, 0, 0f, 360f);
            for (int i = 0; i < p.fronds; i++)
            {
                float yaw = y0 + 360f * i / p.fronds + PropKit.R(p.seed, 10 + i, -9f, 9f);
                float pitch = PropKit.R(p.seed, 30 + i, p.pitch0, p.pitch1);
                float len = p.flen * PropKit.R(p.seed, 50 + i, 0.88f, 1.08f), droop = p.droop * PropKit.R(p.seed, 70 + i, 0.85f, 1.15f);
                PalmShape.Frond(kit, PalmShape.FrondPath(crown, yaw, pitch, len, droop), p.fwid, 0.45f, LEAF);
            }
            kit.Mass(frondFrom, crown + Vector3.down * (p.flen * 0.15f), new Vector3(p.flen * 0.95f, p.flen * 0.55f, p.flen * 0.95f), 0.55f);
            // two old fronds dried and hanging under the crown
            for (int i = 0; i < 2; i++)
            {
                float yaw = y0 + 180f * i + 90f + 23f;
                PalmShape.Frond(kit, PalmShape.FrondPath(crown + Vector3.down * 0.25f, yaw, -55f, p.flen * 0.7f, 0.12f), p.fwid * 0.55f, 0.6f, DEAD, 8);
            }
            PalmShape.Nuts(kit, p, crown, y0 * Mathf.Deg2Rad, NUT);

            var mesh = NatureAssets.SaveMesh(kit.ToMesh($"{name}_Mesh"), $"{Dir}/{name}_Mesh.asset");
            NatureAssets.SavePrefab($"{Dir}/{name}.prefab", mesh, _m, go =>
            {
                var col = go.AddComponent<CapsuleCollider>();
                col.center = PalmShape.Center(p, 0.5f); col.height = p.h; col.radius = p.r0 * 1.2f;
            });
            return $"{name} {kit.TriangleCount} tris";
        }
    }
}
