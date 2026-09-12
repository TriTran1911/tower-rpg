using TowerRpg.Player;
using UnityEngine;
using UnityEngine.UI;

namespace TowerRpg.UI
{
    /// <summary>
    /// Thanh máu người chơi. Không có nó thì không thấy được cái giá của việc đứng yên —
    /// mà cái giá đó chính là nửa còn lại của luật §5.3.
    /// </summary>
    public sealed class PlayerHealthUI : MonoBehaviour
    {
        [SerializeField] private PlayerHealth health;
        [SerializeField] private Image fillImage;

        private void Start()
        {
            if (health == null) health = PlayerHealth.Current;
            if (fillImage == null) Debug.LogError("[PlayerHealthUI] Chưa gán 'fillImage'.", this);
        }

        private void Update()
        {
            if (health == null || fillImage == null) return;

            fillImage.fillAmount = health.Fraction;
        }
    }
}
