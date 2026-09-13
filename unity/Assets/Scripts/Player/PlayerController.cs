using TowerRpg.Core;
using UnityEngine;

namespace TowerRpg.Player
{
    /// <summary>
    /// Di chuyển bằng cần gạt ảo. Phơi ra cờ IsMoving — đầu vào của LUẬT CỐT LÕI §5.3.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private VirtualJoystick joystick;

        private Rigidbody2D _body;
        private float _moveSpeed;
        private float _deadzone;
        private bool _ready;

        public bool IsMoving { get; private set; }

        // Đầu vào do AutoBattle đẩy vào. Để null nghĩa là "máy không lái" — phải phân biệt
        // được với Vector2.zero, vì zero là một MỆNH LỆNH hợp lệ ("đứng yên mà đánh").
        private Vector2? _autoInput;

        public void SetAutoInput(Vector2 v) => _autoInput = v;
        public void ClearAutoInput() => _autoInput = null;

        private void Awake() => _body = GetComponent<Rigidbody2D>();

        private void Start()
        {
            if (joystick == null)
                Debug.LogError("[PlayerController] Chưa gán joystick — nhân vật sẽ đứng im.", this);

            BalanceConfig.TryUse(this, ApplyBalance);
        }

        private void ApplyBalance(BalanceConfig balance)
        {
            _moveSpeed = balance.Get("player.moveSpeed");
            _deadzone = balance.Get("player.moveDeadzone");
            _ready = true;
        }

        private void FixedUpdate()
        {
            if (!_ready) return;

            Vector2 stick = joystick != null ? joystick.Value : Vector2.zero;
            // Ngón tay thắng máy: chỉ dùng đầu vào của auto khi cần gạt đang nghỉ.
            Vector2 input = stick.sqrMagnitude > 0f ? stick : (_autoInput ?? Vector2.zero);

            IsMoving = input.sqrMagnitude > _deadzone * _deadzone;

            _body.linearVelocity = IsMoving ? input * _moveSpeed : Vector2.zero;
        }
    }
}
