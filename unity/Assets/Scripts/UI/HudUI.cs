using TMPro;
using TowerRpg.Progression;
using UnityEngine;

namespace TowerRpg.UI
{
    /// <summary>Số tầng và số Mảnh trên HUD. Nghe GameState.Changed, không polling.</summary>
    public sealed class HudUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text floorNumber;
        [SerializeField] private TMP_Text shardCount;

        private void OnEnable()
        {
            if (GameState.Instance != null) GameState.Instance.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (GameState.Instance != null) GameState.Instance.Changed -= Refresh;
        }

        // GameState nạp save bất đồng bộ nên có thể sẵn sàng SAU OnEnable.
        private bool _hooked;
        private void Update()
        {
            if (_hooked || GameState.Instance == null || !GameState.Instance.Ready) return;
            GameState.Instance.Changed += Refresh;
            _hooked = true;
            Refresh();
        }

        private void Refresh()
        {
            GameState gs = GameState.Instance;
            if (gs == null || !gs.Ready) return;

            if (floorNumber != null) floorNumber.text = gs.Floor.ToString("00");
            if (shardCount  != null) shardCount.text  = $"{gs.Shards:N0}";
        }
    }
}
