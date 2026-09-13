using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace TowerRpg.Core
{
    /// <summary>
    /// Đọc số liệu cân bằng từ StreamingAssets/m1-balance.csv.
    /// QUY TẮC CỨNG §9.2: không một con số cân bằng nào được viết trong .cs.
    ///
    /// CHÚ Ý VỀ THỜI ĐIỂM: trên Android, StreamingAssets nằm trong APK nên phải đọc qua
    /// UnityWebRequest — BẤT ĐỒNG BỘ. Mọi script đọc số PHẢI đi qua WhenReady(), không được
    /// gọi thẳng Get() trong Start(). Trong Editor lỗi này vô hình; trên Android nó giết cả game.
    /// </summary>
    public sealed class BalanceConfig : MonoBehaviour
    {
        public static BalanceConfig Instance { get; private set; }

        [SerializeField] private string fileName = "m1-balance.csv";

        private readonly Dictionary<string, float> _values = new Dictionary<string, float>();
        private Action _readyCallbacks;

        public bool IsLoaded { get; private set; }
        public bool LoadFailed { get; private set; }

        private static readonly string[] RequiredKeys =
        {
            "player.maxHp", "player.moveSpeed", "player.moveDeadzone",
            "player.attackDamage", "player.attacksPerSecond", "player.attackRange",
            "player.hpPerFloor", "crit.meterSize", "crit.multiplier",
            "gear.maxLevel", "gear.weapon.perLevel", "gear.armor.perLevel",
            "gear.glove.perLevel", "gear.ring.perLevel", "gear.costBase",
            "gear.costGrowth", "enemy.hpFloor1", "enemy.hpGrowth",
            "enemy.dpsFloor1", "enemy.dpsGrowth", "enemy.count",
            "enemy.spawnRadius", "enemy.attacksPerSecond", "enemy.attackRange",
            "shard.perFloor1", "shard.growth", "tower.floors",
            "tower.bossEvery", "boss.hpMult1", "boss.hpMult2",
            "boss.hpMult3", "boss.hpMult4", "boss.count",
            "boss.attackRangeMult", "character.k0", "character.k1",
            "character.k2", "character.k3", "character.k4",
            "core.perBoss", "core.gate1", "core.gate2",
            "core.gate3", "core.gate4", "core.gate5",
            "core.capStep", "respec.costBase", "respec.costGrowth",
            "sweep.unlockOnClear", "sweep.seconds", "sweep.totalMult",
            "auto.unlockFloor", "juice.shakeDuration", "juice.shakeMagnitude",
            "juice.shakeYRatio", "juice.popupRiseSpeed", "juice.popupLifetime",
            "juice.enemyDeathSeconds", "juice.healthBarHideAbove", "juice.popupDecimalBelow",
            "audio.sfxVolume", "audio.playerHitCooldown", "loot.popDistance",
            "loot.popSeconds", "loot.magnetRadius", "loot.pickupRadius",
            "loot.flySpeed", "loot.maxLifetime", "hud.countSeconds",
            "hud.flashSeconds", "hud.floorBannerSeconds", "hud.bossBannerSeconds",
            "boss.healOnEnter", "enemy.telegraphSeconds", "enemy.telegraphNudge",
            "juice.slashSeconds", "audio.musicVolume",
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // Huỷ ĐÚNG component này, không huỷ cả GameObject — Bootstrap còn mang
                // GameBootstrap, EnemySpawner, DamagePopupSpawner.
                Debug.LogWarning("[BalanceConfig] Đã có một BalanceConfig khác. Huỷ bản trùng.");
                Destroy(this);
                return;
            }

            Instance = this;
            StartCoroutine(LoadRoutine());
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            _readyCallbacks = null;
        }

        /// <summary>
        /// Gọi cb khi số liệu đã sẵn sàng — ngay lập tức nếu đã nạp xong.
        /// GỌI TỪ Start(), KHÔNG gọi từ Awake(): Instance chỉ được gán trong Awake của
        /// BalanceConfig, mà thứ tự Awake giữa các component là không xác định.
        /// </summary>
        public void WhenReady(Action cb)
        {
            if (cb == null) return;

            if (IsLoaded) cb();
            else _readyCallbacks += cb;
        }

        private IEnumerator LoadRoutine()
        {
            string path = Path.Combine(Application.streamingAssetsPath, fileName);
            string text = null;

            if (path.Contains("://"))
            {
                using (UnityWebRequest request = UnityWebRequest.Get(path))
                {
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                        text = request.downloadHandler.text;
                    else
                        Debug.LogError($"[BalanceConfig] Không đọc được '{path}': {request.error}");
                }
            }
            else if (File.Exists(path))
            {
                text = File.ReadAllText(path);
            }
            else
            {
                Debug.LogError($"[BalanceConfig] Không tìm thấy '{path}'.");
            }

            if (text != null) Parse(text);

            LoadFailed = text == null || !ValidateRequiredKeys();
            IsLoaded = true;

            if (LoadFailed)
            {
                Debug.LogError("[BalanceConfig] NẠP SỐ LIỆU THẤT BẠI. Không khởi động trận đấu — " +
                               "sửa m1-balance.csv rồi chạy lại.");
            }

            Action callbacks = _readyCallbacks;
            _readyCallbacks = null;   // sự kiện chỉ bắn một lần trong đời
            callbacks?.Invoke();
        }

        private void Parse(string text)
        {
            _values.Clear();

            // Tách cả \n lẫn \r\n — file soạn trên Windows vẫn đọc được.
            string[] lines = text.Split('\n');

            var known = new HashSet<string>(RequiredKeys);

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line[0] == '#') continue;

                string[] parts = line.Split(',');
                if (parts.Length < 2) continue;

                string key = parts[0].Trim();
                if (key.Length == 0) continue;

                if (!float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                {
                    Debug.LogError($"[BalanceConfig] Giá trị không đọc được ở khoá '{key}': '{parts[1]}'");
                    continue;
                }

                if (!known.Contains(key))
                    Debug.LogWarning($"[BalanceConfig] Khoá lạ '{key}' — gõ sai tên? Không ai đọc nó cả.");

                _values[key] = value;
            }
        }

        private bool ValidateRequiredKeys()
        {
            bool ok = true;

            foreach (string key in RequiredKeys)
            {
                if (_values.ContainsKey(key)) continue;

                Debug.LogError($"[BalanceConfig] THIẾU KHOÁ BẮT BUỘC: '{key}'. Bổ sung vào {fileName}.");
                ok = false;
            }

            return ok;
        }

        /// <summary>Lấy một số cân bằng. Thiếu khoá thì báo lỗi to và trả 0 — sai sẽ lộ ngay.</summary>
        public float Get(string key)
        {
            if (_values.TryGetValue(key, out float value)) return value;

            Debug.LogError($"[BalanceConfig] Không có khoá '{key}'. Trả về 0.");
            return 0f;
        }

        public int GetInt(string key) => Mathf.RoundToInt(Get(key));

        /// <summary>
        /// Khoá có tồn tại không — KHÔNG kêu ca nếu thiếu.
        /// Dùng cho nhóm khoá đánh số mà số lượng thay đổi theo nội dung (boss.hpMult1..N):
        /// đọc tới khi hết thay vì viết cứng số boss ở hai nơi rồi để chúng lệch nhau.
        /// Mọi trường hợp khác dùng Get — thiếu khoá PHẢI kêu to.
        /// </summary>
        public bool Has(string key) => _values.ContainsKey(key);

        /// <summary>
        /// Lối vào chung cho mọi script cần số liệu. Báo lỗi RÕ RÀNG nếu thiếu component
        /// BalanceConfig, thay vì để năm chỗ khác nhau ném NullReferenceException.
        /// </summary>
        public static bool TryUse(MonoBehaviour caller, Action<BalanceConfig> onReady)
        {
            if (Instance == null)
            {
                Debug.LogError($"[{caller.GetType().Name}] Không tìm thấy BalanceConfig trong scene. " +
                               "Thêm component BalanceConfig vào object Bootstrap.", caller);
                caller.enabled = false;
                return false;
            }

            BalanceConfig config = Instance;
            config.WhenReady(() =>
            {
                if (caller == null) return;             // đối tượng đã bị huỷ trong lúc chờ
                if (config.LoadFailed) { caller.enabled = false; return; }
                onReady(config);
            });

            return true;
        }
    }
}
