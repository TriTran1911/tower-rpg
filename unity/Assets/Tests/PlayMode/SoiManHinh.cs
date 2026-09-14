using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TMPro;
using TowerRpg.Core;
using TowerRpg.Player;
using TowerRpg.Progression;
using TowerRpg.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TowerRpg.Tests
{
    /// <summary>
    /// BỘ SOI MÀN HÌNH — thay việc ngồi chơi để tìm lỗi hiển thị.
    ///
    /// Lý lẽ: mọi lỗi "phải chơi mới thấy" của dự án này thật ra đều ĐO ĐƯỢC. Nút chết
    /// bắt bằng cách bấm qua EventSystem; thanh không vơi bắt bằng cách đo lưới; nhân vật
    /// chìm vào sàn bắt bằng ΔE; banner đè nút bắt bằng hình học. Thứ duy nhất còn thủ
    /// công là NHÌN ẢNH CHỤP — và ảnh chụp là dữ liệu, không phải cảm giác.
    ///
    /// Lớp này lái game qua một loạt TRẠNG THÁI tiêu biểu, ở nhiều TỈ LỆ màn hình, rồi
    /// với mỗi cảnh xuất ra hai thứ:
    ///   · một file PNG
    ///   · một file JSON liệt kê MỌI phần tử giao diện đang hiện: tên, hình chữ nhật trên
    ///     màn hình, chữ, màu chữ, cỡ chữ thật sau khi tự co, và bề rộng chữ cần thiết
    ///
    /// `tools/soi-man-hinh.py` đọc cặp đó rồi báo lỗi ĐO ĐƯỢC: chữ tràn khung, tương phản
    /// dưới ngưỡng, phần tử lọt ra ngoài màn hình, hai vùng chạm chồng nhau.
    ///
    /// CÁI NÓ KHÔNG LÀM ĐƯỢC, và đừng giả vờ là làm được: nó không biết câu chữ có DỄ HIỂU
    /// không, và không biết game có VUI không. Hai thứ đó vẫn cần người.
    /// </summary>
    public class SoiManHinh
    {
        private const int W = 540;
        private static string OutDir =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "../../soi-man-hinh"));

        private static readonly (string ten, int w, int h)[] TiLe =
        {
            ("16-9", W, 960), ("20-9", W, 1200),
        };

        private GameState _gs;
        private Camera _cam;

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
            _cam = Camera.main;
            foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                                       .Where(c => c.renderMode == RenderMode.ScreenSpaceOverlay))
            { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = _cam; c.planeDistance = 1f; }
        }

        [TearDown]
        public void TearDown()
        {
            if (_cam != null) _cam.targetTexture = null;
            Time.timeScale = 1f;
            SaveSystem.Delete();
        }

        private static Button Nut(string t) => Resources.FindObjectsOfTypeAll<Button>()
            .FirstOrDefault(b => b.name == t && b.gameObject.scene.isLoaded);

        /// <summary>Chụp một cảnh ở MỌI tỉ lệ, kèm bản kê phần tử giao diện.</summary>
        private IEnumerator Canh(string ten)
        {
            Directory.CreateDirectory(OutDir);
            foreach ((string tl, int w, int h) in TiLe)
            {
                var rt = new RenderTexture(w, h, 24);
                _cam.targetTexture = rt; _cam.aspect = (float)w / h;
                yield return null; yield return null;
                Canvas.ForceUpdateCanvases();
                yield return null;

                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
                File.WriteAllBytes(Path.Combine(OutDir, $"{ten}__{tl}.png"), tex.EncodeToPNG());
                RenderTexture.active = null;
                Object.Destroy(tex);

                File.WriteAllText(Path.Combine(OutDir, $"{ten}__{tl}.json"), BanKe(w, h));
                Debug.Log($"[soi] {ten}__{tl}");
                _cam.targetTexture = null;
            }
        }

        /// <summary>Bản kê JSON mọi phần tử giao diện đang HIỆN THẬT.</summary>
        private string BanKe(int w, int h)
        {
            var sb = new StringBuilder("{\n  \"canvas\": [").Append(w).Append(", ").Append(h).Append("],\n");
            sb.Append("  \"manHinhMo\": \"").Append(An(ManHinhMo())).Append("\",\n");
            sb.Append("  \"chu\": [\n");
            var dong = new List<string>();
            foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            {
                if (!t.isActiveAndEnabled || string.IsNullOrWhiteSpace(t.text)) continue;
                if (!TrenMan(t.rectTransform, w, h, out Rect r)) continue;
                t.ForceMeshUpdate();
                // SO TRONG CÙNG MỘT HỆ. Bề rộng CẦN và bề rộng KHUNG đều lấy ở toạ độ
                // cục bộ của canvas, nên so thẳng được, không phải quy đổi gì — bản đầu
                // tôi nhân canRong với scaleFactor rồi so với rect ĐÃ quy ra pixel ảnh,
                // và bộ soi báo tràn khắp nơi.
                // BỀ RỘNG ĐÃ VẼ RA, không phải bề rộng mong muốn. GetPreferredValues tính
                // theo cỡ chữ GỐC, nên với nhãn bật tự co nó luôn báo tràn dù thực tế
                // chữ đã co vừa khít — bản đầu của bộ soi này báo tràn ở mọi cảnh vì thế.
                float can = t.textBounds.size.x;
                float khung = t.rectTransform.rect.width - t.margin.x - t.margin.z;
                dong.Add($"    {{\"ten\": \"{An(TenDay(t.transform))}\", \"chu\": \"{An(t.text)}\", " +
                         $"\"rect\": [{r.x:0.0}, {r.y:0.0}, {r.width:0.0}, {r.height:0.0}], " +
                         $"\"mau\": [{t.color.r:0.000}, {t.color.g:0.000}, {t.color.b:0.000}], " +
                         // CỠ CHỮ THIẾT KẾ, không phải cỡ trên ảnh. Ảnh chụp ở 540 rộng
                         // trong khi khung thiết kế là 1080, nên mọi con chữ trên ảnh chỉ
                         // bằng NỬA cỡ người chơi thật sự nhìn — lấy cỡ ảnh làm ngưỡng
                         // "chữ lớn" là bắt nhầm cả những nhãn vốn đã đủ to.
                         $"\"coChuThietKe\": {t.fontSize:0.0}, \"coChuAnh\": {r.height:0.0}, " +
                         $"\"canRong\": {can:0.0}, \"khungRong\": {khung:0.0}, " +
                         $"\"nhieuDong\": {(t.textInfo.lineCount > 1 ? "true" : "false")}, " +
                         $"\"biChe\": {(BiChe(t.rectTransform, r) ? "true" : "false")}, " +
                         $"\"tuCo\": {(t.enableAutoSizing ? "true" : "false")}}}");
            }
            sb.Append(string.Join(",\n", dong)).Append("\n  ],\n  \"nut\": [\n");
            dong.Clear();
            foreach (Button b in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if (!b.isActiveAndEnabled) continue;
                if (!TrenMan((RectTransform)b.transform, w, h, out Rect r)) continue;
                // CHẠM THẬT, không phải so hình chữ nhật. Hai nút chồng nhau về hình học
                // mà một cái nằm sau tấm phủ của màn hình đang mở thì KHÔNG phải lỗi —
                // thứ đáng hỏi là "cú chạm vào giữa nút này rơi trúng cái gì".
                dong.Add($"    {{\"ten\": \"{An(b.name)}\", \"bamDuoc\": {(b.interactable ? "true" : "false")}, " +
                         $"\"chamTrung\": \"{An(ChamTrung(r))}\", " +
                         $"\"rect\": [{r.x:0.0}, {r.y:0.0}, {r.width:0.0}, {r.height:0.0}]}}");
            }
            sb.Append(string.Join(",\n", dong)).Append("\n  ]\n}\n");
            return sb.ToString();
        }

        /// <summary>Màn hình che toàn bộ nào đang mở — để bộ soi biết cái gì là CÓ CHỦ Ý.</summary>
        private string ManHinhMo()
        {
            var up = Object.FindFirstObjectByType<UpgradeScreen>();
            if (up != null && up.IsOpen) return "TRANG BỊ";
            var ch = Object.FindFirstObjectByType<CharacterScreen>();
            if (ch != null && ch.IsOpen) return "NHÂN VẬT";
            var ms = Object.FindFirstObjectByType<MilestoneOverlay>();
            if (ms != null && ms.IsOpen) return "CỘT MỐC";
            var vc = Object.FindFirstObjectByType<VictoryScreen>();
            if (vc != null && vc.IsOpen) return "ĐỈNH THÁP";
            return "";
        }

        /// <summary>
        /// Chữ này có bị một tấm phủ nào đè lên không?
        ///
        /// Cần thiết vì bộ soi so MÀU DANH NGHĨA của chữ với MÀU NỀN THẬT trên ảnh. Chữ
        /// nằm sau một tấm phủ mờ thì nó cũng bị tối đi đúng như nền, nhưng màu danh
        /// nghĩa thì không đổi — so hai thứ đó là báo tương phản thấp ở khắp nơi.
        /// </summary>
        private static bool BiChe(RectTransform rt, Rect r)
        {
            Transform goc = rt; while (goc.parent != null && goc.parent.parent != null) goc = goc.parent;
            int cua = goc.GetSiblingIndex();
            foreach (Image img in Object.FindObjectsByType<Image>(FindObjectsSortMode.None))
            {
                if (!img.isActiveAndEnabled || img.name != "Dim") continue;
                Transform g2 = img.transform;
                while (g2.parent != null && g2.parent.parent != null) g2 = g2.parent;
                if (g2.GetSiblingIndex() > cua) return true;
            }
            return false;
        }

        /// <summary>Cú chạm vào giữa khung này rơi trúng đối tượng nào?</summary>
        private string ChamTrung(Rect r)
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null) return "(không có EventSystem)";
            var data = new UnityEngine.EventSystems.PointerEventData(es)
                       { position = new Vector2(r.center.x, r.center.y) };
            var hit = new List<UnityEngine.EventSystems.RaycastResult>();
            es.RaycastAll(data, hit);
            return hit.Count == 0 ? "(không trúng gì)" : hit[0].gameObject.name;
        }

        private static string TenDay(Transform t)
        {
            string s = t.name;
            for (Transform p = t.parent; p != null && p.parent != null; p = p.parent) s = p.name + "/" + s;
            return s;
        }

        private static string An(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " / ").Replace("\r", "");

        /// <summary>Hình chữ nhật trên màn hình, theo pixel, gốc góc trái-dưới.</summary>
        private bool TrenMan(RectTransform rt, int w, int h, out Rect r)
        {
            r = default;
            var goc = new Vector3[4];
            rt.GetWorldCorners(goc);
            // Camera đang có targetTexture, nên WorldToScreenPoint TRẢ THẲNG pixel của
            // RenderTexture — KHÔNG được quy đổi theo Screen.height thêm lần nữa.
            var p0 = RectTransformUtility.WorldToScreenPoint(_cam, goc[0]);
            var p2 = RectTransformUtility.WorldToScreenPoint(_cam, goc[2]);
            r = new Rect(Mathf.Min(p0.x, p2.x), Mathf.Min(p0.y, p2.y),
                         Mathf.Abs(p2.x - p0.x), Mathf.Abs(p2.y - p0.y));
            return r.width > 0.5f && r.height > 0.5f;
        }

        // ── CÁC CẢNH ──────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Soi_moi_canh()
        {
            yield return Canh("01-tang1-moi-vao");

            Nut("SweepButton").onClick.Invoke();             // nút khoá -> banner lý do
            yield return null; yield return null;
            yield return Canh("02-bam-nut-khoa");

            _gs.AddShards(40000f);
            for (int i = 0; i < 5; i++)
                foreach (Slot s in new[] { Slot.Weapon, Slot.Armor, Slot.Glove, Slot.Ring })
                    _gs.TryUpgrade(s);
            yield return null;
            Nut("GearButton").onClick.Invoke();
            yield return null; yield return null;
            yield return Canh("03-bang-trang-bi");
            Nut("Close").onClick.Invoke();
            yield return null;

            _gs.AwardBoss(10); _gs.AwardBoss(20);
            yield return null;
            Nut("CharButton").onClick.Invoke();
            yield return null; yield return null;
            yield return Canh("04-man-nhan-vat");
            Nut("CloseChar").onClick.Invoke();
            yield return null;

            var hp = Object.FindFirstObjectByType<PlayerHealth>();
            hp.TakeDamage(hp.MaxHp * 0.62f);
            yield return null;
            yield return Canh("05-mau-vơi");

            var ms = Object.FindFirstObjectByType<MilestoneOverlay>();
            ms.Show(10, 3, CharacterRoster.Name(1));
            yield return null;
            yield return Canh("06-man-cot-moc");
            float t0 = 0f; while (t0 < 0.8f) { t0 += Time.unscaledDeltaTime; yield return null; }
            ms.Dong(); Time.timeScale = 1f;
            yield return null;

            _gs.AddShards(200000f);
            while (_gs.Floor < _gs.TowerFloors) _gs.AdvanceFloor();
            yield return null;
            var vc = Object.FindFirstObjectByType<VictoryScreen>();
            vc.Show();
            yield return null;
            yield return Canh("07-dinh-thap");
            float t1 = 0f; while (t1 < 1.4f) { t1 += Time.unscaledDeltaTime; yield return null; }
            vc.Dong(); Time.timeScale = 1f;

            Debug.Log($"[soi] XONG -> {OutDir}");
            Assert.Pass();
        }
    }
}
