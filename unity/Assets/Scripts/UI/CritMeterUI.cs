using TowerRpg.Player;
using UnityEngine;
using UnityEngine.UI;

namespace TowerRpg.UI
{
    /// <summary>
    /// Thanh chí mạng hiển thị ngay dưới nhân vật. Người chơi PHẢI nhìn thấy nó đầy dần —
    /// đó là thứ biến chí mạng từ một con số thành một quyết định (§5.4).
    /// </summary>
    public sealed class CritMeterUI : MonoBehaviour
    {
        [SerializeField] private CritMeter meter;
        [SerializeField] private Image fillImage;

        [Header("Thẩm mỹ, không phải cân bằng")]
        [SerializeField] private Color chargingColour = new Color(0.55f, 0.75f, 1f);
        [SerializeField] private Color readyColour = new Color(1f, 0.82f, 0.2f);

        private void Start()
        {
            if (meter == null) Debug.LogError("[CritMeterUI] Chưa gán 'meter'.", this);
            if (fillImage == null) Debug.LogError("[CritMeterUI] Chưa gán 'fillImage'.", this);
        }

        private void Update()
        {
            if (meter == null || fillImage == null) return;

            fillImage.fillAmount = meter.Fill01;
            fillImage.color = meter.IsReady ? readyColour : chargingColour;
        }
    }
}
