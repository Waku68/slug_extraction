using System.Collections.Generic;
using System.Text;
using SlugMove;
using SlugWorld;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SlugExtract
{
    /// <summary>
    /// IslandTest_800, part 5: the waterfall under the pool on the mountain's south bench (Oct 10), as a model the
    /// terrain cannot make. The south river drops 12 m over an overhanging lip into a round plunge pool (an amphitheatre
    /// of wet rock, 15 m across, open downstream to a short gorge the falls have cut back). Behind the curtain the
    /// water has hollowed an alcove: a ledge at the back of the pool, reached by a ramp of fallen rock along the west
    /// wall, a rock roof over it, the curtain 2 m in front (滝の裏の岩棚, spec ch. 3). The terrain is carved out under
    /// the model (the pool, the ledge, the ramp); the model is the wall round them with its cap back onto the ground,
    /// sampled from the terrain so it fits wherever the river puts the fall. Water: a curtain that leaves the lip and
    /// falls in an arc, white strands over it, foam where it lands, mist, the pool.
    /// </summary>
    public static partial class IslandTest800Builder
    {
        // ---- 数字（m）
        const float FallHeight = 12f;        // 滝の高さ
        const float FallLipDist = 16f;       // たまり池の真ん中から滝の口まで
        const float FallR = 7.5f;            // 滝つぼの丸い壁の半径
        const float LedgeUp = 1.8f;          // 滝の裏の岩棚の高さ（滝つぼの水際から）
        const float AlcoveHigh = 3.4f;       // 岩棚の上の天井までの高さ
        const float AlcoveDeep = 3.1f;       // 岩棚の奥行き（壁の奥へ）

        static bool _fallOK;
        static Vector2 _fallC, _fallD, _fallS, _fallLip;
        static float _fallTop, _fallFoot;

        static float LedgeH => _fallFoot + LedgeUp;
        static float PoolLevel => _fallFoot + 0.3f;

        /// <summary>Makes the south river fall 12 m where it leaves the pool's bench, and the gorge below it.</summary>
        static void MakeFall(River r, float[] ground)
        {
            int m = r.p.Count;
            r.gorge = new float[m];
            Vector2 pool = W(PoolSvg);
            int f = -1;
            for (int i = 0; i + 3 < m; i++) if ((r.p[i] - pool).magnitude >= FallLipDist) { f = i; break; }
            if (f < 0) { _fallOK = false; return; }
            r.fall = f;
            _fallTop = r.bed[f];
            _fallFoot = _fallTop - FallHeight;
            for (int i = f + 1; i < m; i++) r.bed[i] = Mathf.Min(r.bed[i], _fallFoot - 0.004f * (i - f));
            // the gorge: steep walls until the ground comes down to the river again
            int c = f + 1;
            while (c < m - 1 && ground[c] - r.bank[c] > r.bed[c] + 0.5f) c++;
            for (int i = f + 1; i < m; i++) r.gorge[i] = i <= c ? 1f : 1f - Smooth(0f, 6f, i - c);
            _fallLip = r.p[f];
            _fallD = (r.p[f + 3] - r.p[f]).normalized;
            _fallS = new Vector2(-_fallD.y, _fallD.x);
            _fallC = _fallLip + _fallD * FallR;
            _fallOK = true;
        }

        /// <summary>A point round the plunge pool: the angle from the back (the lip, 0°) toward +side, and the radius.</summary>
        static Vector2 FallAt(float deg, float rr)
        {
            float a = deg * Mathf.Deg2Rad;
            return _fallC + (-_fallD * Mathf.Cos(a) + _fallS * Mathf.Sin(a)) * rr;
        }

        static void FallLocal(float x, float z, out float deg, out float rr)
        {
            var v = new Vector2(x, z) - _fallC;
            rr = v.magnitude;
            deg = Mathf.Atan2(Vector2.Dot(v, _fallS), -Vector2.Dot(v, _fallD)) * Mathf.Rad2Deg;
        }

        /// <summary>The ground carved for the falls: the pool, the shore, the ledge behind the curtain and its ramp.</summary>
        static float FallGround(float x, float z, float h, out float rock, out float pool)
        {
            rock = 0f; pool = 0f;
            if (!_fallOK) return h;
            FallLocal(x, z, out float deg, out float rr);
            if (rr > FallR + AlcoveDeep + 2f) return h;
            float ad = Mathf.Abs(deg);
            float alcove = 1f - Smooth(44f, 54f, ad);
            float reach = Mathf.Lerp(FallR + 0.5f, FallR + AlcoveDeep + 0.6f, alcove);   // (the ground stays down a little past the alcove's back wall)
            float carve = rr < FallR ? 1f : ad > 146f ? 0f : 1f - Smooth(reach, reach + 1.2f, rr);
            if (carve <= 0f) return h;
            // the pool: a bowl 1.5 m deep, a rocky shore round it
            float inner = FallR - 1.8f, target;
            if (rr < inner) { float k = rr / inner; target = _fallFoot + 0.1f - 1.5f * (1f - k * k); pool = 1f; }
            else target = _fallFoot + 0.1f + 0.4f * Smooth(inner, FallR, rr);
            // the ledge behind the curtain
            float ledge = alcove * Smooth(FallR - 1.4f, FallR - 0.9f, rr);
            target = Mathf.Lerp(target, LedgeH, ledge);
            // the ramp of fallen rock up the west wall to the ledge
            if (deg < -40f && deg > -112f && rr > FallR - 2.6f)
            {
                float t = Smooth(-112f, -46f, deg), band = Smooth(FallR - 2.6f, FallR - 2.1f, rr);
                target = Mathf.Max(target, Mathf.Lerp(target, Mathf.Lerp(_fallFoot + 0.5f, LedgeH, t), band));
            }
            rock = carve * (1f - pool);
            pool *= carve;
            return Mathf.Lerp(h, target + 0.1f * Fbm(x * 0.3f, z * 0.3f, 2, 111) * (1f - pool), carve);
        }

        static Material Transparent(string name, Color c, float smooth)
        {
            var m = MoveTestBuilder.SaveMaterial(name, Shader.Find("Universal Render Pipeline/Lit"), c);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_Cull", 0f);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Mesh SaveMesh(Mesh mesh)
        {
            string path = $"{MoveTestBuilder.GenDir}/{mesh.name}.asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old) { old.Clear(); EditorUtility.CopySerialized(mesh, old); EditorUtility.SetDirty(old); return old; }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static GameObject MeshObject(Transform parent, string name, Mesh mesh, Material mat, bool collide)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collide) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }

        /// <summary>Builds the rock round the pool, the water, the mist and the moisture zones (after the terrain).</summary>
        static void BuildFalls(Transform map, Terrain terrain, Material water)
        {
            if (!_fallOK) { Debug.LogWarning("[IslandTest800] no waterfall"); return; }
            var root = new GameObject("滝").transform;
            root.SetParent(map, false);
            var rockMat = Mat("IT_FallRock", new Color(0.5f, 0.5f, 0.48f));
            var curtainMat = Transparent("IT_FallWater", new Color(0.8f, 0.9f, 0.95f, 0.55f), 0.85f);
            var foamMat = Transparent("IT_FallFoam", new Color(1f, 1f, 1f, 0.82f), 0.3f);
            float Natural(Vector2 p) => Height(p.x, p.y);

            // ---- the rock wall: a horseshoe of profiles from the pool's floor up the face, over the lip and back onto the ground
            const int Cols = 72;
            const float From = -158f, To = 158f;
            var verts = new List<Vector3>();
            int rows = 0;
            for (int j = 0; j <= Cols; j++)
            {
                float deg = Mathf.Lerp(From, To, j / (float)Cols), ad = Mathf.Abs(deg);
                float w = 1f - Smooth(38f, 58f, ad);                                        // the alcove behind the curtain
                float top = Natural(FallAt(deg, FallR + 1f));
                top = Mathf.Max(top, Mathf.Lerp(_fallFoot + 1.5f, LedgeH + AlcoveHigh + 1.5f, w));
                float roof = LedgeH + AlcoveHigh, bottom = _fallFoot - 2.5f;
                float overhang = 0.4f + 0.7f * Mathf.Exp(-deg * deg / 400f);
                var prof = new List<Vector2>
                {
                    new Vector2(Mathf.Lerp(FallR - 0.2f, FallR + AlcoveDeep, w), Mathf.Lerp(bottom, LedgeH - 0.6f, w)),
                    new Vector2(Mathf.Lerp(FallR - 0.2f, FallR + AlcoveDeep, w), Mathf.Lerp(Mathf.Lerp(bottom, top, 0.15f), roof, w)),
                    new Vector2(Mathf.Lerp(FallR - 0.2f, FallR - 0.3f, w), Mathf.Lerp(Mathf.Lerp(bottom, top, 0.25f), roof + 0.05f, w)),
                };
                float y0 = prof[2].y;
                for (int k = 1; k <= 9; k++)
                {
                    float y = Mathf.Lerp(y0, top, k / 9f), f = k / 9f;
                    float strata = 0.25f * Smooth(0.6f, 0.9f, Mathf.Repeat((y - _fallFoot) / 2.4f, 1f));   // a little set back every 2.4 m
                    prof.Add(new Vector2(FallR - 0.3f + strata - overhang * Smooth(0.75f, 1f, f), y));
                }
                prof.Add(new Vector2(FallR + 0.4f, top + 0.12f));
                float capOut = FallR + Mathf.Lerp(2.2f, AlcoveDeep + 2.6f, w);
                prof.Add(new Vector2(capOut, Natural(FallAt(deg, capOut)) + 0.08f));
                rows = prof.Count;
                for (int k = 0; k < rows; k++)
                {
                    var pr = prof[k];
                    bool keep = k == 2 || k >= rows - 2;                                   // the roof edge and the cap stay clean
                    float n = keep ? 0f : 0.4f * Fbm(deg * 0.12f, pr.y * 0.35f, 3, 113) + 0.15f * Fbm(deg * 0.5f, pr.y * 1.2f, 2, 117);
                    if (k < 2) n *= 1f - 0.7f * w;                                        // the alcove's back wall is smoother
                    if (k == 3) n *= 0.4f;                                               // and the face just over the roof's edge
                    var at = FallAt(deg, pr.x + n);
                    verts.Add(new Vector3(at.x, pr.y, at.y));
                }
            }
            var tris = new List<int>();
            for (int j = 0; j < Cols; j++)
                for (int k = 0; k + 1 < rows; k++)
                {
                    int a = j * rows + k, b = a + rows;
                    tris.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                }
            var wall = new Mesh { name = "IT_FallRock", indexFormat = IndexFormat.UInt32 };
            wall.SetVertices(verts); wall.SetTriangles(tris, 0); wall.RecalculateNormals();
            // the face must look into the pool: flip if it came out the other way
            {
                int probe = (Cols / 3) * rows + 6;
                Vector3 toC = new Vector3(_fallC.x, verts[probe].y, _fallC.y) - verts[probe];
                if (Vector3.Dot(wall.normals[probe], toC) < 0f)
                {
                    for (int i = 0; i < tris.Count; i += 3) (tris[i + 1], tris[i + 2]) = (tris[i + 2], tris[i + 1]);
                    wall.SetTriangles(tris, 0); wall.RecalculateNormals();
                }
            }
            wall.RecalculateBounds();
            MeshObject(root, "滝つぼの岩壁", SaveMesh(wall), rockMat, true);

            // ---- the water: the pool, the curtain in an arc, strands, foam, mist
            {
                var pv = new List<Vector3> { new Vector3(_fallC.x, PoolLevel, _fallC.y) };
                var pt = new List<int>();
                for (int i = 0; i <= 48; i++)
                {
                    var q = FallAt(i * 7.5f, FallR - 0.9f);
                    pv.Add(new Vector3(q.x, PoolLevel, q.y));
                    if (i > 0) pt.AddRange(new[] { 0, i + 1, i });
                }
                var pm = new Mesh { name = "IT_FallPool" };
                pm.SetVertices(pv); pm.SetTriangles(pt, 0); pm.RecalculateNormals(); pm.RecalculateBounds();
                if (Vector3.Dot(pm.normals[0], Vector3.up) < 0f) { for (int i = 0; i < pt.Count; i += 3) (pt[i + 1], pt[i + 2]) = (pt[i + 2], pt[i + 1]); pm.SetTriangles(pt, 0); pm.RecalculateNormals(); }
                MeshObject(root, "滝つぼの水", SaveMesh(pm), water, false);
            }
            var river = _rivers[1];
            float lipLevel = river.bed[river.fall] + river.water[river.fall];
            float hw = Mathf.Max(1.6f, river.half[river.fall] + 0.6f), drop = lipLevel - PoolLevel;
            Vector3 Curtain(float u, float v, float spread, float forward)
            {
                // v 0: on the lip, 0.08: just past its edge, then the arc of the falling water (2.2 m/s off the lip)
                float y, x;
                if (v < 0.08f) { x = Mathf.Lerp(0f, 1.25f, v / 0.08f); y = lipLevel - 0.05f * v / 0.08f; }
                else { float fall = (v - 0.08f) / 0.92f * (drop - 0.05f); x = 1.25f + 2.2f * Mathf.Sqrt(2f * fall / 9.81f); y = lipLevel - 0.05f - fall; }
                Vector2 p = _fallLip + _fallD * (x + forward) + _fallS * u * hw * (1f + spread * Mathf.Max(0f, v - 0.08f));
                return new Vector3(p.x, y, p.y);
            }
            Mesh Sheet(string name, float u0, float u1, float spread, float forward, float v1, int cols)
            {
                var sv = new List<Vector3>(); var st = new List<int>();
                const int R = 24;
                for (int k = 0; k <= R; k++)
                    for (int c = 0; c <= cols; c++)
                        sv.Add(Curtain(Mathf.Lerp(u0, u1, c / (float)cols), Mathf.Pow(k / (float)R, 1.6f) * v1, spread, forward));
                for (int k = 0; k < R; k++)
                    for (int c = 0; c < cols; c++)
                    {
                        int a = k * (cols + 1) + c, b = a + cols + 1;
                        st.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                    }
                var mm = new Mesh { name = name };
                mm.SetVertices(sv); mm.SetTriangles(st, 0); mm.RecalculateNormals(); mm.RecalculateBounds();
                return SaveMesh(mm);
            }
            MeshObject(root, "滝の水", Sheet("IT_FallCurtain", -1f, 1f, 0.25f, 0f, 1f, 8), curtainMat, false);
            var rnd = new System.Random(812);
            var strands = new GameObject("白い筋").transform;
            strands.SetParent(root, false);
            for (int i = 0; i < 11; i++)
            {
                float u = (float)rnd.NextDouble() * 1.8f - 0.9f, wdt = 0.06f + (float)rnd.NextDouble() * 0.12f;
                float len = 0.75f + 0.25f * (float)rnd.NextDouble();
                MeshObject(strands, "筋", Sheet($"IT_FallStrand_{i}", u - wdt, u + wdt, 0.25f, 0.06f + 0.05f * (float)rnd.NextDouble(), len, 1), foamMat, false);
            }
            Vector3 land = Curtain(0f, 1f, 0.25f, 0f);
            var foam = WeatherSceneSetup.Prim(root, "白い泡", PrimitiveType.Sphere, foamMat, new Vector3(land.x, PoolLevel + 0.05f, land.z), new Vector3(hw * 3.2f, 0.25f, 3.8f), 0f);
            foam.transform.rotation = Quaternion.LookRotation(new Vector3(_fallS.x, 0f, _fallS.y));
            Object.DestroyImmediate(foam.GetComponent<Collider>());
            Spray(root, new Vector3(land.x, PoolLevel + 0.2f, land.z), hw);

            // ---- wet: the pool, and behind the curtain the spray keeps everything wet
            WeatherSceneSetup.Zone(root, "Pool_Zone", MoistureZone.Kind.Water, new Vector3(_fallC.x, PoolLevel, _fallC.y), new Vector3(FallR * 2f - 2f, 3f, FallR * 2f - 2f));
            var behind = FallAt(0f, FallR + 1f);
            WeatherSceneSetup.Zone(root, "Spray_Zone", MoistureZone.Kind.Water, new Vector3(behind.x, LedgeH + 1f, behind.y), new Vector3(9f, 4f, 9f));
            MoveTestBuilder.Label("滝（12 m）", new Vector3(_fallLip.x, _fallTop + 6f, _fallLip.y), 6f);
            MoveTestBuilder.Label("滝の裏の岩棚", new Vector3(behind.x, LedgeH + 2.2f, behind.y), 2.5f);
        }

        /// <summary>The spray where the water lands: soft white puffs rising and drifting (a particle system).</summary>
        static void Spray(Transform parent, Vector3 at, float width)
        {
            string texPath = $"{MoveTestBuilder.GenDir}/IT_SoftDot.asset";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (!tex)
            {
                tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 64; x++)
                    {
                        float d = new Vector2(x - 31.5f, y - 31.5f).magnitude / 31.5f, a = Mathf.Clamp01(1f - d);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a * (3f - 2f * a)));
                    }
                tex.Apply();
                AssetDatabase.CreateAsset(tex, texPath);
            }
            var mat = MoveTestBuilder.SaveMaterial("IT_FallSpray", Shader.Find("Universal Render Pipeline/Particles/Unlit"), new Color(1f, 1f, 1f, 0.35f));
            mat.SetTexture("_BaseMap", tex);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);

            var go = new GameObject("しぶき");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.prewarm = true; main.duration = 5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startColor = new Color(1f, 1f, 1f, 0.5f);
            main.gravityModifier = -0.02f;
            main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 45f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 55f; sh.radius = width;
            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.55f, 0.2f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            ps.Play();
        }

        /// <summary>The falls at the slug's eye: from the pool, from the gorge, behind the curtain, on the ramp; and from the air.</summary>
        [MenuItem("Slug/IslandTest 800 m/Shoot Falls")]
        public static string ShootFalls()
        {
            var t = OpenTerrain();
            if (!t || !_fallOK) return "build the scene first";
            float G(Vector2 p) => t.SampleHeight(new Vector3(p.x, 0f, p.y)) + t.transform.position.y;
            Vector3 On(Vector2 p, float up) => new Vector3(p.x, G(p) + up, p.y);
            Vector3 lip = new Vector3(_fallLip.x, _fallTop - 2f, _fallLip.y);
            foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Exclude)) ps.Simulate(5f, true, true);   // the spray, as it is a while after it starts
            var sb = new StringBuilder();
            void Shot(string file, Vector3 eye, Vector3 look, float fov = 60f) => sb.AppendLine(ShotUtil.Capture($"{ShotDir}/{file}.png", eye, look, fov, 1200, 600, 3000f));
            Shot("40_falls_from_shore", On(FallAt(-125f, FallR - 1.2f), 0.47f), lip);
            var river = _rivers[1];
            int g = river.fall;
            while (g < river.p.Count - 1 && (river.p[g] - _fallC).magnitude < FallR + 2.5f) g++;
            Shot("41_falls_from_gorge", On(river.p[g] + river.n[g] * (river.half[g] + 0.8f), 0.47f), lip + Vector3.down * 3f);
            var ledge = FallAt(8f, FallR + 1.2f);
            Shot("42_behind_falls", new Vector3(ledge.x, LedgeH + 0.47f, ledge.y), new Vector3(_fallC.x, PoolLevel + 1.5f, _fallC.y) + new Vector3(_fallD.x, 0f, _fallD.y) * 6f, 70f);
            Shot("43_falls_ramp", On(FallAt(-95f, FallR - 1.6f), 0.47f), new Vector3(ledge.x, LedgeH + 1f, ledge.y));
            Vector3 c = new Vector3(_fallC.x, PoolLevel, _fallC.y);
            Shot("air_falls", c + new Vector3(_fallD.x, 0f, _fallD.y) * 28f + new Vector3(_fallS.x, 0f, _fallS.y) * 10f + Vector3.up * 22f, c + Vector3.up * 4f, 50f);
            return sb.ToString();
        }
    }
}
