using TowerRpg.Combat;
using TowerRpg.Core;
using UnityEngine;

namespace TowerRpg.Enemies
{
    /// <summary>
    /// M1: đặt quái thành vòng tròn quanh tâm đấu trường. Hoàn toàn xác định — không Random,
    /// đúng nguyên tắc nền của thiết kế.
    /// </summary>
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private Enemy enemyPrefab;
        [SerializeField] private Transform arenaCentre;

        public void SpawnWave()
        {
            if (enemyPrefab == null)
            {
                Debug.LogError("[EnemySpawner] Chưa gán enemyPrefab.", this);
                return;
            }

            if (BalanceConfig.Instance == null || BalanceConfig.Instance.LoadFailed) return;

            EnemyRegistry.ClearAll();

            BalanceConfig balance = BalanceConfig.Instance;
            int count = balance.GetInt("enemy.count");
            float radius = balance.Get("enemy.spawnRadius");
            float hp = balance.Get("enemy.hp");
            float damage = balance.Get("enemy.damage");
            float rate = balance.Get("enemy.attacksPerSecond");
            float range = balance.Get("enemy.attackRange");

            Vector3 centre = arenaCentre != null ? arenaCentre.position : Vector3.zero;

            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / Mathf.Max(1, count);
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;

                Enemy enemy = Instantiate(enemyPrefab, centre + offset, Quaternion.identity, transform);
                enemy.name = $"Enemy_{i:00}";
                enemy.Initialise(hp, damage, rate, range);
            }
        }
    }
}
