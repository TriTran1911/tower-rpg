using TMPro;
using TowerRpg.Core;
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

        // Hai nút ĐANG MỞ. Trước gói này HudUI không giữ nổi một tham chiếu tới chúng,
        // trong khi hai nút đang KHOÁ được chăm từng li — kể cả đếm ngược "CÒN 20 TẦNG".
        // Tức là tôi đã dồn công giải thích thứ người chơi CHƯA có, và bỏ mặc thứ họ ĐANG có.
        [SerializeField] private Button gearButton;
        [SerializeField] private TMP_Text gearLabel;
        [SerializeField] private TMP_Text charLabel;
        [SerializeField] private GameObject eventBannerRoot;
        [SerializeField] private TMP_Text eventBanner;
        [SerializeField] private Juice.CameraShake cameraShake;
        [SerializeField] private MilestoneOverlay milestone;

        // QUYẾT ĐỊNH #28 áp cho cả HUD: nút dùng gỗ SÁNG (243,140,76), mà trên nền đó
        // vàng đạt 1,27:1 · ngọc 1,25:1 · mờ 1,45:1 — đều dưới xa 4,5:1, tức là không đọc
        // được. Chỉ MỰC đạt (7,41:1). Nên trạng thái nút phân biệt bằng SẮC NỀN, còn chữ
        // luôn là mực. (Chú thích cũ ở đây ghi "cái bẫy đã sửa ở UpgradeScreen mà HUD còn
        // sót" — NGƯỢC SỰ THẬT: HUD sửa trước, UpgradeScreen mới là chỗ còn sót, và nó sót
        // thêm một đợt nữa mới có người soi ra.)
        // MÀU CHỮ ĐẢO THEO TRẠNG THÁI, và đây là chỗ trực giác dễ sai:
        //   nút MỞ  = gỗ cam nguyên bản (243,140,76) -> nền SÁNG -> chữ MỰC   (7,37:1)
        //   nút KHOÁ = cùng gỗ đó nhân TintLock      -> nền TỐI (112,62,32) -> chữ GIẤY (4,7:1)
        // Bản đầu tôi dùng một màu "mực nhạt" cho nút khoá và đo được 1,05:1 — tức là
        // dòng "CÒN 20 TẦNG", thứ mang toàn bộ thông điệp, gần như vô hình.
        // Banner có NỀN GỖ TỐI riêng, không nằm thẳng trên sàn: đo được vàng trên sàn
        // đấu trường (116,116,116) chỉ đạt 2,44:1, dưới cả ngưỡng 3:1 dành cho chữ lớn.
        // Trên nền gỗ tối (70,64,46) thì vàng đạt 5,40 và ngọc 5,33.
        private static readonly Color Gold = new Color(0.91f, 0.70f, 0.29f);
        private static readonly Color Jade = new Color(0.28f, 0.81f, 0.70f);

        private static readonly Color Ink  = new Color(0.10f, 0.09f, 0.08f);
        private static readonly Color InkOff = new Color(0.78f, 0.75f, 0.70f);

        private static readonly Color TintOn   = new Color(0.62f, 1f, 0.90f, 1f);   // đang chạy — ngả ngọc
        private static readonly Color TintOpen = Color.white;                        // dùng được
        private static readonly Color TintLock = new Color(0.46f, 0.44f, 0.42f, 1f); // còn khoá

        // Số Mảnh hiển thị CHẠY tới số thật thay vì nhảy phắt. Mắt người bắt được chuyển
        // động ở ngoài vùng nhìn trung tâm rất tốt, nhưng gần như không bắt được một con
        // số đổi tức thì ở góc màn hình. Viên Mảnh bay lên HUD rồi con số đứng im thì
        // đường bay đó kết thúc trong hư không.
        private float _shownShards = -1f;
        private float _countFrom, _countTarget, _countElapsed;
        private float _countSeconds = 0.25f, _flashSeconds = 0.18f;
        private float _flashUntil, _bannerUntil;
        private Color _shardBase = Color.white;

        private void Start()
        {
            if (sweepButton != null) sweepButton.onClick.AddListener(OnSweep);
            if (autoButton  != null) autoButton.onClick.AddListener(OnAuto);
            if (sweep != null) sweep.Progress += OnSweepProgress;
            if (runner != null)
            {
                runner.FloorStarted += _ => Refresh();
                runner.BossDefeated += OnBossDefeated;
                runner.FloorCleared += OnFloorCleared;
            }
            if (bossBanner != null) bossBanner.gameObject.SetActive(false);
            if (bossBarRoot != null) bossBarRoot.SetActive(false);
            if (eventBannerRoot != null) eventBannerRoot.SetActive(false);
            if (shardCount != null) _shardBase = shardCount.color;

            BalanceConfig.TryUse(this, b =>
            {
                _countSeconds = Mathf.Max(0.01f, b.Get("hud.countSeconds"));
                _flashSeconds = Mathf.Max(0f, b.Get("hud.flashSeconds"));
                _floorBannerSeconds = b.Get("hud.floorBannerSeconds");
                _bossBannerSeconds = b.Get("hud.bossBannerSeconds");
            });
        }

        private float _floorBannerSeconds = 1f, _bossBannerSeconds = 2f;

        /// <summary>
        /// Dọn sạch một tầng. FloorRunner đã bắn sự kiện này từ M2 mà KHÔNG MỘT AI NGHE —
        /// nên 400 Mảnh vào ví trong im lặng hoàn toàn, không một pixel nào đổi ngoài con
        /// số ở góc màn hình.
        /// </summary>
        private void OnFloorCleared(int floor, float reward)
        {
            // TẦNG BOSS KHÔNG hiện banner này. Màn cột mốc đã nói cùng một điều, to hơn và
            // dừng cả trò chơi lại — hai thứ cùng lúc thì chúng đè chữ lên nhau và người
            // chơi không đọc trọn cái nào. Đúng lý do FloorRunner cũng đã bỏ tiếng dọn tầng
            // ở tầng boss. Nhìn ảnh chụp phiên chơi mới thấy mình chỉ chặn có một nửa.
            GameState gs = GameState.Instance;
            if (gs != null && gs.IsBossFloor(floor)) return;

            // Tiếng đã do FloorRunner phát; ở đây chỉ lo phần nhìn.
            ShowBanner($"TẦNG {floor:00} XONG   ·   +{reward:N0} MẢNH", Gold, _floorBannerSeconds);
        }

        private void ShowBanner(string text, Color colour, float seconds)
        {
            if (eventBanner == null) return;
            eventBanner.text = text;
            eventBanner.color = colour;
            if (eventBannerRoot != null) eventBannerRoot.SetActive(true);
            _bannerUntil = Time.unscaledTime + Mathf.Max(0.1f, seconds);
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

        /// <summary>
        /// Đỉnh duy nhất của 8 phút đầu. Trước Việc 6 nó là MỘT DÒNG Debug.Log — người chơi
        /// hạ con boss đầu tiên, nhận Lõi, mở nhân vật mới, và trên màn hình không có gì
        /// xảy ra cả. §5.2 tự ghi "cột mốc lớn — cần màn hình chúc mừng riêng"; màn hình
        /// riêng để Đợt 2, nhưng im lặng hoàn toàn thì không chấp nhận được.
        /// </summary>
        private void OnBossDefeated(int floor, int cores)
        {
            // Màn hình cột mốc DỪNG trò chơi lại; banner chỉ là bản dự phòng khi chưa
            // nối được overlay. Không chạy cả hai — hai thứ cùng nói một điều là ồn.
            if (milestone != null)
            {
                // MÌN HẸN GIỜ CHO M4: CharacterRoster.Name() trả "?" cho chỉ số >= 5. Tháp
                // hiện có 4 boss nên vừa đủ 4 nhân vật; khi tower.floors lên 100 thì boss
                // 5-10 sẽ DỪNG HẲN GAME sáu lần để khoe "MỞ NHÂN VẬT: ?". Hết nhân vật thì
                // cột mốc chỉ nói về Lõi — vẫn là cột mốc, chỉ là không hứa thứ không có.
                GameState gs2 = GameState.Instance;
                int soBoss = gs2 != null ? gs2.BossesKilled : 0;
                string ten = soBoss > 0 && soBoss < CharacterRoster.Count
                           ? CharacterRoster.Name(soBoss) : null;
                milestone.Show(floor, cores, ten);
            }
            else
            {
                ShowBanner($"HẠ BOSS   ·   +{cores} LÕI   ·   MỞ NHÂN VẬT MỚI",
                           Jade, _bossBannerSeconds);
                if (cameraShake != null) cameraShake.Shake();
            }
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

            TickShardCounter();
            TickBanner();

            if (_hooked || GameState.Instance == null || !GameState.Instance.Ready) return;
            GameState.Instance.Changed += Refresh;
            _hooked = true;
            Refresh();
        }

        private void TickShardCounter()
        {
            GameState gs = GameState.Instance;
            if (shardCount == null || gs == null || !gs.Ready) return;

            if (_shownShards < 0f) { _shownShards = gs.Shards; _countTarget = gs.Shards; }

            // Nội suy TUYẾN TÍNH theo mốc thời gian, không phải "mỗi khung tiến một phần
            // quãng đường còn lại". Bản đầu tính lại bước đi từ khoảng cách CÒN LẠI mỗi
            // khung hình, tức phân rã mũ: nó tiệm cận mãi mà không tới nơi — đo được còn
            // thiếu 17 Mảnh sau 2 giây. Cách này chạm đích đúng sau _countSeconds.
            if (!Mathf.Approximately(_countTarget, gs.Shards))
            {
                _countFrom = _shownShards;
                _countTarget = gs.Shards;
                _countElapsed = 0f;
            }

            if (!Mathf.Approximately(_shownShards, _countTarget))
            {
                _countElapsed += Time.deltaTime;
                float k = Mathf.Clamp01(_countElapsed / _countSeconds);
                _shownShards = k >= 1f ? _countTarget : Mathf.Lerp(_countFrom, _countTarget, k);
            }

            shardCount.text = $"{_shownShards:N0}";
            shardCount.color = Time.unscaledTime < _flashUntil
                             ? Color.Lerp(_shardBase, Color.white, 0.85f)
                             : _shardBase;
        }

        private void TickBanner()
        {
            if (eventBannerRoot == null || !eventBannerRoot.activeSelf) return;
            if (Time.unscaledTime >= _bannerUntil) eventBannerRoot.SetActive(false);
        }

        private void Refresh()
        {
            GameState gs = GameState.Instance;
            if (gs == null || !gs.Ready) return;

            if (floorNumber != null)
                floorNumber.text = gs.Floor.ToString("00") +
                                   (gs.IsBossFloor(gs.Floor) ? "  ☠" : "");
            // Số Mảnh do Update() lo (nó phải chạy mượt giữa hai lần Refresh), nhưng ghi
            // nhận mốc nháy sáng ở đây vì Refresh mới là chỗ biết Mảnh vừa đổi.
            if (shardCount != null && _shownShards >= 0f && gs.Shards > _shownShards + 0.01f)
                _flashUntil = Time.unscaledTime + _flashSeconds;
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

            RefreshGear(gs);
            RefreshChar(gs);
            RefreshSweep(gs);
            RefreshAuto(gs);
        }

        // KHOÁ NHƯNG THẤY ĐƯỢC — đây là sửa một lỗi thiết kế, không phải thêm tính năng.
        // Cũ: SetActive(unlocked) làm nút BIẾN MẤT HOÀN TOÀN tới khi mở. Người chơi mới
        // đánh 16 phút đầu mà KHÔNG CÓ CÁCH NÀO biết là có chế độ tự đánh đang chờ —
        // nên nó không kéo được ai đi tiếp. Một tính năng vô hình thì bằng không tồn tại.
        // Mốc tầng 20 GIỮ NGUYÊN: §5.2 giải thích rất kỹ vì sao phải là 20 (đủ lâu để
        // hiểu hệ thống bằng tay, đủ sớm để chưa chán). Thứ sai là cách bày, không phải mốc.
        /// <summary>
        /// Nút TRANG BỊ tự nói trạng thái: còn thiếu bao nhiêu Mảnh, hay đã mua được.
        /// Người chơi mới không có tutorial, không có chấm đỏ — dòng "CÒN 180" này là thứ
        /// duy nhất nối "đánh quái" với "bấm vào đây".
        /// </summary>
        private void RefreshGear(GameState gs)
        {
            if (gearLabel == null) return;

            // NextCost trả -1 khi ô đã chạm trần — bỏ qua số âm, nếu không nút báo "CÒN -1".
            float reNhat = float.MaxValue;
            bool dotPhaDuoc = false;
            for (int i = 0; i < Equipment.SlotCount; i++)
            {
                var s = (Slot)i;
                if (gs.Gear.AtCap(s))
                {
                    int loi = gs.Gear.NextTierCost(s);
                    if (loi >= 0 && gs.Cores >= loi) dotPhaDuoc = true;
                    continue;
                }
                float gia = gs.Gear.NextCost(s);
                if (gia >= 0f && gia < reNhat) reNhat = gia;
            }

            if (dotPhaDuoc)                      gearLabel.text = "TRANG BỊ\nĐỘT PHÁ ĐƯỢC";
            else if (reNhat <= gs.Shards)        gearLabel.text = "TRANG BỊ\nNÂNG ĐƯỢC";
            else if (reNhat < float.MaxValue)    gearLabel.text = $"TRANG BỊ\nCÒN {reNhat - gs.Shards:N0}";
            else                                 gearLabel.text = "TRANG BỊ\nTỚI HẠN";

            // Nền LUÔN sáng: cánh cửa này chưa bao giờ khoá, đừng làm nó trông như bị khoá.
            if (gearButton != null && gearButton.targetGraphic is Image bg) bg.color = TintOpen;
            gearLabel.color = Ink;
        }

        /// <summary>Nút NHÂN VẬT đếm ngược tới con kế tiếp — đúng khuôn "CÒN 20 TẦNG".</summary>
        private void RefreshChar(GameState gs)
        {
            if (charLabel == null) return;

            if (gs.BossesKilled >= CharacterRoster.Count - 1)
            {
                charLabel.text = "NHÂN\nVẬT";
            }
            else
            {
                int tangMo = (gs.BossesKilled + 1) * gs.BossEvery;
                int con = Mathf.Max(0, tangMo - gs.HighestCleared);
                charLabel.text = $"NHÂN VẬT\nCÒN {con} TẦNG";
            }
            charLabel.color = Ink;
        }

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
                // Nút khoá phải nói ĐÚNG lý do. Bản đầu ghi "DỌN TẦNG 1" cho mọi trường
                // hợp không bấm được — kể cả khi người chơi đã dọn tầng 1 từ lâu và thứ
                // đang chặn họ là TRẦN NGÂN SÁCH. Một cái nút nói dối về lý do còn tệ hơn
                // một cái nút im lặng: người chơi đi làm đúng việc nó bảo và vẫn không mở.
                bool daDon = gs.HighestCleared >= 1;
                if (on)         sweepLabel.text = $"QUÉT\nT{sweep.TargetFloor}";
                else if (can)   sweepLabel.text = "QUÉT\nNHANH";
                else if (daDon) sweepLabel.text = "QUÉT NHANH\nHẾT NGÂN SÁCH";
                else            sweepLabel.text = "QUÉT NHANH\nDỌN TẦNG 1";
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
