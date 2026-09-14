using System.Text;
using TMPro;
using TowerRpg.Progression;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TowerRpg.UI
{
    /// <summary>
    /// ĐỈNH THÁP — màn hình chúc mừng khi dọn sạch tầng cuối. Mục cuối của mốc M5.
    ///
    /// Trước M5 tầng 100 KHÔNG CÓ KẾT THÚC: FloorRunner ghi một dòng Debug.Log còn sót
    /// chữ "M3" rồi `continue` — bày lại đúng tầng đó, mãi mãi. Một người leo 100 tầng
    /// trong hai tiếng rưỡi nhận được đúng thứ họ nhận ở tầng 99. §8 giao M5 là "trông
    /// như sản phẩm thật"; không có thứ gì đọc ra "chưa xong" nhanh bằng một trò chơi
    /// không biết mình đã kết thúc.
    ///
    /// CHỈ HIỆN MỘT LẦN. Điều kiện là HighestCleared vừa chạm TowerFloors lần đầu —
    /// suy từ save nên nó sống qua cả việc tắt app. Sau đó tầng 100 vẫn chơi lại được
    /// để cày, nhưng không ai bị chặn bởi một màn hình chúc mừng lặp lại mỗi lượt.
    /// </summary>
    public sealed class VictoryScreen : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text stats;
        [SerializeField] private TMP_Text hint;
        [SerializeField] private Juice.CameraShake shake;
        [SerializeField] private AudioSource endTheme;

        [SerializeField] private float khoaChamSeconds = 1.2f;

        private float _moTai;
        private bool _dangMo;

        public bool IsOpen => _dangMo;

        private void Start()
        {
            if (root == null)
            {
                Debug.LogError("[VictoryScreen] Chưa gán 'root'.", this);
                enabled = false;
                return;
            }
            root.SetActive(false);
        }

        public void Show()
        {
            if (root == null) return;

            GameState gs = GameState.Instance;
            if (title != null) title.text = "ĐỈNH THÁP";
            if (stats != null) stats.text = BangTongKet(gs);
            // Nói thẳng chuyện gì xảy ra tiếp. Người chơi vừa đọc "ĐỈNH THÁP" hoàn toàn
            // có quyền nghĩ là game đóng lại — nếu không nói, cú chạm kế tiếp của họ là
            // một cú chạm đầy nghi ngờ.
            if (hint != null) hint.text = "Tầng cuối vẫn chơi lại được để cày\nchạm để tiếp";

            root.SetActive(true);
            root.transform.SetAsLastSibling();
            _dangMo = true;
            _moTai = Time.unscaledTime;

            if (shake != null) shake.Shake();

            // Nhạc kết. AudioDirector vẫn chạy nền — hạ nó xuống thì phải đụng vào vòng
            // fade của nó; thay vào đó End Theme phát ở một nguồn riêng to hơn, và
            // AudioDirector tự về nhạc tầng thường sau khi màn hình đóng.
            if (endTheme != null && endTheme.clip != null) endTheme.Play();

            Time.timeScale = 0f;
        }

        /// <summary>
        /// Bảng tổng kết. Mỗi dòng là một con số người chơi TỰ LÀM RA — không có dòng
        /// nào là hằng số của game. Bỏ dòng thời gian khi save cũ không có số đó.
        /// </summary>
        private static string BangTongKet(GameState gs)
        {
            if (gs == null) return "";
            var sb = new StringBuilder();

            // CỘT SỐ CĂN BẰNG <pos=52%>, không bằng khoảng trắng. Nhãn tiếng Việt dài
            // ngắn khác nhau ("Lõi đã tiêu" và "Mảnh tích luỹ" lệch nhau 4 chữ), và font
            // không phải chữ đều — căn bằng dấu cách thì cột số răng cưa, đọc ra như một
            // danh sách chứ không phải một bảng. <mspace> thì hỏng chiều ngược lại: nó
            // ép luôn cả phần nhãn thành chữ đều, mà tiếng Việt có dấu trông rất xấu.
            void Dong(string nhan, string gt) => sb.Append($"{nhan}<pos=52%>{gt}\n");

            if (gs.PlaySeconds > 1f) Dong("Thời gian leo", DocGio(gs.PlaySeconds));
            Dong("Boss đã hạ",    $"{gs.BossesKilled}");
            Dong("Mảnh tích luỹ", $"{gs.ShardsClimbed + gs.ShardsSwept:N0}");
            Dong("Lõi đã tiêu",   $"{gs.Gear.CoresSpent()}/{gs.CoresTotalInGame}");
            Dong("Trang bị",      string.Join("/", gs.Gear.Snapshot()));
            Dong("Bậc đột phá",   string.Join("/", gs.Gear.TierSnapshot()));
            sb.Append($"Nhân vật<pos=52%>{CharacterRoster.Name(gs.CharacterIndex)}");
            return sb.ToString();
        }

        /// <summary>"2 giờ 41 phút" — giờ và phút, không bao giờ hiện giây.</summary>
        public static string DocGio(float giay)
        {
            int tong = Mathf.Max(0, Mathf.FloorToInt(giay));
            int gio = tong / 3600;
            int phut = (tong % 3600) / 60;
            if (gio > 0) return $"{gio} giờ {phut} phút";
            // Dưới một phút thì "0 phút" đọc như một lỗi; nói giây.
            return phut > 0 ? $"{phut} phút" : $"{tong} giây";
        }

        public void OnPointerDown(PointerEventData eventData) => Dong();

        private void Update()
        {
            if (!_dangMo) return;
            if (Input.anyKeyDown) Dong();
        }

        public void Dong()
        {
            if (!_dangMo) return;
            // Khoá chạm dài hơn màn cột mốc (1,2s so với 0,6s): đây là bảng SÁU DÒNG SỐ
            // sau hai tiếng rưỡi, không phải một dòng "+3 Lõi".
            if (Time.unscaledTime - _moTai < khoaChamSeconds) return;

            _dangMo = false;
            Time.timeScale = 1f;
            if (endTheme != null && endTheme.isPlaying) endTheme.Stop();
            if (root != null) root.SetActive(false);
        }

        // Cùng cái bẫy của MilestoneOverlay: thoát scene lúc đang mở là cả game đứng hình.
        private void OnDisable() { if (_dangMo) { _dangMo = false; Time.timeScale = 1f; } }
    }
}
