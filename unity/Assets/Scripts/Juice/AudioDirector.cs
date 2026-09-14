using TowerRpg.Core;
using TowerRpg.Progression;
using UnityEngine;

namespace TowerRpg.Juice
{
    /// <summary>
    /// Nhạc nền: một bài cho tầng thường, một bài cho tầng boss, chuyển bằng fade chéo.
    ///
    /// Import phải là STREAMING chứ không phải Compressed In Memory như hiệu ứng: một bài
    /// 2-3 phút giải nén vào RAM là hàng chục MB cho đúng một thứ đang phát. Hiệu ứng thì
    /// ngược lại — ngắn, phát liên tục, nên nạp sẵn.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public sealed class AudioDirector : MonoBehaviour
    {
        [SerializeField] private AudioClip normalTrack;
        [SerializeField] private AudioClip bossTrack;
        [SerializeField] private FloorRunner runner;
        [SerializeField] private float fadeSeconds = 0.8f;

        private AudioSource _a, _b;
        private AudioSource _cur;
        // Mảng dựng SẴN MỘT LẦN. Bản M3 viết `foreach (var s in new[] { _a, _b })` ngay
        // trong Update — tức cấp phát một mảng mỗi khung hình, 60 mảng/giây suốt hai
        // tiếng rưỡi chơi. Đúng kiểu rác GC mà SfxPlayer và DamagePopupSpawner đã phải
        // dựng pool để tránh, lọt lại ở lớp nhạc vì lớp nhạc trông như chỉ chạy lúc đổi bài.
        private AudioSource[] _both;
        private float _volume = 0.3f;
        private bool _ready, _inBoss;

        private void Awake()
        {
            _a = Make("MusicA");
            _b = Make("MusicB");
            _cur = _a;
            _both = new[] { _a, _b };
        }

        private AudioSource Make(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            AudioSource s = go.AddComponent<AudioSource>();
            s.loop = true;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.volume = 0f;
            return s;
        }

        private void Start() => BalanceConfig.TryUse(this, b =>
        {
            _volume = Mathf.Clamp01(b.Get("audio.musicVolume"));
            _ready = true;
            Doi(false, tucThi: true);
        });

        private void Update()
        {
            if (!_ready) return;

            bool boss = runner != null && runner.InBossFight;
            if (boss != _inBoss) Doi(boss, tucThi: false);

            // Fade chéo: nguồn đang chạy lên _volume, nguồn kia về 0 rồi dừng hẳn.
            // unscaledDeltaTime, không phải deltaTime: màn hình cột mốc và màn hình đỉnh
            // tháp đặt timeScale = 0, và chúng bật lên ĐÚNG lúc vừa hạ boss — tức đúng
            // lúc nhạc phải chuyển từ bài boss về bài thường. Với deltaTime thì bài boss
            // treo nguyên âm lượng suốt thời gian người chơi đọc màn hình chúc mừng.
            float step = fadeSeconds > 0f ? _volume / fadeSeconds * Time.unscaledDeltaTime : _volume;
            foreach (AudioSource s in _both)
            {
                if (s == null) continue;
                float dich = s == _cur ? _volume : 0f;
                s.volume = Mathf.MoveTowards(s.volume, dich, step);
                if (s != _cur && s.volume <= 0.001f && s.isPlaying) s.Stop();
            }
        }

        private void Doi(bool boss, bool tucThi)
        {
            _inBoss = boss;
            AudioClip clip = boss ? bossTrack : normalTrack;
            if (clip == null) return;

            AudioSource moi = _cur == _a ? _b : _a;
            moi.clip = clip;
            moi.volume = tucThi ? _volume : 0f;
            moi.Play();
            _cur = moi;

            if (tucThi)
            {
                AudioSource kia = _cur == _a ? _b : _a;
                if (kia != null) { kia.volume = 0f; kia.Stop(); }
            }
        }
    }
}
