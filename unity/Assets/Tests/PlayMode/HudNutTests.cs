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
