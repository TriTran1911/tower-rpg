using System;
using System.Collections.Generic;
using TowerRpg.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace TowerRpg.Loot
{
    /// <summary>
    /// Pool viên Mảnh. Cùng khuôn với DamagePopupSpawner — pool có sẵn, prewarm, đọc số
    /// liệu qua BalanceConfig.TryUse. Một tầng 6 quái thả tối đa 6 viên cùng lúc, nhưng
    /// đường Retry có thể chồng lượt nên pool rộng hơn cho chắc.
    /// </summary>
    public sealed class ShardDropSpawner : MonoBehaviour
    {
        [SerializeField] private ShardPickup pickupPrefab;
        [SerializeField] private int prewarm = 16;
        [SerializeField] private int maxSize = 64;

        /// <summary>Bắn khi một viên thật sự vào ví. FloorRunner nghe để tính sổ hũ Mảnh.</summary>
        public event Action<float> Collected;

        private ObjectPool<ShardPickup> _pool;
        private readonly List<ShardPickup> _live = new List<ShardPickup>();

        private float _popDistance, _popSeconds, _magnetRadius, _pickupRadius, _flySpeed, _maxLifetime;
        private bool _ready;

        public int LiveCount => _live.Count;

        private void Awake()
        {
            if (pickupPrefab == null)
            {
                Debug.LogError("[ShardDropSpawner] Chưa gán pickupPrefab.", this);
                enabled = false;
                return;
            }

            _pool = new ObjectPool<ShardPickup>(
                CreateInstance,
                actionOnGet: null,
                actionOnRelease: p => { if (p != null) p.gameObject.SetActive(false); },
                actionOnDestroy: p => { if (p != null) Destroy(p.gameObject); },
                collectionCheck: false, defaultCapacity: prewarm, maxSize: maxSize);

            Prewarm();
        }

        // TryUse PHẢI gọi từ Start, KHÔNG BAO GIỜ từ Awake: BalanceConfig.Instance được
        // gán trong Awake của chính nó, mà thứ tự Awake giữa các component là KHÔNG XÁC
        // ĐỊNH. Gọi ở Awake thì tuỳ hôm nay Unity duyệt thế nào mà nó thấy hoặc không
        // thấy Instance — ở đây là không thấy, và cả 45 test hỏng cùng một dòng.
        // Cùng khuôn với DamagePopupSpawner, thứ tôi tưởng mình đã sao chép đúng.
        private void Start() => BalanceConfig.TryUse(this, ApplyBalance);

        private void ApplyBalance(BalanceConfig b)
        {
            _popDistance  = b.Get("loot.popDistance");
            _popSeconds   = b.Get("loot.popSeconds");
            _magnetRadius = b.Get("loot.magnetRadius");
            _pickupRadius = b.Get("loot.pickupRadius");
            _flySpeed     = b.Get("loot.flySpeed");
            _maxLifetime  = b.Get("loot.maxLifetime");
            _ready = true;
        }

        private ShardPickup CreateInstance()
        {
            ShardPickup p = Instantiate(pickupPrefab, transform);
            p.gameObject.SetActive(false);
            return p;
        }

        private void Prewarm()
        {
            if (prewarm <= 0) return;
            var warmed = new List<ShardPickup>(prewarm);
            for (int i = 0; i < prewarm; i++) warmed.Add(_pool.Get());
            for (int i = 0; i < warmed.Count; i++) _pool.Release(warmed[i]);
        }

        /// <summary>
        /// Thả một viên tại chỗ quái vừa gục.
        ///
        /// value = 0 là hợp lệ và có nghĩa: viên LOAN BÁO (Lõi tím của boss). Lõi đã do
        /// AwardBoss cấp rồi, viên bay ra chỉ để người chơi nhìn thấy điều đó xảy ra.
        /// Nhặt nó không cộng gì — và đó là chủ ý, không phải thiếu sót.
        /// </summary>
        public void Drop(Vector3 position, float value)
        {
            if (!_ready || value < 0f) return;

            ShardPickup p = _pool.Get();
            _live.Add(p);
            p.Launch(position, value, _popDistance, _popSeconds, _magnetRadius,
                     _pickupRadius, _flySpeed, _maxLifetime, OnCollected, Release);
        }

        private void OnCollected(float value) => Collected?.Invoke(value);

        private void Release(ShardPickup p)
        {
            _live.Remove(p);
            _pool.Release(p);
        }

        /// <summary>Cộng ngay mọi viên còn trên sàn. Dùng lúc dọn sạch tầng.</summary>
        public void FlushAll()
        {
            // Duyệt ngược: Collect() gọi Release() và Release() sửa _live ngay trong vòng lặp.
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                ShardPickup p = _live[i];
                if (p != null) p.CollectNow();
            }
            _live.Clear();
        }

        /// <summary>
        /// Trả hết về pool mà KHÔNG cộng Mảnh.
        ///
        /// BẮT BUỘC gọi ở đầu SpawnFloor: EnemyRegistry.ClearAll() chỉ dọn QUÁI, không dọn
        /// viên đang nằm trên sàn. Không có hàm này thì đường Retry (chết rồi bày lại) để
        /// nguyên viên cũ, và chúng được cộng vào hũ của lượt mới.
        /// </summary>
        public void DiscardAll()
        {
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                ShardPickup p = _live[i];
                if (p != null) p.Discard();
            }
            _live.Clear();
        }
    }
}
