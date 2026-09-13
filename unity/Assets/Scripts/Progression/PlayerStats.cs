using TowerRpg.Core;
using UnityEngine;

namespace TowerRpg.Progression
{
    /// <summary>
    /// Chỉ số cuối cùng của người chơi = chỉ số nền × hệ số trang bị (§5.7).
    ///
    /// Một chỗ DUY NHẤT tính ra chúng. Trước M2, AutoAttack và PlayerHealth mỗi bên tự
    /// đọc CSV rồi giữ riêng — thêm trang bị vào là hai bên lệch nhau ngay.
    /// </summary>
    [DefaultExecutionOrder(-50)]   // phải cấu hình TRƯỚC mọi thứ đọc nó
    public sealed class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; private set; }

        private float _baseDmg, _baseAs, _baseHp, _hpPerFloor, _baseCrit;
        public bool Ready { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Start() => BalanceConfig.TryUse(this, b =>
        {
            _baseDmg    = b.Get("player.attackDamage");
            _baseAs     = b.Get("player.attacksPerSecond");
            _baseHp     = b.Get("player.maxHp");
            _hpPerFloor = b.Get("player.hpPerFloor");
            _baseCrit   = b.Get("crit.multiplier");
            Ready = true;
        });

        private Equipment Gear => GameState.Instance != null ? GameState.Instance.Gear : null;
        private int Floor => GameState.Instance != null ? GameState.Instance.Floor : 1;

        public float Damage         => _baseDmg * (Gear?.Mult(Slot.Weapon) ?? 1f);
        public float AttacksPerSec  => _baseAs  * (Gear?.Mult(Slot.Glove)  ?? 1f);
        public float CritMultiplier => _baseCrit * (Gear?.Mult(Slot.Ring)  ?? 1f);

        /// <summary>Máu nền tăng theo tầng ĐÃ QUA — van an toàn #4 của §5.8 — rồi nhân Giáp.</summary>
        public float MaxHp =>
            _baseHp * Mathf.Pow(1f + _hpPerFloor, Mathf.Max(0, Floor - 1)) * (Gear?.Mult(Slot.Armor) ?? 1f);
    }
}
