using TowerRpg.Enemies;
using TowerRpg.Player;
using UnityEngine;

namespace TowerRpg.Core
{
    /// <summary>
    /// Điểm khởi động của M1. Số liệu cân bằng nạp bất đồng bộ trên Android, nên KHÔNG được
    /// sinh quái trước khi nạp xong. Cũng chịu trách nhiệm bày lại đợt quái khi người chơi chết.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private BalanceConfig balance;
        [SerializeField] private EnemySpawner spawner;
        [SerializeField] private PlayerHealth playerHealth;

        private void Start()
        {
            if (balance == null) balance = FindFirstObjectByType<BalanceConfig>();
            if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
            if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();

            if (balance == null)
            {
                Debug.LogError("[GameBootstrap] Không tìm thấy BalanceConfig trong scene.", this);
                return;
            }

            if (spawner == null)
            {
                Debug.LogError("[GameBootstrap] Không tìm thấy EnemySpawner trong scene.", this);
                return;
            }

            if (playerHealth != null) playerHealth.Died += OnPlayerDied;

            balance.WhenReady(OnBalanceReady);
        }

        private void OnDestroy()
        {
            if (playerHealth != null) playerHealth.Died -= OnPlayerDied;
        }

        private void OnBalanceReady()
        {
            if (balance.LoadFailed) return;
            spawner.SpawnWave();
        }

        /// <summary>M1: chết thì bày lại đợt quái. Không có màn hình thua, không có save.</summary>
        private void OnPlayerDied()
        {
            Debug.Log("[M1] Người chơi chết — bày lại đợt quái.");
            playerHealth.ResetHealth();
            spawner.SpawnWave();
        }
    }
}
