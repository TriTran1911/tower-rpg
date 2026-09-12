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

            Vector2 input = joystick != null ? joystick.Value : Vector2.zero;
            IsMoving = input.sqrMagnitude > _deadzone * _deadzone;

            _body.linearVelocity = IsMoving ? input * _moveSpeed : Vector2.zero;
        }
    }
}
