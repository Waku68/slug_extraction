using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 山に手で置く部品（Nov 4 わくさん「全部なし。おれが手で修正できるようにしてほしい」→ 前の山に戻し、地形のブラシと部品で直す）。
    /// メニュー Slug/Nature/Build Mountain Parts で作り直す（Assets/Art/Nature/MountainParts）。見本は Assets/Scenes/Preview/MountainPartsLook.unity。
    /// どれも原点は「歩く面の手前の端の真ん中」、+z が進む向き（上り）、x が横。置いて、回して、地面に少しめり込ませて使う。
    /// 歩く面の当たり判定は見た目と別のなめらかな板（ナメクジは 50° まで歩ける、段差は 0.15 m まで）。地面が板より上に出ていると引っかかるので、
    /// そこは地形のブラシで下げる。
    /// - 坂の道：長さ 6 m、歩ける幅 2.6 m（人なら 9 m）、20° で 2.2 m 上がる。平らな踊り場は 3 m × 3 m。3 種：
    ///   Rock 岩の棚（下に岩の根）、Stone 石の段（平たい石の段と石積みの壁）、Board 風化した板の桟道（柱つき）
    ///   続けるときは、前の部品の終わり（z=6, y=2.18）に次の部品の原点を合わせる
    /// - よじ登る段 Climb_3m / 6m / 10m：石を 2 列で互い違いに、0.38 m ずつ上がって 0.3 m ずつ奥（+z）へ。ナメクジは 0.45 m まで
    ///   よじ登れるので、横の列へよじ登りながら上がる。壁の根元に、+z を壁に向けて置く。いちばん上の石が上の棚と同じ高さになるように
    /// - 壁の岩は前に作った Assets/Art/Nature/MountainWalls の Wall_Block / Strata / Column（幅 6 m・高さ 10 m、伸ばして使う）
    /// </summary>
    public static class MountainPartsBuilder
    {
        public const string Dir = "Assets/Art/Nature/MountainParts";
        const float W = 2.6f, Len = 6f, Grade = 0.364f, Land = 3f;
        const float Rise = 0.38f, StepIn = 0.3f, BlockW = 0.96f, ColGap = 0.49f, BlockDepth = 2f;

        [MenuItem("Slug/Nature/Build Mountain Parts")]
        public static string Build()
        {
            if (!AssetDatabase.LoadAssetAtPath<Material>(MountainWallBuilder.MatPath("WoodOld"))) MountainWallBuilder.Build();
            var rock = AssetDatabase.LoadAssetAtPath<Material>(MountainWallBuilder.MatPath("Block"));
            var stone = AssetDatabase.LoadAssetAtPath<Material>(MountainWallBuilder.MatPath("PathStone"));
            var wood = AssetDatabase.LoadAssetAtPath<Material>(MountainWallBuilder.MatPath("Wood"));
            var woodOld = AssetDatabase.LoadAssetAtPath<Material>(MountainWallBuilder.MatPath("WoodOld"));
            System.IO.Directory.CreateDirectory(Dir);
            var sb = new StringBuilder();
            foreach (bool flat in new[] { false, true })
            {
                float len = flat ? Land : Len, g = flat ? 0f : Grade;
                string tail = flat ? "Flat" : "Ramp";
                var r = new SimpleMesh(1); RockShelf(r, len, g, 11 + (flat ? 1 : 0));
                sb.AppendLine(Save($"Way_Rock_{tail}", r.ToMesh($"Way_Rock_{tail}_Mesh"), new[] { rock }, len, g));
                var s = new NatureMeshKit(1); StoneStair(s, len, g, 21 + (flat ? 1 : 0));
                sb.AppendLine(Save($"Way_Stone_{tail}", s.ToMesh($"Way_Stone_{tail}_Mesh"), new[] { stone }, len, g));
                var b = new SimpleMesh(2); Boards(b, len, g, 31 + (flat ? 1 : 0));
                sb.AppendLine(Save($"Way_Board_{tail}", b.ToMesh($"Way_Board_{tail}_Mesh"), new[] { wood, woodOld }, len, g));
            }
            foreach (float h in new[] { 3f, 6f, 10f }) sb.AppendLine(Climb(h, rock));
            AssetDatabase.SaveAssets();
            return sb.ToString();
        }

        static float H(int seed, int i) => NatureAssets.Hash(seed, i);
        static Vector3 P(float x, float z, float g, float dy = 0f) => new Vector3(x, z * g + dy, z);

        // ------------------------------------------------------------------ ways
        static string Save(string name, Mesh mesh, Material[] mats, float len, float g)
        {
            mesh = NatureAssets.SaveMesh(mesh, $"{Dir}/{name}_Mesh.asset");
            var col = NatureAssets.SaveMesh(Slab(len, g, $"{name}_Collider"), $"{Dir}/{name}_Collider.asset");
            NatureAssets.SavePrefab($"{Dir}/{name}.prefab", mesh, mats, go =>
            {
                var c = go.AddComponent<MeshCollider>();
                c.sharedMesh = col;
            });
            return $"{name} {mesh.triangles.Length / 3}";
        }

        // the walking surface: a smooth slab 0.35 m thick, its top the way's top
        static Mesh Slab(float len, float g, string name)
        {
            var m = new SimpleMesh(1);
            float x0 = -0.5f * W, x1 = 0.5f * W;
            Vector3 a = P(x0, 0f, g), b = P(x1, 0f, g), c = P(x1, len, g), d = P(x0, len, g), dn = Vector3.down * 0.35f;
            m.Quad(0, a, b, c, d, Vector3.up);
            m.Quad(0, a + dn, b + dn, c + dn, d + dn, Vector3.down);
            m.Quad(0, a, b, b + dn, a + dn, Vector3.back);
            m.Quad(0, d, c, c + dn, d + dn, Vector3.forward);
            m.Quad(0, a, d, d + dn, a + dn, Vector3.left);
            m.Quad(0, b, c, c + dn, b + dn, Vector3.right);
            return m.ToMesh(name);
        }

        // a rock shelf: a flat top, rough lips on both sides, craggy roots below (bury them in the ground or the wall)
        static void RockShelf(SimpleMesh m, float len, float g, int seed)
        {
            int n = Mathf.CeilToInt(len / 0.5f);
            foreach (float side in new[] { 1f, -1f })
            {
                int[] pa = null;
                for (int i = 0; i <= n; i++)
                {
                    float z = len * i / n;
                    float j1 = 0.2f * (Mathf.PerlinNoise(z * 0.5f + seed, side * 3f) - 0.4f), j2 = Mathf.PerlinNoise(z * 0.5f + seed + 5f, side);
                    var q = new int[6];
                    q[0] = m.V(P(0f, z, g)); q[1] = m.V(P(side * (0.5f * W + j1), z, g));
                    q[2] = m.V(P(side * (0.5f * W + 0.5f * j1 + 0.08f), z, g, -0.3f - 0.25f * j2));
                    for (int k = 3; k < 6; k++)
                    {
                        float t = (k - 2) / 3f;
                        float crag = 0.3f * (Mathf.PerlinNoise(z * 0.55f + seed * 2f, k * 1.37f + side) - 0.5f);
                        q[k] = m.V(P(side * Mathf.Lerp(0.5f * W + 0.1f, 0.6f, t) + side * crag, z, g, Mathf.Lerp(-0.4f, -1.8f, t)));
                    }
                    var o = new Vector3(side, 0f, 0f);
                    if (pa != null)
                    {
                        m.Quad(0, pa[0], pa[1], q[1], q[0], Vector3.up);
                        for (int k = 1; k < 5; k++) m.Quad(0, pa[k], pa[k + 1], q[k + 1], q[k], o + Vector3.down * (k == 1 ? 0f : 0.3f));
                    }
                    else for (int k = 1; k < 5; k++) m.Tri(0, q[0], q[k], q[k + 1], Vector3.back);
                    pa = q;
                }
                for (int k = 1; k < 5; k++) m.Tri(0, pa[0], pa[k], pa[k + 1], Vector3.forward);
            }
        }

        // laid stones: each tread two or three flat stones of different widths; a dry-stone wall down both sides
        static void StoneStair(NatureMeshKit kit, float len, float g, int seed)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(len / 0.42f));
            float run = len / n;
            for (int j = 0; j < n; j++)
            {
                float zm = (j + 0.5f) * run, yt = zm * g + 0.02f;
                int stones = H(seed, j) < 0.5f ? 2 : 3;
                float x0 = -0.5f * W - 0.03f;
                for (int s = 0; s < stones; s++)
                {
                    float share = (0.7f + 0.6f * H(seed, j * 10 + s + 1)) / (stones - s);
                    float x1 = s == stones - 1 ? 0.5f * W + 0.03f : Mathf.Lerp(x0, 0.5f * W, Mathf.Clamp(share, 0.25f, 0.75f));
                    float top = yt + 0.03f * (H(seed, j * 10 + s + 5) - 0.5f);
                    var rot = Quaternion.Euler(0f, 6f * (H(seed, j * 10 + s + 7) - 0.5f), 0f);
                    float topY = top;
                    PropKit.Lump(kit, new Vector3(0.5f * (x0 + x1), top - 0.24f, zm + 0.05f * (H(seed, j * 10 + s + 9) - 0.5f)), new Vector3(0.5f * (x1 - x0) + 0.03f, 0.26f, 0.5f * run + 0.04f),
                        4, 5, 0, seed * 1000 + j * 7 + s, 0.09f, 1.8f, float.NaN, rot, 0f, 1f, null, p => { if (p.y > topY) p.y = topY; return p; }, 4.5f);
                    x0 = x1;
                }
            }
            foreach (float side in new[] { 1f, -1f })
            {
                float depth = 0.45f;
                for (int row = 0; row < 4; row++)
                {
                    float rowH = 0.3f + 0.25f * H(seed + 50, row + (side > 0 ? 0 : 20));
                    float z = -0.6f * H(seed + 51, row);
                    for (int k = 0; z < len; k++)
                    {
                        float l = 0.4f + 0.6f * H(seed + 52, row * 100 + k + (side > 0 ? 0 : 5000));
                        float zc = Mathf.Clamp(z + 0.5f * l, 0.1f, len - 0.1f);
                        float h = rowH * (0.75f + 0.3f * H(seed + 53, row * 100 + k));
                        var cpos = new Vector3(side * (0.5f * W - 0.2f + 0.1f * (H(seed + 54, row * 100 + k) - 0.5f)), zc * g + 0.02f - depth - 0.5f * rowH, zc);
                        var rot = Quaternion.Euler(0f, 8f * (H(seed + 55, row * 100 + k) - 0.5f), 4f * (H(seed + 56, row * 100 + k) - 0.5f));
                        PropKit.Lump(kit, cpos, new Vector3(0.18f + 0.1f * H(seed + 57, row * 100 + k), 0.5f * h + 0.02f, 0.5f * l + 0.02f), 4, 5, 0, seed * 2000 + row * 97 + k * 13, 0.1f, 1.8f, float.NaN,
                            rot, 0f, 1f, null, null, 5.5f);
                        z += l;
                    }
                    depth += rowH;
                }
            }
        }

        // weathered boards: planks across (7 in 10 gone silver-grey), uneven widths and gaps, some turned, tilted, broken short
        // at one end, now and then one missing; two beams; leaning posts 2 m down (sink them into the ground)
        static void Boards(SimpleMesh m, float len, float g, int seed)
        {
            int idx = 0; bool lastMissing = false;
            for (float z = 0f; z < len - 0.1f; idx++)
            {
                float w = 0.22f + 0.1f * H(seed, idx * 10 + 1), gap = 0.03f + 0.06f * H(seed, idx * 10 + 2);
                float zb = Mathf.Min(z + w, len);
                bool missing = !lastMissing && H(seed, idx * 10 + 3) < 0.07f && z > 1f && zb < len - 1f;
                lastMissing = missing;
                if (!missing)
                {
                    int sub = H(seed, idx * 10 + 4) < 0.7f ? 1 : 0;
                    float y = (0.5f * (z + zb)) * g + 0.02f * (H(seed, idx * 10 + 5) - 0.5f), tilt = 0.035f * (H(seed, idx * 10 + 6) - 0.5f);
                    float cut = H(seed, idx * 10 + 7) < 0.25f ? 0.25f + 0.8f * H(seed, idx * 10 + 8) : 0f;
                    float sideCut = H(seed, idx * 10 + 9) < 0.5f ? 1f : -1f;
                    float turn = 0.07f * (H(seed, idx * 10 + 10) - 0.5f);
                    float xa = -0.5f * W, xb = 0.5f * W;
                    if (sideCut > 0f) xb -= cut; else xa += cut;
                    Vector3 a = new Vector3(xa, y + tilt, z - turn), b = new Vector3(xa, y + tilt, zb - turn), c = new Vector3(xb, y - tilt, zb + turn), d = new Vector3(xb, y - tilt, z + turn);
                    if (cut > 0f)
                    {
                        // a jagged broken end
                        float jag = 0.12f * (H(seed, idx * 10 + 11) - 0.3f) * sideCut;
                        Vector3 mid = sideCut > 0f ? Vector3.Lerp(c, d, 0.5f) + Vector3.right * jag : Vector3.Lerp(a, b, 0.5f) - Vector3.right * jag;
                        if (sideCut > 0f) { m.Tri(sub, a, b, c, Vector3.up); m.Tri(sub, a, c, mid, Vector3.up); m.Tri(sub, a, mid, d, Vector3.up); }
                        else { m.Tri(sub, d, c, b, Vector3.up); m.Tri(sub, d, b, mid, Vector3.up); m.Tri(sub, d, mid, a, Vector3.up); }
                    }
                    else m.Quad(sub, a, b, c, d, Vector3.up);
                    var dn = Vector3.down * 0.07f;
                    m.Quad(sub, a, d, d + dn, a + dn, Vector3.back);
                    m.Quad(sub, b, c, c + dn, b + dn, Vector3.forward);
                    m.Quad(sub, a, b, b + dn, a + dn, Vector3.left);
                    m.Quad(sub, d, c, c + dn, d + dn, Vector3.right);
                }
                z = zb + gap;
            }
            foreach (float x in new[] { -0.9f, 0.9f })
            {
                Vector3 a = new Vector3(x - 0.08f, -0.07f, 0f), b = new Vector3(x + 0.08f, -0.07f, 0f);
                var t = new Vector3(0f, len * g, len);
                var dn = Vector3.down * 0.2f;
                m.Quad(1, a + dn, b + dn, b + dn + t, a + dn + t, Vector3.down);
                m.Quad(1, a, a + dn, a + dn + t, a + t, Vector3.left);
                m.Quad(1, b, b + dn, b + dn + t, b + t, Vector3.right);
            }
            int pi = 0;
            for (float z = 0.4f; z <= len; z += 2.4f, pi++)
                foreach (float x in new[] { -0.9f, 0.9f })
                {
                    if (pi > 0 && H(seed + 7, pi * 2 + (x > 0 ? 1 : 0)) < 0.15f) continue;
                    var lean = new Vector3(0.06f * (H(seed + 8, pi) - 0.5f), 0f, 0.18f * (H(seed + 9, pi * 2 + (x > 0 ? 1 : 0)) - 0.5f));
                    float top = z * g - 0.07f, bottom = top - 2f;
                    var shaft = Vector3.up * (top - bottom) + lean;
                    var axis = shaft.normalized; var sideV = Vector3.Cross(axis, Vector3.forward).normalized; var fwd = Vector3.Cross(sideV, axis);
                    m.Box(H(seed + 10, pi) < 0.6f ? 1 : 0, new Vector3(x, bottom, z), sideV, axis, fwd, new Vector3(-0.08f, 0f, -0.08f), new Vector3(0.08f, shaft.magnitude, 0.08f));
                }
        }

        // ------------------------------------------------------------------ climbing stairs
        static string Climb(float height, Material mat)
        {
            int n = Mathf.CeilToInt(height / Rise);
            float rise = height / n;
            var kit = new NatureMeshKit(1);
            var boxes = new List<(Vector3 c, Vector3 s)>();
            for (int i = 0; i < n; i++)
            {
                float top = rise * (i + 1), front = StepIn * i, x = (i % 2 == 0 ? -ColGap : ColGap);
                float bottom = Mathf.Min(top - 1.2f, -0.3f);
                if (i >= 2) bottom = Mathf.Max(bottom, rise * (i - 1) - 0.4f);
                if (i < 2) bottom = -0.3f;
                var c = new Vector3(x, 0.5f * (top + bottom), front + 0.5f * BlockDepth);
                var size = new Vector3(BlockW, top - bottom, BlockDepth);
                boxes.Add((c, size));
                float topY = top;
                PropKit.Lump(kit, c, 0.52f * size, 6, 10, 0, 700 + i * 13 + Mathf.RoundToInt(height), 0.07f, 1.6f, float.NaN, null, 0f, 1f,
                    nn => PropKit.Strata(nn, i, 2f, 0.03f) + PropKit.Chips(nn, i + 2, 0.1f), p => { if (p.y > topY) p.y = topY; return p; }, 6f);
            }
            string name = $"Climb_{height:0}m";
            var mesh = NatureAssets.SaveMesh(kit.ToMesh($"{name}_Mesh"), $"{Dir}/{name}_Mesh.asset");
            NatureAssets.SavePrefab($"{Dir}/{name}.prefab", mesh, new[] { mat }, go =>
            {
                foreach (var (c, s) in boxes) { var bc = go.AddComponent<BoxCollider>(); bc.center = c; bc.size = s; }
            });
            return $"{name} blocks {n} rise {rise:0.00} {kit.TriangleCount}";
        }

        // ------------------------------------------------------------------ a scene to look at them
        [MenuItem("Slug/Nature/Mountain Parts Look")]
        public static string Look()
        {
            Build();
            const string path = "Assets/Scenes/Preview/MountainPartsLook.unity";
            var prev = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground"; ground.transform.localScale = new Vector3(8f, 1f, 4f);
            var cam = new GameObject("Camera").AddComponent<Camera>();
            cam.transform.SetPositionAndRotation(new Vector3(14f, 6f, -14f), Quaternion.Euler(18f, -30f, 0f));
            float x = -24f;
            foreach (var name in new[] { "Way_Rock_Ramp", "Way_Rock_Flat", "Way_Stone_Ramp", "Way_Stone_Flat", "Way_Board_Ramp", "Way_Board_Flat", "Climb_3m", "Climb_6m", "Climb_10m" })
            {
                var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/{name}.prefab");
                if (!src) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(src, scene);
                go.transform.position = new Vector3(x, name.StartsWith("Way_Board") ? 0.4f : name.StartsWith("Way") ? 0.3f : 0f, 0f);
                x += name.StartsWith("Climb") ? 4f : 5f;
            }
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, path);
            if (prev.IsValid()) UnityEngine.SceneManagement.SceneManager.SetActiveScene(prev);
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
            return path;
        }
    }
}
