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
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.4f, 0.4f);
        [SerializeField] private float hitFlashSeconds = 0.06f;

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

        public bool IsAlive => _hp > 0f;
        public Vector3 Position => transform.position;

        /// <summary>Tỉ lệ máu 0..1. Dùng cho test và cho thanh máu quái ở mốc sau.</summary>
        public float HealthFraction => _maxHp > 0f ? Mathf.Clamp01(_hp / _maxHp) : 0f;

        private void Awake()
        {
            _sprite = GetComponent<SpriteRenderer>();
            _baseColor = _sprite.color;
        }

        /// <summary>Gọi bởi EnemySpawner SAU khi số liệu cân bằng đã nạp xong.</summary>
        public void Initialise(float maxHp, float damage, float attacksPerSecond, float attackRange)
        {
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

            _attackCooldown -= Time.deltaTime;
            if (_attackCooldown > 0f) return;

            float sqr = (player.transform.position - transform.position).sqrMagnitude;
            if (sqr > _attackRange * _attackRange) return;

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
