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
            if (open) root.transform.SetAsLastSibling();
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
                hintLabel.text = "Sát thương nhân k, máu chia k — tích không đổi, nên không con "
                               + "nào mạnh hơn con nào. Khác nhau ở chỗ tha thứ sai lầm đến đâu.";
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

                c.Stats.text = $"sát thương ×{k:0.00}   ·   máu ×{1f / k:0.00}";

                if (!unlocked)
                {
                    c.State.text = $"Hạ boss {i} để mở";
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
                if (c.Portrait != null)
                    c.Portrait.color = unlocked ? Color.white : new Color(0.15f, 0.15f, 0.15f, 1f);
                c.Name.color = unlocked ? paper : dim;
            }
        }
    }
}
