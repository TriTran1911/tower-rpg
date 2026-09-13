using TowerRpg.Core;
using UnityEngine;

namespace TowerRpg.Progression
{
    /// <summary>
    /// Danh sách nhân vật của §5.5b. MỘT nhân vật trên màn hình — đây không phải đội hình.
    ///
    /// Luật sống còn của cả hệ thống: mỗi nhân vật nhân sát thương lên `k` và chia máu
    /// cho `k`. Biên an toàn ở §5.10 tỉ lệ với TÍCH sát thương × máu, mà tích ấy là
    /// k × (1/k) = 1 — không đổi. Nhờ vậy thêm bao nhiêu nhân vật cũng KHÔNG đụng một
    /// dòng nào của can-bang.xlsx.
    ///
    /// Ai định thêm "nhân vật này còn +10% chí mạng nữa cho thú vị": đó chính là thứ
    /// phá vỡ tính chất trên và kéo theo phải cân bằng lại toàn bộ bảng boss.
    /// </summary>
    public static class CharacterRoster
    {
        public const int Count = 5;

        /// <summary>Tên hiển thị — khớp bảng ở §5.5b.</summary>
        public static readonly string[] Names =
            { "Cân bằng", "Kiếm sĩ", "Sát thủ", "Vệ binh", "Tăng" };

        /// <summary>Thư mục sprite trong Assets/Art/NinjaAdventure-MucSon/Actor/Character/.</summary>
        public static readonly string[] ArtFolders =
            { "NinjaGreen", "Samurai", "NinjaBlue", "Knight", "Monk" };

        public static readonly string[] Blurbs =
        {
            "Trung dung. Không nhanh, không dày.",
            "Nhỉnh sát thương, bớt chút máu.",
            "Giết chớp nhoáng — sai một nhịp là chết.",
            "Dày hơn, chậm hơn. Đứng được lâu.",
            "Chậm mà chắc. Tha thứ sai lầm.",
        };

        private static readonly float[] K = new float[Count];
        private static bool _loaded;

        public static void Configure(BalanceConfig b)
        {
            for (int i = 0; i < Count; i++) K[i] = b.Get($"character.k{i}");
            _loaded = true;
        }

        /// <summary>Hệ số k. Sát thương ×k, máu ÷k — tích không đổi.</summary>
        public static float DamageMult(int index) =>
            _loaded && index >= 0 && index < Count ? K[index] : 1f;

        public static float HealthMult(int index)
        {
            float k = DamageMult(index);
            return k > 0f ? 1f / k : 1f;
        }

        /// <summary>Nhân vật mở khoá theo số boss đã hạ: nhân vật 0 có sẵn, n mở sau boss n.</summary>
        public static bool IsUnlocked(int index, int bossesKilled) =>
            index >= 0 && index < Count && index <= bossesKilled;

        public static string Name(int i) => i >= 0 && i < Count ? Names[i] : "?";
    }
}
