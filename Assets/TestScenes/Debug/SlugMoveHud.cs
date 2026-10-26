using UnityEngine;

namespace SlugMove
{
    /// <summary>Debug readout in the top-left corner: speed, slope angle, view mode, controls.</summary>
    public class SlugMoveHud : MonoBehaviour
    {
        public SlugMoveController controller;
        public SlugCameraRig cameraRig;

        GUIStyle _style;

        void OnGUI()
        {
            if (!controller) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 18, richText = true };
                _style.normal.textColor = Color.white;
            }
            var s = controller.settings;
            string steep = controller.Climbing ? "  <color=#a0e0ff>よじ登り中</color>"
                : controller.TooSteep ? "  <color=#ff8080>登れない</color>" : "";
            string text =
                $"速度  {controller.Speed:0.00} m/s  （最高 {s.maxSpeed:0.00}）\n" +
                $"坂の角度  {controller.SlopeAngle:0.0}°  （限界 {s.slopeLimit:0}°）{steep}\n" +
                $"視点  {(cameraRig && cameraRig.thirdPerson ? (cameraRig.shoulder ? "三人称（右後ろ）" : "三人称") : "一人称")}\n" +
                "<size=14>WASD 移動 / スペース 縮んで伸びる（長押しで大きく） / マウス 視点 / V 視点切替 / Esc カーソル解放（クリックで戻る）</size>";
            GUI.Box(new Rect(10, 10, 520, 110), GUIContent.none);
            GUI.Label(new Rect(20, 16, 510, 110), text, _style);
        }
    }
}
