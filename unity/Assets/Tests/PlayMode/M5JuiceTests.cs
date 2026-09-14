using System.Collections;
using NUnit.Framework;
using TowerRpg.Combat;
using TowerRpg.Core;
using TowerRpg.Juice;
using TowerRpg.Progression;
using TowerRpg.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TowerRpg.Tests
{
    /// <summary>
    /// M5 — BÓNG BẨY. §8 giao "hiệu ứng, rung màn hình, âm thanh, chuyển cảnh, màn hình
    /// chúc mừng" và cảnh báo đây là phần người xem portfolio đánh giá đầu tiên.
    ///
    /// Mọi thứ M5 thêm vào đều THUẦN TRANG TRÍ, và đó chính là lý do phải có test: một
    /// hiệu ứng hỏng không làm sai một con số nào, nên không lớp kiểm nào khác thấy nó.
    /// Hai kiểu hỏng thật sự nguy hiểm ở mốc này, cả hai đều biến trò chơi thành KHÔNG
    /// CHƠI ĐƯỢC chứ không phải xấu đi:
    ///
    ///   1. TIMESCALE KẸT Ở 0 — dự án này đã bị cắn BA LẦN (bảng bấm giờ, vòng chờ của
    ///      phiên chơi thử, và chính màn cột mốc). M5 thêm overlay thứ hai cũng đặt
    ///      timeScale = 0, tức thêm một đường nữa để kẹt.
    ///   2. TẤM PHỦ KẸT Ở ĐEN — chuyển cảnh không mở màn là màn hình đen vĩnh viễn.
    ///      Game vẫn chạy, test số vẫn xanh, người chơi thì nhìn vào một tấm đen.
    /// </summary>
    public class M5JuiceTests
    {
        private GameState _gs;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            SaveSystem.Delete();
            SceneManager.LoadScene("M1", LoadSceneMode.Single);
            yield return null; yield return null;

            float t = 0f;
            while ((GameState.Instance == null || !GameState.Instance.Ready) && t < 10f)
            { t += Time.deltaTime; yield return null; }
            yield return null;
            _gs = GameState.Instance;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            SaveSystem.Delete();
        }

        // ── MÀN HÌNH CHÚC MỪNG ────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Man_dinh_thap_dung_game_roi_TRA_LAI_timeScale()
        {
            var vc = Object.FindFirstObjectByType<VictoryScreen>();
            Assert.IsNotNull(vc, "không có VictoryScreen trong scene");
            yield return null;

            Assert.AreEqual(1f, Time.timeScale, 0.001f);
            vc.Show();
            yield return null;

            Assert.IsTrue(vc.IsOpen, "gọi Show mà màn hình không mở");
            Assert.AreEqual(0f, Time.timeScale, 0.001f, "màn đỉnh tháp phải DỪNG trò chơi");

            // Khoá chạm 1,2 giây: người chơi vừa hạ boss cuối đang bấm liên tục sẽ đóng
            // mất bảng tổng kết trước khi đọc được dòng nào.
            vc.Dong();
            Assert.IsTrue(vc.IsOpen, "đóng được ngay lập tức — khoá chạm không chạy");

            float t = 0f;
            while (t < 1.4f) { t += Time.unscaledDeltaTime; yield return null; }

            vc.Dong();
            Assert.IsFalse(vc.IsOpen);
            Assert.AreEqual(1f, Time.timeScale, 0.001f,
                "ĐÓNG MÀN HÌNH MÀ KHÔNG TRẢ timeScale — cả trò chơi đứng hình vĩnh viễn");
        }

        [UnityTest]
        public IEnumerator Tat_man_dinh_thap_giua_chung_van_tra_timeScale()
        {
            // Thoát scene lúc overlay đang mở. Không có OnDisable trả timeScale thì
            // scene sau nạp lên với timeScale = 0 và không gì nhúc nhích.
            var vc = Object.FindFirstObjectByType<VictoryScreen>();
            Assert.IsNotNull(vc);
            yield return null;

            vc.Show();
            yield return null;
            Assert.AreEqual(0f, Time.timeScale, 0.001f);

            vc.enabled = false;
            vc.gameObject.SetActive(false);
            yield return null;

            Assert.AreEqual(1f, Time.timeScale, 0.001f,
                "tắt component lúc đang mở mà không trả timeScale");
        }

        [UnityTest]
        public IEnumerator Bang_tong_ket_in_so_THAT_chu_khong_in_rong()
        {
            var vc = Object.FindFirstObjectByType<VictoryScreen>();
            Assert.IsNotNull(vc);
            yield return null;

            _gs.AddShards(1234f);
            yield return null;

            vc.Show();
            yield return null;

            TMPro.TMP_Text stats = null;
            foreach (TMPro.TMP_Text t in vc.GetComponentsInChildren<TMPro.TMP_Text>(true))
                if (t.name == "Stats") stats = t;
            Assert.IsNotNull(stats, "không thấy ô Stats");

            // Đây chính là kiểu hỏng của quyết định #29: nối đúng, không lỗi, chữ rỗng.
            Assert.IsNotEmpty(stats.text, "bảng tổng kết RỖNG");
            StringAssert.Contains("Boss đã hạ", stats.text);
            StringAssert.Contains("Trang bị", stats.text);
            StringAssert.Contains("<pos=", stats.text, "cột số không căn — sẽ răng cưa");

            Time.timeScale = 1f;
        }

        [Test]
        public void Doc_gio_khong_bao_gio_in_0_phut()
        {
            // "0 phút" hiện ra với người vừa leo 100 tầng thì đọc như một lỗi.
            Assert.AreEqual("45 giây",       VictoryScreen.DocGio(45f));
            Assert.AreEqual("3 phút",        VictoryScreen.DocGio(200f));
            Assert.AreEqual("1 giờ 0 phút",  VictoryScreen.DocGio(3600f));
            Assert.AreEqual("2 giờ 41 phút", VictoryScreen.DocGio(9660f));
            Assert.AreEqual("0 giây",        VictoryScreen.DocGio(-5f), "số âm phải kẹp về 0");
        }

        [UnityTest]
        public IEnumerator Gio_choi_cong_don_va_song_qua_mot_lan_luu()
        {
            float truoc = _gs.PlaySeconds;
            float t = 0f;
            while (t < 0.4f) { t += Time.unscaledDeltaTime; yield return null; }

            Assert.Greater(_gs.PlaySeconds, truoc, "giờ chơi không nhích — Update không đếm");

            _gs.Save();
            SaveData d = SaveSystem.Load();
            Assert.AreEqual(_gs.PlaySeconds, d.playSeconds, 0.05f,
                "giờ chơi không đi vào save — mở lại game là mất sạch");
        }

        // ── CHUYỂN CẢNH ───────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Tam_phu_LUON_tro_ve_trong_suot()
        {
            var tr = Object.FindFirstObjectByType<SceneTransition>();
            Assert.IsNotNull(tr, "không có SceneTransition trong scene");
            Image veil = null;
            foreach (Image i in tr.GetComponentsInChildren<Image>(true))
                if (i.name == "Veil") veil = i;
            Assert.IsNotNull(veil, "không thấy tấm phủ");
            yield return null;

            Assert.Less(veil.color.a, 0.01f, "tấm phủ phải bắt đầu trong suốt");

            yield return tr.ChuyenTang();
            Assert.Greater(veil.color.a, 0.1f, "chớp tối mà tấm phủ không tối đi");

            yield return tr.MoMan();
            Assert.Less(veil.color.a, 0.01f,
                "MỞ MÀN XONG MÀ TẤM PHỦ CÒN TỐI — người chơi nhìn vào một màn hình đen " +
                "trong khi game vẫn chạy bình thường phía sau");
            Assert.IsFalse(veil.enabled, "tấm phủ trong suốt vẫn bật — vẽ thừa mỗi khung hình");
        }

        [UnityTest]
        public IEnumerator The_chuong_hien_dung_ten_va_dung_dai_tang()
        {
            var tr = Object.FindFirstObjectByType<SceneTransition>();
            Assert.IsNotNull(tr);
            TMPro.TMP_Text ten = null, so = null, dai = null;
            foreach (TMPro.TMP_Text t in tr.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                if (t.name == "ChapterName")  ten = t;
                if (t.name == "ChapterNo")    so  = t;
                if (t.name == "ChapterRange") dai = t;
            }
            Assert.IsNotNull(ten); Assert.IsNotNull(so); Assert.IsNotNull(dai);
            yield return null;

            yield return tr.TheChuong(1, 21, 40);

            Assert.AreEqual("CHƯƠNG 2", so.text);
            Assert.AreEqual("CHIẾU & GIẤY DẦU", ten.text);
            Assert.AreEqual("TẦNG 21 – 40", dai.text);

            // Thẻ chương KHÔNG tự mở màn — FloorRunner mở sau khi bày xong tầng mới.
            // Nhưng nó PHẢI mở được, nếu không màn đen là vĩnh viễn.
            yield return tr.MoManChuong();
            Image veil = null;
            foreach (Image i in tr.GetComponentsInChildren<Image>(true))
                if (i.name == "Veil") veil = i;
            Assert.Less(veil.color.a, 0.01f);
        }

        [Test]
        public void Du_ten_cho_MOI_chuong_cua_thap()
        {
            // Thiếu một tên là một tấm thẻ chương trống trơn giữa game — đúng kiểu lỗi
            // mà mìn CharacterRoster.Name() trả "?" đã gài sẵn cho M4.
            int moiChuong = BalanceConfig.Instance.GetInt("tower.floorsPerChapter");
            int tongTang  = BalanceConfig.Instance.GetInt("tower.floors");
            int soChuong  = Mathf.CeilToInt((float)tongTang / moiChuong);

            Assert.GreaterOrEqual(SceneTransition.TenChuong.Length, soChuong,
                $"tháp {tongTang} tầng chia {moiChuong} tầng/chương = {soChuong} chương, " +
                $"mà chỉ có {SceneTransition.TenChuong.Length} tên");
            for (int i = 0; i < soChuong; i++)
                Assert.IsNotEmpty(SceneTransition.TenChuong[i], $"chương {i + 1} không có tên");
        }

        [UnityTest]
        public IEnumerator Tam_phu_KHONG_BAO_GIO_nuot_cu_cham()
        {
            // Tấm phủ trải kín màn hình và sống suốt cả lượt chơi. Ăn raycast là mọi nút
            // và cả cần gạt chết — đúng con bọ "vùng chạm nuốt nút" đã sửa ở M2, chỉ khác
            // là lần này tấm phủ nằm TRÊN CÙNG nên không cứu được bằng thứ tự anh em.
            var tr = Object.FindFirstObjectByType<SceneTransition>();
            Image veil = null;
            foreach (Image i in tr.GetComponentsInChildren<Image>(true))
                if (i.name == "Veil") veil = i;
            Assert.IsNotNull(veil);

            Assert.IsFalse(veil.raycastTarget);
            yield return tr.ChuyenTang();
            Assert.IsFalse(veil.raycastTarget, "đang tối thì tấm phủ bắt đầu ăn chạm");
            yield return tr.MoMan();
            Assert.IsFalse(veil.raycastTarget);
        }

        [UnityTest]
        public IEnumerator Leo_mot_tang_THAT_thi_tam_phu_co_chop_toi()
        {
            // TEST QUAN TRỌNG NHẤT CỦA FILE NÀY. Mọi test chuyển cảnh phía trên đều gọi
            // THẲNG vào SceneTransition — chúng chứng minh cái máy chạy được, không
            // chứng minh có ai bật nó. Hiệu ứng doạ đòn của quái từng nối đúng, test
            // xanh, đẩy lên git và KHÔNG BAO GIỜ CHẠY. Đây là test bắt được kiểu đó:
            // chơi thật, lên một tầng, rồi hỏi tấm phủ có tối đi lúc nào không.
            var tr = Object.FindFirstObjectByType<SceneTransition>();
            Image veil = null;
            foreach (Image i in tr.GetComponentsInChildren<Image>(true))
                if (i.name == "Veil") veil = i;
            Assert.IsNotNull(veil);
            yield return null; yield return null;

            int tangDau = _gs.Floor;
            float toiNhat = 0f;
            EnemyRegistry.ClearAll();          // dọn sạch tầng -> FloorRunner sang tầng sau

            float t = 0f;
            while (_gs.Floor == tangDau && t < 12f)
            {
                toiNhat = Mathf.Max(toiNhat, veil.color.a);
                t += Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual(tangDau + 1, _gs.Floor, "không lên được tầng");

            Assert.Greater(toiNhat, 0.2f,
                $"lên tầng mà tấm phủ chưa bao giờ tối (đậm nhất {toiNhat:0.00}) — chuyển " +
                "cảnh nối đúng nhưng KHÔNG AI BẬT NÓ");

            // Và nó phải mở ra lại. Kẹt đen sau khi sang tầng là màn hình đen vĩnh viễn.
            t = 0f;
            while (veil.color.a > 0.01f && t < 3f) { t += Time.deltaTime; yield return null; }
            Assert.Less(veil.color.a, 0.01f, "sang tầng xong mà màn vẫn đen");
        }

        [UnityTest]
        public IEnumerator Chuyen_canh_KHONG_an_mat_thoi_gian_choi()
        {
            // Chớp tối xảy ra 95 lần một lượt chơi. Nếu FloorRunner CHỜ cả lúc mở màn
            // thì mỗi tầng cộng thêm nửa giây chết — hơn 45 giây cả game, và tệ hơn là
            // nửa giây đó người chơi bấm gì cũng không ăn.
            var tr = Object.FindFirstObjectByType<SceneTransition>();
            Assert.IsNotNull(tr);
            yield return null; yield return null;

            int tangDau = _gs.Floor;
            EnemyRegistry.ClearAll();
            float t = 0f;
            while (_gs.Floor == tangDau && t < 12f) { t += Time.deltaTime; yield return null; }

            // Ngay sau khi tầng mới bày xong, quái PHẢI có mặt và đánh được — tức trò
            // chơi đã chạy tiếp trong lúc màn còn đang mờ dần.
            yield return null;
            Assert.Greater(EnemyRegistry.Count, 0,
                "tầng mới chưa có quái sau khi chuyển cảnh — trò chơi đang chờ hiệu ứng");
        }

        // ── HIỆU ỨNG ──────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Quai_chet_thi_NHA_KHOI()
        {
            var puffs = Object.FindFirstObjectByType<PuffFxSpawner>();
            Assert.IsNotNull(puffs, "không có PuffFxSpawner trong scene");
            yield return null; yield return null;

            int truoc = puffs.GetComponentsInChildren<PuffFx>(true).Length;
            int dangChay = DemKhoiDangChay(puffs);
            Assert.AreEqual(0, dangChay, "chưa ai chết mà đã có khói");

            PuffFxSpawner.Puff(Vector3.zero, false);
            yield return null;

            Assert.AreEqual(1, DemKhoiDangChay(puffs),
                "gọi Puff mà không có cụm khói nào bật — hiệu ứng nối đúng nhưng không chạy");
            Assert.AreEqual(truoc, puffs.GetComponentsInChildren<PuffFx>(true).Length,
                "pool không tái dùng — mỗi cái chết đẻ một GameObject mới");

            // Và nó phải TỰ TẮT. Khói không tắt là rác chồng lên sàn mãi mãi.
            float t = 0f;
            while (DemKhoiDangChay(puffs) > 0 && t < 3f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(0, DemKhoiDangChay(puffs), "cụm khói không tự tắt sau khi chạy hết");
        }

        [UnityTest]
        public IEnumerator Khoi_boss_TO_HON_khoi_quai_thuong()
        {
            var puffs = Object.FindFirstObjectByType<PuffFxSpawner>();
            Assert.IsNotNull(puffs);
            yield return null; yield return null;

            PuffFxSpawner.Puff(Vector3.zero, false);
            yield return null;
            float coQuai = LayKhoiDangChay(puffs).transform.localScale.x;

            float t = 0f;
            while (DemKhoiDangChay(puffs) > 0 && t < 3f) { t += Time.deltaTime; yield return null; }

            PuffFxSpawner.Puff(Vector3.zero, true);
            yield return null;
            float coBoss = LayKhoiDangChay(puffs).transform.localScale.x;

            // Boss to gấp 2 lần quái thường (bossScale); khói bằng nhau thì cái chết của
            // boss trông NHỎ HƠN cái chết của lâu la, vì nó phủ ít thân hơn.
            Assert.Greater(coBoss, coQuai * 1.5f,
                $"khói boss ({coBoss:0.00}) không to hơn khói quái thường ({coQuai:0.00})");
        }

        private static int DemKhoiDangChay(PuffFxSpawner s)
        {
            int n = 0;
            foreach (PuffFx f in s.GetComponentsInChildren<PuffFx>(true))
                if (f.gameObject.activeSelf) n++;
            return n;
        }

        private static PuffFx LayKhoiDangChay(PuffFxSpawner s)
        {
            foreach (PuffFx f in s.GetComponentsInChildren<PuffFx>(true))
                if (f.gameObject.activeSelf) return f;
            return null;
        }

        // ── ÂM THANH ──────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Co_tieng_cho_luc_nga_xuong()
        {
            var sfx = Object.FindFirstObjectByType<SfxPlayer>();
            Assert.IsNotNull(sfx);
            yield return null;

            Assert.IsNotNull(sfx.ClipAt((int)Sfx.PlayerDie),
                "ngã xuống không có tiếng — khoảnh khắc câm nhất trong game");

            // Tiếng đó PHẢI ngắn hơn hoặc bằng thời gian bày lại tầng, nếu không nó còn
            // đang kêu "thua" trong lúc tầng mới đã hiện ra.
            AudioClip clip = sfx.ClipAt((int)Sfx.PlayerDie);
            var runner = Object.FindFirstObjectByType<FloorRunner>();
            // Đọc bằng reflection, KHÔNG bằng SerializedObject: asmdef này để
            // includePlatforms rỗng (chạy được cả trong bản build), nên không được
            // đụng vào UnityEditor — nó chỉ tồn tại trong Editor.
            var f = typeof(FloorRunner).GetField("deathDelay",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(f, "FloorRunner không còn trường deathDelay — đổi tên rồi?");
            float deathDelay = (float)f.GetValue(runner);
            Assert.LessOrEqual(clip.length, deathDelay + 0.05f,
                $"tiếng ngã xuống dài {clip.length:0.00}s, mà tầng bày lại sau {deathDelay:0.00}s");
        }

        [UnityTest]
        public IEnumerator Nhac_nen_fade_duoc_KE_CA_khi_game_dang_dung()
        {
            // Màn cột mốc và màn đỉnh tháp đặt timeScale = 0, và chúng bật lên ĐÚNG lúc
            // vừa hạ boss — tức đúng lúc nhạc phải chuyển từ bài boss về bài thường.
            // Dùng Time.deltaTime ở vòng fade là bài boss treo nguyên âm lượng suốt thời
            // gian người chơi đọc màn hình chúc mừng.
            var music = Object.FindFirstObjectByType<AudioDirector>();
            Assert.IsNotNull(music);
            yield return null; yield return null;

            AudioSource nguon = null;
            foreach (AudioSource a in music.GetComponentsInChildren<AudioSource>(true))
                if (a.name.StartsWith("Music") && a.isPlaying) nguon = a;
            Assert.IsNotNull(nguon, "không có nguồn nhạc nào đang phát");

            // Kéo âm lượng xuống 0 rồi ĐÓNG BĂNG trò chơi. Vòng fade phải tự kéo nó về
            // trần. (Bản đầu của test này đo nguồn ĐANG ở trần 0,3 rồi đòi nó tăng thêm —
            // hỏng vì test sai, không vì mã sai. Đúng kiểu "bộ đo không chơi như người
            // chơi" đã gặp ở bảng bấm giờ M4.)
            nguon.volume = 0f;
            Time.timeScale = 0f;
            float t = 0f;
            while (t < 0.4f) { t += Time.unscaledDeltaTime; yield return null; }
            Time.timeScale = 1f;

            Assert.Greater(nguon.volume, 0.05f,
                "timeScale = 0 thì vòng fade nhạc đứng hình — nhạc kẹt ở âm lượng cũ");
        }
    }
}
