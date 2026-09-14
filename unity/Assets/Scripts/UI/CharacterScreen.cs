using System.Collections.Generic;
using TMPro;
using TowerRpg.Player;
using TowerRpg.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TowerRpg.UI
{
    /// <summary>
    /// Chọn nhân vật — §5.5b. Dựng thẻ lúc chạy theo CharacterRoster.Count.
    ///
    /// Màn này phải nói thật một điều khó nói: các nhân vật MẠNH NHƯ NHAU. Người chơi quen
    /// game gacha sẽ đi tìm con "tốt nhất" và không tin là không có. Nên thẻ không hiện
    /// chỉ số sát thương/máu tuyệt đối (sẽ trông như con này hơn con kia) mà hiện đúng cặp
    /// ×k / ÷k cạnh nhau, kèm một dòng nói rõ tích không đổi.
    /// </summary>
    public sealed class CharacterScreen : MonoBehaviour
    {
        [Header("Khung")]
        [SerializeField] private GameObject root;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform hudInfo;
        [SerializeField] private RectTransform cardParent;
        [SerializeField] private TMP_Text hintLabel;

        [Header("Sprite dùng chung")]
        [SerializeField] private Sprite panelSprite;   // gỗ SÁNG — nút nhỏ
        [SerializeField] private Sprite bgSprite;      // gỗ TỐI  — thẻ lớn (xem UpgradeScreen)
        [SerializeField] private Sprite cellSprite;
        [SerializeField] private Sprite[] portraits = new Sprite[CharacterRoster.Count];

        [Header("Màu — tầng giao diện")]
        [SerializeField] private Color paper = new Color(0.91f, 0.88f, 0.81f);
        [SerializeField] private Color gold  = new Color(0.91f, 0.70f, 0.29f);
        [SerializeField] private Color jade  = new Color(0.28f, 0.81f, 0.70f);
        [SerializeField] private Color dim   = new Color(0.71f, 0.67f, 0.63f);

        private const int RowH = 200, Pad = 20, Touch = 144;

        private sealed class Card
        {
            public TMP_Text Name, Stats, Blurb, State;
            public Button Button;
            public Image Bg, Portrait;
        }

        private readonly List<Card> _cards = new List<Card>();
        private bool _built;

        public bool IsOpen => root != null && root.activeSelf;

        private void Start()
        {
            if (root == null || cardParent == null)
            {
                Debug.LogError("[CharacterScreen] Chưa gán 'root' hoặc 'cardParent'.", this);
                enabled = false;
                return;
            }
            root.SetActive(false);
            // Nút ĐÓNG nối Ở ĐÂY, lúc chạy. Nối trong bộ dựng scene thì AddListener bay
            // mất lúc lưu scene — xem ghi chú dài trong HudUI.Start().
            if (closeButton != null) closeButton.onClick.AddListener(Toggle);
        }

        private void OnDisable()
        {
            if (GameState.Instance != null) GameState.Instance.Changed -= Refresh;
        }

        public void Toggle()
        {
            if (root == null) return;

            bool open = !root.activeSelf;
            root.SetActive(open);
            if (open) { root.transform.SetAsLastSibling(); NhacHudLen(); }
            else TraHudVeCho();
            if (!open) return;

            if (!_built) Build();
            if (GameState.Instance != null)
            {
                GameState.Instance.Changed -= Refresh;
                GameState.Instance.Changed += Refresh;
            }
            Refresh();
        }

        private void Build()
        {
            for (int i = 0; i < CharacterRoster.Count; i++) _cards.Add(BuildCard(i));
            _built = true;

            if (hintLabel != null)
                // KHÔNG dùng dấu "÷": font dự phòng của TMP không có glyph đó và nó hiện
                // ra thành "+", tức là nói NGƯỢC hẳn ý nghĩa. Viết chữ cho chắc.
                // Nói THẲNG rằng đổi nhân vật không làm mạnh lên. Người chơi quen gacha sẽ
                // đi tìm con "tốt nhất" và không tin là không có; giấu điều đó chỉ khiến họ
                // mở được Kiếm sĩ sau 8 phút rồi thấy hụt hẫng mà không hiểu vì sao.
                hintLabel.text = "Cùng một tổng sức mạnh, chia khác nhau. Đổi nhân vật KHÔNG "
                               + "làm bạn mạnh hơn — đòn đau hơn thì máu mỏng hơn, đúng bấy "
                               + "nhiêu. Khác nhau ở chỗ tha thứ sai lầm đến đâu.";
        }

        private Card BuildCard(int index)
        {
            var go = new GameObject($"Char_{index}", typeof(RectTransform));
            go.transform.SetParent(cardParent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, RowH);
            rt.anchoredPosition = new Vector2(0f, -index * (RowH + Pad));

            var c = new Card();

            Sprite cardSp = bgSprite != null ? bgSprite : panelSprite;
            c.Bg = go.AddComponent<Image>();
            c.Bg.sprite = cardSp;
            c.Bg.type = cardSp != null && cardSp.border != Vector4.zero
                      ? Image.Type.Sliced : Image.Type.Simple;

            c.Button = go.AddComponent<Button>();
            c.Button.targetGraphic = c.Bg;
            int captured = index;
            c.Button.onClick.AddListener(() => OnPick(captured));

            // ô chân dung
            var cell = new GameObject("Cell", typeof(RectTransform)).AddComponent<Image>();
            cell.transform.SetParent(go.transform, false);
            cell.sprite = cellSprite;
            cell.type = cellSprite != null && cellSprite.border != Vector4.zero
                      ? Image.Type.Sliced : Image.Type.Simple;
            cell.raycastTarget = false;
            cell.rectTransform.anchorMin = cell.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            cell.rectTransform.pivot = new Vector2(0f, 0.5f);
            cell.rectTransform.anchoredPosition = new Vector2(24f, 0f);
            cell.rectTransform.sizeDelta = new Vector2(140f, 140f);

            c.Portrait = new GameObject("Portrait", typeof(RectTransform)).AddComponent<Image>();
            c.Portrait.transform.SetParent(cell.transform, false);
            if (index < portraits.Length) c.Portrait.sprite = portraits[index];
            c.Portrait.raycastTarget = false;
            c.Portrait.preserveAspect = true;
            c.Portrait.rectTransform.anchorMin = c.Portrait.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            c.Portrait.rectTransform.sizeDelta = new Vector2(104f, 104f);

            float x = 184f;
            c.Name  = Label(go.transform, CharacterRoster.Name(index), 36f, paper, x, -22f);
            c.Stats = Label(go.transform, "", 26f, gold, x, -68f);
            c.Blurb = Label(go.transform, CharacterRoster.Blurbs[index], 22f, dim, x, -106f);
            c.State = Label(go.transform, "", 26f, jade, x, -146f);

            return c;
        }

        private static TMP_Text Label(Transform parent, string text, float size, Color colour,
                                      float x, float y)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = colour;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0f, 1f);
            t.rectTransform.pivot = new Vector2(0f, 1f);
            t.rectTransform.anchoredPosition = new Vector2(x, y);
            t.rectTransform.sizeDelta = new Vector2(680f, size * 1.4f);
            return t;
        }

        private void OnPick(int index)
        {
            if (GameState.Instance == null) return;
            if (!GameState.Instance.TrySetCharacter(index)) return;

            // k đổi -> máu tối đa đổi ngay. Không gọi lại là thanh máu nói dối.
            PlayerHealth.Current?.Rescale();
        }

        private void Refresh()
        {
            GameState gs = GameState.Instance;
            if (gs == null || !gs.Ready || !_built) return;

            for (int i = 0; i < _cards.Count; i++)
            {
                Card c = _cards[i];
                bool unlocked = CharacterRoster.IsUnlocked(i, gs.BossesKilled);
                bool current = gs.CharacterIndex == i;
                float k = CharacterRoster.DamageMult(i);

                // SỐ THẬT thay vì hệ số. "×1,80 · ×0,56" bắt người chơi tự nhân; "đòn 23 ·
                // máu 84" là thứ họ thấy lại trên màn hình ngay sau khi đổi. Quy đổi từ
                // chỉ số HIỆN TẠI nên nó đúng với trang bị đang mặc, không phải số trên giấy.
                PlayerStats st = PlayerStats.Instance;
                if (st != null && st.Ready)
                {
                    float kNay = CharacterRoster.DamageMult(gs.CharacterIndex);
                    float donCon = st.Damage / (kNay > 0f ? kNay : 1f) * k;
                    float mauCon = st.MaxHp * (kNay > 0f ? kNay : 1f) / k;
                    c.Stats.text = $"đòn {donCon:0.#}   ·   máu {mauCon:0}";
                }
                else c.Stats.text = $"sát thương ×{k:0.00}   ·   máu ×{1f / k:0.00}";

                if (!unlocked)
                {
                    // NÓI RA SỐ TẦNG. Bản đầu ghi "Hạ boss 2 để mở" mà không nói boss 2 là
                    // tầng 20 — đúng câu chủ dự án hỏi ("khi nào tôi mới dùng được nhân
                    // vật"), và màn hình từ chối trả lời dù gs.BossEvery nằm sẵn trong tay.
                    // Ngay cạnh đó, nút TỰ ĐÁNH ghi "CÒN 20 TẦNG" — cùng một dự án, hai
                    // chuẩn khác nhau.
                    int tangMo = i * gs.BossEvery;
                    int con = Mathf.Max(0, tangMo - gs.HighestCleared);
                    c.State.text = con > 0 ? $"Tầng {tangMo}  ·  còn {con} tầng"
                                           : $"Tầng {tangMo}  ·  hạ boss để mở";
                    c.State.color = dim;
                }
                else if (current)
                {
                    c.State.text = "ĐANG DÙNG";
                    c.State.color = gold;
                }
                else
                {
                    c.State.text = "Chạm để đổi";
                    c.State.color = jade;
                }

                c.Button.interactable = unlocked && !current;
                c.Bg.color = unlocked ? Color.white : new Color(0.62f, 0.60f, 0.58f, 1f);
                // Chân dung khoá chỉ mờ đi, KHÔNG tô gần đen: hình con nhân vật chính là
                // phần thưởng duy nhất của một hệ sưu tầm mà §5.5b cố ý không cho thêm sức
                // mạnh. Tô 0,15 là xoá mất thứ duy nhất còn lại để thèm.
                if (c.Portrait != null)
                    c.Portrait.color = unlocked ? Color.white : new Color(0.55f, 0.52f, 0.50f, 1f);
                c.Name.color = unlocked ? paper : dim;
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
