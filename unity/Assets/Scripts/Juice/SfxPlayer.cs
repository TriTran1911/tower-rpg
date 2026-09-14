using TowerRpg.Core;
using UnityEngine;

namespace TowerRpg.Juice
{
    /// <summary>Các tiếng động của game. Thứ tự KHÔNG được đổi — nó là chỉ mục vào mảng clips.</summary>
    public enum Sfx
    {
        Hit = 0,        // đòn thường trúng quái
        Crit = 1,       // đòn chí mạng — phải nghe KHÁC HẲN đòn thường (§5.4)
        EnemyDie = 2,   // quái chết
        Pickup = 3,     // nhặt Mảnh — chưa nối, chờ Việc 5
        FloorClear = 4, // dọn sạch một tầng
        BossDown = 5,   // hạ boss
        Upgrade = 6,    // nâng cấp trang bị thành công
        PlayerHit = 7,  // người chơi ăn đòn
        PlayerDie = 8,  // người chơi ngã xuống — M5
        Locked = 9,     // bấm vào nút đang khoá — phải nghe KHÁC hẳn một cú bấm thành công
    }

    /// <summary>
    /// Phát tiếng động bằng một dàn AudioSource quay vòng.
    ///
    /// VÌ SAO KHÔNG DÙNG AudioSource.PlayClipAtPoint: nó Instantiate một GameObject mới cho
    /// MỖI tiếng động rồi Destroy sau khi phát xong. Ở nhịp 1,2 đòn/giây suốt 40 phút chơi
    /// thì đó là hàng nghìn lần cấp phát — đúng kiểu giật GC mà DamagePopupSpawner đã phải
    /// dựng pool để tránh.
    ///
    /// Dàn quay vòng nghĩa là tiếng thứ N+8 cắt ngang tiếng thứ N. Với clip 0,24-0,47 giây
    /// và 8 nguồn thì phải phát trên 17 tiếng/giây mới cắt được nhau — không xảy ra.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public sealed class SfxPlayer : MonoBehaviour
    {
        public const int SfxCount = 10;

        [SerializeField] private AudioClip[] clips = new AudioClip[SfxCount];
        [SerializeField] private int sourceCount = 8;

        private static SfxPlayer _instance;

        private AudioSource[] _sources;
        private int _next;
        private float _volume = 1f;
        private float _playerHitCooldown = 0.15f;
        private float _lastPlayerHitAt = -99f;
        private bool _ready;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(this); return; }
            _instance = this;

            sourceCount = Mathf.Clamp(sourceCount, 1, 32);
            _sources = new AudioSource[sourceCount];
            for (int i = 0; i < sourceCount; i++)
            {
                var go = new GameObject($"Sfx{i:00}");
                go.transform.SetParent(transform, false);
                AudioSource a = go.AddComponent<AudioSource>();
                a.playOnAwake = false;
                a.spatialBlend = 0f;      // 2D: đấu trường vừa một màn hình, âm thanh nổi
                                          // theo vị trí chỉ tổ làm tiếng lệch tai vô cớ
                _sources[i] = a;
            }
        }

        // Dọn tay: static giữ tham chiếu sống qua cả lúc đổi scene nếu không gỡ.
        private void OnDestroy() { if (_instance == this) _instance = null; }

        private void Start() => BalanceConfig.TryUse(this, b =>
        {
            _volume = Mathf.Clamp01(b.Get("audio.sfxVolume"));
            _playerHitCooldown = Mathf.Max(0f, b.Get("audio.playerHitCooldown"));
            _ready = true;
        });

        /// <summary>
        /// Phát một tiếng. Gọi được từ bất cứ đâu, và KHÔNG BAO GIỜ ném lỗi: thiếu
        /// SfxPlayer trong scene, thiếu clip, hay đang tắt game đều chỉ là không kêu.
        /// Âm thanh là thứ trang trí — nó không có quyền làm hỏng một trận đấu.
        /// </summary>
        public static void Play(Sfx id, float volumeScale = 1f)
        {
            SfxPlayer p = _instance;
            if (p == null || !p._ready || p._sources == null) return;

            int i = (int)id;
            if (i < 0 || i >= p.clips.Length) return;
            AudioClip clip = p.clips[i];
            if (clip == null) return;

            // Sáu con quái đánh liên tục thì PlayerHit thành tiếng nhiễu liên tục.
            if (id == Sfx.PlayerHit)
            {
                if (Time.unscaledTime - p._lastPlayerHitAt < p._playerHitCooldown) return;
                p._lastPlayerHitAt = Time.unscaledTime;
            }

            AudioSource src = p._sources[p._next];
            p._next = (p._next + 1) % p._sources.Length;
            src.PlayOneShot(clip, Mathf.Clamp01(p._volume * volumeScale));
        }

        /// <summary>Clip nào đang thiếu — dùng cho VerifyM1Scene, không dùng lúc chạy.</summary>
        public AudioClip ClipAt(int index) =>
            clips != null && index >= 0 && index < clips.Length ? clips[index] : null;
    }
}
