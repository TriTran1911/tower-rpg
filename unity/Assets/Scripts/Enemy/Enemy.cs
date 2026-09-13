using TowerRpg.Combat;
using TowerRpg.Player;
using UnityEngine;

namespace TowerRpg.Enemies
{
    /// <summary>
    /// M1: quái đứng yên, có máu, đánh người chơi khi người chơi vào tầm, chết thì biến mất.
    /// Không di chuyển, không AI.
    ///
    /// Tầm đánh của quái LỚN HƠN tầm đánh của người chơi (xem m1-balance.csv). Nhờ vậy,
    /// vào được vị trí đánh cũng là vào vị trí ăn đòn — đó chính là sức căng của §5.3.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class Enemy : MonoBehaviour, IDamageable
    {
        [Header("Phản hồi khi trúng đòn (thẩm mỹ, không phải cân bằng)")]
        // TRẮNG chứ không phải đỏ nhạt: bảng màu "Mực & Son" (quyết định #18/#19) làm
        // TOÀN BỘ quái ngả đỏ, nên nhân một màu đỏ nhạt lên nền đỏ là gần như vô hình.
        // 0,06 giây = 3,6 khung hình ở 60fps, mắt không kịp bắt. 0,10 = 6 khung hình.
        [SerializeField] private Color hitFlashColor = Color.white;
        [SerializeField] private float hitFlashSeconds = 0.10f;

        private SpriteRenderer _sprite;
        private Color _baseColor;
        private float _flashTimer;

        private float _hp;
        private float _maxHp;
        private float _damage;
        private float _attackRange;
        private float _attackInterval;
        private float _attackCooldown;
        private bool _armed;

        /// <summary>Boss hay quái thường. Chỉ đổi cách hiển thị và cách tính điểm rơi — §5.10.</summary>
        public bool IsBoss { get; private set; }

        public bool IsAlive => _hp > 0f;
        public Vector3 Position => transform.position;

        /// <summary>Tỉ lệ máu 0..1. Dùng cho test và cho thanh máu quái ở mốc sau.</summary>
        public float HealthFraction => _maxHp > 0f ? Mathf.Clamp01(_hp / _maxHp) : 0f;

        private void Awake()
        {
            _sprite = GetComponent<SpriteRenderer>();
            _baseColor = _sprite.color;
        }

        /// <summary>Gọi bởi FloorRunner SAU khi số liệu cân bằng đã nạp xong.</summary>
        public void Initialise(float maxHp, float damage, float attacksPerSecond, float attackRange,
                               bool isBoss = false)
        {
            IsBoss = isBoss;
            _hp = maxHp;
            _maxHp = maxHp;
            _damage = damage;
            _attackRange = attackRange;
            _attackInterval = attacksPerSecond > 0f ? 1f / attacksPerSecond : float.MaxValue;
            _attackCooldown = _attackInterval;          // không đánh ngay lúc vừa sinh
            _armed = maxHp > 0f;

            EnemyRegistry.Register(this);
        }

        private void Update()
        {
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0f) _sprite.color = _baseColor;
            }

            if (!_armed || !IsAlive) return;

            PlayerHealth player = PlayerHealth.Current;
            if (player == null || !player.IsAlive) return;

            // ĐỐI XỨNG VỚI AutoAttack: người chơi di chuyển thì hồi chiêu ĐỨNG YÊN;
            // quái ngoài tầm thì hồi chiêu cũng phải ĐỨNG YÊN. Thứ tự hai khối này là
            // toàn bộ vấn đề, không phải một chi tiết.
            //
            // Bản cũ trừ hồi chiêu BẤT KỂ người chơi ở đâu, rồi ra ngoài tầm thì return
            // mà KHÔNG reset -> hồi chiêu tụt âm sâu -> người chơi vừa bước vào lại là
            // ăn đòn NGAY khung hình đó. Lùi ra dưới một nhịp né được ĐÚNG 0 đòn, nên
            // di chuyển chỉ tổ mất DPS: bị phạt hai lần, thưởng không lần nào.
            //
            // Sau khi đảo, sát thương nhận TỈ LỆ THUẬN với thời gian đứng trong tầm.
            // Đó là điều ô 'Thông số'!B9 = 0,5 của can-bang.xlsx mô tả.
            //
            // KHÔNG reset _attackCooldown khi ra ngoài tầm: làm thế là tặng một cửa sổ
            // ân huệ đầy mỗi lần quay lại, tức nới biên thật chứ không phải vá lỗi.
            float sqr = (player.transform.position - transform.position).sqrMagnitude;
            if (sqr > _attackRange * _attackRange) return;

            _attackCooldown -= Time.deltaTime;
            if (_attackCooldown > 0f) return;

            player.TakeDamage(_damage);
            _attackCooldown = _attackInterval;
        }

        public void TakeDamage(float amount, bool isCrit)
        {
            if (!IsAlive) return;

            _hp -= amount;

            _sprite.color = hitFlashColor;
            _flashTimer = hitFlashSeconds;

            if (_hp <= 0f) Destroy(gameObject);
        }

        // Một chỗ gỡ đăng ký duy nhất — chạy cho cả khi chết lẫn khi bị huỷ theo scene.
        private void OnDestroy() => EnemyRegistry.Unregister(this);
    }
}
