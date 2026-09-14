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
        public const int CurrentVersion = 3;

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

        // Tách nguồn Mảnh để áp trần quét ('Thông số'!B32 = 2). Phải LƯU, vì trần tính
        // trên tổng tích luỹ cả đời chứ không phải trong một phiên.
        public float shardsClimbed;     // Mảnh kiếm từ LEO (dọn tầng)
        public float shardsSwept;       // Mảnh kiếm từ QUÉT

        // "Hũ Mảnh" của tầng đang chơi dở. Đường rò thứ tư của Việc 5: thoát app giữa
        // tầng mà không lưu hai số này thì mở lại game là hũ đầy lại, và lượt tầng đó
        // trả thưởng hai lần. KHÔNG cần tăng version: bản v3 cũ nạp lên có paidFloor = 0,
        // mà SpawnFloor coi "tầng khác với tầng đang tính sổ" là bắt đầu lượt mới — đúng.
        public int paidFloor;           // tầng đang tính sổ
        public float paidShards;        // Mảnh của tầng đó ĐÃ vào ví trong cả lượt

        // ── M5 ────────────────────────────────────────────────────────────────────
        // Tổng giây đã chơi, cộng dồn qua mọi phiên. Màn hình đỉnh tháp không có con số
        // nào đáng giá hơn con số này — nó là thứ duy nhất đo được cái §1 hứa: khoảng
        // cách định bởi chăm chỉ. KHÔNG cần tăng version, cùng lý lẽ với paidFloor: bản
        // v3 cũ nạp lên có playSeconds = 0, và màn hình đỉnh tháp BỎ HẲN dòng thời gian
        // khi số đó bằng 0 — thà không nói còn hơn nói "0 phút" với người vừa leo 100 tầng.
        public float playSeconds;
    }
}
