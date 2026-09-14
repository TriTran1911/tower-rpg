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

        private float _baseDmg, _baseAs, _baseHp, _hpPerFloor;
        private float _critP1, _critPg, _critPCap, _critM1, _critMg;
        private float _hutPerLevel, _hutCap;
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
            _critP1   = b.Get("crit.chance1");
            _critPg   = b.Get("crit.chanceGrowth");
            _critPCap = b.Get("crit.chanceCap");
            _critM1   = b.Get("crit.mult1");
            _critMg   = b.Get("crit.multGrowth");
            _hutPerLevel = b.Get("lifesteal.perLevel");
            _hutCap      = b.Get("lifesteal.cap");
            Ready = true;
        });

        private Equipment Gear => GameState.Instance != null ? GameState.Instance.Gear : null;
        private int Floor => GameState.Instance != null ? GameState.Instance.Floor : 1;

        private int CharIndex => GameState.Instance != null ? GameState.Instance.CharacterIndex : 0;

        /// <summary>Hệ số k của nhân vật (§5.5b). Nhân vào sát thương...</summary>
        private float CharDamage => CharacterRoster.DamageMult(CharIndex);

        /// <summary>...và chia vào máu. Tích CharDamage × CharHealth luôn = 1, nên biên
        /// an toàn ở §5.10 không đổi một chữ số dù đổi nhân vật nào.</summary>
        private float CharHealth => CharacterRoster.HealthMult(CharIndex);

        public float Damage         => _baseDmg * (Gear?.Mult(Slot.Weapon) ?? 1f) * CharDamage;
        public float AttacksPerSec  => _baseAs  * (Gear?.Mult(Slot.Glove)  ?? 1f);
        // ── CHÍ MẠNG NGẪU NHIÊN, DO VŨ KHÍ ĐIỀU KHIỂN (quyết định #40) ───────────
        // Thay hẳn thanh dồn của §5.4. CẢ HAI con số đều theo cấp VŨ KHÍ, nên nâng
        // Vũ khí vừa làm đòn thường đau hơn, vừa làm chí mạng đến nhiều hơn VÀ đau hơn.
        //
        // Bộ số không chọn bằng cảm tính: khớp vào đúng đường cong nhân-DPS cũ
        // F(L) = 0,8 + 0,4 × 1,03441^(L−1) với lệch tối đa 2,46%, nên mười hệ số máu
        // boss của §5.10 giữ nguyên giá trị. Kiểm bằng mô phỏng leo hết 100 tầng:
        // biên nhỏ nhất 1,55.
        private int WeaponLevel => Gear?.Level(Slot.Weapon) ?? 1;

        /// <summary>Tỉ lệ chí mạng 0..1. CÓ TRẦN — chí mạng thường quá thì hết là chí mạng.</summary>
        public float CritChance =>
            Mathf.Min(_critPCap, _critP1 * Mathf.Pow(1f + _critPg, WeaponLevel - 1));

        public float CritMultiplier => _critM1 * Mathf.Pow(1f + _critMg, WeaponLevel - 1);

        /// <summary>
        /// Hút máu (ô Nhẫn): hồi % sát thương GÂY RA. Tuyến tính theo cấp, CÓ TRẦN.
        ///
        /// VÌ SAO PHẢI CÓ TRẦN, và vì sao trần thấp: đây là vòng lặp phản hồi — DPS
        /// càng cao thì hồi càng nhiều, nên ngưỡng "hồi nhanh hơn mất" (bất tử, biên vô
        /// cực) phải đo bằng build MẠNH NHẤT mua nổi chứ không phải build trung bình.
        /// Ở trần 1,20% còn cách ngưỡng đó 2,15 lần; ở 3% thì build dồn Vũ khí BẤT TỬ.
        /// </summary>
        public float Lifesteal =>
            Mathf.Min(_hutCap, _hutPerLevel * Mathf.Max(0, (Gear?.Level(Slot.Ring) ?? 1) - 1));

        /// <summary>Máu nền tăng theo tầng ĐÃ QUA — van an toàn #4 của §5.8 — rồi nhân Giáp.</summary>
        public float MaxHp =>
            _baseHp * Mathf.Pow(1f + _hpPerFloor, Mathf.Max(0, Floor - 1))
                    * (Gear?.Mult(Slot.Armor) ?? 1f) * CharHealth;
    }
}
