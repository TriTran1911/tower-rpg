using System;
using TowerRpg.Player;
using UnityEngine;

namespace TowerRpg.Loot
{
    /// <summary>
    /// Một viên Mảnh nằm trên sàn.
    ///
    /// VÒNG ĐỜI XÁC ĐỊNH TUYỆT ĐỐI — không một lời gọi Random nào. §1 cấm hên xui, và
    /// cấm cả "hên xui nhỏ cho vui": lượng Mảnh cố định, hướng bật cố định (về phía người
    /// chơi), thời gian cố định. Hai người chơi cày như nhau phải nhận đúng như nhau.
    ///
    /// bật ra khỏi xác -> nhấp nhô tại chỗ -> người chơi vào bán kính hút thì bay tới
    /// -> chạm bán kính nhặt thì cộng Mảnh, kêu một tiếng, trả về pool.
    ///
    /// Lấy người chơi qua PlayerHealth.Current (đã static sẵn) nên không phải nối tham chiếu.
    /// </summary>
    public sealed class ShardPickup : MonoBehaviour
    {
        private Action<ShardPickup> _onDone;
        private Action<float> _onCollected;

        private float _value;
        private Vector3 _from, _to;
        private float _popSeconds, _magnetSqr, _pickupSqr, _flySpeed, _maxLifetime;
        private float _t, _age;
        private bool _popping;

        public float Value => _value;

        /// <summary>Gọi bởi ShardDropSpawner. Hướng bật tính từ vị trí người chơi lúc này.</summary>
        public void Launch(Vector3 origin, float value, float popDistance, float popSeconds,
                           float magnetRadius, float pickupRadius, float flySpeed,
                           float maxLifetime, Action<float> onCollected, Action<ShardPickup> onDone)
        {
            _value = value;
            _onCollected = onCollected;
            _onDone = onDone;
            _popSeconds = Mathf.Max(0.01f, popSeconds);
            _magnetSqr = magnetRadius * magnetRadius;
            _pickupSqr = pickupRadius * pickupRadius;
            _flySpeed = flySpeed;
            _maxLifetime = maxLifetime;
            _t = 0f;
            _age = 0f;
            _popping = true;

            _from = origin;

            // Bật VỀ PHÍA người chơi, không bật ngẫu nhiên: lúc quái gục người chơi đứng
            // cách xác <= 2,0 nên viên rơi cách chân ~0,9, tức nằm sẵn trong bán kính hút.
            // Rơi đồ ở đây là thứ NHÌN THẤY, không phải thứ bắt đi nhặt.
            Vector3 dir = Vector3.up;
            PlayerHealth p = PlayerHealth.Current;
            if (p != null)
            {
                Vector3 d = p.transform.position - origin;
                d.z = 0f;
                if (d.sqrMagnitude > 0.0001f) dir = d.normalized;
            }
            _to = origin + dir * popDistance;

            transform.position = origin;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            _age += Time.deltaTime;

            if (_popping)
            {
                _t += Time.deltaTime;
                float k = Mathf.Clamp01(_t / _popSeconds);
                // Cung bật: đi ngang đều, nảy lên rồi rơi xuống. 4k(1-k) đạt đỉnh 1 ở k=0,5.
                Vector3 pos = Vector3.Lerp(_from, _to, k);
                pos.y += 0.45f * (4f * k * (1f - k));
                transform.position = pos;
                if (k >= 1f) _popping = false;
                return;
            }

            PlayerHealth player = PlayerHealth.Current;
            if (player == null) return;

            Vector3 delta = player.transform.position - transform.position;
            delta.z = 0f;
            float sqr = delta.sqrMagnitude;

            if (sqr <= _pickupSqr) { Collect(); return; }

            // Hết hạn thì tự bay về: một viên nằm lại vĩnh viễn vì người chơi không đi ngang
            // là Mảnh bị treo, mà kế toán ở FloorRunner coi nó là đã phát ra.
            bool magnet = sqr <= _magnetSqr || _age >= _maxLifetime;
            if (!magnet)
            {
                // nhấp nhô tại chỗ cho dễ thấy trên nền sàn lát
                Vector3 p = transform.position;
                p.y += Mathf.Sin(Time.time * 4f) * 0.25f * Time.deltaTime;
                transform.position = p;
                return;
            }

            transform.position += (Vector3)(delta.normalized * (_flySpeed * Time.deltaTime));
        }

        /// <summary>Hút về ngay lập tức — dùng cho lệnh dọn cuối tầng.</summary>
        public void CollectNow() => Collect();

        private void Collect()
        {
            float v = _value;
            _value = 0f;                       // chặn cộng hai lần nếu bị gọi chồng
            _onCollected?.Invoke(v);
            Juice.SfxPlayer.Play(Juice.Sfx.Pickup);
            Release();
        }

        /// <summary>Trả về pool mà KHÔNG cộng Mảnh — dùng khi bày lại tầng sau khi chết.</summary>
        public void Discard()
        {
            _value = 0f;
            Release();
        }

        private void Release()
        {
            gameObject.SetActive(false);
            Action<ShardPickup> done = _onDone;
            _onDone = null;
            _onCollected = null;
            done?.Invoke(this);
        }
    }
}
