using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TowerRpg.Combat;
using TowerRpg.Core;
using TowerRpg.Enemies;
using TowerRpg.Loot;
using TowerRpg.Player;
using TowerRpg.Progression;
using TowerRpg.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TowerRpg.Tests
{
    /// <summary>
    /// NGHIỆM THU đợt sửa nhịp độ — docs/KE-HOACH-NHIP-DO.md mục KIỂM CHỨNG.
    ///
    /// Bài học quyết định #29 là "nối đúng ≠ chạy đúng ≠ nhìn được". §8 còn một nấc thứ tư
    /// mà M1 đã bỏ qua trong im lặng: **"nhìn được ≠ chơi thấy vui"**. 30 test xanh không
    /// phát hiện được một giây nào trong 45 giây trống rỗng của một tầng.
    ///
    /// File này đo những gì MÁY đo được trong bảng chỉ tiêu. Chỉ tiêu số 10 — "chơi liền
    /// 15 phút, đặt máy xuống, có vui không" — không có ở đây và không thể có: nó là việc
    /// của người, và đó đúng là tiêu chí mà DESIGN.md giao cho M1 rồi bị đổi lặng lẽ thành
    /// "30 test xanh" ở quyết định #21.
    /// </summary>
    public class M4FeedbackTests
    {
        private GameState _gs;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // BẢO HIỂM: MilestoneOverlay đặt Time.timeScale = 0 và chờ người chạm. Test
            // nào giết boss mà không đóng nó thì mọi vòng lặp đo bằng Time.deltaTime sau
            // đó TREO VÔ HẠN — deltaTime bằng 0. Đặt lại ở đây rẻ hơn đi tìm chỗ treo.
            Time.timeScale = 1f;   // bảo hiểm
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
            Time.timeScale = 1f;
            SaveSystem.Delete();
        }

        // ── Chỉ tiêu đo được bằng máy ─────────────────────────────────────────────

        /// <summary>
        /// Ô 'Thông số'!B9 = 0,5 của can-bang.xlsx: "tỉ lệ sát thương thực nhận — nhờ luật
        /// di chuyển ở mục 5.3". Trước Việc 2.1 hồi chiêu quái chạy cả khi người chơi ở
        /// ngoài tầm, nên lùi ra né được ĐÚNG 0 đòn và ô B9 là lời hứa suông.
        /// </summary>
        [UnityTest]
        public IEnumerator Ngoai_tam_thi_khong_don_don()
        {
            var hp = Object.FindFirstObjectByType<PlayerHealth>();
            var ctrl = Object.FindFirstObjectByType<PlayerController>();
            IDamageable near = EnemyRegistry.Nearest(Vector3.zero, 999f);
            Assert.IsNotNull(near);

            Vector3 inRange = near.Position - Vector3.up * 1.0f;   // sát con quái
            Vector3 outRange = near.Position - Vector3.up * 30f;   // ngoài mọi tầm

            // Ca A: đứng lì trong tầm 8 giây.
            ctrl.transform.position = inRange;
            hp.ResetHealth();
            yield return null;
            float a0 = hp.Hp;
            yield return new WaitForSeconds(8f);
            float lostA = a0 - hp.Hp;

            // Ca B: trong tầm 4 giây, ngoài tầm 4 giây.
            hp.ResetHealth();
            yield return null;
            float b0 = hp.Hp;
            yield return new WaitForSeconds(4f);
            ctrl.transform.position = outRange;
            yield return new WaitForSeconds(4f);
            float lostB = b0 - hp.Hp;

            Assert.Greater(lostA, 0f, "đứng lì 8 giây mà không mất máu — quái không đánh?");
            Assert.Less(lostB, lostA * 0.75f,
                $"đứng nửa thời gian mà mất {lostB:F1} máu so với {lostA:F1} khi đứng lì — "
                + "né đòn không có tác dụng, ô B9 = 0,5 vẫn là lời hứa suông");
        }

        /// <summary>Chỉ tiêu 4: lần nâng cấp ĐẦU TIÊN phải đổi pixel trên màn hình.</summary>
        [UnityTest]
        public IEnumerator Nang_cap_dau_tien_phai_doi_pixel_tren_man_hinh()
        {
            var st = PlayerStats.Instance;
            float ngưỡng = BalanceConfig.Instance.Get("juice.popupDecimalBelow");

            string Ve(float x) => x < ngưỡng ? x.ToString("0.0") : Mathf.RoundToInt(x).ToString();

            string truoc = Ve(st.Damage);
            _gs.AddShards(100000f);
            Assert.IsTrue(_gs.TryUpgrade(Slot.Weapon));
            yield return null;
            string sau = Ve(st.Damage);

            // Người chơi trả 300 Mảnh = 3/4 thu nhập cả tầng 1, quay lại đánh, và nếu pixel
            // y hệt thì họ kết luận hệ thống nâng cấp là đồ trang trí — mất luôn cả vòng
            // lặp kinh tế chứ không chỉ mất một phần thưởng.
            Assert.AreNotEqual(truoc, sau,
                $"nâng cấp Vũ khí lần đầu mà số sát thương vẫn in ra '{truoc}' — "
                + "người chơi không có cách nào biết mình vừa mạnh lên");
        }

        /// <summary>Chỉ tiêu 5: số đòn giết một con phải NHÌN THẤY ĐƯỢC và phải giảm.</summary>
        [UnityTest]
        public IEnumerator So_don_giet_mot_con_phai_giam_sau_vai_cap_vu_khi()
        {
            var st = PlayerStats.Instance;
            BalanceConfig b = BalanceConfig.Instance;
            float hpEach = b.Get("enemy.hpFloor1") / b.GetInt("enemy.count");

            int DonCan() => Mathf.CeilToInt(hpEach / st.Damage);

            int truoc = DonCan();
            _gs.AddShards(100000f);
            for (int i = 0; i < 3; i++) _gs.TryUpgrade(Slot.Weapon);
            yield return null;
            int sau = DonCan();

            Assert.Less(sau, truoc,
                $"sau 3 cấp Vũ khí vẫn cần {sau} đòn như cũ ({truoc}) — thanh máu quái là thứ "
                + "DUY NHẤT biến +4,4%/cấp thành con số đếm được, mà nó không đổi thì vô nghĩa");
        }

        /// <summary>Chỉ tiêu 3: một con quái chết phải sinh ít nhất 3 kênh phản hồi.</summary>
        [UnityTest]
        public IEnumerator Mot_con_quai_chet_co_it_nhat_ba_kenh_phan_hoi()
        {
            var drops = Object.FindFirstObjectByType<ShardDropSpawner>();
            var sfx = Object.FindFirstObjectByType<Juice.SfxPlayer>();
            yield return null; yield return null;

            var enemy = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)[0];
            int liveBefore = drops.LiveCount;

            enemy.TakeDamage(1e9f, false);
            yield return null;

            var kenh = new List<string>();
            if (drops.LiveCount > liveBefore) kenh.Add("viên Mảnh rơi");
            if (sfx != null && sfx.ClipAt((int)Juice.Sfx.EnemyDie) != null) kenh.Add("tiếng chết");
            if (enemy != null && !enemy.IsAlive) kenh.Add("hoạt ảnh xác tan");

            // Trước đợt này con số là 0: Destroy(gameObject) trong im lặng tuyệt đối,
            // sáu lần mỗi tầng, không lần nào để lại dấu vết trong trí nhớ.
            Assert.GreaterOrEqual(kenh.Count, 3,
                $"quái chết chỉ có {kenh.Count} kênh phản hồi ({string.Join(", ", kenh)})");
        }

        /// <summary>
        /// Nút khoá phải nói ĐÚNG lý do nó khoá. "Dọn tầng 1 đi" khi người chơi đã dọn
        /// tầng 1 từ lâu là một cái nút NÓI DỐI — họ đi làm đúng việc nó bảo và vẫn không mở.
        /// </summary>
        [UnityTest]
        public IEnumerator Nut_quet_noi_dung_ly_do_khoa()
        {
            GameObject go = null;
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == "SweepButton") { go = t.gameObject; break; }
            Assert.IsNotNull(go, "không thấy SweepButton");
            var label = go.GetComponentInChildren<TMPro.TMP_Text>(true);
            yield return null; yield return null;

            StringAssert.Contains("DỌN TẦNG", label.text,
                "chưa dọn tầng nào thì nhãn phải bảo đi dọn tầng");

            // Dọn tầng 1 rồi tiêu sạch ngân sách quét.
            _gs.AddShards(_gs.ShardReward(1));
            _gs.MarkCleared(1);
            yield return null;
            float con = _gs.SweepBudgetLeft;
            Assert.Greater(con, 0f);
            _gs.AddShards(con, fromSweep: true);
            yield return null; yield return null;

            StringAssert.Contains("NGÂN SÁCH", label.text,
                $"hết ngân sách mà nhãn vẫn ghi '{label.text.Replace("\n", " / ")}' — "
                + "người chơi đã dọn tầng 1 rồi, bảo họ dọn nữa là nói dối");
        }

        // ── Đợt 2 ─────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Cua_boss_thi_HOI_DAY_MAU()
        {
            var hp = Object.FindFirstObjectByType<PlayerHealth>();
            yield return null; yield return null;

            // Tụt máu xuống thấp rồi leo tới cửa boss.
            hp.TakeDamage(hp.MaxHp * 0.9f);
            yield return null;
            Assert.Less(hp.Fraction, 0.2f);

            while (_gs.Floor < 9) _gs.AdvanceFloor();
            yield return null;
            EnemyRegistry.ClearAll();

            float t = 0f;
            while (_gs.Floor < 10 && t < 10f) { t += Time.deltaTime; yield return null; }
            yield return null; yield return null;

            // Bảng tính 'Kiểm chứng build'!T5 = P5/(R5 x B9) tính thời gian sống từ máu
            // TỐI ĐA. Vào boss với 9% máu thì biên thật là 0,14 chứ không phải 1,50 — cả
            // đường cong máu boss §5.10 mất nghĩa. Và trước đợt này, cách DUY NHẤT để vào
            // boss với máu đầy là cố tình CHẾT trước đó.
            Assert.AreEqual(1f, hp.Fraction, 0.02f,
                $"vào tầng boss với {hp.Fraction * 100:F0}% máu — bảng tính giả định máu ĐẦY, "
                + "và không hồi thì chết-trước-boss lại thành nước đi tối ưu");
        }

        [UnityTest]
        public IEnumerator Bao_hieu_ra_don_phai_NHIN_THAY_DUOC()
        {
            var ctrl = Object.FindFirstObjectByType<PlayerController>();
            IDamageable near = EnemyRegistry.Nearest(Vector3.zero, 999f);
            var quai = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)[0];
            var sr = quai.GetComponent<SpriteRenderer>();

            ctrl.transform.position = quai.transform.position - Vector3.up * 1.0f;
            yield return null;

            Vector3 scaleNghi = quai.transform.localScale;
            Color mauNghi = sr.color;

            // Theo dõi suốt CẢ cửa sổ báo hiệu và lấy giá trị lớn nhất. Chộp đúng một
            // khung hình là may rủi: lúc IsTelegraphing vừa bật thì k≈0 nên chưa đổi gì,
            // và ở batchmode khung hình trôi rất nhanh.
            float scaleMax = scaleNghi.x, lechMauMax = 0f;
            bool tungBaoHieu = false;
            float t = 0f;
            while (t < 5f)
            {
                t += Time.deltaTime;
                yield return null;
                if (!quai.IsTelegraphing) { if (tungBaoHieu) break; continue; }
                tungBaoHieu = true;
                scaleMax = Mathf.Max(scaleMax, quai.transform.localScale.x);
                lechMauMax = Mathf.Max(lechMauMax, Mathf.Abs(sr.color.b - mauNghi.b));
            }
            Assert.IsTrue(tungBaoHieu, "quái không bao giờ vào trạng thái báo hiệu");

            // BẢN ĐẦU HỎNG ĐÚNG Ở ĐÂY: nó "làm sáng lên" bằng Lerp(_baseColor, trắng) mà
            // _baseColor đã là trắng — phép toán rỗng. Tính năng được nối, được verify,
            // và không làm gì cả. Chỉ đo pixel trên ảnh chụp mới lộ ra.
            bool toHon = scaleMax > scaleNghi.x * 1.05f;
            bool doiMau = lechMauMax > 0.1f;
            Assert.IsTrue(toHon && doiMau,
                $"báo hiệu không nhìn thấy được: scale lớn nhất {scaleMax:F3} so với "
                + $"{scaleNghi.x:F3}, lệch kênh lam lớn nhất {lechMauMax:F3}");
        }

        [UnityTest]
        public IEnumerator Tang_boss_KHONG_hien_ca_hai_thong_bao()
        {
            GameObject banner = null;
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == "EventBanner") { banner = t.gameObject; break; }
            Assert.IsNotNull(banner);

            var runner = Object.FindFirstObjectByType<FloorRunner>();
            var ms = Object.FindFirstObjectByType<UI.MilestoneOverlay>();

            while (_gs.Floor < 9) _gs.AdvanceFloor();
            yield return null;
            EnemyRegistry.ClearAll();
            float t0 = 0f;
            while ((!runner.InBossFight || EnemyRegistry.Count == 0) && t0 < 12f)
            { t0 += Time.deltaTime; yield return null; }

            Enemy boss = System.Array.Find(
                Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None), e => e.IsBoss && e.IsAlive);
            Assert.IsNotNull(boss, "phải vào được trận boss");
            boss.TakeDamage(1e9f, false);

            float t1 = 0f;
            while (!ms.IsOpen && t1 < 6f) { t1 += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ms.IsOpen, "hạ boss phải mở màn cột mốc");

            // Hai thông báo cùng lúc thì chúng đè chữ lên nhau và không cái nào đọc trọn.
            Assert.IsFalse(banner.activeSelf,
                "tầng boss hiện CẢ banner dọn tầng LẪN màn cột mốc — hai thứ nói cùng một "
                + "điều, đè lên nhau");

            yield return new WaitForSecondsRealtime(0.8f);
            ms.Dong();
        }

        [UnityTest]
        public IEnumerator Man_cot_moc_dung_game_roi_TRA_LAI_timeScale()
        {
            var ms = Object.FindFirstObjectByType<UI.MilestoneOverlay>();
            Assert.IsNotNull(ms, "không có MilestoneOverlay");
            yield return null;

            Assert.AreEqual(1f, Time.timeScale, 0.001f);
            ms.Show(10, 3, "Kiếm sĩ");
            yield return null;

            Assert.IsTrue(ms.IsOpen);
            Assert.AreEqual(0f, Time.timeScale, 0.001f, "màn cột mốc phải DỪNG trò chơi");

            // Khoá chạm một nhịp: người chơi đang bấm liên tục lúc hạ boss sẽ đóng mất
            // màn hình trước khi đọc được chữ nào.
            ms.Dong();
            Assert.IsTrue(ms.IsOpen, "đóng được ngay lập tức — người chơi sẽ không kịp đọc");

            yield return new WaitForSecondsRealtime(0.8f);
            ms.Dong();

            // TRẢ LẠI timeScale là phần sống còn: quên một đường thoát là CẢ GAME đứng hình
            // và không có gì báo cho biết vì sao.
            Assert.IsFalse(ms.IsOpen);
            Assert.AreEqual(1f, Time.timeScale, 0.001f, "đóng màn cột mốc mà không trả timeScale");
        }

        [UnityTest]
        public IEnumerator Hoat_anh_doi_theo_trang_thai_va_huong()
        {
            var anim = Object.FindFirstObjectByType<PlayerAnimator>();
            var sr = anim.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(anim); Assert.IsNotNull(sr);
            yield return null; yield return null;

            Sprite dung = sr.sprite;
            anim.BaoDanh(Vector3.right);
            yield return null;
            Sprite danhPhai = sr.sprite;

            anim.BaoDanh(Vector3.up);
            yield return null;
            Sprite danhLen = sr.sprite;

            Assert.AreNotEqual(dung, danhPhai, "ra đòn mà hình không đổi");
            Assert.AreNotEqual(danhPhai, danhLen, "đánh sang phải và đánh lên phải khác hình");
        }

        /// <summary>Bất biến của hoạt ảnh chết: Count giảm NGAY, không đợi xác tan.</summary>
        [UnityTest]
        public IEnumerator Quai_chet_thi_Count_giam_ngay_trong_khung_hinh_do()
        {
            yield return null; yield return null;
            int before = EnemyRegistry.Count;
            var enemy = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)[0];

            enemy.TakeDamage(1e9f, false);
            // KHÔNG yield. Đây là cả nội dung của test.
            Assert.AreEqual(before - 1, EnemyRegistry.Count,
                "hoạt ảnh chết 0,22s đang cộng vào thời lượng mỗi tầng");
        }
    }
}
