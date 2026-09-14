using System.Collections.Generic;
using TowerRpg.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace TowerRpg.Juice
{
    /// <summary>
    /// Pool cụm khói chết. Cùng khuôn SlashFxSpawner: pool, prewarm, TryUse ở Start.
    ///
    /// prewarm 8 chứ không phải 6: một tầng có 6 quái, nhưng đòn quét cuối có thể giết
    /// hai con trong cùng một khung hình trong lúc khói của tầng trước chưa trả về pool.
    /// </summary>
    public sealed class PuffFxSpawner : MonoBehaviour
    {
        [SerializeField] private PuffFx prefab;
        [SerializeField] private int prewarm = 8;
        [SerializeField] private int maxSize = 24;

        // TRUY CẬP TĨNH, cùng khuôn SfxPlayer và vì cùng một lý do: khói chết do Enemy
        // gọi, mà Enemy sinh ra hàng loạt trong SpawnFloor. Luồn thêm một tham chiếu qua
        // Initialise() — đã 7 tham số — chỉ để mang một thứ THUẦN TRANG TRÍ là bắt mã
        // cân bằng gánh mã hiệu ứng. SlashFxSpawner thì tiêm thẳng, vì nó chỉ có MỘT
        // người gọi (AutoAttack) và người gọi đó sống suốt scene.
        private static PuffFxSpawner _instance;

        private ObjectPool<PuffFx> _pool;
        private float _seconds = 0.3f;
        private float _scale = 1f, _bossScale = 2.2f;
        private bool _ready;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(this); return; }
            _instance = this;

            if (prefab == null) { enabled = false; return; }
            _pool = new ObjectPool<PuffFx>(
                () => { PuffFx f = Instantiate(prefab, transform); f.gameObject.SetActive(false); return f; },
                actionOnRelease: f => { if (f != null) f.gameObject.SetActive(false); },
                actionOnDestroy: f => { if (f != null) Destroy(f.gameObject); },
                collectionCheck: false, defaultCapacity: prewarm, maxSize: maxSize);

            var warm = new List<PuffFx>(prewarm);
            for (int i = 0; i < prewarm; i++) warm.Add(_pool.Get());
            foreach (PuffFx f in warm) _pool.Release(f);
        }

        // Start chứ không phải Awake — BalanceConfig.Instance gán trong Awake của chính nó
        // và thứ tự Awake là không xác định. Luật này đã trả giá bằng 45/45 test hỏng ở Việc 5.
        private void Start() => BalanceConfig.TryUse(this, b =>
        {
            _seconds   = Mathf.Max(0.05f, b.Get("juice.puffSeconds"));
            _scale     = Mathf.Max(0.1f,  b.Get("juice.puffScale"));
            _bossScale = Mathf.Max(0.1f,  b.Get("juice.puffBossScale"));
            _ready = true;
        });

        // Static giữ tham chiếu sống qua cả lúc đổi scene nếu không gỡ tay.
        private void OnDestroy() { if (_instance == this) _instance = null; }

        public void Play(Vector3 at, bool boss)
        {
            if (!_ready || _pool == null) return;
            _pool.Get().Play(at, _seconds, boss ? _bossScale : _scale, f => _pool.Release(f));
        }

        /// <summary>Không có spawner trong scene thì im lặng bỏ qua — khói là trang trí.</summary>
        public static void Puff(Vector3 at, bool boss)
        {
            if (_instance != null) _instance.Play(at, boss);
        }
    }
}
