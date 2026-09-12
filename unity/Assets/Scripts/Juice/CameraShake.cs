using System.Collections;
using TowerRpg.Core;
using UnityEngine;

namespace TowerRpg.Juice
{
    /// <summary>
    /// Rung màn hình khi chí mạng. Dùng sóng sin tắt dần thay vì nhiễu ngẫu nhiên —
    /// vừa hợp nguyên tắc "không ngẫu nhiên", vừa cho cú rung chắc và gọn hơn.
    /// </summary>
    public sealed class CameraShake : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float frequency = 38f;

        private Vector3 _restPosition;
        private Coroutine _running;
        private float _duration;
        private float _magnitude;
        private float _yRatio;
        private bool _ready;

        private void Awake()
        {
            if (target == null) target = transform;
            _restPosition = target.localPosition;
        }

        private void Start() => BalanceConfig.TryUse(this, ApplyBalance);

        private void ApplyBalance(BalanceConfig balance)
        {
            _duration = balance.Get("juice.shakeDuration");
            _magnitude = balance.Get("juice.shakeMagnitude");
            _yRatio = balance.Get("juice.shakeYRatio");
            _ready = true;
        }

        public void Shake()
        {
            if (!_ready || _duration <= 0f || _magnitude <= 0f) return;

            if (_running != null)
            {
                StopCoroutine(_running);
                target.localPosition = _restPosition;
            }

            _running = StartCoroutine(ShakeRoutine());
        }

        private IEnumerator ShakeRoutine()
        {
            float elapsed = 0f;

            while (elapsed < _duration)
            {
                elapsed += Time.deltaTime;

                float decay = 1f - Mathf.Clamp01(elapsed / _duration);
                float offset = Mathf.Sin(elapsed * frequency) * _magnitude * decay;

                target.localPosition = _restPosition + new Vector3(offset, offset * _yRatio, 0f);
                yield return null;
            }

            target.localPosition = _restPosition;
            _running = null;
        }
    }
}
