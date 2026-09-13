using TowerRpg.Core;
using TowerRpg.Enemies;
using UnityEngine;

namespace TowerRpg.UI
{
    /// <summary>
    /// Thanh máu nổi trên đầu quái.
    ///
    /// ĐÂY LÀ THỨ ĐẮT GIÁ NHẤT TRONG CẢ ĐỢT SỬA NHỊP ĐỘ, và lý do rất cụ thể: §5.11 khoá
    /// cứng tăng trưởng ở 4,4%/cấp, nên trên số sát thương bay lên, Vũ khí cấp 1 lên cấp 2
    /// đổi "10,0" thành "10,4" — thấy được nhưng không CẢM được. Trên một thanh máu chia
    /// theo số đòn, đúng 4,4% đó đọc ra là "tám đòn còn bảy đòn". Cùng một con số, một
    /// đằng là chữ số thập phân, một đằng là cả một đòn bớt đi.
    ///
    /// Nói cách khác: đây là cách DUY NHẤT làm cho tiến trình cảm nhận được mà không phải
    /// đụng một chữ số nào ràng buộc 3 đang khoá.
    ///
    /// DÙNG SpriteRenderer, KHÔNG DÙNG Canvas thế giới: một tầng có 6-20 quái, mỗi con một
    /// Canvas là 6-20 lần dựng lưới giao diện mỗi khung hình. Thanh máu chỉ là hai hình
    /// chữ nhật — SpriteRenderer đủ và rẻ hơn hẳn.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer background;
        [SerializeField] private SpriteRenderer fill;

        /// <summary>
        /// Đối tượng cha của ruột, đặt tại MÉP TRÁI của thanh. PHẢI co giãn cái này chứ
        /// không phải co giãn chính `fill`: co giãn `fill` thì nó co quanh tâm CỦA CHÍNH NÓ,
        /// và thanh máu teo lại đều hai bên như một viên thuốc — thay vì vơi từ phải sang
        /// trái như mọi thanh máu trên đời. Bản đầu tôi dựng đúng cái pivot này rồi vẫn co
        /// nhầm đứa con; test xanh, thanh vẫn sai, chỉ đếm pixel trên ảnh chụp mới thấy.
        /// </summary>
        [SerializeField] private Transform fillPivot;

        [Header("Thẩm mỹ, không phải cân bằng")]
        [SerializeField] private Color fullColour = new Color(0.80f, 0.24f, 0.26f);
        [SerializeField] private Color lowColour  = new Color(0.95f, 0.55f, 0.20f);
        [SerializeField] private float lowBelow = 0.3f;

        private Enemy _enemy;
        private float _hideAbove = 0.999f;
        private Vector3 _fillScale0;
        private bool _shown;

        private void Awake()
        {
            _enemy = GetComponent<Enemy>();
            if (fillPivot == null && fill != null) fillPivot = fill.transform.parent;
            if (fillPivot != null) _fillScale0 = fillPivot.localScale;
            SetVisible(false);
        }

        private void Start() => BalanceConfig.TryUse(this, b =>
            _hideAbove = b.Get("juice.healthBarHideAbove"));

        private void SetVisible(bool on)
        {
            _shown = on;
            if (background != null) background.enabled = on;
            if (fill != null) fill.enabled = on;
        }

        // LateUpdate: chạy SAU khi AutoAttack đã gây sát thương trong Update, nên thanh
        // luôn hiện đúng máu của khung hình này chứ không trễ một khung.
        private void LateUpdate()
        {
            if (_enemy == null || fill == null || fillPivot == null) return;

            float f = _enemy.HealthFraction;

            // Ẩn tới khi trúng đòn đầu tiên: một tầng vừa bày mà có 6 thanh đầy nằm đó thì
            // chúng chỉ là rác thị giác — không thanh nào đang kể chuyện gì cả.
            bool want = _enemy.IsAlive && f < _hideAbove;
            if (want != _shown) SetVisible(want);
            if (!want) return;

            fillPivot.localScale = new Vector3(_fillScale0.x * Mathf.Clamp01(f),
                                               _fillScale0.y, _fillScale0.z);
            fill.color = f <= lowBelow ? lowColour : fullColour;
        }
    }
}
