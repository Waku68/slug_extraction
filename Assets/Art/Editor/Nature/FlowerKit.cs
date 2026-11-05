using System;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 小さな草花の部品（Oct 27。ツユリ・ツキシロ用）：曲がる茎、花びらの輪、細い葉。NatureMeshKit に足していく。
    /// 大きさは本物寄り（花まで 20〜25 cm、わくさん Oct 27）。ポップにするのは太さ：茎・花びら・葉は本物より太く厚く。
    /// </summary>
    public static class FlowerKit
    {
        /// <summary>
        /// dir0 から toward の方へ、angleDeg(t) 度だけ曲がりながら伸びる線（長さ len）。茎のうつむき・首の曲がりに使う。
        /// endDir は先の向き（花の向き）。
        /// </summary>
        public static Func<float, Vector3> Curve(Vector3 start, Vector3 dir0, Vector3 toward, float len, Func<float, float> angleDeg, out Vector3 endDir)
        {
            const int N = 24;
            var pts = new Vector3[N + 1];
            var axis = Vector3.Cross(dir0, toward);
            axis = axis.sqrMagnitude < 1e-6f ? Vector3.right : axis.normalized;
            pts[0] = start;
            for (int i = 0; i < N; i++)
                pts[i + 1] = pts[i] + Quaternion.AngleAxis(angleDeg((i + 0.5f) / N), axis) * dir0 * (len / N);
            endDir = (Quaternion.AngleAxis(angleDeg(1f), axis) * dir0).normalized;
            return t => { float x = Mathf.Clamp01(t) * N; int i = Mathf.Min(N - 1, (int)x); return Vector3.Lerp(pts[i], pts[i + 1], x - i); };
        }

        /// <summary>茎（細い筒。根元が少し太い）。rows は曲がりが滑らかに見えるだけ。</summary>
        public static void Stem(NatureMeshKit kit, int sub, Func<float, Vector3> center, float r0, float r1, int sides, int seed)
        {
            var rows = new Vector3[9];
            for (int i = 0; i < rows.Length; i++) rows[i] = new Vector3(i / 8f, 1f, sub);
            int from = kit.VertexCount;
            kit.BeginTube();
            kit.Tube(center, t => Mathf.Lerp(r0, r1, t), rows, sides, sub, sub);
            kit.Paint(from, (p, uv) => new Color(uv.y * 0.7f, 0f, NatureAssets.Hash(seed, 7), 1f));
        }

        /// <summary>花の軸 axis に直交する 2 本の向き。</summary>
        public static void Frame(Vector3 axis, out Vector3 a, out Vector3 b)
        {
            a = Vector3.Cross(axis, Mathf.Abs(axis.y) > 0.9f ? Vector3.forward : Vector3.up).normalized;
            b = Vector3.Cross(axis, a).normalized;
        }

        /// <summary>
        /// 花びらの輪。center から、軸 axis と open 度の向きへ出て、先は open+curl 度へ反る（curl が負なら内へ抱える）。
        /// 幅は先へ尖る（tipRound が大きいほど先が丸い）。花びらの内側の面は軸の向き。
        /// </summary>
        public static void PetalRing(NatureMeshKit kit, int sub, Vector3 center, Vector3 axis, int count, float len, float width,
                                     float open, float curl, float phase, float thick, float fold, int segs, int seed, float tipRound = 0.6f)
        {
            Frame(axis, out var a, out var b);
            for (int i = 0; i < count; i++)
            {
                float ang = phase + Mathf.PI * 2f * i / count + (NatureAssets.Hash(seed, i) - 0.5f) * 0.25f;
                var radial = a * Mathf.Cos(ang) + b * Mathf.Sin(ang);
                var tangent = Vector3.Cross(axis, radial).normalized;
                float l = len * Mathf.Lerp(0.9f, 1.08f, NatureAssets.Hash(seed, i + 30));
                Vector3 Dir(float deg) => axis * Mathf.Cos(deg * Mathf.Deg2Rad) + radial * Mathf.Sin(deg * Mathf.Deg2Rad);
                Vector3 p1 = center + Dir(open) * (l * 0.55f), p2 = p1 + Dir(open + curl) * (l * 0.45f);
                Vector3 Path(float s) => (1f - s) * (1f - s) * center + 2f * s * (1f - s) * p1 + s * s * p2;
                float Width(float s) => width * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Lerp(0.12f, 1f, s))), tipRound) * Mathf.Lerp(0.55f, 1f, Mathf.Min(1f, s * 3f));
                int from = kit.VertexCount;
                kit.Blade(Path, Width, segs, thick, fold, sub, tangent);
                kit.Paint(from, (p, uv) => new Color(0.8f + 0.2f * uv.y, uv.y * 0.6f, NatureAssets.Hash(seed, 9), 1f));
            }
        }

        /// <summary>根元から外へ弓なりに出る細い葉（rise は出る角度、droop は先の垂れ）。</summary>
        public static void StrapLeaf(NatureMeshKit kit, int sub, Vector3 root, float yaw, float rise, float len, float width,
                                     float droop, float thick, int segs, int seed)
        {
            var h = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
            var d = h * Mathf.Cos(rise * Mathf.Deg2Rad) + Vector3.up * Mathf.Sin(rise * Mathf.Deg2Rad);
            Vector3 Path(float s) => root + d * (len * s) - Vector3.up * (droop * len * s * s);
            int from = kit.VertexCount;
            kit.Blade(Path, s => width * Mathf.Pow(Mathf.Max(0f, 1f - s), 0.6f) * Mathf.Lerp(0.7f, 1f, Mathf.Min(1f, s * 4f)), segs, thick, 0.25f, sub, Vector3.Cross(Vector3.up, h));
            kit.Paint(from, (p, uv) => new Color(uv.y * 0.8f, uv.y, NatureAssets.Hash(seed, 11), 1f));
        }

        /// <summary>地面に寝る幅広の葉（カタクリやロゼットの葉。先が少し持ち上がる）。</summary>
        public static void FlatLeaf(NatureMeshKit kit, int sub, Vector3 root, float yaw, float len, float width, float lift, float thick, int segs, int seed)
        {
            var h = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
            Vector3 Path(float s) => root + h * (len * s) + Vector3.up * (lift * Mathf.Sin(Mathf.PI * 0.8f * s) + 0.004f);
            int from = kit.VertexCount;
            kit.Blade(Path, s => width * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Lerp(0.08f, 1f, s))), 0.7f), segs, thick, 0.18f, sub, Vector3.Cross(Vector3.up, h));
            kit.Paint(from, (p, uv) => new Color(uv.y * 0.4f, uv.y * 0.6f, NatureAssets.Hash(seed, 13), 1f));
        }
    }
}
