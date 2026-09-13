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
    /// Trang bị: bốn ô, mỗi ô một cấp. Không có ô nào "tốt hơn" ô nào — chúng nhân vào
    /// bốn thừa số khác nhau của công thức ở §5.7.
    ///
    /// M2 chưa có Lõi nên mọi ô đều chung một trần (`gear.maxLevel`). Từ M3, Lõi nâng
    /// trần riêng cho từng ô — đó là toàn bộ cuộc chơi phân bổ ở §5.6.
    /// </summary>
    [Serializable]
    public sealed class Equipment
    {
        public const int SlotCount = 4;

        [SerializeField] private int[] levels = { 1, 1, 1, 1 };

        private float _costBase, _costGrowth;
        private float[] _perLevel = new float[SlotCount];
        private int _maxLevel = 1;

        public int MaxLevel => _maxLevel;

        public void Configure(BalanceConfig b)
        {
            _maxLevel   = Mathf.Max(1, b.GetInt("gear.maxLevel"));
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

            for (int i = 0; i < SlotCount; i++) levels[i] = Mathf.Clamp(levels[i], 1, _maxLevel);
        }

        public int Level(Slot s) => levels[(int)s];
        public bool AtCap(Slot s) => levels[(int)s] >= _maxLevel;

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
