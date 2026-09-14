using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TowerRpg.Core;
using TowerRpg.Progression;
using TowerRpg.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TowerRpg.Tests
{
    /// <summary>
    /// ĐƯỜNG NGÓN TAY ĐI. Lớp kiểm này lẽ ra phải có từ M2 và không có, nên hai màn hình
    /// lớn nhất của game CÂM SUỐT BA MỐC mà không ai biết.
    ///
    /// Nguyên nhân gốc: `BuildM1Scene.cs` nối bốn nút bằng `btn.onClick.AddListener(...)`.
    /// Dòng đó chạy đúng trong phiên Editor dựng scene, rồi BIẾN MẤT khi scene được lưu —
    /// `AddListener` tạo đăng ký LÚC CHẠY, Unity chỉ tuần tự hoá `m_PersistentCalls`, và
    /// file M1.unity ghi đúng điều đó: `m_Calls: []` ở cả 66 chỗ.
    ///
    /// VÌ SAO KHÔNG LỚP NÀO BẮT ĐƯỢC:
    ///   · VerifyM1Scene soi THAM CHIẾU [SerializeField] — nút được nối đủ, không thiếu gì.
    ///   · Test PlayMode gọi thẳng `UpgradeScreen.Toggle()` — đi vòng qua đúng cái hỏng.
    ///   · Ảnh chụp màn nâng cấp cũng do test gọi Toggle() dựng ra, nên nhìn vẫn đẹp.
    /// Cả bốn lớp đều tránh đúng chỗ duy nhất bị hỏng. Chỉ có bấm mới biết.
    ///
    /// Nên mọi test ở đây BẤM, không gọi hàm.
    /// </summary>
    public class HudNutTests
    {
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
            yield return null; yield return null;
        }

        [TearDown] public void TearDown() { Time.timeScale = 1f; SaveSystem.Delete(); }

        /// <summary>Tìm theo tên, và BẮT tên phải là duy nhất — trùng tên là tìm kiểu xổ số.</summary>
        private static Button Nut(string ten)
        {
            Button[] ds = Resources.FindObjectsOfTypeAll<Button>()
                                   .Where(b => b.name == ten && b.gameObject.scene.isLoaded)
                                   .ToArray();
            Assert.AreEqual(1, ds.Length,
                $"phải có ĐÚNG một nút tên '{ten}' trong scene, thấy {ds.Length}. Trùng tên thì "
                + "mọi phép tìm theo tên thành xổ số — đã cắn một lần ngay trong bộ đo của lỗi này.");
            return ds[0];
        }

        [UnityTest]
        public IEnumerator Nut_TRANG_BI_mo_va_dong_duoc_bang_CU_BAM()
        {
            var up = Object.FindFirstObjectByType<UpgradeScreen>();
            Assert.IsFalse(up.IsOpen, "bảng phải đóng lúc vào game");

            Nut("GearButton").onClick.Invoke();
            yield return null; yield return null;
            Assert.IsTrue(up.IsOpen,
                "BẤM nút TRANG BỊ mà bảng không mở. Kiểm xem onClick có được nối LÚC CHẠY "
                + "không — nối trong BuildM1Scene bằng AddListener là bay mất lúc lưu scene.");

            Nut("Close").onClick.Invoke();
            yield return null; yield return null;
            Assert.IsFalse(up.IsOpen, "BẤM ĐÓNG mà bảng không đóng — mở được mà không thoát ra được");
        }

        [UnityTest]
        public IEnumerator Nut_NHAN_VAT_mo_va_dong_duoc_bang_CU_BAM()
        {
            var ch = Object.FindFirstObjectByType<CharacterScreen>();
            Assert.IsFalse(ch.IsOpen);

            Nut("CharButton").onClick.Invoke();
            yield return null; yield return null;
            Assert.IsTrue(ch.IsOpen, "BẤM nút NHÂN VẬT mà màn hình không mở");

            Nut("CloseChar").onClick.Invoke();
            yield return null; yield return null;
            Assert.IsFalse(ch.IsOpen, "BẤM ĐÓNG mà màn nhân vật không đóng");
        }

        [UnityTest]
        public IEnumerator Ngay_TANG_1_bang_trang_bi_da_co_du_bon_hang_co_chu()
        {
            // Chủ dự án viết: "hiện thị nút bấm nhưng không có gì trông rất kì". Sau khi
            // nút sống lại thì bảng phải có NỘI DUNG THẬT ngay từ tầng 1, không phải một
            // cái túi rỗng — bốn ô trang bị tồn tại từ giây 0 (§5.5), chỉ là chưa đủ Mảnh.
            var up = Object.FindFirstObjectByType<UpgradeScreen>();
            Nut("GearButton").onClick.Invoke();
            yield return null; yield return null;

            var rowParent = (RectTransform)typeof(UpgradeScreen).GetField("rowParent",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(up);
            Assert.AreEqual(Equipment.SlotCount, rowParent.childCount,
                $"tầng 1 mà bảng chỉ có {rowParent.childCount} hàng — phải đủ {Equipment.SlotCount} ô");

            string het = string.Join(" | ", rowParent.GetComponentsInChildren<TMPro.TMP_Text>(true)
                                                     .Select(t => t.text));
            foreach (string can in new[] { "Vũ khí", "Giáp", "Găng", "Nhẫn" })
                StringAssert.Contains(can, het, $"bảng tầng 1 không có ô {can}");
            // Và phải nói được GIÁ, nếu không người chơi không biết cần bao nhiêu.
            StringAssert.Contains("300", het, "bảng tầng 1 không hiện giá nâng cấp");
        }

        [UnityTest]
        public IEnumerator Ngay_TANG_1_man_nhan_vat_co_du_nam_the()
        {
            var ch = Object.FindFirstObjectByType<CharacterScreen>();
            Nut("CharButton").onClick.Invoke();
            yield return null; yield return null;

            var cardParent = (RectTransform)typeof(CharacterScreen).GetField("cardParent",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(ch);
            Assert.AreEqual(CharacterRoster.Count, cardParent.childCount,
                "tầng 1 mà màn nhân vật không đủ thẻ — khoá thì vẫn phải THẤY, xem §7");
        }

        private static (GameObject root, TMPro.TMP_Text chu) Banner()
        {
            var hud = Object.FindFirstObjectByType<HudUI>();
            var f1 = typeof(HudUI).GetField("eventBannerRoot",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var f2 = typeof(HudUI).GetField("eventBanner",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return ((GameObject)f1.GetValue(hud), (TMPro.TMP_Text)f2.GetValue(hud));
        }

        [UnityTest]
        public IEnumerator Bam_nut_KHOA_thi_phai_noi_LY_DO_chu_khong_im_lang()
        {
            // Chủ dự án báo: "bấm QUÉT NHANH tôi không thấy có gì khác biệt". Đúng — với
            // interactable = false, Unity NUỐT cú chạm ở tầng Selectable: không tiếng,
            // không chữ, không gì. Người chơi không phân biệt được "đang khoá" với "hỏng".
            var (root, chu) = Banner();
            Assert.IsFalse(root.activeSelf, "banner phải tắt lúc chưa bấm gì");

            Nut("SweepButton").onClick.Invoke();
            yield return null; yield return null;

            Assert.IsTrue(root.activeSelf,
                "bấm nút QUÉT NHANH đang khoá mà KHÔNG có phản hồi nào — đúng triệu chứng "
                + "chủ dự án báo. Nút khoá phải nói được lý do.");
            StringAssert.Contains("QUÉT NHANH", chu.text);
            StringAssert.Contains("DỌN", chu.text, "phải nói ĐIỀU KIỆN để mở, không chỉ nói 'khoá'");
        }

        [UnityTest]
        public IEnumerator Bam_TU_DANH_khi_khoa_thi_noi_con_bao_nhieu_tang()
        {
            var (root, chu) = Banner();
            Nut("AutoButton").onClick.Invoke();
            yield return null; yield return null;

            Assert.IsTrue(root.activeSelf, "bấm TỰ ĐÁNH đang khoá mà im lặng");
            StringAssert.Contains("TỰ ĐÁNH", chu.text);
            StringAssert.Contains("CÒN", chu.text, "phải đếm ngược, đó là khuôn của ba nút kia");
        }

        [UnityTest]
        public IEnumerator Nhan_nut_khoa_KHONG_duoc_doc_ra_nhu_mot_hanh_dong()
        {
            // "QUÉT NHANH / DỌN TẦNG 1" đọc ra như MÔ TẢ VIỆC NÚT LÀM, không phải điều
            // kiện để mở. Ba nút kia đều dùng khuôn "CÒN N TẦNG" và khuôn đó không thể
            // hiểu nhầm thành một hành động. Test khoá sự nhất quán đó.
            yield return null;
            string quet = Nut("SweepButton").GetComponentInChildren<TMPro.TMP_Text>(true).text;
            string tu   = Nut("AutoButton").GetComponentInChildren<TMPro.TMP_Text>(true).text;
            foreach (var (ten, t) in new[] { ("QUÉT NHANH", quet), ("TỰ ĐÁNH", tu) })
                StringAssert.Contains("CÒN", t,
                    $"nhãn nút {ten} lúc khoá là \"{t.Replace("\n", " / ")}\" — không theo khuôn "
                    + "đếm ngược \"CÒN N\", nên đọc được thành một hành động");
        }

        [UnityTest]
        public IEnumerator Moi_thanh_voi_day_PHAI_THAT_SU_VOI()
        {
            // LỖI TỆ NHẤT MÀ BỐN LỚP KIỂM ĐỀU CẤP GIẤY THÔNG HÀNH. Ba thanh của game
            // (máu người chơi, máu boss, tiến trình quét) dựng với Image.Type.Filled
            // nhưng sprite RỖNG. Image.OnPopulateMesh thoát ngay ở dòng đầu khi không có
            // sprite và vẽ NGUYÊN KHỐI, nên fillAmount bị bỏ qua hoàn toàn: thanh máu
            // chưa bao giờ vơi kể từ M1, thanh boss kể từ M3, vạch quét chưa bao giờ chạy.
            //
            // Và mục kiểm cũ trong VerifyM1Scene lại đi khẳng định `type == Filled` —
            // tức xác nhận đúng cái tính chất gây ra lỗi.
            //
            // Test này ĐO LƯỚI THẬT mà Image sinh ra, không hỏi thuộc tính. Đó là khác
            // biệt duy nhất giữa "nối đúng" và "chạy đúng" ở đây.
            yield return null; yield return null;

            foreach (string ten in new[] { "HealthBar", "Fill", "SweepFill" })
            {
                Image img = Resources.FindObjectsOfTypeAll<Image>()
                    .FirstOrDefault(i => i.name == ten && i.gameObject.scene.isLoaded);
                Assert.IsNotNull(img, $"không thấy thanh {ten}");
                Assert.IsNotNull(img.sprite,
                    $"thanh {ten}: Image.Type.Filled mà sprite RỖNG — Unity sẽ vẽ nguyên khối "
                    + "100% và bỏ qua fillAmount, không lỗi nào ném ra");

                // ĐO NGAY, KHÔNG yield. PlayerHealthUI ghi lại fillAmount MỖI KHUNG HÌNH
                // theo máu thật, nên nhường một khung hình là nó kéo thanh về đầy và test
                // đo nhầm sản phẩm thành hỏng. (Bản đầu của test này dính đúng vậy — con
                // số 556/556 là do bộ đo đua với mã, không phải do mã sai.)
                img.fillAmount = 0.25f;
                float ve = BeNgangVe(img), khung = img.rectTransform.rect.width;
                Assert.Less(ve, khung * 0.6f,
                    $"thanh {ten}: đặt fillAmount = 0,25 mà lưới vẫn vẽ {ve:0}/{khung:0} px "
                    + $"({100f * ve / khung:0}%) — thanh này KHÔNG VƠI, nó chỉ trông như một thanh");
                img.fillAmount = 1f;
            }
        }

        /// <summary>Bề ngang lưới mà Image THẬT SỰ sinh ra — hỏi mesh, không hỏi thuộc tính.</summary>
        private static float BeNgangVe(Image img)
        {
            var vh = new VertexHelper();
            var m = typeof(Graphic).GetMethod("OnPopulateMesh",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null, new[] { typeof(VertexHelper) }, null);
            m.Invoke(img, new object[] { vh });
            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < vh.currentVertCount; i++)
            { UIVertex v = default; vh.PopulateUIVertex(ref v, i);
              min = Mathf.Min(min, v.position.x); max = Mathf.Max(max, v.position.x); }
            return vh.currentVertCount == 0 ? 0f : max - min;
        }

        [UnityTest]
        public IEnumerator Mat_mau_THAT_thi_thanh_mau_phai_ngan_lai()
        {
            // Đường đầu-cuối, không poke fillAmount: đánh người chơi mất máu thật rồi đo
            // lưới. Đây mới là thứ người chơi nhìn thấy trong lúc đánh nhau.
            var hp = Object.FindFirstObjectByType<Player.PlayerHealth>();
            Image thanh = Resources.FindObjectsOfTypeAll<Image>()
                .First(i => i.name == "HealthBar" && i.gameObject.scene.isLoaded);
            yield return null; yield return null;

            float day = BeNgangVe(thanh);
            Assert.Greater(day, 1f, "thanh máu đầy mà không vẽ gì");

            hp.TakeDamage(hp.MaxHp * 0.7f);
            yield return null; yield return null;

            float con = BeNgangVe(thanh);
            Assert.Less(con, day * 0.6f,
                $"mất 70% máu mà thanh vẫn vẽ {con:0}/{day:0} px — thanh máu KHÔNG VƠI. " +
                "Đây là thứ người chơi nhìn suốt 100 tầng để biết mình sắp chết.");
            hp.ResetHealth();
        }

        [UnityTest]
        public IEnumerator Truoc_khi_GameState_san_sang_nut_khoa_van_phai_tu_choi_ra_tieng()
        {
            // Refresh() thoát sớm khi GameState chưa Ready, nên RefreshSweep/RefreshAuto
            // chưa chạy lần nào và hai nút còn mặc bộ áo "đang mở" lưu trong scene. Nếu
            // hai cờ khoá mặc định là false thì cú bấm trong cửa sổ đó rơi thẳng xuống
            // Toggle() và bị từ chối IM LẶNG.
            var hud = Object.FindFirstObjectByType<HudUI>();
            foreach (string ten in new[] { "_quetBiKhoa", "_tuDanhBiKhoa" })
            {
                var f = typeof(HudUI).GetField(ten,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(f, $"HudUI không còn trường {ten}");
            }
            yield return null;

            // Ở tầng 1 cả hai vẫn phải khoá, và bấm vào vẫn phải nói lý do.
            var (root, _) = Banner();
            Nut("AutoButton").onClick.Invoke();
            yield return null; yield return null;
            Assert.IsTrue(root.activeSelf, "TỰ ĐÁNH khoá mà bấm vào vẫn im lặng");
        }

        [UnityTest]
        public IEnumerator Doi_ti_le_man_hinh_thi_dau_truong_van_lot_khung()
        {
            // Camera trực giao khoá nửa chiều CAO, nên màn hình càng cao thì thấy càng
            // HẸP. Đo được: OrthoSize 7,2 cho nửa bề ngang 4,05 ở 16:9 — vừa đủ cho quái
            // ở bán kính 3,5 cộng nửa thân, dư 1,2%. Ở 20:9 tụt còn 3,24, tức TÂM hai con
            // quái hai bên nằm NGOÀI màn hình: người chơi đánh nhau với thứ họ thấy một nửa.
            //
            // Mục kiểm trong VerifyM1Scene tính chuyện này bằng công thức. Test này chứng
            // minh CameraFit THẬT SỰ CHẠY — khác biệt duy nhất giữa "nối đúng" và
            // "chạy đúng", và dự án đã trả giá cho khoảng cách đó nhiều lần.
            var cam = Camera.main;
            Assert.IsNotNull(cam.GetComponent<Core.CameraFit>(), "camera không có CameraFit");
            float can = BalanceConfig.Instance.Get("enemy.spawnRadius")
                      + BalanceConfig.Instance.Get("camera.marginX");
            yield return null; yield return null;

            foreach (float aspect in new[] { 1080f / 1920f, 1080f / 2340f, 1080f / 2400f, 1080f / 2520f })
            {
                cam.aspect = aspect;
                yield return null; yield return null;

                float nuaNgang = cam.orthographicSize * cam.aspect;
                Assert.GreaterOrEqual(nuaNgang, can - 0.01f,
                    $"tỉ lệ {aspect:0.000}: chỉ thấy được {nuaNgang:0.00} đơn vị mỗi bên, " +
                    $"mà quái bày ở {can:0.00} — quái bị cắt mất một phần thân");
            }
            cam.ResetAspect();
        }

        [UnityTest]
        public IEnumerator Khong_co_gi_che_len_bon_nut_HUD()
        {
            // Cú chạm phải tới được nút. Dự án đã dính một lần: vùng chạm cần gạt trong
            // suốt nằm SAU các nút nên nuốt hết — bấm QUÉT NHANH lại hoá ra đi bộ.
            var canvas = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                               .First(c => c.GetComponent<GraphicRaycaster>() != null);
            var es = EventSystem.current;
            Assert.IsNotNull(es, "scene không có EventSystem — không cú chạm nào tới được đâu cả");
            yield return null;

            foreach (string ten in new[] { "GearButton", "CharButton", "SweepButton", "AutoButton" })
            {
                Button b = Nut(ten);
                var rt = (RectTransform)b.transform;
                var goc = new Vector3[4];
                rt.GetWorldCorners(goc);
                Vector2 diem = RectTransformUtility.WorldToScreenPoint(
                    canvas.worldCamera, (goc[0] + goc[2]) * 0.5f);

                var hit = new List<RaycastResult>();
                es.RaycastAll(new PointerEventData(es) { position = diem }, hit);
                Assert.Greater(hit.Count, 0, $"{ten}: chạm vào giữa nút mà không trúng gì");
                Assert.AreEqual(ten, hit[0].gameObject.name,
                    $"{ten}: cú chạm bị '{hit[0].gameObject.name}' nuốt mất trước khi tới nút");
            }
        }
    }
}
