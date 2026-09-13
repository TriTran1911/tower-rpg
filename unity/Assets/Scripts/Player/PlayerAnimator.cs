using TowerRpg.Progression;
using UnityEngine;

namespace TowerRpg.Player
{
    /// <summary>
    /// Hình nhân vật theo trạng thái: đứng · đi · đánh, và quay theo hướng.
    ///
    /// Bộ asset cho mỗi nhân vật bốn file 64x16 (bốn hướng, mỗi hướng một khung 16x16):
    /// Idle · Walk · Attack. Không dùng Animator/AnimationClip — bốn sprite đổi tay bằng
    /// một dòng rẻ hơn hẳn một máy trạng thái, và nó khớp với cách PlayerAppearance (§5.5b)
    /// đã đổi hình khi đổi nhân vật.
    ///
    /// Thứ tự hướng trong file: 0 xuống · 1 trái · 2 phải · 3 lên.
    /// </summary>
    [DefaultExecutionOrder(-30)]
    public sealed class PlayerAnimator : MonoBehaviour
    {
        public const int Huong = 4;

        [SerializeField] private SpriteRenderer target;
        [SerializeField] private PlayerController controller;

        // [nhân vật][hướng] — phẳng hoá vì Unity không serialize mảng hai chiều.
        [SerializeField] private Sprite[] idle;
        [SerializeField] private Sprite[] walk;
        [SerializeField] private Sprite[] attack;

        [SerializeField] private float attackSeconds = 0.18f;

        private int _nhanVat, _huong = 0;
        private float _danhToi;

        private void Awake()
        {
            if (target == null) target = GetComponent<SpriteRenderer>();
            if (controller == null) controller = GetComponent<PlayerController>();
        }

        private void OnEnable()
        {
            if (GameState.Instance != null) GameState.Instance.CharacterChanged += DoiNhanVat;
        }

        private void OnDisable()
        {
            if (GameState.Instance != null) GameState.Instance.CharacterChanged -= DoiNhanVat;
        }

        private bool _hooked;
        private void DoiNhanVat(int i) => _nhanVat = Mathf.Max(0, i);

        /// <summary>AutoAttack gọi khi vừa ra đòn.</summary>
        public void BaoDanh(Vector3 huongToiMucTieu)
        {
            _danhToi = Time.time + attackSeconds;
            _huong = TinhHuong(huongToiMucTieu);
        }

        private static int TinhHuong(Vector3 v)
        {
            if (Mathf.Abs(v.x) > Mathf.Abs(v.y)) return v.x < 0f ? 1 : 2;
            return v.y > 0f ? 3 : 0;
        }

        private void Update()
        {
            if (!_hooked && GameState.Instance != null && GameState.Instance.Ready)
            {
                GameState.Instance.CharacterChanged += DoiNhanVat;
                _nhanVat = GameState.Instance.CharacterIndex;
                _hooked = true;
            }

            if (target == null) return;

            bool dangDanh = Time.time < _danhToi;
            bool dangDi = controller != null && controller.IsMoving;

            if (dangDi && !dangDanh)
            {
                Vector2 v = controller.LastInput;
                if (v.sqrMagnitude > 0.0001f) _huong = TinhHuong(v);
            }

            Sprite[] bo = dangDanh ? attack : dangDi ? walk : idle;
            Sprite s = LayO(bo, _nhanVat, _huong);
            if (s != null) target.sprite = s;
        }

        private static Sprite LayO(Sprite[] bo, int nhanVat, int huong)
        {
            if (bo == null) return null;
            int i = nhanVat * Huong + huong;
            return i >= 0 && i < bo.Length ? bo[i] : null;
        }
    }
}
