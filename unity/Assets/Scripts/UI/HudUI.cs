using TMPro;
using TowerRpg.Player;
using TowerRpg.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TowerRpg.UI
{
    /// <summary>
    /// Số tầng, Mảnh, Lõi, và ba nút của M3: quét nhanh · tự đánh · đổi nhân vật.
    /// Nghe GameState.Changed, không polling.
    /// </summary>
    public sealed class HudUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text floorNumber;
        [SerializeField] private TMP_Text shardCount;

        [Header("M3")]
        [SerializeField] private TMP_Text coreCount;
        [SerializeField] private GameObject coreGroup;     // ẩn tới khi có Lõi đầu tiên
        [SerializeField] private TMP_Text bossBanner;

        [SerializeField] private SweepRunner sweep;
        [SerializeField] private Button sweepButton;
        [SerializeField] private TMP_Text sweepLabel;
        [SerializeField] private Image sweepFill;

        [SerializeField] private AutoBattle auto;
        [SerializeField] private Button autoButton;
        [SerializeField] private TMP_Text autoLabel;

        [SerializeField] private FloorRunner runner;

        private static readonly Color Gold = new Color(0.91f, 0.70f, 0.29f);
        private static readonly Color Jade = new Color(0.28f, 0.81f, 0.70f);
        private static readonly Color Dim  = new Color(0.58f, 0.53f, 0.46f);

        private void Start()
        {
            if (sweepButton != null) sweepButton.onClick.AddListener(OnSweep);
            if (autoButton  != null) autoButton.onClick.AddListener(OnAuto);
            if (sweep != null) sweep.Progress += OnSweepProgress;
            if (runner != null)
            {
                runner.FloorStarted += _ => Refresh();
                runner.BossDefeated += OnBossDefeated;
            }
            if (bossBanner != null) bossBanner.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (GameState.Instance != null) GameState.Instance.Changed += Refresh;
            Refresh();
        }

        private void OnSweep()
        {
            if (sweep != null) sweep.Toggle();
            Refresh();
        }

        private void OnAuto()
        {
            if (auto != null) auto.Toggle();
            Refresh();
        }

        private void OnSweepProgress(float t)
        {
            if (sweepFill != null) sweepFill.fillAmount = t;
        }

        private void OnBossDefeated(int floor, int cores)
        {
            Debug.Log($"[HudUI] Hạ boss tầng {floor}: +{cores} Lõi, mở nhân vật mới.");
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

            if (floorNumber != null)
                floorNumber.text = gs.Floor.ToString("00") +
                                   (gs.IsBossFloor(gs.Floor) ? "  ☠" : "");
            if (shardCount  != null) shardCount.text  = $"{gs.Shards:N0}";
            if (coreCount   != null) coreCount.text   = gs.Cores.ToString();

            // Lõi chưa tồn tại với người chơi mới — đừng bày một ô số 0 khó hiểu lên HUD.
            if (coreGroup != null)
                coreGroup.SetActive(gs.Cores > 0 || gs.BossesKilled > 0);

            if (bossBanner != null)
            {
                bool boss = runner != null && runner.InBossFight;
                bossBanner.gameObject.SetActive(boss);
                if (boss) bossBanner.text = $"BOSS  ·  TẦNG {gs.Floor}";
            }

            RefreshSweep(gs);
            RefreshAuto(gs);
        }

        private void RefreshSweep(GameState gs)
        {
            if (sweepButton == null) return;

            bool can = sweep != null && sweep.CanSweep;
            sweepButton.gameObject.SetActive(can || (sweep != null && sweep.Running));
            sweepButton.interactable = can;

            if (sweepLabel != null)
            {
                bool on = sweep != null && sweep.Running;
                sweepLabel.text = on ? $"QUÉT  T{sweep.TargetFloor}" : "QUÉT NHANH";
                sweepLabel.color = on ? Jade : (can ? Gold : Dim);
            }
            if (sweepFill != null && (sweep == null || !sweep.Running)) sweepFill.fillAmount = 0f;
        }

        private void RefreshAuto(GameState gs)
        {
            if (autoButton == null) return;

            bool unlocked = auto != null && auto.IsUnlocked;
            autoButton.gameObject.SetActive(unlocked);
            autoButton.interactable = unlocked;

            if (autoLabel != null)
            {
                bool on = auto != null && auto.Enabled;
                autoLabel.text = on ? "TỰ ĐÁNH  ●" : "TỰ ĐÁNH";
                autoLabel.color = on ? Jade : Gold;
            }
        }
    }
}
