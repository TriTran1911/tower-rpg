using TowerRpg.Combat;
using TowerRpg.Core;
using TowerRpg.Progression;
using UnityEngine;

namespace TowerRpg.Player
{
    /// <summary>
    /// Tự động chiến đấu — §5.2, mở sau khi dọn xong tầng 20.
    ///
    /// CỐ Ý LÀM NGU: nó đi tới con gần nhất rồi ĐỨNG LẠI, đúng bằng tầm đánh của người
    /// chơi. Nó không lùi ra né đòn, không chọn mục tiêu máu thấp, không canh thanh chí
    /// mạng. Vì tầm đánh của quái LỚN HƠN tầm của người chơi (§5.3), đứng đánh là ăn đòn —
    /// nên tay người chơi luôn nhỉnh hơn máy.
    ///
    /// Nếu sửa cho nó biết né, auto sẽ giỏi hơn người và cả §1 ("khoảng cách do chăm chỉ")
    /// sụp đổ: chăm chỉ biến thành bật auto rồi đi ngủ.
    /// </summary>
    public sealed class AutoBattle : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private VirtualJoystick joystick;

        /// <summary>Tầng phải dọn xong mới mở được — đọc từ CSV.</summary>
        public int UnlockFloor { get; private set; } = 20;

        public bool Enabled { get; private set; }

        private float _range;
        private bool _ready;

        public bool IsUnlocked =>
            GameState.Instance != null && GameState.Instance.AutoUnlocked(UnlockFloor);

        private void Start()
        {
            if (player == null) player = GetComponent<PlayerController>();
            if (player == null)
            {
                Debug.LogError("[AutoBattle] Chưa gán 'player'.", this);
                enabled = false;
                return;
            }
            BalanceConfig.TryUse(this, b =>
            {
                UnlockFloor = Mathf.Max(1, b.GetInt("auto.unlockFloor"));
                // Dừng sát mép tầm đánh: gần hơn không đánh nhanh hơn, chỉ ăn thêm đòn.
                _range = b.Get("player.attackRange") * 0.9f;
                _ready = true;
            });
        }

        public bool Toggle()
        {
            if (!IsUnlocked) return false;
            Enabled = !Enabled;
            if (!Enabled && player != null) player.ClearAutoInput();
            return Enabled;
        }

        private void Update()
        {
            if (!_ready || player == null) return;

            if (!Enabled || !IsUnlocked)
            {
                player.ClearAutoInput();
                return;
            }

            // Ngón tay luôn thắng máy. Chạm vào là auto nhường quyền ngay, không cần tắt.
            if (joystick != null && joystick.IsHeld)
            {
                player.ClearAutoInput();
                return;
            }

            IDamageable target = EnemyRegistry.Nearest(transform.position, float.MaxValue);
            if (target == null) { player.ClearAutoInput(); return; }

            Vector3 delta = target.Position - transform.position;
            // Trong tầm rồi thì ĐỨNG YÊN — §5.3: di chuyển là không đánh.
            player.SetAutoInput(delta.sqrMagnitude <= _range * _range
                                ? Vector2.zero
                                : ((Vector2)delta).normalized);
        }
    }
}
