using TowerRpg.Combat;
using TowerRpg.Core;
using TowerRpg.Juice;
using UnityEngine;

namespace TowerRpg.Player
{
    /// <summary>
    /// LUẬT CỐT LÕI CỦA TOÀN BỘ GAME (§5.3):
    ///     Di chuyển thì an toàn nhưng không gây sát thương.
    ///     Đứng yên thì gây sát thương nhưng ăn đòn.
    /// Không có nút tấn công. Nhân vật tự đánh kẻ gần nhất, và CHỈ khi đang đứng yên.
    /// </summary>
    public sealed class AutoAttack : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private CritMeter critMeter;
        [SerializeField] private DamagePopupSpawner popups;
        [SerializeField] private CameraShake cameraShake;

        private float _damage;
        private float _attacksPerSecond;
        private float _range;
        private float _critMultiplier;

        private float _cooldown;
        private bool _ready;

        private void Start()
        {
            // FAIL-CLOSED. Thiếu tham chiếu này thì luật §5.3 bị vô hiệu trong im lặng —
            // nhân vật vừa chạy vừa đánh, và M1 trả lời sai câu hỏi của chính nó.
            if (player == null)
            {
                Debug.LogError("[AutoAttack] Chưa gán 'player'. Không có nó thì LUẬT CỐT LÕI §5.3 " +
                               "(di chuyển thì không đánh) không thể thực thi. Tắt component.", this);
                enabled = false;
                return;
            }

            if (critMeter == null)
                Debug.LogError("[AutoAttack] Chưa gán 'critMeter' — sẽ không bao giờ có chí mạng.", this);

            BalanceConfig.TryUse(this, ApplyBalance);
        }

        private void ApplyBalance(BalanceConfig balance)
        {
            _damage = balance.Get("player.attackDamage");
            _attacksPerSecond = balance.Get("player.attacksPerSecond");
            _range = balance.Get("player.attackRange");
            _critMultiplier = balance.Get("crit.multiplier");
            _ready = true;
        }

        private void Update()
        {
            if (!_ready) return;

            // §5.3 — đang di chuyển thì KHÔNG đánh, và hồi chiêu cũng KHÔNG chạy.
            // Nếu hồi chiêu vẫn chạy khi di chuyển, người chơi chỉ cần nhấp-nhả cần gạt
            // theo đúng nhịp đánh là vừa chạy vừa giữ trọn sát thương — luật mất răng.
            if (player.IsMoving) return;

            if (_cooldown > 0f)
            {
                _cooldown -= Time.deltaTime;
                return;
            }

            IDamageable target = EnemyRegistry.Nearest(transform.position, _range);
            if (target == null) return;

            // Lấy vị trí TRƯỚC khi gây sát thương: TakeDamage có thể huỷ mục tiêu.
            Vector3 hitPosition = target.Position;

            bool isCrit = critMeter != null && critMeter.RegisterAttack();
            float amount = isCrit ? _damage * _critMultiplier : _damage;

            target.TakeDamage(amount, isCrit);

            if (popups != null) popups.Show(hitPosition, amount, isCrit);
            if (isCrit && cameraShake != null) cameraShake.Shake();

            _cooldown = _attacksPerSecond > 0f ? 1f / _attacksPerSecond : float.MaxValue;
        }
    }
}
