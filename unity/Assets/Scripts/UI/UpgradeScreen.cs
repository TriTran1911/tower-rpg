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
    /// </summary>
    public sealed class UpgradeScreen : MonoBehaviour
    {
        [Header("Khung")]
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform rowParent;
        [SerializeField] private TMP_Text shardLabel;

        [Header("Sprite dùng chung")]
        [SerializeField] private Sprite panelSprite;
        [SerializeField] private Sprite cellSprite;
        [SerializeField] private Sprite[] slotIcons = new Sprite[Equipment.SlotCount];

        [Header("Màu — tầng giao diện, không đổi theo chương")]
        [SerializeField] private Color paper = new Color(0.91f, 0.88f, 0.81f);
        [SerializeField] private Color gold  = new Color(0.91f, 0.70f, 0.29f);
        [SerializeField] private Color jade  = new Color(0.28f, 0.81f, 0.70f);
        [SerializeField] private Color dim   = new Color(0.58f, 0.53f, 0.46f);

        private const int RowH = 260, Pad = 24, Touch = 144;

        private sealed class Row
        {
            public TMP_Text Name, Level, Cost, Progress;
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

            Sliced(rowGo.transform, panelSprite, Color.white, Vector2.zero, Vector2.one, "Bg");

            // ô icon
            var cell = Sliced(rowGo.transform, cellSprite, Color.white, new Vector2(0f, 0.5f),
                              new Vector2(0f, 0.5f), "Cell");
            cell.rectTransform.pivot = new Vector2(0f, 0.5f);
            cell.rectTransform.anchoredPosition = new Vector2(28f, 0f);
            cell.rectTransform.sizeDelta = new Vector2(150f, 150f);

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
                Name     = Label(rowGo.transform, Equipment.DisplayName(slot), 38f, paper, textX, -26f),
                Level    = Label(rowGo.transform, "Cấp 1", 32f, gold, textX, -78f),
                Progress = Label(rowGo.transform, Equipment.StatName(slot), 24f, dim, textX, -124f),
            };

            // thanh tiến tới cấp kế
            var track = new GameObject("Track", typeof(RectTransform)).AddComponent<Image>();
            track.transform.SetParent(rowGo.transform, false);
            track.color = new Color(0.20f, 0.15f, 0.10f, 1f);
            track.raycastTarget = false;
            track.rectTransform.anchorMin = new Vector2(0f, 1f);
            track.rectTransform.anchorMax = new Vector2(0f, 1f);
            track.rectTransform.pivot = new Vector2(0f, 1f);
            track.rectTransform.anchoredPosition = new Vector2(textX, -170f);
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

            Label(btnGo.transform, "NÂNG", 30f, paper, 0f, -38f, TextAlignmentOptions.Center, true);
            r.Cost = Label(btnGo.transform, "0", 24f, gold, 0f, -86f, TextAlignmentOptions.Center, true);

            return r;
        }

        private static Image Sliced(Transform parent, Sprite sp, Color c,
                                    Vector2 aMin, Vector2 aMax, string name)
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
            go.transform.SetAsFirstSibling();
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

        private void OnUpgrade(Slot slot)
        {
            if (GameState.Instance == null) return;
            if (!GameState.Instance.TryUpgrade(slot)) return;

            // Giáp đổi -> máu tối đa đổi. Không gọi lại là thanh máu nói dối.
            if (slot == Slot.Armor) PlayerHealth.Current?.Rescale();
        }

        private void Refresh()
        {
            GameState gs = GameState.Instance;
            if (gs == null || !gs.Ready || !_built) return;

            if (shardLabel != null) shardLabel.text = $"{gs.Shards:N0} Mảnh";

            for (int i = 0; i < _rows.Count; i++)
            {
                Slot s = (Slot)i;
                Row r = _rows[i];
                int lvl = gs.Gear.Level(s);
                bool atCap = gs.Gear.AtCap(s);
                float cost = gs.Gear.NextCost(s);
                bool afford = !atCap && gs.Shards >= cost;

                r.Level.text = $"Cấp {lvl}";
                r.Progress.text = atCap
                    ? $"{Equipment.StatName(s)} — CHẠM TRẦN {gs.Gear.MaxLevel}"
                    : $"{Equipment.StatName(s)} — ×{gs.Gear.Mult(s):0.00}";
                r.Progress.color = atCap ? gold : dim;

                r.Fill.fillAmount = atCap ? 1f : Mathf.Clamp01((float)(lvl - 1) / (gs.Gear.MaxLevel - 1));
                r.Fill.color = atCap ? gold : jade;

                r.Cost.text = atCap ? "cần Lõi" : $"{cost:N0}";
                r.Button.interactable = afford;
                r.ButtonBg.color = afford ? Color.white : new Color(0.55f, 0.55f, 0.55f, 1f);
            }
        }
    }
}
