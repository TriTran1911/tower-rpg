using System;
using TowerRpg.Core;
using UnityEngine;

namespace TowerRpg.Progression
{
    /// <summary>Bốn ô trang bị của §5.5. Thứ tự KHÔNG được đổi — nó là thứ tự lưu vào save.</summary>
    public enum Slot
    {
        Weapon = 0,   // Vũ khí — sát thương
        Armor  = 1,   // Giáp   — máu (+ hút máu, chưa cài)
        Glove  = 2,   // Găng   — tốc độ đánh
        Ring   = 3,   // Nhẫn   — hệ số chí mạng
    }

    /// <summary>
    /// Trang bị: bốn ô, mỗi ô một cấp, nhân vào bốn thừa số khác nhau của công thức §5.7.
    ///
    /// ⚠️ CHÚ THÍCH CŨ Ở ĐÂY GHI "Không có ô nào tốt hơn ô nào" — SAI, VÀ ĐO ĐƯỢC LÀ SAI.
    /// Cùng một cái giá 300 Mảnh cho lần nâng đầu: Vũ khí +4,40% DPS · Găng +3,44% ·
    /// Nhẫn +1,15% (vì hệ số chí mạng chỉ vào công thức qua 1 + (cm-1)/meterSize, tức bị
    /// chia cho 5). Nhẫn đắt gấp 3,8 lần Vũ khí trên mỗi phần trăm sức mạnh — NGAY TỪ
    /// TẦNG 1, không phải chỉ ở tầng 100 như §5.5 thú nhận.
    /// Chính câu sai này là lý do suốt từ M2 không ai đi kiểm lại ô Nhẫn.
    ///
    /// Từ M3 mỗi ô có TRẦN RIÊNG, nâng bằng Lõi: trần = maxLevel + bậc × capStep.
    /// Đó là toàn bộ cuộc chơi phân bổ ở §5.6 — Lõi hữu hạn tuyệt đối, tiêu vào ô nào
    /// là đóng cánh cửa của ô kia.
    /// </summary>
    [Serializable]
    public sealed class Equipment
    {
        public const int SlotCount = 4;

        [SerializeField] private int[] levels = { 1, 1, 1, 1 };
        [SerializeField] private int[] tiers  = { 0, 0, 0, 0 };   // bậc đột phá từng ô

        private float _costBase, _costGrowth;
        private float[] _perLevel = new float[SlotCount];
        private int _maxLevel = 1;
        private int _capStep = 10;
        private float[] _gateCost = new float[MaxTier];

        /// <summary>Số bậc đột phá tối đa — năm cổng của §5.6.</summary>
        public const int MaxTier = 5;

        /// <summary>Trần NỀN, khi chưa đột phá. Giữ tên cũ vì test và giao diện đang dùng.</summary>
        public int MaxLevel => _maxLevel;

        public int Tier(Slot s) => tiers[(int)s];

        /// <summary>Trần thật của một ô — cái quyết định AtCap.</summary>
        public int CapOf(Slot s) => _maxLevel + tiers[(int)s] * _capStep;

        public bool AtMaxTier(Slot s) => tiers[(int)s] >= MaxTier;

        /// <summary>Lõi cần cho lần đột phá kế tiếp của ô. Trả về -1 khi đã hết cổng.</summary>
        public int NextTierCost(Slot s) =>
            AtMaxTier(s) ? -1 : Mathf.RoundToInt(_gateCost[tiers[(int)s]]);

        public bool Breakthrough(Slot s)
        {
            if (AtMaxTier(s)) return false;
            tiers[(int)s]++;
            return true;
        }

        /// <summary>Tổng Lõi đã tiêu — tẩy điểm hoàn lại đúng chừng này (§5.8 van 2).</summary>
        public int CoresSpent()
        {
            int total = 0;
            for (int i = 0; i < SlotCount; i++)
                for (int t = 0; t < tiers[i]; t++)
                    total += Mathf.RoundToInt(_gateCost[t]);
            return total;
        }

