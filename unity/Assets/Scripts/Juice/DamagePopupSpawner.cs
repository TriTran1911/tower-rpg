using System.Collections.Generic;
using TowerRpg.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace TowerRpg.Juice
{
    /// <summary>
    /// Sinh số sát thương qua ObjectPool. §9.3: sinh/huỷ liên tục gây giật vì gom rác —
    /// số sát thương là thứ xuất hiện dày đặc nhất nên bắt buộc phải pool VÀ làm nóng trước.
    /// </summary>
    public sealed class DamagePopupSpawner : MonoBehaviour
    {
        [SerializeField] private DamagePopup popupPrefab;
        [SerializeField] private int prewarm = 16;
        [SerializeField] private int maxSize = 64;

        private ObjectPool<DamagePopup> _pool;
        private float _riseSpeed;
        private float _lifetime;
        private bool _ready;

        private void Awake()
        {
            _pool = new ObjectPool<DamagePopup>(
                createFunc: CreateInstance,
                actionOnGet: null,
                actionOnRelease: popup => { if (popup != null) popup.gameObject.SetActive(false); },
                actionOnDestroy: popup => { if (popup != null) Destroy(popup.gameObject); },
                collectionCheck: true,
                defaultCapacity: prewarm,
                maxSize: maxSize);
        }

        private void Start()
        {
            if (popupPrefab == null)
            {
                Debug.LogError("[DamagePopupSpawner] Chưa gán popupPrefab — sẽ không có số sát thương.", this);
                enabled = false;
                return;
            }

            Prewarm();
            BalanceConfig.TryUse(this, ApplyBalance);
        }

        private void ApplyBalance(BalanceConfig balance)
        {
            _riseSpeed = balance.Get("juice.popupRiseSpeed");
            _lifetime = balance.Get("juice.popupLifetime");
            _ready = true;
        }

        /// <summary>
        /// Cấp phát sẵn rồi trả hết về pool. Không có bước này thì 16 popup đầu tiên vẫn
        /// cấp phát lúc đang đánh — đúng cái giật GC mà pool sinh ra để tránh.
        /// </summary>
        private void Prewarm()
        {
            if (prewarm <= 0) return;

            var warmed = new List<DamagePopup>(prewarm);
            for (int i = 0; i < prewarm; i++) warmed.Add(_pool.Get());
            for (int i = 0; i < warmed.Count; i++) _pool.Release(warmed[i]);
        }

        private DamagePopup CreateInstance()
        {
            DamagePopup popup = Instantiate(popupPrefab, transform);
            popup.gameObject.SetActive(false);
            return popup;
        }

        public void Show(Vector3 worldPosition, float amount, bool isCrit)
        {
            if (!_ready) return;

            DamagePopup popup = _pool.Get();
            popup.Play(worldPosition, amount, isCrit, _riseSpeed, _lifetime, Release);
        }

        private void Release(DamagePopup popup) => _pool.Release(popup);
    }
}
