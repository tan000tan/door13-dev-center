using UnityEngine;

namespace Door13.Player
{
    /// <summary>
    /// 第一人称玩家：CharacterController 平面移动 + 重力。
    /// 视角水平旋转作用于身体（改变移动朝向），俯仰作用于头部节点。
    /// 不含交互逻辑（交互见 Interaction 模块）。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Tooltip("头部节点（承载俯仰与相机）")]
        public Transform head;

        [Tooltip("行走速度 m/s")]
        public float walkSpeed = 2.4f;

        [Tooltip("俯仰限制（度）")]
        public float pitchLimit = 80f;

        [Tooltip("重力加速度")]
        public float gravity = -18f;

        CharacterController cc;
        float pitch;
        float vy;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
        }

        void Update()
        {
            if (MobileInput.Instance == null) return;

            // 视角：水平转身体，俯仰转头（向上拖/上移鼠标为抬头）
            Vector2 look = MobileInput.Instance.Look;
            transform.Rotate(0f, look.x, 0f);
            pitch = Mathf.Clamp(pitch - look.y, -pitchLimit, pitchLimit);
            if (head != null) head.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            // 移动（相对身体朝向）+ 重力
            Vector2 m = MobileInput.Instance.Move;
            Vector3 vel = (transform.right * m.x + transform.forward * m.y) * walkSpeed;
            if (cc.isGrounded && vy < 0f) vy = -2f;
            vy += gravity * Time.deltaTime;
            vel.y = vy;
            cc.Move(vel * Time.deltaTime);
        }
    }
}