        /// <summary>Hạ mọi bậc về 0 và ép cấp về trần nền. Cấp vượt trần bị CẮT, không hoàn Mảnh.</summary>
        public void ResetTiers()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                tiers[i] = 0;
                levels[i] = Mathf.Clamp(levels[i], 1, _maxLevel);
            }
        }

        public int[] TierSnapshot() => (int[])tiers.Clone();

        public void RestoreTiers(int[] saved)
        {
            if (saved == null || saved.Length != SlotCount) return;
            for (int i = 0; i < SlotCount; i++) tiers[i] = Mathf.Clamp(saved[i], 0, MaxTier);
        }

        public void Configure(BalanceConfig b)
        {
            _maxLevel   = Mathf.Max(1, b.GetInt("gear.maxLevel"));
            _capStep    = Mathf.Max(1, b.GetInt("core.capStep"));
            for (int t = 0; t < MaxTier; t++) _gateCost[t] = b.Get($"core.gate{t + 1}");
            _costBase   = b.Get("gear.costBase");
            _costGrowth = b.Get("gear.costGrowth");

            _perLevel[(int)Slot.Weapon] = b.Get("gear.weapon.perLevel");
            _perLevel[(int)Slot.Armor]  = b.Get("gear.armor.perLevel");
            _perLevel[(int)Slot.Glove]  = b.Get("gear.glove.perLevel");
            _perLevel[(int)Slot.Ring]   = b.Get("gear.ring.perLevel");

            // §5.5 khoá bất biến này: ba nấc Giáp 6/10/15 chỉ bằng nhau khi dmgL == hpL.
            if (!Mathf.Approximately(_perLevel[(int)Slot.Weapon], _perLevel[(int)Slot.Armor]))
                Debug.LogWarning("[Equipment] gear.weapon.perLevel != gear.armor.perLevel. " +
                                 "§5.5 dựa trên việc hai số này BẰNG NHAU — hoán vị Lõi giữa Vũ khí " +
                                 "và Giáp sẽ không còn cho biên như nhau.");

            for (int i = 0; i < SlotCount; i++) levels[i] = Mathf.Clamp(levels[i], 1, CapOf((Slot)i));
        }

        public int Level(Slot s) => levels[(int)s];
        public bool AtCap(Slot s) => levels[(int)s] >= CapOf(s);

        /// <summary>Mảnh để lên cấp kế tiếp. Trả về -1 khi đã chạm trần.</summary>
        public float NextCost(Slot s)
        {
            if (AtCap(s)) return -1f;
            return _costBase * Mathf.Pow(1f + _costGrowth, levels[(int)s] - 1);
        }

        public bool Upgrade(Slot s)
        {
            if (AtCap(s)) return false;
            levels[(int)s]++;
            return true;
        }

        /// <summary>Hệ số nhân của một ô: (1 + tăng trưởng)^(cấp - 1).</summary>
        public float Mult(Slot s) => Mathf.Pow(1f + _perLevel[(int)s], levels[(int)s] - 1);

        /// <summary>Hệ số của ô nếu nó ở cấp cho trước — để giao diện xem trước cấp kế tiếp.</summary>
        public float MultAtLevel(Slot s, int level) =>
            Mathf.Pow(1f + _perLevel[(int)s], Mathf.Max(1, level) - 1);

        public int[] Snapshot() => (int[])levels.Clone();

        public void Restore(int[] saved)
        {
            if (saved == null || saved.Length != SlotCount) return;
            for (int i = 0; i < SlotCount; i++) levels[i] = Mathf.Max(1, saved[i]);
        }

        public static string DisplayName(Slot s) => s switch
        {
            Slot.Weapon => "Vũ khí",
            Slot.Armor  => "Giáp",
            Slot.Glove  => "Găng",
            Slot.Ring   => "Nhẫn",
            _           => s.ToString(),
        };

        public static string StatName(Slot s) => s switch
        {
            Slot.Weapon => "Sát thương",
            Slot.Armor  => "Máu tối đa",
            Slot.Glove  => "Tốc độ đánh",
            Slot.Ring   => "Hệ số chí mạng",
            _           => "",
        };
    }
}
