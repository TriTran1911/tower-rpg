using TowerRpg.Combat;
using TowerRpg.Core;
using TowerRpg.Juice;
using TowerRpg.Progression;
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
        [SerializeField] private PlayerHealth health;
        [SerializeField] private DamagePopupSpawner popups;
        [SerializeField] private CameraShake cameraShake;
        [SerializeField] private Juice.SlashFxSpawner slashes;
        [SerializeField] private PlayerAnimator animator;

        private float _range;

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

            if (health == null) health = PlayerHealth.Current;
            if (health == null)
                Debug.LogError("[AutoAttack] Không tìm thấy PlayerHealth — hút máu sẽ không chạy.", this);

            BalanceConfig.TryUse(this, ApplyBalance);
        }

        private void ApplyBalance(BalanceConfig balance)
        {
            _range = balance.Get("player.attackRange");
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

            // Chỉ số lấy từ PlayerStats — MỘT chỗ tính duy nhất, gồm cả hệ số trang bị (§5.7)
            PlayerStats st = PlayerStats.Instance;
            if (st == null || !st.Ready) return;

            // TUNG XÚC XẮC (quyết định #40). Trước đây là thanh dồn: cứ đúng 5 đòn thì
            // đòn thứ 5 chắc chắn chí mạng, không có xác suất nào trong cả game.
            // Chủ dự án đổi hướng: chí mạng giờ hên xui, và cả tỉ lệ lẫn hệ số đều do
            // VŨ KHÍ quyết định.
            bool isCrit = Random.value < st.CritChance;
            float amount = isCrit ? st.Damage * st.CritMultiplier : st.Damage;

            target.TakeDamage(amount, isCrit);

            // HÚT MÁU (ô Nhẫn). Hồi theo sát thương THẬT SỰ GÂY RA, nên đòn chí mạng
            // hồi nhiều hơn — đó là cách nó nối vào trục ngẫu nhiên mới.
            float hut = st.Lifesteal;
            if (hut > 0f && health != null) health.Heal(amount * hut);

            // Chí mạng dùng tiếng CHÉM sắc (độ sắc đo được 0,448) còn đòn thường dùng tiếng
            // ĐẤM đục (0,065). Chênh lệch đó là chủ ý: §5.4 chọn thanh dồn XÁC ĐỊNH để người
            // chơi ĐẾM được, nên đòn thứ 5 phải nghe khác hẳn chứ không chỉ to hơn.
            Juice.SfxPlayer.Play(isCrit ? Juice.Sfx.Crit : Juice.Sfx.Hit);

            if (animator != null) animator.BaoDanh(hitPosition - transform.position);
            if (slashes != null) slashes.Play(hitPosition, isCrit);
            if (popups != null) popups.Show(hitPosition, amount, isCrit);
            if (isCrit && cameraShake != null) cameraShake.Shake();

            _cooldown = st.AttacksPerSec > 0f ? 1f / st.AttacksPerSec : float.MaxValue;
        }
    }
}
