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

        [Header("Nhịp")]
        [SerializeField] private float clearDelay = 1.2f;   // khoảng nghỉ sau khi dọn sạch
        [SerializeField] private float deathDelay = 1.5f;

        public event Action<int> FloorStarted;     // tầng vừa bày xong
        public event Action<int, float> FloorCleared;  // tầng, Mảnh nhận được

        private float _hp1, _hpG, _dps1, _dpsG, _radius, _rate, _range;
        private int _count;
        private bool _running;

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

                float reward = GameState.Instance.ShardReward(floor);
                GameState.Instance.AddShards(reward);
                GameState.Instance.Save();
                FloorCleared?.Invoke(floor, reward);

                yield return new WaitForSeconds(clearDelay);

                if (floor >= GameState.Instance.TowerFloors)
                {
                    Debug.Log($"[FloorRunner] Đã lên tới đỉnh tháp M2 (tầng {floor}). Bày lại tầng này.");
                    continue;                    // M2 dừng ở đây; M4 mở tiếp 100 tầng
                }

                GameState.Instance.AdvanceFloor();
            }
        }

        /// <summary>Bày quái với chỉ số của đúng tầng đó — §5.7.</summary>
        private void SpawnFloor(int floor)
        {
            EnemyRegistry.ClearAll();

            float totalHp = _hp1  * Mathf.Pow(1f + _hpG,  floor - 1);
            float totalDps = _dps1 * Mathf.Pow(1f + _dpsG, floor - 1);
            float hpEach = totalHp / _count;
            float dmgEach = _rate > 0f ? totalDps / _count / _rate : 0f;

            Vector3 centre = arenaCentre != null ? arenaCentre.position : Vector3.zero;
            for (int i = 0; i < _count; i++)
            {
                float a = i * Mathf.PI * 2f / _count;
                Vector3 off = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * _radius;
                Enemy e = Instantiate(enemyPrefab, centre + off, Quaternion.identity, transform);
                e.name = $"F{floor}_Enemy{i:00}";
                e.Initialise(hpEach, dmgEach, _rate, _range);
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
