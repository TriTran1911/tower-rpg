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
                return d;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Đọc '{path}' thất bại: {e.Message}");
                return null;
            }
        }

        /// <summary>Xoá sạch tiến trình. Dùng cho test và nút chơi lại.</summary>
        public static void Delete()
        {
            foreach (string p in new[] { Path, TempPath, BackPath })
                try { if (File.Exists(p)) File.Delete(p); } catch { /* không quan trọng */ }
        }
    }
}
