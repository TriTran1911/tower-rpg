using System;
using System.Collections.Generic;
using TMPro;
using TowerRpg.Player;
using TowerRpg.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TowerRpg.UI
{
    /// <summary>
    /// Màn hình nâng cấp trang bị — docs/GIAO-DIEN.md §4.
    ///
    /// Tự dựng các dòng lúc chạy theo Equipment.SlotCount, nên thêm bớt ô trang bị
    /// không phải sửa scene. Bố cục phục vụ đúng một việc: làm cho sự khan hiếm
    /// NHÌN THẤY ĐƯỢC — ở M2 là trần cấp, từ M3 là Lõi.
    ///
    /// MỘT NÚT, HAI NGHĨA: dưới trần nó là "NÂNG" và tiêu Mảnh; chạm trần nó tự đổi thành
    /// "ĐỘT PHÁ" và tiêu Lõi. Không tách hai nút vì bức tường và lối ra khỏi bức tường phải
    /// nằm đúng một chỗ — người chơi chạm trần là thấy ngay phải làm gì, không đi tìm.
    /// </summary>
    public sealed class UpgradeScreen : MonoBehaviour
    {
        [Header("Khung")]
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform rowParent;
        [SerializeField] private TMP_Text shardLabel;
        [SerializeField] private TMP_Text coreLabel;
        [SerializeField] private Button respecButton;
        [SerializeField] private TMP_Text respecLabel;

        [Header("Sprite dùng chung")]
        [SerializeField] private Sprite panelSprite;   // gỗ SÁNG — chỉ dùng cho nút nhỏ
        [SerializeField] private Sprite bgSprite;      // gỗ TỐI  — dùng cho mảng lớn
        [SerializeField] private Sprite cellSprite;
        [SerializeField] private Sprite[] slotIcons = new Sprite[Equipment.SlotCount];

        [Header("Màu — tầng giao diện, không đổi theo chương")]
        [SerializeField] private Color paper = new Color(0.91f, 0.88f, 0.81f);
        [SerializeField] private Color gold  = new Color(0.91f, 0.70f, 0.29f);
        [SerializeField] private Color jade  = new Color(0.28f, 0.81f, 0.70f);
        [SerializeField] private Color dim   = new Color(0.71f, 0.67f, 0.63f);
        [SerializeField] private Color cinnabar = new Color(0.89f, 0.61f, 0.58f);   // son — Lõi
        [SerializeField] private Color ink = new Color(0.10f, 0.09f, 0.08f);        // chữ trên nút sáng

        // VÌ SAO HAI SPRITE KHÁC NHAU: nine_path_panel có RUỘT CAM SÁNG (243,140,76).
        // 9-patch kéo giãn phần ruột, nên panel nhỏ thì viền tối chiếm gần hết (trông tối),
        // còn panel to thì ruột cam chiếm gần hết (trông sáng chói). Chữ giấy trên nền cam
        // chỉ đạt tương phản 1,85:1 — dưới xa mức 4,5:1 và thực tế là không đọc được.
        // Nên: mảng lớn dùng nine_path_bg (tối, chữ giấy đạt 7,87:1);
        //      nút nhỏ giữ nine_path_panel (cam) nhưng chữ phải là MỰC (7,41:1).

        // 4 x (220 + 14) = 936px. Bắt đầu ở -490 thì hết ở -1426, còn nút tẩy điểm ở
        // -1496 — chừa 70px. Đổi hai số này là phải tính lại chỗ đó trong BuildM1Scene.
        private const int RowH = 220, Pad = 14, Touch = 144;

        private sealed class Row
        {
            public TMP_Text Name, Level, Cost, Progress, Action;
            public Image Fill;
            public Button Button;
            public Image ButtonBg;
        }

        private readonly List<Row> _rows = new List<Row>();
        private bool _built;

        private void Start()
        {
            if (root == null || rowParent == null)
            {
                Debug.LogError("[UpgradeScreen] Chưa gán 'root' hoặc 'rowParent'.", this);
                enabled = false;
                return;
            }

            root.SetActive(false);

            if (respecButton != null) respecButton.onClick.AddListener(OnRespec);
        }

        private void OnEnable()
        {
            if (GameState.Instance != null) GameState.Instance.Changed += Refresh;
        }

        private void OnDisable()
        {
            if (GameState.Instance != null) GameState.Instance.Changed -= Refresh;
        }

        public bool IsOpen => root != null && root.activeSelf;

        public void Toggle()
        {
            if (root == null) return;

            bool open = !root.activeSelf;
            root.SetActive(open);

            // Bảo đảm vẽ SAU mọi thứ khác trong Canvas — thứ tự anh em quyết định thứ tự vẽ.
            if (open) root.transform.SetAsLastSibling();

            if (!open) return;

            if (!_built) BuildRows();
            // đăng ký lại phòng khi GameState sẵn sàng SAU khi màn này bật lần đầu
            if (GameState.Instance != null)
            {
                GameState.Instance.Changed -= Refresh;
                GameState.Instance.Changed += Refresh;
            }
            Refresh();
        }

        private void BuildRows()
        {
            for (int i = 0; i < Equipment.SlotCount; i++)
                _rows.Add(BuildRow((Slot)i, i));
            _built = true;
        }

        private Row BuildRow(Slot slot, int index)
        {
            var rowGo = new GameObject($"Row_{slot}", typeof(RectTransform));
            rowGo.transform.SetParent(rowParent, false);
            var rt = rowGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, 0f);
            rt.offsetMax = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(0f, RowH);
            rt.anchoredPosition = new Vector2(0f, -index * (RowH + Pad));

            Sliced(rowGo.transform, bgSprite != null ? bgSprite : panelSprite, Color.white,
                   Vector2.zero, Vector2.one, "Bg", toBack: true);

            // ô icon
            var cell = Sliced(rowGo.transform, cellSprite, Color.white, new Vector2(0f, 0.5f),
                              new Vector2(0f, 0.5f), "Cell");
            cell.rectTransform.pivot = new Vector2(0f, 0.5f);
            cell.rectTransform.anchoredPosition = new Vector2(28f, 0f);
            cell.rectTransform.sizeDelta = new Vector2(140f, 140f);

            if (index < slotIcons.Length && slotIcons[index] != null)
            {
                var ic = new GameObject("Icon", typeof(RectTransform)).AddComponent<Image>();
                ic.transform.SetParent(cell.transform, false);
                ic.sprite = slotIcons[index];
                ic.raycastTarget = false;
                ic.rectTransform.anchorMin = ic.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                ic.rectTransform.sizeDelta = new Vector2(112f, 112f);
            }

            float textX = 200f;
            var r = new Row
            {
                Name     = Label(rowGo.transform, Equipment.DisplayName(slot), 36f, paper, textX, -20f),
                Level    = Label(rowGo.transform, "Cấp 1", 30f, gold, textX, -66f),
                Progress = Label(rowGo.transform, Equipment.StatName(slot), 23f, dim, textX, -108f),
            };

            // thanh tiến tới cấp kế
            var track = new GameObject("Track", typeof(RectTransform)).AddComponent<Image>();
            track.transform.SetParent(rowGo.transform, false);
            track.color = new Color(0.20f, 0.15f, 0.10f, 1f);
            track.raycastTarget = false;
            track.rectTransform.anchorMin = new Vector2(0f, 1f);
            track.rectTransform.anchorMax = new Vector2(0f, 1f);
            track.rectTransform.pivot = new Vector2(0f, 1f);
            track.rectTransform.anchoredPosition = new Vector2(textX, -150f);
            track.rectTransform.sizeDelta = new Vector2(440f, 28f);

            r.Fill = new GameObject("Fill", typeof(RectTransform)).AddComponent<Image>();
            r.Fill.transform.SetParent(track.transform, false);
            r.Fill.color = jade;
            r.Fill.raycastTarget = false;
            r.Fill.type = Image.Type.Filled;
            r.Fill.fillMethod = Image.FillMethod.Horizontal;
            r.Fill.rectTransform.anchorMin = Vector2.zero;
            r.Fill.rectTransform.anchorMax = Vector2.one;
            r.Fill.rectTransform.offsetMin = new Vector2(3f, 3f);
            r.Fill.rectTransform.offsetMax = new Vector2(-3f, -3f);

            // nút — vùng chạm 144px
            var btnGo = new GameObject("Upgrade", typeof(RectTransform));
            btnGo.transform.SetParent(rowGo.transform, false);
            var brt = btnGo.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = new Vector2(1f, 0.5f);
            brt.pivot = new Vector2(1f, 0.5f);
            brt.anchoredPosition = new Vector2(-28f, 0f);
            brt.sizeDelta = new Vector2(230f, Touch);

            r.ButtonBg = btnGo.AddComponent<Image>();
            r.ButtonBg.sprite = panelSprite;
            r.ButtonBg.type = Image.Type.Sliced;
            r.Button = btnGo.AddComponent<Button>();
            r.Button.targetGraphic = r.ButtonBg;

            Slot captured = slot;
            r.Button.onClick.AddListener(() => OnUpgrade(captured));

            // Chữ trên nút là MỰC vì nút dùng gỗ sáng — xem ghi chú ở đầu lớp.
            r.Action = Label(btnGo.transform, "NÂNG", 30f, ink, 0f, -38f, TextAlignmentOptions.Center, true);
            r.Cost = Label(btnGo.transform, "0", 24f, ink, 0f, -86f, TextAlignmentOptions.Center, true);

            return r;
        }

        /// <param name="toBack">
        /// Đẩy xuống dưới cùng. CHỈ đúng với nền của dòng. Ô icon mà cũng đẩy xuống thì nó
        /// nằm SAU nền và biến mất hoàn toàn — lỗi này test không bắt được (tham chiếu vẫn
        /// đủ, icon vẫn tồn tại), chỉ nhìn ảnh chụp mới thấy ô trống trơn.
        /// </param>
        private static Image Sliced(Transform parent, Sprite sp, Color c,
                                    Vector2 aMin, Vector2 aMax, string name, bool toBack = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sp;
            img.type = sp != null && sp.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            img.color = c;
            img.raycastTarget = false;
            img.rectTransform.anchorMin = aMin;
            img.rectTransform.anchorMax = aMax;
            if (aMin == Vector2.zero && aMax == Vector2.one)
                img.rectTransform.offsetMin = img.rectTransform.offsetMax = Vector2.zero;
            if (toBack) go.transform.SetAsFirstSibling();
            return img;
        }

        private static TMP_Text Label(Transform parent, string text, float size, Color colour,
                                      float x, float y,
                                      TextAlignmentOptions align = TextAlignmentOptions.Left,
                                      bool stretch = false)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = colour;
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;

            RectTransform rt = t.rectTransform;
            if (stretch)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.offsetMin = new Vector2(0f, 0f);
                rt.offsetMax = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(0f, y);
                rt.sizeDelta = new Vector2(0f, size * 1.4f);
            }
            else
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(x, y);
                rt.sizeDelta = new Vector2(460f, size * 1.4f);
            }
            return t;
        }

        /// <summary>Một nút: chạm trần thì đột phá bằng Lõi, chưa chạm thì nâng bằng Mảnh.</summary>
        private void OnUpgrade(Slot slot)
        {
            GameState gs = GameState.Instance;
            if (gs == null) return;

            bool ok = gs.Gear.AtCap(slot) ? gs.TryBreakthrough(slot) : gs.TryUpgrade(slot);
            if (!ok) return;

            Juice.SfxPlayer.Play(Juice.Sfx.Upgrade);

            // Giáp đổi -> máu tối đa đổi. Không gọi lại là thanh máu nói dối.
            if (slot == Slot.Armor) PlayerHealth.Current?.Rescale();
        }

        private void OnRespec()
        {
            if (GameState.Instance == null) return;
            if (!GameState.Instance.TryRespec()) return;
            PlayerHealth.Current?.Rescale();      // cấp bị cắt về trần nền -> máu tụt theo
        }

        /// <summary>
        /// Nút phân biệt NÂNG với ĐỘT PHÁ bằng SẮC NỀN, không bằng màu chữ: chữ trên gỗ
        /// sáng bắt buộc phải là mực mới đọc được, nên màu chữ không còn là kênh rảnh.
        /// </summary>
        private static void SetButton(Row r, bool on, Color tint)
        {
            r.Button.interactable = on;
            r.ButtonBg.color = on ? tint : new Color(0.42f, 0.40f, 0.37f, 1f);
            r.Action.color = on ? Ink : new Color(0.30f, 0.28f, 0.26f);
            r.Cost.color = r.Action.color;
        }

        private static readonly Color Ink = new Color(0.10f, 0.09f, 0.08f);
        private static readonly Color TintUpgrade = Color.white;                       // gỗ cam nguyên bản
        private static readonly Color TintBreak = new Color(1f, 0.72f, 0.66f, 1f);     // ngả son
        private static readonly Color TintDone  = new Color(0.78f, 0.72f, 0.66f, 1f);

        private void RefreshRespec(GameState gs)
        {
            if (respecButton == null) return;

            int refund = gs.Gear.CoresSpent();
            float cost = gs.RespecCost();
            bool can = refund > 0 && gs.Shards >= cost;

            respecButton.interactable = can;
            if (respecLabel != null)
                respecLabel.text = refund <= 0
                    ? "Chưa tiêu Lõi nào"
                    : $"TẨY ĐIỂM  ·  hoàn {refund} Lõi  ·  {cost:N0} Mảnh";
        }

        private void Refresh()
        {
            GameState gs = GameState.Instance;
            if (gs == null || !gs.Ready || !_built) return;

            if (shardLabel != null) shardLabel.text = $"{gs.Shards:N0} Mảnh";
            if (coreLabel  != null) coreLabel.text  = $"{gs.Cores} Lõi";

            RefreshRespec(gs);

            for (int i = 0; i < _rows.Count; i++)
            {
                Slot s = (Slot)i;
                Row r = _rows[i];
                int lvl = gs.Gear.Level(s);
                int cap = gs.Gear.CapOf(s);
                int tier = gs.Gear.Tier(s);
                bool atCap = gs.Gear.AtCap(s);
                bool maxTier = gs.Gear.AtMaxTier(s);

                r.Level.text = $"Cấp {lvl}/{cap}" + (tier > 0 ? $"  ·  đột phá {tier}" : "");

                if (atCap && maxTier)
                {
                    // Hết đường: hết cấp, hết cổng. Không có gì để bấm nữa.
                    r.Progress.text = $"{Equipment.StatName(s)} — TỚI HẠN ×{gs.Gear.Mult(s):0.00}";
                    r.Progress.color = cinnabar;
                    r.Action.text = "TỚI HẠN";
                    r.Cost.text = "—";
                    SetButton(r, false, TintDone);
                }
                else if (atCap)
                {
                    // Bức tường. Nút tự đổi nghĩa sang Lõi — lối ra nằm ngay tại chỗ tắc.
                    int cores = gs.Gear.NextTierCost(s);
                    bool afford = gs.Cores >= cores;
                    r.Progress.text = $"{Equipment.StatName(s)} — CHẠM TRẦN {cap}, cần Lõi";
                    r.Progress.color = cinnabar;
                    r.Action.text = "ĐỘT PHÁ";
                    r.Cost.text = $"{cores} Lõi";
                    SetButton(r, afford, TintBreak);
                }
                else
                {
                    float cost = gs.Gear.NextCost(s);
                    bool afford = gs.Shards >= cost;
                    r.Progress.text = $"{Equipment.StatName(s)} — ×{gs.Gear.Mult(s):0.00}";
                    r.Progress.color = dim;
                    r.Action.text = "NÂNG";
                    r.Cost.text = $"{cost:N0}";
                    SetButton(r, afford, TintUpgrade);
                }

                r.Fill.fillAmount = Mathf.Clamp01(cap > 1 ? (float)(lvl - 1) / (cap - 1) : 1f);
                r.Fill.color = atCap ? (maxTier ? cinnabar : gold) : jade;
            }
        }
    }
}
