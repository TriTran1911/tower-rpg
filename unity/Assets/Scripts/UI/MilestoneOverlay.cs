using TMPro;
using TowerRpg.Progression;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TowerRpg.UI
{
    /// <summary>
    /// Màn hình cột mốc khi hạ boss — §5.2 tự ghi "cột mốc lớn, cần màn hình chúc mừng riêng".
    ///
    /// Đây là đỉnh duy nhất của mười phút đầu. Trước Việc 6 nó là một dòng Debug.Log; Việc 6
    /// nâng lên thành banner 2 giây. Banner vẫn trôi qua trong lúc người chơi đang đánh —
    /// màn hình này thì DỪNG hẳn trò chơi lại và bắt chạm để tiếp, vì một cột mốc mà không
    /// cắt được nhịp thì không phải cột mốc.
    ///
    /// Dùng Time.timeScale = 0 nên mọi coroutine theo thời gian game (FloorRunner, xác quái
    /// tan, viên Mảnh bay) đứng im luôn. Đếm giờ ở đây phải dùng unscaledTime.
    /// </summary>
    public sealed class MilestoneOverlay : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text detail;
        [SerializeField] private TMP_Text hint;
        [SerializeField] private Juice.CameraShake shake;

        [SerializeField] private float khoaChamSeconds = 0.6f;

        private float _moTai;
        private bool _dangMo;

        public bool IsOpen => _dangMo;

        private void Start()
        {
            if (root == null) { Debug.LogError("[MilestoneOverlay] Chưa gán 'root'.", this); enabled = false; return; }
            root.SetActive(false);
        }

        public void Show(int floor, int cores, string character)
        {
            if (root == null) return;

            if (title  != null) title.text  = $"HẠ BOSS TẦNG {floor:00}";
            // character = null nghĩa là boss này KHÔNG mở nhân vật nào (đã hết nhân vật).
            // Thà không nhắc còn hơn hứa một cái tên rỗng.
            if (detail != null)
                detail.text = string.IsNullOrEmpty(character)
                            ? $"+{cores} LÕI"
                            : $"+{cores} LÕI\nMỞ NHÂN VẬT: {character}";
            // CHỈ ĐƯỜNG. Người chơi vừa nhận 3 Lõi và không có gì nói cho họ biết tiêu ở
            // đâu — Lõi chỉ dùng được trong màn TRANG BỊ, và chỉ khi một ô đã chạm cấp 10.
            if (hint != null) hint.text = "Lõi tiêu ở nút TRANG BỊ, khi một ô chạm trần cấp\nchạm để tiếp";

            root.SetActive(true);
            root.transform.SetAsLastSibling();
            _dangMo = true;
            _moTai = Time.unscaledTime;

            if (shake != null) shake.Shake();

            // DỪNG hẳn trò chơi. Cột mốc mà không cắt được nhịp thì không phải cột mốc.
            Time.timeScale = 0f;
        }

        public void OnPointerDown(PointerEventData eventData) => Dong();

        private void Update()
        {
            if (!_dangMo) return;
            // Cho bấm bằng phím để test và để người chơi trên máy bàn không kẹt.
            if (Input.anyKeyDown) Dong();
        }

        public void Dong()
        {
            if (!_dangMo) return;
            // Khoá chạm một nhịp ngắn: người chơi đang bấm liên tục lúc hạ boss sẽ đóng
            // mất màn hình trước khi kịp đọc một chữ nào.
            if (Time.unscaledTime - _moTai < khoaChamSeconds) return;

            _dangMo = false;
            Time.timeScale = 1f;
            if (root != null) root.SetActive(false);
        }

        // Thoát scene giữa lúc đang mở thì timeScale phải trả về, nếu không cả game đứng hình.
        private void OnDisable() { if (_dangMo) { _dangMo = false; Time.timeScale = 1f; } }
    }
}
