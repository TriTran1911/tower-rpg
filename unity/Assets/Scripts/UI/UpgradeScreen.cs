using System;
using System.Collections.Generic;
using TMPro;
using TowerRpg.Core;
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
        [SerializeField] private TMP_Text critLabel;
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

        // 4 x (212 + 12) = 896px. Bắt đầu ở -510 thì hết ở -1406, nút tẩy điểm ở -1496. Đổi hai số này là phải tính lại chỗ đó trong BuildM1Scene.
        private const int RowH = 212, Pad = 12, Touch = 144;

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
            // Nút ĐÓNG cũng phải nối Ở ĐÂY. Nối trong bộ dựng scene thì AddListener bay
            // mất lúc lưu scene — xem ghi chú dài trong HudUI.Start().
            if (closeButton != null) closeButton.onClick.AddListener(Toggle);
        }

        private void OnEnable()
        {
            if (GameState.Instance != null) GameState.Instance.Changed += Refresh;
        }

        private void OnDisable()
        {
            if (GameState.Instance != null) GameState.Instance.Changed -= Refresh;
        }

        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform hudInfo;
        [SerializeField] private Player.AutoBattle auto;

        public bool IsOpen => root != null && root.activeSelf;

        public void Toggle()
        {
            if (root == null) return;

            bool open = !root.activeSelf;
            root.SetActive(open);

            // Bảo đảm vẽ SAU mọi thứ khác trong Canvas — thứ tự anh em quyết định thứ tự vẽ.
            // ...rồi nhấc HUD lên TRÊN nó, để người chơi thấy trận đánh vẫn đang chạy.
            if (open) { root.transform.SetAsLastSibling(); NhacHudLen(); }
            else TraHudVeCho();

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
                // TỰ CO. Dòng này mang mô tả DÀI NHẤT của bảng và nội dung đổi lúc chạy
                // ("6→5 đòn t.bình · chí mạng 26 % ×2.2 → 27 % ×2.3"), nên câu dài nhất
                // không phải câu viết trong mã — nó là câu chưa ai viết. Bộ soi màn hình
                // đo được nó cần 538 đơn vị trong khung 460.
                Progress = Label(rowGo.transform, Equipment.StatName(slot), 23f, dim, textX, -108f,
                                 coChuToiThieu: 15f),
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
                                      bool stretch = false, float coChuToiThieu = 0f)
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
            if (coChuToiThieu > 0f)
            {
                t.enableAutoSizing = true;
                t.fontSizeMax = size;
                t.fontSizeMin = coChuToiThieu;
            }

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
        /// <summary>
        /// In ra HẬU QUẢ ĐẾM ĐƯỢC, không phải chỉ số thô.
        ///
        /// §5.11 khoá cứng tăng trưởng ở 4,4%/cấp, nên "Sát thương 10,0 → 10,4" là đúng
        /// nhưng không ai CẢM được 4%. Cùng con số đó, đọc trên số đòn cần để giết một
        /// con quái, ra "8 đòn → 7 đòn" — tức 12,5% nhanh hơn ngay ở trận kế tiếp.
        ///
        /// Đây là việc duy nhất làm bốn ô thành bốn thứ KHÁC NHAU mà không đụng một con
        /// số cân bằng nào: Vũ khí và Nhẫn đổi SỐ ĐÒN, Găng đổi ĐỒNG HỒ, Giáp đổi THỜI
        /// GIAN SỐNG. Và vì bậc thang số đòn xê dịch theo tầng, câu hỏi "mua ô nào" có
        /// đáp án khác nhau ở mỗi tầng — thứ mà bảng "×1,04" không bao giờ nói ra.
        /// </summary>
        private static string MoTaNang(GameState gs, Slot s)
        {
            PlayerStats st = PlayerStats.Instance;
            BalanceConfig b = BalanceConfig.Instance;
            if (st == null || !st.Ready || b == null || !b.IsLoaded)
                return $"{Equipment.StatName(s)} — ×{gs.Gear.Mult(s):0.00}";

            int cap = gs.Gear.Level(s);
            bool conNang = cap < gs.Gear.CapOf(s);

            // Máu một con quái ở ĐÚNG tầng đang đứng — bậc thang đổi theo tầng.
            int soQuai = Mathf.Max(1, b.GetInt("enemy.count"));
            float mauMotCon = b.Get("enemy.hpFloor1")
                            * Mathf.Pow(1f + b.Get("enemy.hpGrowth"), gs.Floor - 1) / soQuai;
            // KHÔNG chia cho số quái: lấy sát thương của CẢ TẦNG. Chia ra thì ở tầng 1
            // con số là "chịu được 400 giây" — to tới mức vô nghĩa, và nó không bao giờ
            // nhỏ lại vì máu nền tăng 4,5%/tầng còn sát thương quái chỉ 4,0%. Lấy cả tầng
            // cho ra 67 giây: vẫn là cận dưới an toàn (hình học cho thấy thường chỉ 1 trong
            // 6 con với tới người chơi), nhưng là con số đọc được và so sánh được.
            float dpsTang = b.Get("enemy.dpsFloor1")
                          * Mathf.Pow(1f + b.Get("enemy.dpsGrowth"), gs.Floor - 1);

            // Hệ số của ô ở cấp bất kỳ, để xem trước mà không phải mua thật.
            float MultO(Slot o, int lv) => gs.Gear.MultAtLevel(o, lv);
            float TiLe(Slot o, int lv) => gs.Gear.Mult(o) > 0f ? MultO(o, lv) / gs.Gear.Mult(o) : 1f;

            // SÁT THƯƠNG TRUNG BÌNH mỗi đòn. Chí mạng giờ NGẪU NHIÊN (quyết định #40)
            // nên đây là KỲ VỌNG, không phải con số chắc chắn như thời thanh dồn: một
            // lượt xui thật sự cần nhiều đòn hơn con số in ra. Vẫn in kỳ vọng vì đó là
            // thứ duy nhất so sánh được giữa hai cấp — nhưng chữ "trung bình" phải có
            // trên màn hình, xem nhánh Vũ khí bên dưới.
            float TiLeChiMang(int lvVuKhi) => Mathf.Min(
                b.Get("crit.chanceCap"),
                b.Get("crit.chance1") * Mathf.Pow(1f + b.Get("crit.chanceGrowth"), lvVuKhi - 1));
            float HeSoChiMang(int lvVuKhi) =>
                b.Get("crit.mult1") * Mathf.Pow(1f + b.Get("crit.multGrowth"), lvVuKhi - 1);

            float SatThuongMoiDon(int lvVuKhi)
            {
                float d = st.Damage * TiLe(Slot.Weapon, lvVuKhi);
                return d * (1f + TiLeChiMang(lvVuKhi) * (HeSoChiMang(lvVuKhi) - 1f));
            }

            int DonCan(int lvVuKhi)
            {
                float moiDon = SatThuongMoiDon(lvVuKhi);
                return moiDon > 0f ? Mathf.CeilToInt(mauMotCon / moiDon) : 999;
            }

            string ten = Equipment.StatName(s);

            switch (s)
            {
                case Slot.Weapon:
                {
                    int nay = DonCan(cap);
                    string cm = $"chí mạng {TiLeChiMang(cap):P0} ×{HeSoChiMang(cap):0.0}";
                    if (!conNang) return $"{nay} đòn trung bình  ·  {cm}  —  tới hạn";

                    int sau = DonCan(cap + 1);
                    string cmSau = $"{TiLeChiMang(cap + 1):P0} ×{HeSoChiMang(cap + 1):0.0}";
                    if (sau < nay) return $"{nay}→{sau} đòn t.bình  ·  {cm} → {cmSau}";
                    for (int them = 2; cap + them <= gs.Gear.CapOf(s); them++)
                        if (DonCan(cap + them) < nay)
                            return $"{nay} đòn t.bình  ·  {cm}  ·  còn {them} cấp nữa xuống {DonCan(cap + them)}";
                    return $"{nay} đòn t.bình  ·  {cm} → {cmSau}";
                }

                case Slot.Ring:
                {
                    // Ô Nhẫn giờ cầm HÚT MÁU (quyết định #40), không còn là hệ số nhân.
                    // Nói bằng thứ ĐẾM ĐƯỢC: hồi bao nhiêu máu cho mỗi con quái giết được,
                    // chứ không phải một con số phần trăm trừu tượng.
                    // ĐƠN VỊ PHẢI ĐẾM ĐƯỢC VÀ PHẢI ĐỔI MỖI CẤP. Hút máu trần 1,20% chia
                    // cho 39 cấp là 0,031%/cấp — in ra phần trăm thì mọi cấp đều hiện
                    // "0,0%", tức trả 300 Mảnh để đổi một con số thành chính nó (đúng cái
                    // bẫy mà test Moi_o_trang_bi_deu_phai_DOI_SO bắt được).
                    // Đơn vị đúng là MỖI TẦNG: dọn một tầng nghĩa là gây đúng tổng máu
                    // của cả tầng đó, nên máu hồi mỗi tầng = hút × tổng máu tầng — vừa là
                    // con số người chơi kiểm chứng được, vừa nhích mỗi lần nâng.
                    float HoiMoiTang(int lv)
                    {
                        float hut = Mathf.Min(b.Get("lifesteal.cap"),
                                              b.Get("lifesteal.perLevel") * Mathf.Max(0, lv - 1));
                        return mauMotCon * soQuai * hut;
                    }
                    float h = HoiMoiTang(cap);
                    string nayS = h <= 0f ? "chưa hồi máu" : $"hồi {h:0.0} máu mỗi tầng";
                    if (!conNang) return $"{nayS}  —  tới hạn";
                    float hSau = HoiMoiTang(cap + 1);
                    if (hSau <= h) return $"{nayS}  ·  đã chạm trần hút máu";
                    return $"{nayS} → {hSau:0.0}";
                }

                case Slot.Glove:
                {
                    int don = DonCan(gs.Gear.Level(Slot.Weapon));
                    float nay = st.AttacksPerSec > 0f ? don / st.AttacksPerSec : 0f;
                    if (!conNang) return $"{don} đòn  ·  {nay:0.0}s  —  tới hạn";

                    float apsSau = st.AttacksPerSec * TiLe(Slot.Glove, cap + 1);
                    float sau = apsSau > 0f ? don / apsSau : 0f;
                    // Nói thẳng: Găng KHÔNG đổi số đòn, chỉ đổi đồng hồ.
                    return $"{don} đòn  ·  {nay:0.0}s → {sau:0.0}s";
                }

                default:
                {
                    float nay = dpsTang > 0f ? st.MaxHp / dpsTang : 0f;
                    if (!conNang) return $"Trụ giữa bầy: {nay:0.0}s  —  tới hạn";

                    float mauSau = st.MaxHp * TiLe(Slot.Armor, cap + 1);
                    float sau = dpsTang > 0f ? mauSau / dpsTang : 0f;
                    return $"Trụ giữa bầy: {nay:0.0}s → {sau:0.0}s";
                }
            }
        }

        private static void SetButton(Row r, bool on, Color tint)
        {
            r.Button.interactable = on;
            r.ButtonBg.color = on ? tint : new Color(0.42f, 0.40f, 0.37f, 1f);
            // Nền nút khoá bị nhân tối còn (102,56,28), nên chữ phải SÁNG — chữ mực trên
            // nền đó chỉ đạt 1,07:1 so với ngưỡng 4,5 của chính dự án, tức "NÂNG" và cái
            // giá "300" gần như vô hình đúng trong 42 giây người chơi mới quyết định game
            // này có đáng chơi không. Cùng màu HudUI đã dùng cho nút khoá: 5,37:1.
            r.Action.color = on ? Ink : InkOff;
            r.Cost.color = r.Action.color;
        }

        private static readonly Color Ink = new Color(0.10f, 0.09f, 0.08f);
        private static readonly Color InkOff = new Color(0.78f, 0.75f, 0.70f);
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

            // "+N đang chạy" — NÓI THẲNG rằng mở bảng này KHÔNG dừng trò chơi. Đo A/B:
            // 30 giây ngồi trong bảng lúc tự đánh vẫn +1 tầng, +400 Mảnh, y hệt lúc không
            // mở. Điều đó đúng từ M2 và chưa bao giờ hiện ra một chữ nào, nên người chơi
            // hợp lý mà cho rằng phải đóng bảng lại mới cày tiếp được.
            // Chỉ nói khi TỰ ĐÁNH đang chạy: lúc đó vòng lặp thật sự tự tiếp diễn. Tắt tự
            // đánh mà vẫn hứa "vẫn đang chạy" thì là nói dối — tấm phủ chặn cần gạt nên
            // người chơi đứng im, và đứng im thì không kiếm được gì.
            if (shardLabel != null)
            {
                bool dangCay = auto != null && auto.Enabled;
                shardLabel.text = dangCay
                    ? $"{gs.Shards:N0} Mảnh   ·   tầng {gs.Floor}, tự đánh vẫn chạy"
                    : $"{gs.Shards:N0} Mảnh   ·   tầng {gs.Floor}";
            }
            // "x / N cả game" — §5.6: Lõi hữu hạn TUYỆT ĐỐI. N suy từ tower.floors và
            // tower.bossEvery chứ KHÔNG cắm cứng 100 như bản đầu (§9.2). Con số tổng
            // phải nằm cạnh con số đang có, nếu không người chơi không có cách nào biết
            // mình đã tiêu bao nhiêu phần của một nguồn không bao giờ sinh thêm.
            if (coreLabel != null)
            {
                int daTieu = gs.Gear.CoresSpent();
                int tongCaGame = gs.CoresTotalInGame;
                coreLabel.text = $"{gs.Cores} Lõi  ·  đã tiêu {daTieu}/{tongCaGame} cả game";
            }

            // Chí mạng giờ là XÁC SUẤT (quyết định #40), không còn thanh dồn. Câu chú
            // thích cũ ở đây dặn nhãn phải ghi "đánh liên tục" vì §5.4 cho phép lùi lại
            // chờ mà không mất thanh — luật đó không còn nữa, và giữ lại chú thích ấy sẽ
            // dạy người sau một cơ chế đã bị gỡ.
            if (critLabel != null)
            {
                // Nói cả hai con số, và nói cả cái giá của ngẫu nhiên: "trung bình" là
                // chữ quan trọng nhất dòng này.
                PlayerStats st = PlayerStats.Instance;
                float p = st != null && st.Ready ? st.CritChance : 0f;
                float m = st != null && st.Ready ? st.CritMultiplier : 2f;
                float hut = st != null && st.Ready ? st.Lifesteal : 0f;
                string sHut = hut > 0f ? $"  —  hút máu {hut:P2}" : "";
                critLabel.text = $"Chí mạng {p:P0} · ×{m:0.00}  —  trung bình mỗi đòn "
                               + $"×{1f + p * (m - 1f):0.00}{sHut}";
            }

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
                    // Số THẬT thay vì hệ số trừu tượng: "×1,04" không nói gì, còn
                    // "Sát thương 10,0 → 10,4" là thứ người chơi thấy lại trên màn hình
                    // ngay sau khi bấm. Cùng một phép nhân, hai mức đọc được khác hẳn.
                    r.Progress.text = MoTaNang(gs, s);
                    r.Progress.color = dim;
                    r.Action.text = "NÂNG";
                    r.Cost.text = $"{cost:N0}";
                    SetButton(r, afford, TintUpgrade);
                }

                r.Fill.fillAmount = Mathf.Clamp01(cap > 1 ? (float)(lvl - 1) / (cap - 1) : 1f);
                r.Fill.color = atCap ? (maxTier ? cinnabar : gold) : jade;
            }
        }

        // ── GIỮ HUD SỐNG TRÊN BẢNG (quyết định #36) ───────────────────────────────
        // Đo A/B: mở bảng này giữa lúc tự đánh KHÔNG tốn gì cả — 30 giây ngồi trong
        // bảng vẫn +1 tầng, +400 Mảnh, nhân vật vẫn đi 11,8 đơn vị, y hệt lúc không
        // mở. Khả năng "vừa thu thập vừa nâng cấp" ĐÃ CÓ SẴN từ M2; thứ thiếu là
        // người chơi không có cách nào THẤY nó, vì tấm phủ che kín màn hình.
        // Nhấc cụm HUD lên trên tấm phủ là đủ: số tầng nhảy, Mảnh chạy, máu vơi —
        // ngay trước mắt trong lúc họ đang cân nhắc tiêu tiền.
        //
        // TRẢ VỀ ĐÚNG CHỖ CŨ khi đóng. Để nó nằm trên cùng vĩnh viễn thì thẻ chương
        // và màn đỉnh tháp bị HUD đè lên — hai thứ đó phải che được mọi thứ.
        private int _choCuHud = -1;

        private void NhacHudLen()
        {
            if (hudInfo == null) return;
            if (_choCuHud < 0) _choCuHud = hudInfo.GetSiblingIndex();
            hudInfo.SetAsLastSibling();
        }

        private void TraHudVeCho()
        {
            if (hudInfo == null || _choCuHud < 0) return;
            hudInfo.SetSiblingIndex(_choCuHud);
            _choCuHud = -1;
        }

    }
}
