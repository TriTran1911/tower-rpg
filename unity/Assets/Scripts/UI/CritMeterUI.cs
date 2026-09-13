using System.Collections.Generic;
using TowerRpg.Core;
using TowerRpg.Player;
using UnityEngine;
using UnityEngine.UI;

namespace TowerRpg.UI
{
    /// <summary>
    /// Thanh chí mạng dưới chân nhân vật, CHIA ĐÚNG SỐ VẠCH bằng crit.meterSize.
    ///
    /// VÌ SAO CHIA VẠCH CHỨ KHÔNG LIỀN MẠCH (docs/GIAO-DIEN.md §3): §5.4 hứa "cứ N đòn thì
    /// đòn thứ N chắc chắn chí mạng". Thanh liền mạch bắt người chơi ƯỚC LƯỢNG; thanh chia
    /// vạch cho họ ĐẾM. Đó là toàn bộ lý do chọn thanh dồn thay vì xác suất — nếu giao diện
    /// không cho thấy điều đó thì lợi thế mất sạch.
    ///
    /// Số vạch đọc từ CSV, KHÔNG viết cứng: đổi crit.meterSize là thanh tự chia lại.
    /// </summary>
    public sealed class CritMeterUI : MonoBehaviour
    {
        [SerializeField] private CritMeter meter;
        [SerializeField] private RectTransform segmentRoot;
        [SerializeField] private Image segmentPrefab;

        [Header("Thẩm mỹ, không phải cân bằng")]
        [SerializeField] private Color emptyColour = new Color(0.17f, 0.15f, 0.12f, 0.95f);
        [SerializeField] private Color chargingColour = new Color(0.91f, 0.70f, 0.29f);
        [SerializeField] private Color readyColour = new Color(1f, 0.95f, 0.75f);
        [SerializeField] private float gap = 3f;

        private readonly List<Image> _segments = new List<Image>();
        private bool _ready;

        private void Start()
        {
            if (meter == null) { Debug.LogError("[CritMeterUI] Chưa gán 'meter'.", this); enabled = false; return; }
            if (segmentRoot == null) { Debug.LogError("[CritMeterUI] Chưa gán 'segmentRoot'.", this); enabled = false; return; }
            if (segmentPrefab == null) { Debug.LogError("[CritMeterUI] Chưa gán 'segmentPrefab'.", this); enabled = false; return; }

            BalanceConfig.TryUse(this, ApplyBalance);
        }

        private void ApplyBalance(BalanceConfig balance)
        {
            int size = Mathf.Max(2, balance.GetInt("crit.meterSize"));
            Build(size);
            _ready = true;
        }

        /// <summary>Dựng đúng <paramref name="size"/> vạch, chia đều bề ngang của segmentRoot.</summary>
        private void Build(int size)
        {
            foreach (Image s in _segments) if (s != null) Destroy(s.gameObject);
            _segments.Clear();

            float total = segmentRoot.rect.width;
            float w = (total - gap * (size - 1)) / size;

            for (int i = 0; i < size; i++)
            {
                Image seg = Instantiate(segmentPrefab, segmentRoot);
                seg.gameObject.name = $"Seg{i}";
                seg.gameObject.SetActive(true);

                RectTransform rt = seg.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(w, segmentRoot.rect.height);
                rt.anchoredPosition = new Vector2(i * (w + gap), 0f);

                seg.color = emptyColour;
                _segments.Add(seg);
            }

            segmentPrefab.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_ready || meter == null) return;

            // Số vạch đã nạp = mức đầy × tổng số vạch, làm tròn xuống.
            // Vạch cuối chỉ sáng khi thanh THẬT SỰ sẵn sàng — không được nói dối người chơi.
            int lit = Mathf.FloorToInt(meter.Fill01 * _segments.Count + 0.0001f);
            bool ready = meter.IsReady;
            if (ready) lit = _segments.Count;

            Color on = ready ? readyColour : chargingColour;
            for (int i = 0; i < _segments.Count; i++)
                _segments[i].color = i < lit ? on : emptyColour;
        }
    }
}
