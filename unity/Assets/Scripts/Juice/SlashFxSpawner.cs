using System.Collections.Generic;
using TowerRpg.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace TowerRpg.Juice
{
    /// <summary>Pool vệt chém. Cùng khuôn DamagePopupSpawner — pool, prewarm, TryUse ở Start.</summary>
    public sealed class SlashFxSpawner : MonoBehaviour
    {
        [SerializeField] private SlashFx prefab;
        [SerializeField] private int prewarm = 8;
        [SerializeField] private int maxSize = 32;

        private ObjectPool<SlashFx> _pool;
        private float _seconds = 0.16f;
        private bool _ready;

        private void Awake()
        {
            if (prefab == null) { enabled = false; return; }
            _pool = new ObjectPool<SlashFx>(
                () => { SlashFx f = Instantiate(prefab, transform); f.gameObject.SetActive(false); return f; },
                actionOnRelease: f => { if (f != null) f.gameObject.SetActive(false); },
                actionOnDestroy: f => { if (f != null) Destroy(f.gameObject); },
                collectionCheck: false, defaultCapacity: prewarm, maxSize: maxSize);

            var warm = new List<SlashFx>(prewarm);
            for (int i = 0; i < prewarm; i++) warm.Add(_pool.Get());
            foreach (SlashFx f in warm) _pool.Release(f);
        }

        // TryUse PHẢI ở Start, không phải Awake — thứ tự Awake là không xác định và
        // BalanceConfig.Instance gán trong Awake của chính nó. Đã trả giá một lần ở Việc 5.
        private void Start() => BalanceConfig.TryUse(this, b =>
        {
            _seconds = Mathf.Max(0.02f, b.Get("juice.slashSeconds"));
            _ready = true;
        });

        public void Play(Vector3 at, bool crit)
        {
            if (!_ready || _pool == null) return;
            _pool.Get().Play(at, _seconds, crit, f => _pool.Release(f));
        }
    }
}
