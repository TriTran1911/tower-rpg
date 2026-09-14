using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TowerRpg.Juice
{
    /// <summary>
    /// Chuyển cảnh giữa hai tầng — mục "chuyển cảnh" của mốc M5.
    ///
    /// HAI CẤP ĐỘ, cố ý khác hẳn nhau:
    ///
    ///   • TẦNG THƯỜNG — chớp tối nhanh, KHÔNG đen hẳn (tới `dipAlpha`), tổng ~0,3 giây.
    ///     Nó xảy ra 95 lần một lượt chơi nên phải rẻ về mặt chú ý: đủ để mắt biết "đã
    ///     sang tầng khác", không đủ để thành một cái cửa phải chờ.
    ///
    ///   • ĐỔI CHƯƠNG — đen hẳn, giữ tối trong lúc hiện tên chương, tổng ~2 giây. Nó xảy
    ///     ra ĐÚNG 4 lần cả game (tầng 21, 41, 61, 81). Trước M5 người chơi chỉ thấy màu
    ///     sàn đột nhiên khác đi giữa hai tầng mà không ai nói gì — 2,4 MB bảng nền và
    ///     năm loại quái của M4 đi qua mà không được đặt tên.
    ///
    /// VÌ SAO unscaledDeltaTime: màn hình cột mốc (hạ boss) đặt Time.timeScale = 0, và
    /// boss đứng ở ĐÚNG tầng 20/40/60/80 — tức ngay trước mọi lần đổi chương. Dùng
    /// deltaTime là cả bốn tấm thẻ chương đứng hình vĩnh viễn. Đây là cái bẫy timeScale
    /// thứ ba của dự án; hai lần trước nằm ở bảng bấm giờ và ở vòng chờ của phiên chơi.
    /// </summary>
    public sealed class SceneTransition : MonoBehaviour
    {
        [SerializeField] private Image veil;          // tấm phủ đen, phủ kín màn hình
        [SerializeField] private TMP_Text chapterNo;  // "CHƯƠNG 2"
        [SerializeField] private TMP_Text chapterName;// "CHIẾU & GIẤY DẦU"
        [SerializeField] private TMP_Text chapterRange; // "TẦNG 21 – 40"

        [Header("Tầng thường")]
        [SerializeField] private float dipAlpha = 0.55f;
        [SerializeField] private float dipOut = 0.14f;
        [SerializeField] private float dipIn  = 0.16f;

        [Header("Đổi chương")]
        [SerializeField] private float chapterOut  = 0.45f;
        [SerializeField] private float chapterHold = 1.30f;
        [SerializeField] private float chapterIn   = 0.55f;

        /// <summary>Tên năm chương — khớp §3 quyết định 11 và thư mục Art/Chapters/.</summary>
        public static readonly string[] TenChuong =
        {
            "NỀN ĐÁ", "CHIẾU & GIẤY DẦU", "NẮNG ĐỒNG", "CHÀM ĐÊM", "SƯƠNG LỆCH",
        };

        private bool _dangChay;

        /// <summary>Đang che màn hình — FloorRunner dùng để không chồng hai lần chuyển cảnh.</summary>
        public bool IsRunning => _dangChay;

        private void Awake()
        {
            // Tấm phủ phải nằm TRÊN mọi thứ của HUD nhưng DƯỚI màn hình cột mốc: cột mốc
            // là thứ người chơi phải đọc, che nó bằng màn đen là xoá mất phần thưởng.
            // Thứ tự này do BuildM1Scene xếp; ở đây chỉ đảm bảo bắt đầu trong suốt.
            DatAlpha(0f);
            HienThe(false);
        }

        /// <summary>Chớp tối ngắn giữa hai tầng thường. Gọi TRƯỚC khi bày tầng mới.</summary>
        public IEnumerator ChuyenTang()
        {
            if (veil == null || _dangChay) yield break;
            _dangChay = true;
            yield return Mo(0f, dipAlpha, dipOut);
            _dangChay = false;
        }

        /// <summary>Kéo màn đen về trong suốt. Gọi SAU khi tầng mới đã bày xong.</summary>
        public IEnumerator MoMan()
        {
            if (veil == null) yield break;
            _dangChay = true;
            yield return Mo(veil.color.a, 0f, dipIn);
            HienThe(false);
            _dangChay = false;
        }

        /// <summary>
        /// Thẻ chương: đen hẳn → hiện tên → giữ. Gọi TRƯỚC khi bày tầng đầu của chương.
        /// Không tự mở màn — người gọi phải gọi MoMan() sau khi đã bày xong tầng mới,
        /// để tấm nền mới của chương lộ ra ĐÃ SẴN SÀNG chứ không lộ ra lúc đang dựng.
        /// </summary>
        public IEnumerator TheChuong(int chuong0, int tangDau, int tangCuoi)
        {
            if (veil == null) yield break;
            _dangChay = true;

            if (chapterNo   != null) chapterNo.text   = $"CHƯƠNG {chuong0 + 1}";
            if (chapterName != null) chapterName.text = chuong0 >= 0 && chuong0 < TenChuong.Length
                                                      ? TenChuong[chuong0] : "";
            if (chapterRange != null) chapterRange.text = $"TẦNG {tangDau} – {tangCuoi}";

            yield return Mo(veil.color.a, 1f, chapterOut);
            HienThe(true);
            yield return DoiThuc(chapterHold);
            HienThe(false);

            // Giữ nguyên màn đen. MoMan() ở đầu vòng sau sẽ kéo nó ra bằng chapterIn.
            _dangChay = false;
        }

        /// <summary>Thời gian mở màn sau thẻ chương dài hơn sau một tầng thường.</summary>
        public IEnumerator MoManChuong()
        {
            if (veil == null) yield break;
            _dangChay = true;
            yield return Mo(veil.color.a, 0f, chapterIn);
            HienThe(false);
            _dangChay = false;
        }

        private IEnumerator Mo(float tu, float den, float giay)
        {
            if (giay <= 0f) { DatAlpha(den); yield break; }
            float t = 0f;
            while (t < giay)
            {
                t += Time.unscaledDeltaTime;
                DatAlpha(Mathf.Lerp(tu, den, Mathf.Clamp01(t / giay)));
                yield return null;
            }
            DatAlpha(den);
        }

        private IEnumerator DoiThuc(float giay)
        {
            float t = 0f;
            while (t < giay) { t += Time.unscaledDeltaTime; yield return null; }
        }

        private void DatAlpha(float a)
        {
            if (veil == null) return;
            Color c = veil.color;
            veil.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(a));
            // KHÔNG BAO GIỜ chặn chạm. Tấm phủ này thuần trang trí; để nó ăn raycast là
            // nuốt mất cú chạm của người chơi, và tệ hơn là cướp cần điều khiển giữa lúc
            // ngón tay đang kéo. Tắt hẳn Image lúc trong suốt để không vẽ thừa một lớp
            // toàn màn hình mỗi khung hình suốt cả lượt chơi.
            veil.raycastTarget = false;
            veil.enabled = a > 0.001f;
        }

        private void HienThe(bool hien)
        {
            if (chapterNo    != null) chapterNo.enabled    = hien;
            if (chapterName  != null) chapterName.enabled  = hien;
            if (chapterRange != null) chapterRange.enabled = hien;
        }
    }
}
