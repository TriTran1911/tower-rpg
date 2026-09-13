using TowerRpg.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TowerRpg.Player
{
    /// <summary>
    /// Cần gạt ảo ĐỘNG: hiện ra ngay nơi ngón tay đặt xuống, biến mất khi nhấc lên.
    ///
    /// VÌ SAO ĐỘNG CHỨ KHÔNG CỐ ĐỊNH (docs/GIAO-DIEN.md §3): bản đầu đặt cố định ở góc
    /// dưới-trái, mà phần lớn người chơi cầm máy tay phải — ngón cái không với tới. Cần gạt
    /// động giải quyết triệt để: không phải chọn tay thuận, không phải với, và không chiếm
    /// chỗ trên màn hình khi không dùng.
    ///
    /// Dùng EventSystems nên chạy với cả Input Manager cũ lẫn Input System mới.
    /// </summary>
    public sealed class VirtualJoystick : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Vùng nhận chạm — nên phủ nửa dưới màn hình")]
        [SerializeField] private RectTransform touchZone;

        [Header("Phần nhìn thấy, ẩn khi không chạm")]
        [SerializeField] private RectTransform visual;
        [SerializeField] private RectTransform handle;
        [SerializeField] private Canvas canvas;

        /// <summary>Vector đã chuẩn hoá, độ dài 0..1.</summary>
        public Vector2 Value { get; private set; }

        /// <summary>Đang có ngón đặt trên vùng chạm — dùng cho hiệu ứng, không dùng cho §5.3.</summary>
        public bool IsHeld { get; private set; }

        private float _radius;

        private void Awake()
        {
            if (touchZone == null) touchZone = transform as RectTransform;
            if (canvas == null) canvas = GetComponentInParent<Canvas>();

            if (visual == null)
                Debug.LogError("[VirtualJoystick] Chưa gán 'visual'. Cần gạt sẽ không hiện.", this);
            else
                visual.gameObject.SetActive(false);

            if (handle == null)
                Debug.LogWarning("[VirtualJoystick] Chưa gán 'handle' — núm sẽ không nhúc nhích.", this);
        }

        private void Start() => BalanceConfig.TryUse(this, ApplyBalance);

        private void ApplyBalance(BalanceConfig balance)
        {
            // Bán kính lấy từ CHÍNH kích thước của phần nhìn thấy, không viết cứng.
            _radius = visual != null ? visual.rect.width * 0.5f : 0f;

            if (_radius <= 0f)
                Debug.LogError("[VirtualJoystick] Bán kính bằng 0 — RectTransform của 'visual' " +
                               "chưa có kích thước. Đặt Width/Height cụ thể, đừng dùng neo kéo giãn.", this);
        }

        private Camera EventCamera =>
            canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (visual == null || _radius <= 0f) return;

            // đặt cần gạt đúng nơi ngón chạm
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)visual.parent, eventData.position, EventCamera, out Vector2 local))
                return;

            visual.anchoredPosition = local;
            visual.gameObject.SetActive(true);
            IsHeld = true;

            if (handle != null) handle.anchoredPosition = Vector2.zero;
            Value = Vector2.zero;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsHeld || visual == null || _radius <= 0f) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    visual, eventData.position, EventCamera, out Vector2 local))
                return;

            Vector2 clamped = Vector2.ClampMagnitude(local, _radius);
            Value = clamped / _radius;

            if (handle != null) handle.anchoredPosition = clamped;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsHeld = false;
            Value = Vector2.zero;

            if (handle != null) handle.anchoredPosition = Vector2.zero;
            if (visual != null) visual.gameObject.SetActive(false);
        }
    }
}
