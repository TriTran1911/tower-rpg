using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TowerRpg.Combat;
using TowerRpg.Core;
using TowerRpg.Player;
using TowerRpg.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TowerRpg.Tests
{
    /// <summary>
    /// BẢNG BẤM GIỜ TỰ ĐỘNG — mục C của KIỂM CHỨNG trong docs/KE-HOACH-NHIP-DO.md.
    ///
    /// Kế hoạch giao việc "ghi tay 6 cột cho 10 tầng". Máy ghi được 5 trong 6 cột, nên nó
    /// ghi. Cột thứ sáu ("ghi chú cảm giác") và chỉ tiêu số 10 ("chơi 15 phút, có vui
    /// không") thì không — đó là việc của người, và không nên giả vờ là đã đo được.
    ///
    /// Cách chơi: bật TỰ ĐÁNH để nó tự leo, VÀ tiêu Mảnh mỗi tầng theo kiểu tham lam
    /// (mua ô rẻ nhất trước). Bản đầu của bộ đo này quên mất vế thứ hai, nên nó đánh boss
    /// tầng 10 bằng trang bị CẤP 1 và tất nhiên là không qua nổi — 300 giây, 0 Mảnh. Số
    /// đo sai không phải vì game sai mà vì bộ đo không chơi như người chơi.
    ///
    /// AutoBattle cố ý làm ngu (đi tới rồi đứng, không né đòn) nên số đo vẫn là CẬN DƯỚI —
    /// người chơi thật sẽ khá hơn, nhất là ở cột HP%. Đó là hướng sai an toàn cho nghiệm thu.
    /// </summary>
    public class M4BangBamGio
    {
        private const int SoTang = 10;
        // KHÔNG kéo lên 20: đã thử, và 20 tầng x ~50 giây game ở tốc độ tua 6x vẫn vượt
        // hạn 900 giây vì boss tầng 20 dày máu và AutoBattle chơi cố ý dở. Ranh giới
        // chương kiểm bằng một test riêng, nhanh hơn nhiều — xem Doi_chuong_thi_DOI_SAN.
        private const float TocDo = 6f;      // tua nhanh; 10 tầng x ~45s = 450s thật

        /// <summary>Mua ô rẻ nhất trước, tới khi không đủ Mảnh. Giáp đổi thì máu phải tính lại.</summary>
        private static void MuaThamLam(GameState gs)
        {
            while (true)
            {
                Slot? re = null;
                float reNhat = float.MaxValue;
                for (int i = 0; i < Equipment.SlotCount; i++)
                {
                    var s = (Slot)i;
                    if (gs.Gear.AtCap(s)) continue;
                    float gia = gs.Gear.NextCost(s);
                    if (gia >= 0f && gia < reNhat) { reNhat = gia; re = s; }
                }
                if (re == null || gs.Shards < reNhat) return;
                if (!gs.TryUpgrade(re.Value)) return;
                if (re.Value == Slot.Armor) PlayerHealth.Current?.Rescale();
            }
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator Bang_bam_gio_muoi_tang()
        {
            SaveSystem.Delete();
            SceneManager.LoadScene("M1", LoadSceneMode.Single);
            yield return null; yield return null;

            float w = 0f;
            while ((GameState.Instance == null || !GameState.Instance.Ready) && w < 10f)
            { w += Time.deltaTime; yield return null; }

            GameState gs = GameState.Instance;
            var hp = Object.FindFirstObjectByType<PlayerHealth>();
            var runner = Object.FindFirstObjectByType<FloorRunner>();
            var auto = Object.FindFirstObjectByType<AutoBattle>();
            var motCot = Object.FindFirstObjectByType<TowerRpg.UI.MilestoneOverlay>();
            Assert.IsNotNull(gs); Assert.IsNotNull(hp); Assert.IsNotNull(auto);

            // Mở tự đánh rồi bật: đây là cách duy nhất "chơi" được trong batchmode.
            gs.MarkCleared(auto.UnlockFloor);
            yield return null;
            Assert.IsTrue(auto.Toggle(), "không bật được tự đánh");

            Time.timeScale = TocDo;

            var hang = new List<string>();
            var nhipThuong = new List<float>();    // CHỈ tầng thường
            var nhipBoss = new List<float>();
            var lang = new List<float>();
            float hpVaoTang10 = -1f;

            float tienMoc = gs.ShardsClimbed, mauMoc = hp.Hp;
            int quaiMoc = EnemyRegistry.Count;
            float lanCuoiCoSuKien = Time.time;
            float lanCuoiNhanManh = Time.time;

            for (int tang = 1; tang <= SoTang; tang++)
            {
                // chờ đúng tầng này được bày
                float chờ = 0f;
                while (gs.Floor != tang && chờ < 60f)
                { chờ += Time.unscaledDeltaTime; yield return null; }
                if (gs.Floor != tang) break;

                // HAI ĐỒNG HỒ, hai việc khác nhau:
                //  · Time.time       = giây TRONG GAME — thứ người chơi cảm nhận, dùng để BÁO CÁO.
                //  · unscaledTime    = giây thật — chỉ dùng cho mốc chống treo, vì bộ đo
                //                      chạy ở timeScale 6 và màn cột mốc có thể đặt nó về 0.
                // Bản trước đo bằng unscaledTime và báo "8 giây mỗi tầng" — đó là 48 giây
                // game chia cho 6, tức con số đúng cho cái máy chứ không đúng cho người chơi.
                float batDau = Time.time;
                float hpVao = hp.MaxHp > 0f ? hp.Hp / hp.MaxHp : 0f;
                // ShardsClimbed chỉ TĂNG, không bao giờ giảm. gs.Shards thì trừ đi mỗi lần
                // mua nâng cấp, nên bản đầu báo "Mảnh nhận" ÂM — đo số dư ví chứ không đo
                // thu nhập. Cột này phải là thu nhập.
                float thuVao = gs.ShardsClimbed;
                bool laBoss = gs.IsBossFloor(tang);
                if (tang == 10) hpVaoTang10 = hpVao;

                float langDaiNhat = 0f;

                float treoTu = Time.unscaledTime;
                while (gs.Floor == tang && Time.unscaledTime - treoTu < 300f)
                {
                    // Màn cột mốc boss đặt Time.timeScale = 0 và chờ người chạm. Trong
                    // batchmode không có ai chạm, nên vòng lặp này phải tự đóng nó — và
                    // vòng lặp phải đo bằng unscaledTime, nếu không nó đứng hình cùng game.
                    if (motCot != null && motCot.IsOpen) motCot.Dong();

                    // Tiêu Mảnh như người chơi: mua ô RẺ NHẤT trước. Đây chính là giả định
                    // của 'Đường cong tầng'!G trong can-bang.xlsx (cấp kỳ vọng = ngân sách/4).
                    MuaThamLam(gs);

                    // "Có sự kiện" = có gì đó đổi trên màn hình: máu quái/số quái, Mảnh,
                    // hoặc máu người chơi. Đây là proxy cho "người chơi thấy điều gì đó".
                    bool suKien = false;
                    if (EnemyRegistry.Count != quaiMoc) { quaiMoc = EnemyRegistry.Count; suKien = true; }
                    if (gs.ShardsClimbed > tienMoc + 0.01f)
                    {
                        // Tầng boss có ĐÚNG MỘT con nên đúng một lần trả thưởng cho cả trận
                        // 150 giây — gộp chung vào trung bình là làm hỏng phép đo. Tách ra.
                        (laBoss ? nhipBoss : nhipThuong).Add(Time.time - lanCuoiNhanManh);
                        lanCuoiNhanManh = Time.time;
                        tienMoc = gs.ShardsClimbed; suKien = true;
                    }
                    if (!Mathf.Approximately(hp.Hp, mauMoc)) { mauMoc = hp.Hp; suKien = true; }

                    if (suKien) lanCuoiCoSuKien = Time.time;
                    else langDaiNhat = Mathf.Max(langDaiNhat, Time.time - lanCuoiCoSuKien);

                    yield return null;
                }

                float giay = Time.time - batDau;
                lang.Add(langDaiNhat);
                hang.Add($"  {tang,2}{(laBoss ? " B" : "  ")}{giay,8:F1}  " +
                         $"{gs.ShardsClimbed - thuVao,10:N0}  " +
                         $"{hpVao * 100,8:F0}%  {langDaiNhat,10:F1}");
            }

            Time.timeScale = 1f;

            // ── in bảng ──────────────────────────────────────────────────────────
            int[] capCuoi = gs.Gear.Snapshot();

            var sb = new StringBuilder();
            sb.AppendLine("\n╔══ BẢNG BẤM GIỜ — TỰ ĐÁNH LEO 10 TẦNG ════════════════════════════");
            sb.AppendLine("  tầng    giây/tầng   Mảnh thu   HP% vào   lặng dài nhất   (B = tầng boss)");
            foreach (string h in hang) sb.AppendLine(h);

            float nhipTb = 0f;
            if (nhipThuong.Count > 0)
            {
                foreach (float x in nhipThuong) nhipTb += x;
                nhipTb /= nhipThuong.Count;
            }
            float langMax = 0f;
            foreach (float x in lang) langMax = Mathf.Max(langMax, x);
            float giayTb = 0f;
            foreach (string _ in hang) { }
            
            sb.AppendLine("\n╔══ CHỈ TIÊU NGHIỆM THU ═══════════════════════════════════════════");
            void Cham(int so, string ten, string homNay, string dat, bool ok) =>
                sb.AppendLine($"  {(ok ? "✓" : "✗")} {so,2}. {ten,-38} {homNay,-14} (ngưỡng {dat})");

            float nhipBossTb = 0f;
            if (nhipBoss.Count > 0)
            {
                foreach (float x in nhipBoss) nhipBossTb += x;
                nhipBossTb /= nhipBoss.Count;
            }
            Cham(1, "nhịp trả thưởng, tầng thường (giây)", $"{nhipTb:F1}", "<= 10", nhipTb <= 10f);
            sb.AppendLine($"     · tầng boss: {nhipBossTb:F0}s cho cả trận — đúng thiết kế, " +
                          "boss là MỘT con (§5.10), không gộp vào trung bình trên");
            Cham(2, "khoảng lặng dài nhất (giây)", $"{langMax:F1}", "<= 2,0", langMax <= 2f);
            Cham(6, "HP% lúc vào tầng 10", $"{hpVaoTang10 * 100:F0}%", ">= 50%", hpVaoTang10 >= 0.5f);
            sb.AppendLine($"  ·  leo tới tầng {gs.Floor}, còn {gs.Shards:N0} Mảnh, {gs.Cores} Lõi, " +
                          $"trang bị cấp {string.Join("/", capCuoi)}");
            sb.AppendLine("  ·  chỉ tiêu 10 (\"chơi 15 phút, có vui không\") KHÔNG đo được bằng máy");
            sb.AppendLine("═══════════════════════════════════════════════════════════════════");

            Debug.Log(sb.ToString());

            // Test này ĐO, không phán. Chỉ hỏng khi nó không chơi nổi — còn các chỉ tiêu
            // thì in ra để người đọc, vì vài chỉ tiêu phụ thuộc lối chơi mà AutoBattle
            // cố ý chơi dở.
            Assert.GreaterOrEqual(hang.Count, 5,
                $"chỉ leo được {hang.Count} tầng — tự đánh không qua nổi, xem lại cân bằng");
        }
    }
}
