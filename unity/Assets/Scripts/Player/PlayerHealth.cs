using System;
using TowerRpg.Core;
using TowerRpg.Progression;
using UnityEngine;

namespace TowerRpg.Player
{
    /// <summary>
    /// Máu người chơi. Tồn tại để LUẬT CỐT LÕI §5.3 có RĂNG.
    ///
    /// Không có sát thương đi vào, "đứng yên thì đánh" là chiến thuật trội tuyệt đối và
    /// M1 không thể trả lời câu hỏi nó sinh ra để trả lời. Người chơi phải có lý do để chạy.
    /// </summary>
    public sealed class PlayerHealth : MonoBehaviour
    {
        public static PlayerHealth Current { get; private set; }

        private float _hp;
        private float _maxHp;

        public bool IsAlive => _hp > 0f;
        public float Fraction => _maxHp > 0f ? Mathf.Clamp01(_hp / _maxHp) : 0f;
        public float Hp => _hp;
        public float MaxHp => _maxHp;

        /// <summary>Bắn khi người chơi chết. GameBootstrap nghe sự kiện này để bày lại đợt quái.</summary>
        public event Action Died;

        private void Awake() => Current = this;

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            Died = null;
        }

        // Tầng đã áp vào _maxHp lần gần nhất. So sánh mỗi khung hình là cách rẻ nhất để
        // bắt MỌI đường đổi tầng (leo, chết bày lại, nạp save) mà không phải đi sửa từng chỗ.
        private int _scaledForFloor = -1;

        private void Start() => BalanceConfig.TryUse(this, _ => Rescale());

        /// <summary>Máu tối đa phụ thuộc tầng và Giáp — gọi lại mỗi khi hai thứ đó đổi.</summary>
        public void Rescale()
        {
            PlayerStats st = PlayerStats.Instance;
            if (st == null || !st.Ready) return;      // Update sẽ thử lại

            float old = _maxHp;
            _maxHp = st.MaxHp;
            _hp = old > 0f ? Mathf.Clamp(_hp / old * _maxHp, 1f, _maxHp) : _maxHp;  // giữ nguyên TỈ LỆ máu
            _scaledForFloor = GameState.Instance != null ? GameState.Instance.Floor : -1;
        }

        // KHÔNG tin thứ tự Start: nếu PlayerStats chưa sẵn sàng lúc Start thì máu tối đa
        // sẽ là 0 và người chơi đọc như đã chết — quái sẽ không thèm đánh. Thử lại tới khi được.
        private void Update()
        {
            if (_maxHp <= 0f) { Rescale(); return; }

            // VAN AN TOÀN #4 CỦA §5.8 TỪNG IM LẶNG KHÔNG CHẠY.
            // PlayerStats.MaxHp có sẵn thừa số (1 + hpPerFloor)^(tầng-1), nhưng PlayerHealth
            // CACHE _maxHp và trước đây chỉ gọi lại Rescale lúc Start / mua Giáp / tẩy điểm /
            // đổi nhân vật — KHÔNG BAO GIỜ khi lên tầng. Ai leo một mạch tới tầng 10 mà không
            // mở màn nâng cấp thì đánh boss bằng máu của tầng 1: mất trọn 1,045^9 = 1,486 lần.
            // Lỗi này ẩn kỹ vì mua Giáp có gọi Rescale, nên người chơi hay mua sẽ không thấy.
            // Đo được: biên boss 1 là 0,51 thay vì 0,75 — cả bốn boss đều dưới 1,00.
            if (GameState.Instance != null && GameState.Instance.Ready &&
                GameState.Instance.Floor != _scaledForFloor)
                Rescale();
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || _maxHp <= 0f) return;

            _hp = Mathf.Max(0f, _hp - amount);
            // Nhỏ hơn hẳn tiếng đánh trúng: đây là tin xấu nền, không phải sự kiện chính.
            // SfxPlayer tự chặn tiếng này ở 0,15 giây/lần nên nhiều quái không thành nhiễu.
            Juice.SfxPlayer.Play(Juice.Sfx.PlayerHit, 0.4f);

            if (_hp <= 0f) Died?.Invoke();
        }

        public void ResetHealth() => _hp = _maxHp;
    }
}
