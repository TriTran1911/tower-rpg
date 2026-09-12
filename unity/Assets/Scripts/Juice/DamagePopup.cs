using System;
using TMPro;
using UnityEngine;

namespace TowerRpg.Juice
{
    /// <summary>Một con số sát thương bay lên rồi mờ đi. Được tái sử dụng qua pool.</summary>
    // Không dùng [RequireComponent(typeof(TMP_Text))]: TMP_Text là lớp TRỪU TƯỢNG nên Unity
    // không tự thêm được — thuộc tính đó chỉ gây lỗi khi kéo script vào object trống.
    public sealed class DamagePopup : MonoBehaviour
    {
        [Header("Thẩm mỹ, không phải cân bằng")]
        [SerializeField] private Color normalColour = Color.white;
        [SerializeField] private Color critColour = new Color(1f, 0.82f, 0.2f);
        [SerializeField] private float critScale = 1.6f;

        private TMP_Text _label;
        private float _riseSpeed;
        private float _lifetime;
        private float _elapsed;
        private bool _finished;
        private Action<DamagePopup> _onFinished;

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();

            if (_label == null)
                Debug.LogError("[DamagePopup] Thiếu component TextMeshPro trên prefab này. " +
                               "Thêm TextMeshPro - Text (không phải bản UI).", this);
        }

        public void Play(Vector3 worldPosition, float amount, bool isCrit,
                         float riseSpeed, float lifetime, Action<DamagePopup> onFinished)
        {
            _onFinished = onFinished;

            if (_label == null)
            {
                onFinished?.Invoke(this);      // trả ngay về pool, đừng rò rỉ
                return;
            }

            transform.position = worldPosition;
            transform.localScale = Vector3.one * (isCrit ? critScale : 1f);

            _label.text = Mathf.RoundToInt(amount).ToString();
            _label.color = isCrit ? critColour : normalColour;

            _riseSpeed = riseSpeed;
            _lifetime = Mathf.Max(0.01f, lifetime);
            _elapsed = 0f;
            _finished = false;

            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (_finished || _label == null) return;

            _elapsed += Time.deltaTime;

            transform.position += Vector3.up * (_riseSpeed * Time.deltaTime);

            Color c = _label.color;
            c.a = Mathf.Clamp01(1f - _elapsed / _lifetime);
            _label.color = c;

            if (_elapsed < _lifetime) return;

            // Chốt cờ TRƯỚC khi gọi callback: nếu không, Update của khung hình sau có thể
            // gọi Release lần thứ hai và ObjectPool (collectionCheck = true) sẽ ném lỗi.
            _finished = true;
            _onFinished?.Invoke(this);
        }
    }
}
