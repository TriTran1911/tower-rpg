using TowerRpg.Progression;
using UnityEngine;

namespace TowerRpg.Player
{
    /// <summary>
    /// Đổi hình người chơi khi đổi nhân vật (§5.5b). Đây là TOÀN BỘ phần "đổi hình dạng" —
    /// sức mạnh do PlayerStats lo, và nó không đổi.
    /// </summary>
    public sealed class PlayerAppearance : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private Sprite[] sprites = new Sprite[CharacterRoster.Count];

        private void Awake()
        {
            if (target == null) target = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            if (GameState.Instance != null) GameState.Instance.CharacterChanged += Apply;
        }

        private void OnDisable()
        {
            if (GameState.Instance != null) GameState.Instance.CharacterChanged -= Apply;
        }

        // GameState nạp save bất đồng bộ nên có thể sẵn sàng SAU OnEnable.
        private bool _hooked;
        private void Update()
        {
            if (_hooked || GameState.Instance == null || !GameState.Instance.Ready) return;
            GameState.Instance.CharacterChanged += Apply;
            _hooked = true;
            Apply(GameState.Instance.CharacterIndex);
        }

        private void Apply(int index)
        {
            if (target == null) return;
            if (index < 0 || index >= sprites.Length || sprites[index] == null) return;
            target.sprite = sprites[index];
        }
    }
}
