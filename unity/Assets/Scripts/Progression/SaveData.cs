using System;

namespace TowerRpg.Progression
{
    /// <summary>
    /// Toàn bộ tiến trình người chơi. Phẳng và đơn giản có chủ ý — JsonUtility
    /// không xử lý được Dictionary hay kiểu lồng phức tạp.
    ///
    /// ĐỔI CẤU TRÚC LÀ PHẢI TĂNG version và xử lý bản cũ trong SaveSystem.
    /// Người chơi mất 20 giờ tiến trình sẽ gỡ game và không quay lại (§4).
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public int version = 1;
        public int floor = 1;           // tầng cao nhất đã mở
        public float shards;            // Mảnh đang có
        public int[] gearLevels = { 1, 1, 1, 1 };   // thứ tự theo enum Slot
    }
}
