using UnityEngine;
using UnityEngine.EventSystems;

namespace Door13.Player
{
    /// <summary>
    /// 屏幕虚拟摇杆：按住底座拖动圆钮，输出归一化向量；松手回中。
    /// 视觉由场景生成脚本创建（底座+圆钮 Image），本脚本只负责输入逻辑。
    /// </summary>
    public class VirtualJoystick : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Tooltip("摇杆圆钮")]
        public RectTransform knob;

        [Tooltip("拖动最大半径（Canvas 单位）")]
        public float radius = 90f;

        /// <summary>归一化输出向量，长度 ≤ 1</summary>
        public Vector2 Output { get; private set; }

        RectTransform rect;

        void Awake()
        {
            rect = (RectTransform)transform;
            ResetKnob();
        }

        public void OnPointerDown(PointerEventData e) => OnDrag(e);

        public void OnDrag(PointerEventData e)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect, e.position, e.pressEventCamera, out local);
            Output = Vector2.ClampMagnitude(local / radius, 1f);
            if (knob != null) knob.anchoredPosition = Output * radius;
        }

        public void OnPointerUp(PointerEventData e)
        {
            Output = Vector2.zero;
            ResetKnob();
        }

        void OnDisable()
        {
            Output = Vector2.zero;
            ResetKnob();
        }

        void ResetKnob()
        {
            if (knob != null) knob.anchoredPosition = Vector2.zero;
        }
    }
}
