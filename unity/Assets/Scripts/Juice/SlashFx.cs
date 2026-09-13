using System;
using UnityEngine;

namespace TowerRpg.Juice
{
    /// <summary>
    /// Vệt chém hiện ở vị trí mục tiêu khi người chơi ra đòn. Bốn khung hình chạy hết
    /// trong juice.slashSeconds rồi trả về pool.
    ///
    /// Thuần trang trí: nó không gây sát thương, không đổi thời điểm nào. AutoAttack đã
    /// gây sát thương xong trước khi gọi tới đây.
    /// </summary>
    public sealed class SlashFx : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer view;
        [SerializeField] private Sprite[] frames = new Sprite[4];

        private Action<SlashFx> _onDone;
        private float _seconds, _t;

        private void Awake() { if (view == null) view = GetComponent<SpriteRenderer>(); }

        public void Play(Vector3 at, float seconds, bool crit, Action<SlashFx> onDone)
        {
            _onDone = onDone;
            _seconds = Mathf.Max(0.02f, seconds);
            _t = 0f;
            transform.position = at;
            // Chí mạng: to hơn và ngả vàng. Cùng thông tin với tiếng chém sắc và số vàng,
            // ba kênh nói cùng một điều — đòn thứ 5 của thanh dồn §5.4.
            transform.localScale = Vector3.one * (crit ? 1.35f : 1f);
            if (view != null)
            {
                view.color = crit ? new Color(1f, 0.93f, 0.62f) : Color.white;
                if (frames.Length > 0 && frames[0] != null) view.sprite = frames[0];
            }
            gameObject.SetActive(true);
        }

        private void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / _seconds);

            if (view != null && frames.Length > 0)
            {
                int i = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(k * frames.Length));
                if (frames[i] != null) view.sprite = frames[i];
                Color c = view.color;
                view.color = new Color(c.r, c.g, c.b, 1f - k * k);
            }

            if (k >= 1f)
            {
                gameObject.SetActive(false);
                Action<SlashFx> done = _onDone;
                _onDone = null;
                done?.Invoke(this);
            }
        }
    }
}
