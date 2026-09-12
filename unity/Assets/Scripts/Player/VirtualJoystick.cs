using UnityEngine;
using UnityEngine.EventSystems;

namespace TowerRpg.Player
{
    /// <summary>
    /// Cần gạt ảo một ngón. Dùng EventSystems nên chạy được với cả Input Manager cũ lẫn
    /// Input System mới, không cần cài gói nào — hợp phạm vi M1.
    /// Từ M2, khi input phức tạp hơn, chuyển sang Input System đúng như §9.1.
    /// </summary>
    public sealed class VirtualJoystick : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;
        [SerializeField] private Canvas canvas;

        /// <summary>Vector đã chuẩn hoá, độ dài 0..1.</summary>
        public Vector2 Value { get; private set; }

        private void Awake()
        {
            if (background == null) background = transform as RectTransform;
            if (canvas == null) canvas = GetComponentInParent<Canvas>();

            if (handle == null)
                Debug.LogWarning("[VirtualJoystick] Chưa gán 'handle' — núm cần gạt sẽ không nhúc nhích. " +
                                 "Cần gạt vẫn hoạt động, chỉ là không có phản hồi thị giác.", this);
        }

        /// <summary>
        /// Tính mỗi lần chạm thay vì lưu sẵn trong Awake: dùng rect.width (kích thước THẬT
        /// sau khi bố cục chạy xong) chứ không dùng sizeDelta, vì với neo kiểu kéo giãn thì
        /// sizeDelta bằng 0 và cần gạt sẽ chết câm.
        /// </summary>
        private float Radius => background != null ? background.rect.width * 0.5f : 0f;

        public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

        public void OnDrag(PointerEventData eventData)
        {
            if (background == null) return;

            float radius = Radius;
            if (radius <= 0f)
            {
                Debug.LogError("[VirtualJoystick] Bán kính cần gạt bằng 0 — RectTransform chưa có kích thước. " +
                               "Đặt Width/Height cụ thể cho object Joystick.", this);
                return;
            }

            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    background, eventData.position, cam, out Vector2 local))
                return;

            Vector2 clamped = Vector2.ClampMagnitude(local, radius);
            Value = clamped / radius;

            if (handle != null) handle.anchoredPosition = clamped;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Value = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
        }
    }
}
