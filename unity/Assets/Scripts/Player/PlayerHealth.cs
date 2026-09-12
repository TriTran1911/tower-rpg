using System;
using TowerRpg.Core;
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

        /// <summary>Bắn khi người chơi chết. GameBootstrap nghe sự kiện này để bày lại đợt quái.</summary>
        public event Action Died;

        private void Awake() => Current = this;

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            Died = null;
        }

        private void Start() => BalanceConfig.TryUse(this, ApplyBalance);

        private void ApplyBalance(BalanceConfig balance)
        {
            _maxHp = balance.Get("player.maxHp");
            _hp = _maxHp;
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || _maxHp <= 0f) return;

            _hp = Mathf.Max(0f, _hp - amount);

            if (_hp <= 0f) Died?.Invoke();
        }

        public void ResetHealth() => _hp = _maxHp;
    }
}
