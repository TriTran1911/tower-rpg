using System;
using UnityEngine;

namespace TowerRpg.Juice
{
    /// <summary>
    /// Cụm khói ở chỗ quái vừa chết. Giải quyết mục treo ở §7 từ M1: bộ asset KHÔNG có
    /// hoạt ảnh chết cho bất kỳ con quái nào (quyết định #18), và kế hoạch ghi sẵn là
    /// "lấp bằng FX khói/nổ có sẵn — cân nhắc lại ở M5".
    ///
    /// KHÔNG GỘP VÀO SlashFx dù hai lớp nhìn giống nhau. Ba điểm khác về hành vi, và
    /// gộp lại thì cả hai đều phải mang cờ của nhau:
    ///   • SlashFx mờ dần theo k², khói thì bung to ra rồi mới mờ — đọc ra "toả" thay vì "tắt".
    ///   • SlashFx đổi màu theo chí mạng; khói lấy cỡ theo việc đó là boss hay quái thường.
    ///   • SlashFx do người chơi gọi mỗi đòn (1,2 lần/giây); khói do quái gọi lúc chết.
    /// </summary>
    public sealed class PuffFx : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer view;
        [SerializeField] private Sprite[] frames = new Sprite[6];

        private Action<PuffFx> _onDone;
        private float _seconds, _t, _scale;

        private void Awake() { if (view == null) view = GetComponent<SpriteRenderer>(); }

        public void Play(Vector3 at, float seconds, float scale, Action<PuffFx> onDone)
        {
            _onDone = onDone;
            _seconds = Mathf.Max(0.02f, seconds);
            _scale = Mathf.Max(0.1f, scale);
            _t = 0f;
            transform.position = at;
            transform.localScale = Vector3.one * _scale;
            if (view != null)
            {
                view.color = Color.white;
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

                // Bung ra 1,0 -> 1,45 lần rồi mờ ở NỬA SAU. Mờ ngay từ đầu thì sáu khung
                // hình vẽ tay của bộ asset không ai nhìn thấy khung nào.
                transform.localScale = Vector3.one * (_scale * (1f + 0.45f * k));
                view.color = new Color(1f, 1f, 1f, k < 0.5f ? 1f : 1f - (k - 0.5f) * 2f);
            }

            if (k >= 1f)
            {
                gameObject.SetActive(false);
                Action<PuffFx> done = _onDone;
                _onDone = null;
                done?.Invoke(this);
            }
        }
    }
}
