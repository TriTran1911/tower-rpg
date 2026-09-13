using System;
using System.Collections;
using TowerRpg.Combat;
using TowerRpg.Core;
using TowerRpg.Enemies;
using TowerRpg.Player;
using UnityEngine;

namespace TowerRpg.Progression
{
    /// <summary>
    /// Vòng lặp một tầng: bày quái theo đúng chỉ số của tầng hiện tại → người chơi dọn sạch
    /// → nhận Mảnh → sang tầng kế.
    ///
    /// Thay cho GameBootstrap + EnemySpawner của M1, vốn chỉ bày đúng một đợt cố định.
    /// </summary>
    public sealed class FloorRunner : MonoBehaviour
    {
        [SerializeField] private Enemy enemyPrefab;
        [SerializeField] private Transform arenaCentre;
        [SerializeField] private PlayerHealth playerHealth;

        [Header("Boss — §5.10")]
        [SerializeField] private Sprite bossSprite;
        [SerializeField] private float bossScale = 2f;

        [Header("Nhịp")]
        [SerializeField] private float clearDelay = 1.2f;   // khoảng nghỉ sau khi dọn sạch
        [SerializeField] private float deathDelay = 1.5f;

        public event Action<int> FloorStarted;     // tầng vừa bày xong
        public event Action<int, float> FloorCleared;  // tầng, Mảnh nhận được
        public event Action<int, int> BossDefeated;    // tầng, số Lõi vừa nhận

        private float _hp1, _hpG, _dps1, _dpsG, _radius, _rate, _range, _bossRangeMult;
        private int _count, _bossCount;
        private readonly System.Collections.Generic.List<float> _bossMult =
            new System.Collections.Generic.List<float>();
        private bool _running;

        /// <summary>Đang ở tầng boss — giao diện dùng để đổi nhạc/khung.</summary>
        public bool InBossFight { get; private set; }

        private void Start()
        {
            if (enemyPrefab == null) { Debug.LogError("[FloorRunner] Chưa gán enemyPrefab.", this); enabled = false; return; }
            BalanceConfig.TryUse(this, Configure);
        }

        private void OnDestroy()
        {
            if (playerHealth != null) playerHealth.Died -= OnPlayerDied;
        }

        private void Configure(BalanceConfig b)
        {
            _hp1    = b.Get("enemy.hpFloor1");
            _hpG    = b.Get("enemy.hpGrowth");
            _dps1   = b.Get("enemy.dpsFloor1");
            _dpsG   = b.Get("enemy.dpsGrowth");
            _count  = Mathf.Max(1, b.GetInt("enemy.count"));
            _radius = b.Get("enemy.spawnRadius");
            _rate   = b.Get("enemy.attacksPerSecond");
            _range  = b.Get("enemy.attackRange");

            _bossCount     = Mathf.Max(1, b.GetInt("boss.count"));
            _bossRangeMult = b.Get("boss.attackRangeMult");
            _bossMult.Clear();
            // Đọc tới khi hết khoá — số boss bám theo số tầng, không viết cứng.
            for (int i = 1; b.Has($"boss.hpMult{i}"); i++) _bossMult.Add(b.Get($"boss.hpMult{i}"));

            if (playerHealth == null) playerHealth = PlayerHealth.Current;
            if (playerHealth != null) playerHealth.Died += OnPlayerDied;

            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            // GameState nạp save bất đồng bộ như BalanceConfig — phải chờ.
            while (GameState.Instance == null || !GameState.Instance.Ready) yield return null;

            _running = true;
            while (_running)
            {
                int floor = GameState.Instance.Floor;
                SpawnFloor(floor);
                FloorStarted?.Invoke(floor);

                while (EnemyRegistry.Count > 0 && _running) yield return null;
                if (!_running) yield break;

                InBossFight = false;

                // Lõi TRƯỚC Mảnh: hạ boss lần đầu là sự kiện lớn hơn, và AwardBoss tự lưu.
                if (GameState.Instance.IsBossFloor(floor))
                {
                    int before = GameState.Instance.Cores;
                    if (GameState.Instance.AwardBoss(floor))
                        BossDefeated?.Invoke(floor, GameState.Instance.Cores - before);
                }

                float reward = GameState.Instance.ShardReward(floor);
                GameState.Instance.AddShards(reward);
                GameState.Instance.MarkCleared(floor);
                GameState.Instance.Save();
                FloorCleared?.Invoke(floor, reward);

                yield return new WaitForSeconds(clearDelay);

                if (floor >= GameState.Instance.TowerFloors)
                {
                    Debug.Log($"[FloorRunner] Đã lên tới đỉnh tháp M3 (tầng {floor}). Bày lại tầng này.");
                    continue;                    // M3 dừng ở đây; M4 mở tiếp 100 tầng
                }

                GameState.Instance.AdvanceFloor();
            }
        }

        /// <summary>Bày quái với chỉ số của đúng tầng đó — §5.7. Tầng boss thì bày boss.</summary>
        private void SpawnFloor(int floor)
        {
            EnemyRegistry.ClearAll();

            bool boss = GameState.Instance != null && GameState.Instance.IsBossFloor(floor);
            InBossFight = boss;

            float totalHp  = _hp1  * Mathf.Pow(1f + _hpG,  floor - 1);
            float totalDps = _dps1 * Mathf.Pow(1f + _dpsG, floor - 1);

            int count = boss ? _bossCount : _count;
            float range = boss ? _range * _bossRangeMult : _range;

            // Hệ số máu boss của §5.10. Thiếu khoá cho boss thứ n thì dùng 1,0 và kêu to —
            // im lặng rơi về quái thường là kiểu hỏng không ai phát hiện ra.
            if (boss)
            {
                int bi = GameState.Instance.BossIndex(floor) - 1;
                if (bi >= 0 && bi < _bossMult.Count) totalHp *= _bossMult[bi];
                else Debug.LogWarning($"[FloorRunner] Thiếu boss.hpMult{bi + 1} cho tầng {floor} " +
                                      "— dùng hệ số 1,0. Bổ sung vào m1-balance.csv.");
            }

            float hpEach  = totalHp / count;
            float dmgEach = _rate > 0f ? totalDps / count / _rate : 0f;

            Vector3 centre = arenaCentre != null ? arenaCentre.position : Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                Vector3 off = Vector3.zero;
                if (count > 1)
                {
                    float a = i * Mathf.PI * 2f / count;
                    off = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * _radius;
                }
                else off = new Vector3(0f, _radius * 0.6f, 0f);   // boss đứng giữa, hơi lùi

                Enemy e = Instantiate(enemyPrefab, centre + off, Quaternion.identity, transform);
                e.name = boss ? $"F{floor}_BOSS" : $"F{floor}_Enemy{i:00}";

                if (boss)
                {
                    var sr = e.GetComponent<SpriteRenderer>();
                    if (sr != null && bossSprite != null) sr.sprite = bossSprite;
                    e.transform.localScale *= bossScale;
                }

                e.Initialise(hpEach, dmgEach, _rate, range, boss);
            }
        }

        private void OnPlayerDied()
        {
            if (!_running) return;
            StartCoroutine(Retry());
        }

        /// <summary>Chết thì bày lại ĐÚNG tầng đó. Không mất Mảnh, không tụt tầng (§5.8 van 3).</summary>
        private IEnumerator Retry()
        {
            Debug.Log("[FloorRunner] Người chơi chết — bày lại tầng, không mất gì.");
            yield return new WaitForSeconds(deathDelay);
            playerHealth.ResetHealth();
            SpawnFloor(GameState.Instance.Floor);
        }
    }
}
