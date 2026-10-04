using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SlugMove
{
    /// <summary>
    /// Tools for tuning the swing: slow motion (T), the path of the striking point and markers for the aimed point,
    /// the pivot and the contact (G), and a readout of the phase and the last blow.
    /// </summary>
    public class SwingDebug : MonoBehaviour
    {
        public SlugToolHolder holder;

        static readonly float[] Speeds = { 1f, 0.35f, 0.1f };
        int _speed;
        bool _marks = true;
        LineRenderer _trail;
        readonly List<Vector3> _points = new List<Vector3>();
        Transform _target, _pivot, _contact, _expected;
        Material _lineMat;
        GUIStyle _style;
        string _last = "まだ当てていない";
        float _peakSpeed;

        void Start()
        {
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            _lineMat = new Material(unlit);
            _lineMat.SetColor("_BaseColor", new Color(1f, 0.55f, 0.1f));
            _trail = new GameObject("SwingTrail").AddComponent<LineRenderer>();
            _trail.sharedMaterial = _lineMat;
            _trail.widthMultiplier = 0.008f;
            _trail.numCapVertices = 2;
            _trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _trail.positionCount = 0;
            _target = Marker("AimMark", new Color(1f, 0.9f, 0.1f), unlit);
            _pivot = Marker("PivotMark", new Color(0.2f, 0.8f, 1f), unlit);
            _contact = Marker("ContactMark", new Color(1f, 0.2f, 0.2f), unlit);
            _expected = Marker("ExpectedMark", new Color(1f, 0.3f, 1f), unlit);
            _contact.gameObject.SetActive(false);
            holder.SwingStarted += OnSwingStarted;
            holder.Struck += OnStruck;
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
            if (holder) { holder.SwingStarted -= OnSwingStarted; holder.Struck -= OnStruck; }
        }

        static Transform Marker(string name, Color c, Shader unlit)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.localScale = Vector3.one * 0.035f;
            var m = new Material(unlit);
            m.SetColor("_BaseColor", c);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        void OnSwingStarted()
        {
            _points.Clear();
            _peakSpeed = 0f;
            _contact.gameObject.SetActive(false);
        }

        void OnStruck(RaycastHit hit, IPickaxeTarget target)
        {
            var sw = holder.Swing;
            _contact.position = hit.point;
            _contact.gameObject.SetActive(_marks);
            string what = sw.LastMaterial switch
            {
                PickaxeMaterial.Hard => "硬い（跳ね返る）",
                PickaxeMaterial.Soft => "柔らかい（めり込む）",
                _ => "ふつう（地面・壁）",
            };
            string reach = holder.LastGap > 0.01f ? $"、判定だけ届いた（頭は {holder.LastGap * 100f:0} cm 手前）" : "";
            _last = $"{hit.collider.name}：{what}、当たった速さ {sw.LastImpactSpeed:0.0} m/s{reach}";
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.tKey.wasPressedThisFrame)
            {
                _speed = (_speed + 1) % Speeds.Length;
                Time.timeScale = Speeds[_speed];
            }
            if (kb != null && kb.gKey.wasPressedThisFrame)
            {
                _marks = !_marks;
                _trail.enabled = _marks;
            }

            var sw = holder.Swing;
            if (sw.Phase != SwingPhase.Idle)
            {
                _points.Add(holder.TipWorld);
                if (_points.Count > 600) _points.RemoveAt(0);
                _trail.positionCount = _points.Count;
                _trail.SetPositions(_points.ToArray());
                if (sw.Phase == SwingPhase.Strike) _peakSpeed = Mathf.Max(_peakSpeed, sw.TipSpeed);
            }
            var root = holder.controller.transform;
            _target.position = root.TransformPoint(sw.TargetLocal);
            _pivot.position = root.TransformPoint(sw.PivotLocal);
            _target.gameObject.SetActive(_marks && sw.Phase != SwingPhase.Idle);
            _pivot.gameObject.SetActive(_marks && sw.Phase != SwingPhase.Idle);
            _expected.position = holder.ExpectedContactWorld;
            _expected.gameObject.SetActive(_marks && holder.HasPredictedContact && sw.Phase != SwingPhase.Idle);
            if (!_marks) _contact.gameObject.SetActive(false);
        }

        void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 17, richText = true };
                _style.normal.textColor = Color.white;
            }
            var sw = holder.Swing;
            string phase = sw.Phase switch
            {
                SwingPhase.Windup => "振りかぶり",
                SwingPhase.Strike => "振り下ろし",
                SwingPhase.Impact => sw.Frozen ? "当たり（ヒットストップ）" : "当たり",
                SwingPhase.Follow => "振り抜き（空振り）",
                SwingPhase.Recover => "戻り",
                _ => "構え",
            };
            string text =
                $"動き  <b>{phase}</b>　　時間 ×{Speeds[_speed]:0.##}\n" +
                $"照準  {(sw.HasTarget ? "届く所に何かある" : "何もない（空振り）")}　　振り下ろしの最高速 {_peakSpeed:0.0} m/s\n" +
                $"最後の当たり  {_last}\n" +
                "<size=14>左クリック 振る（押しっぱなしで連続）／ T スロー ／ G 軌跡と印 ／ V 視点切替\n" +
                "黄：照準の点　桃：当たると予測した点　赤：当たった点　青：振りの中心　橙：ピッケルの先の軌跡</size>";
            GUI.Box(new Rect(10, 10, 640, 130), GUIContent.none);
            GUI.Label(new Rect(20, 16, 630, 130), text, _style);
        }
    }
}
