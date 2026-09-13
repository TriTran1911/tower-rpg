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
        [SerializeField] private GameObject bossBarRoot;
        [SerializeField] private Image bossFill;

        [SerializeField] private SweepRunner sweep;
        [SerializeField] private Button sweepButton;
        [SerializeField] private TMP_Text sweepLabel;
        [SerializeField] private Image sweepFill;

        [SerializeField] private AutoBattle auto;
        [SerializeField] private Button autoButton;
        [SerializeField] private TMP_Text autoLabel;

        [SerializeField] private FloorRunner runner;

        // QUYẾT ĐỊNH #28 áp cho cả HUD: nút dùng gỗ SÁNG (243,140,76), mà trên nền đó
        // vàng đạt 1,27:1 · ngọc 1,25:1 · mờ 1,45:1 — đều dưới xa 4,5:1, tức là không đọc
        // được. Chỉ MỰC đạt (7,41:1). Nên trạng thái nút phân biệt bằng SẮC NỀN, còn chữ
        // luôn là mực. Đây đúng cái bẫy đã sửa ở UpgradeScreen mà HUD còn sót.
        // MÀU CHỮ ĐẢO THEO TRẠNG THÁI, và đây là chỗ trực giác dễ sai:
        //   nút MỞ  = gỗ cam nguyên bản (243,140,76) -> nền SÁNG -> chữ MỰC   (7,37:1)
        //   nút KHOÁ = cùng gỗ đó nhân TintLock      -> nền TỐI (112,62,32) -> chữ GIẤY (4,7:1)
        // Bản đầu tôi dùng một màu "mực nhạt" cho nút khoá và đo được 1,05:1 — tức là
        // dòng "CÒN 20 TẦNG", thứ mang toàn bộ thông điệp, gần như vô hình.
        private static readonly Color Ink  = new Color(0.10f, 0.09f, 0.08f);
        private static readonly Color InkOff = new Color(0.78f, 0.75f, 0.70f);

        private static readonly Color TintOn   = new Color(0.62f, 1f, 0.90f, 1f);   // đang chạy — ngả ngọc
        private static readonly Color TintOpen = Color.white;                        // dùng được
        private static readonly Color TintLock = new Color(0.46f, 0.44f, 0.42f, 1f); // còn khoá

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
            if (bossBarRoot != null) bossBarRoot.SetActive(false);
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
            // Thanh máu boss phải cập nhật MỖI KHUNG HÌNH, không theo sự kiện: Refresh()
            // chỉ chạy khi GameState.Changed bắn, mà máu boss vơi liên tục trong 75-193
            // giây mà không có sự kiện nào cả.
            if (bossFill != null && runner != null)
            {
                bool fighting = runner.InBossFight;
                if (bossBarRoot != null && bossBarRoot.activeSelf != fighting)
                    bossBarRoot.SetActive(fighting);
                if (fighting) bossFill.fillAmount = runner.BossHealthFraction;
            }

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

        // KHOÁ NHƯNG THẤY ĐƯỢC — đây là sửa một lỗi thiết kế, không phải thêm tính năng.
        // Cũ: SetActive(unlocked) làm nút BIẾN MẤT HOÀN TOÀN tới khi mở. Người chơi mới
        // đánh 16 phút đầu mà KHÔNG CÓ CÁCH NÀO biết là có chế độ tự đánh đang chờ —
        // nên nó không kéo được ai đi tiếp. Một tính năng vô hình thì bằng không tồn tại.
        // Mốc tầng 20 GIỮ NGUYÊN: §5.2 giải thích rất kỹ vì sao phải là 20 (đủ lâu để
        // hiểu hệ thống bằng tay, đủ sớm để chưa chán). Thứ sai là cách bày, không phải mốc.
        private void RefreshSweep(GameState gs)
        {
            if (sweepButton == null) return;

            bool can = sweep != null && sweep.CanSweep;
            bool on = sweep != null && sweep.Running;

            sweepButton.gameObject.SetActive(true);
            sweepButton.interactable = can;

            Image sweepBg = sweepButton.targetGraphic as Image;
            if (sweepBg != null) sweepBg.color = on ? TintOn : can ? TintOpen : TintLock;

            if (sweepLabel != null)
            {
                if (on)       sweepLabel.text = $"QUÉT\nT{sweep.TargetFloor}";
                else if (can) sweepLabel.text = "QUÉT\nNHANH";
                else          sweepLabel.text = "QUÉT NHANH\nDỌN TẦNG 1";
                sweepLabel.color = can || on ? Ink : InkOff;
            }
            if (sweepFill != null && !on) sweepFill.fillAmount = 0f;
        }

        private void RefreshAuto(GameState gs)
        {
            if (autoButton == null) return;

            bool unlocked = auto != null && auto.IsUnlocked;
            bool on = unlocked && auto.Enabled;

            autoButton.gameObject.SetActive(true);
            autoButton.interactable = unlocked;

            Image autoBg = autoButton.targetGraphic as Image;
            if (autoBg != null) autoBg.color = on ? TintOn : unlocked ? TintOpen : TintLock;

            if (autoLabel == null) return;

            if (!unlocked)
            {
                int need = auto != null ? auto.UnlockFloor : 20;
                int left = Mathf.Max(0, need - gs.HighestCleared);
                // Đếm ngược cho người chơi một cái đích. "CÒN 17 TẦNG" là một lời hứa
                // kiểm chứng được; nút biến mất thì không hứa gì cả.
                autoLabel.text = $"TỰ ĐÁNH\nCÒN {left} TẦNG";
                autoLabel.color = InkOff;
                return;
            }

            autoLabel.text = on ? "TỰ ĐÁNH  ●" : "TỰ ĐÁNH";
            autoLabel.color = Ink;
        }
    }
}
