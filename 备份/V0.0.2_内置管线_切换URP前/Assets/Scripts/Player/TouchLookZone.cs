using UnityEngine;
using UnityEngine.EventSystems;

namespace Door13.Player
{
    /// <summary>
    /// 视角输入：聚合本帧所有未命中 UI 的触摸位移；编辑器/桌面下支持鼠标右键拖动。
    /// 输出已乘灵敏度，消费方（PlayerController）直接用于旋转。
    /// </summary>
    public class TouchLookZone : MonoBehaviour
    {
        [Tooltip("触摸像素 → 旋转角系数")]
        public float touchSensitivity = 0.22f;

        [Tooltip("编辑器鼠标灵敏度倍率")]
        public float mouseSensitivity = 10f;

        /// <summary>本帧视角增量（x=水平，y=俯仰，向上拖为正）</summary>
        public Vector2 Delta { get; private set; }

        void Update()
        {
            Vector2 d = Vector2.zero;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase != TouchPhase.Moved) continue;

                var es = EventSystem.current;
                if (es != null && es.IsPointerOverGameObject(t.fingerId)) continue;
                d += t.deltaPosition;
            }

#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButton(1))
                d += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * mouseSensitivity;
#endif

            Delta = d * touchSensitivity;
        }
    }
}
