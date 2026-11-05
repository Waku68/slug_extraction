using System.Collections.Generic;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// What the game asks of the gear a slug wears, whichever body wears it (Nov 3): the crawling slug's
    /// (<see cref="SlugEquipment"/>, cut from its skin) or the two-legged one's (<see cref="DinoEquipment"/>, riding its
    /// bones). The bag, the hitboxes, the rack, the camera and the husk keep one of these.
    ///   The look (what is worn and its colours) is set here, the same for both bodies; each body builds its own shapes
    ///   from it (<see cref="Build"/>) and says where the thing on its back is (<see cref="HasBackItem"/> ..., body space
    ///   at rest, carried by the body's <see cref="SlugBodyBase.FollowPointWorld"/>) and, while it is looked into, where
    ///   the inside of the opened bag is.
    /// </summary>
    public abstract class SlugGearBase : MonoBehaviour
    {
        public enum Armor { None, ShellPlate, SnailShell }

        [Tooltip("ヘルメット（ふつうは触角を出す鉄の鍋）")] public bool helmet = true;
        [Tooltip("触角の形の兜：鍋から触角を包む角が2本伸びる。触角の弱点が消え、音がこもる")] public bool tentacleHelmet;
        [Tooltip("胴の防具（カタツムリの殻はカタツムリ用）")] public Armor armor = Armor.ShellPlate;
        [Tooltip("背中のバッグ（ナメクジ用）")] public bool bag;

        [Header("ヘルメット")]
        [Tooltip("鉄の色")] public Color helmetColor = new Color(0.3f, 0.32f, 0.24f);
        [Tooltip("ヘルメットにも迷彩をかける")] public bool helmetCamo;
        [Tooltip("頭とのすき間（m）")] public float helmetGap = 0.016f;
        [Tooltip("つばの長さ：目の上（m）と、ほかのところ（m）")] public float brimFront = 0.035f;
        public float brimSide = 0.014f;
        [Tooltip("触角の形の兜：角と触角のすき間（m）と、触角の先からさらに伸ばす長さ（m）")] public float hornGap = 0.006f;
        public float hornExtend = 0.012f;

        [Header("迷彩（スキン）")]
        [Tooltip("地の色（枯れ草）。体の苔色から浮くように、明るめにする")] public Color camoBase = new Color(0.62f, 0.57f, 0.38f);
        [Tooltip("ぶちの色 1（茶）")] public Color camo1 = new Color(0.43f, 0.28f, 0.15f);
        [Tooltip("ぶちの色 2（こげ茶）")] public Color camo2 = new Color(0.15f, 0.13f, 0.1f);
        [Tooltip("ぶちの色 3（苔）")] public Color camo3 = new Color(0.31f, 0.38f, 0.19f);
        [Tooltip("ぶちの細かさ")] public float camoScale = 19f;
        [Tooltip("ベルトの色")] public Color strapColor = new Color(0.22f, 0.16f, 0.11f);

        [Header("バッグ")]
        [Tooltip("布の色")] public Color bagColor = new Color(0.52f, 0.4f, 0.26f);
        [Tooltip("ふたとポケットの色")] public Color bagFlapColor = new Color(0.36f, 0.26f, 0.17f);
        [Tooltip("大きさ（幅、高さ、厚み の半分, m）")] public Vector3 bagSize = new Vector3(0.11f, 0.12f, 0.065f);
        /// <summary>How swollen the bag is with what is in it (1 = empty; set at run time by the bag, then <see cref="Build"/>).
        /// It grows out from the back most, taller a little less, wider least.</summary>
        [System.NonSerialized] public float bagSwell = 1f;
        [Tooltip("背中を覗いているあいだ、バッグの口を真上からこの角度（度）まで倒す（だれから見ても）")] public float bagPeekZenith = 45f;
        [Tooltip("倒す向き：体の右から前（頭の側）へこの角度（度）。0 = 真右、90 = 真前。ふくらんだバッグは、頭にかぶさらないよう後ろ（しっぽの側）へ寄せる（パンパンでこの角度）")] public float bagPeekLean = 35f;
        public float bagPeekLeanFull = -30f;
        [Tooltip("背中を覗いているあいだ、口を広げる倍率（幅と奥行き）：空のときと、パンパンのとき")] public float bagPeekMouth = 2f;
        public float bagPeekMouthFull = 3f;
        [Tooltip("底をすぼめる倍率")] public float bagPeekBottom = 0.8f;
        [Tooltip("口を広げても、口の幅の半分はこれ（m）まで（パンパンのバッグが広がりすぎないように）")] public float bagPeekMouthMax = 0.45f;
        [Tooltip("中身の山の色（上の物はそれぞれの形で見える）")] public Color bagHeapColor = new Color(0.6f, 0.56f, 0.46f);
        /// <summary>0..1: how far the bag is opened (set each frame by its owner while looking into it): the lid flips open,
        /// the mouth spreads with the lining and the heap of what is in it showing, and the bag tips up toward the right.
        /// Everyone sees it.</summary>
        [System.NonSerialized] public float bagOpen;
        /// <summary>Something is in the bag (the heap shows when it is opened; empty, the floor of the lining does).</summary>
        [System.NonSerialized] public bool bagHasContents;
        [Tooltip("ふたを開けきったときの角度（度）。ふたは前（背中から遠い面）の終わりの縫い目を軸に、外側へ開く")] public float bagLidAngle = 140f;
        [Tooltip("留め具の色")] public Color buckleColor = SlugEquipment.BuckleColor;

        [Header("ベスト")]
        [Tooltip("胸の前の弾入れ（弾倉と苔玉の袋が並ぶ）")] public bool vest = true;
        [Tooltip("布の色（体の緑と迷彩から浮くように、暗い炭色）")] public Color vestColor = new Color(0.22f, 0.23f, 0.21f);
        [Tooltip("袋の色")] public Color vestPouchColor = new Color(0.3f, 0.31f, 0.28f);
        [Tooltip("弾倉の色")] public Color magColor = new Color(0.5f, 0.52f, 0.54f);
        [Tooltip("弾倉の口に見える塩の色")] public Color saltColor = new Color(0.9f, 0.88f, 0.82f);
        [Tooltip("苔玉の色")] public Color mossColor = new Color(0.42f, 0.62f, 0.28f);

        /// <summary>The body the gear is on.</summary>
        public abstract SlugBodyBase Body { get; }

        /// <summary>Called after the gear is (re)built, so things fitted to it (hitboxes) can follow.</summary>
        public event System.Action Built;
        protected void RaiseBuilt() => Built?.Invoke();

        // what is on the back, for hitboxes, the bag and the rack (body space at rest; carried by Body.FollowPointWorld at BackItemT)
        public bool HasBackItem { get; protected set; }
        public Vector3 BackItemCenter { get; protected set; }
        public Vector3 BackItemNormal { get; protected set; }
        public Quaternion BackItemRotation { get; protected set; }
        public Vector3 BackItemHalf { get; protected set; }
        public float BackItemT { get; protected set; }
        /// <summary>Inside the opened bag (world): the middle of the top of the heap of what is in it (of the floor when
        /// empty), and the bag's turn (x across, y out of the mouth, z out from the back).</summary>
        public Vector3 BagInsidePosition { get; protected set; }
        public Quaternion BagMouthRotation { get; protected set; }
        /// <summary>0..1: how much the bag is too big to lean over the head when opened (0 up to about a third full, 1 nearly
        /// crammed): it leans back toward the tail instead (<see cref="bagPeekLeanFull"/>), and the eyes keep the face in view.</summary>
        public float BagPeekBig { get { float k = Mathf.Clamp01((SlugEquipment.Fill(bagSwell) - 0.3f) / 0.5f); return k * k * (3f - 2f * k); } }
        /// <summary>The opened bag's heap's half size (across, and out from the back; m), for laying things out on it.</summary>
        public Vector2 BagHeapHalf { get; protected set; }

        /// <summary>Puts the gear (back) on, as set above.</summary>
        public abstract void Build();

        /// <summary>
        /// The owner's own camera: while it sits inside the head (first person), the helmet only casts its shadow for
        /// that camera, so its brim never fills the view; every other camera sees it as usual.
        /// </summary>
        public abstract void SetOwnCamera(Camera cam, System.Func<bool> inside);

        /// <summary>The renderers of the gear being worn, one list per piece that falls as one thing (the helmet; the
        /// armour with the bag's belts), the bag itself only with <paramref name="withBag"/>. For leaving the gear behind
        /// when the slug falls.</summary>
        public abstract void CollectGear(List<List<Renderer>> pieces, bool withBag);

        /// <summary>Jolts the bag (+ = its top swings out from the back; -, pushed home): it rocks and settles.</summary>
        public abstract void Jostle(float degreesPerSecond);

        /// <summary>
        /// What rides on the vest: how many pouches of each kind it has (moss balls, the big gun's magazines, the small
        /// gun's), and how many of them are filled. It is rebuilt only when the pouches change.
        /// </summary>
        public abstract void SetVestLoad(int mossPouches, int moss, int bigPouches, int big, int smallPouches, int small);

        /// <summary>Where the opened bag's mouth faces: <see cref="bagPeekZenith"/> from straight up, over between the
        /// right and the front (<see cref="bagPeekLean"/>; a big bag between the right and the back).</summary>
        protected Vector3 PeekMouthDirection(Vector3 up, Vector3 right, Vector3 forward)
        {
            right = Vector3.ProjectOnPlane(right, up).normalized;
            forward = Vector3.ProjectOnPlane(forward, up).normalized;
            float z = bagPeekZenith * Mathf.Deg2Rad, a = Mathf.Lerp(bagPeekLean, bagPeekLeanFull, BagPeekBig) * Mathf.Deg2Rad;
            return up * Mathf.Cos(z) + (right * Mathf.Cos(a) + forward * Mathf.Sin(a)) * Mathf.Sin(z);
        }

        protected void SetBackItem(Vector3 center, Vector3 normal, float t, Quaternion rotation, Vector3 half)
        {
            HasBackItem = true;
            BackItemCenter = center; BackItemNormal = normal; BackItemT = t;
            BackItemRotation = rotation;
            // (x across the back, y up it, z out from it)
            BackItemHalf = half;
        }
    }
}
