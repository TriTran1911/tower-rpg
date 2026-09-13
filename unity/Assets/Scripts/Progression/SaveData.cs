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
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;
        public int floor = 1;           // tầng đang đứng
        public float shards;            // Mảnh đang có
        public int[] gearLevels = { 1, 1, 1, 1 };   // thứ tự theo enum Slot

        // ── M3 ────────────────────────────────────────────────────────────────────
        public int cores;               // Lõi chưa tiêu
        public int[] gearTiers = { 0, 0, 0, 0 };    // bậc đột phá từng ô
        public int bossesKilled;        // boss đã hạ lần đầu — quyết định Lõi VÀ nhân vật
        public int highestCleared;      // tầng cao nhất từng dọn sạch — mốc mở quét nhanh
        public int respecs;             // số lần đã tẩy điểm, để tính giá lần sau
        public int characterIndex;      // nhân vật đang dùng (§5.5b)
    }
}
