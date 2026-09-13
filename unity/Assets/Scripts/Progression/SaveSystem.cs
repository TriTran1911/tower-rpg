using System;
using System.IO;
using UnityEngine;

namespace TowerRpg.Progression
{
    /// <summary>
    /// Ghi save NGUYÊN TỬ (§4, §9.4). Hỏng save là rủi ro thật duy nhất của dự án:
    /// người chơi mất 20 giờ tiến trình sẽ gỡ game và không quay lại.
    ///
    /// Cách làm: ghi ra .tmp -> File.Replace(tmp, chính, .bak).
    /// File.Replace là thao tác nguyên tử của hệ điều hành VÀ để lại một bản backup
    /// miễn phí. Không bao giờ tồn tại trạng thái "ghi dở".
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName = "tien-trinh.json";

        private static string Path      => System.IO.Path.Combine(Application.persistentDataPath, FileName);
        private static string TempPath  => Path + ".tmp";
        private static string BackPath  => Path + ".bak";

        public static bool Save(SaveData data)
        {
            if (data == null) return false;

            try
            {
                File.WriteAllText(TempPath, JsonUtility.ToJson(data, true));

                if (File.Exists(Path))
                    File.Replace(TempPath, Path, BackPath);   // nguyên tử + tự backup
                else
                    File.Move(TempPath, Path);

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Ghi save thất bại: {e.Message}");
                return false;
            }
        }

        /// <summary>Nạp save. Hỏng file chính thì thử bản backup trước khi bỏ cuộc.</summary>
        public static SaveData Load()
        {
            SaveData d = TryRead(Path);
            if (d != null) return d;

            d = TryRead(BackPath);
            if (d != null)
            {
                Debug.LogWarning("[SaveSystem] File chính hỏng, đã khôi phục từ bản backup.");
                return d;
            }

            return new SaveData();   // người chơi mới
        }

        private static SaveData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (d == null || d.version <= 0) return null;
                if (d.gearLevels == null || d.gearLevels.Length != Equipment.SlotCount) return null;
                return Migrate(d);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Đọc '{path}' thất bại: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Nâng save cũ lên cấu trúc hiện tại. Người chơi M2 mở bản M3 phải giữ nguyên
        /// tiến trình — mất 20 giờ cày là gỡ game (§4).
        ///
        /// v1 -> v2: chưa có Lõi nên mọi trường M3 để 0, TRỪ highestCleared: người chơi
        /// v1 đã đi tới `floor` nên các tầng dưới đó coi như đã dọn, quét nhanh mở luôn.
        /// </summary>
        private static SaveData Migrate(SaveData d)
        {
            if (d.version >= SaveData.CurrentVersion) return d;

            if (d.version < 2)
            {
                d.gearTiers = new[] { 0, 0, 0, 0 };
                d.cores = 0;
                d.bossesKilled = 0;
                d.respecs = 0;
                d.characterIndex = 0;
                d.highestCleared = Mathf.Max(0, d.floor - 1);
                Debug.Log($"[SaveSystem] Nâng save v{d.version} -> v2. Giữ nguyên tầng " +
                          $"{d.floor}, {d.shards:0} Mảnh, cấp {string.Join("/", d.gearLevels)}.");
            }

            d.version = SaveData.CurrentVersion;
            return d;
        }

        /// <summary>Xoá sạch tiến trình. Dùng cho test và nút chơi lại.</summary>
        public static void Delete()
        {
            foreach (string p in new[] { Path, TempPath, BackPath })
                try { if (File.Exists(p)) File.Delete(p); } catch { /* không quan trọng */ }
        }
    }
}
