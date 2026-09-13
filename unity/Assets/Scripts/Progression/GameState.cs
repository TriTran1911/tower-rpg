using System;
using TowerRpg.Core;
using UnityEngine;

namespace TowerRpg.Progression
{
    /// <summary>
    /// Toàn bộ trạng thái tiến trình: tầng, Mảnh, trang bị. Một chỗ duy nhất, để save
    /// chỉ phải chụp một đối tượng.
    ///
    /// Lưu khi: qua tầng · nâng cấp · OnApplicationPause(true).
    /// Cái cuối BẮT BUỘC có — iOS giết app trong nền mà không báo (§9.4).
    /// </summary>
    [DefaultExecutionOrder(-50)]   // phải cấu hình TRƯỚC mọi thứ đọc nó
    public sealed class GameState : MonoBehaviour
    {
        public static GameState Instance { get; private set; }

        public Equipment Gear { get; } = new Equipment();

        public float Shards { get; private set; }
        public int Floor { get; private set; } = 1;
        public int TowerFloors { get; private set; } = 1;
        public bool Ready { get; private set; }

        public event Action Changed;        // tiến trình đổi — giao diện nghe cái này

        private float _shardBase, _shardGrowth;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Changed = null;
        }

        private void Start() => BalanceConfig.TryUse(this, Configure);

        private void Configure(BalanceConfig b)
        {
            _shardBase   = b.Get("shard.perFloor1");
            _shardGrowth = b.Get("shard.growth");
            TowerFloors  = Mathf.Max(1, b.GetInt("tower.floors"));

            Gear.Configure(b);

            SaveData d = SaveSystem.Load();
            Floor  = Mathf.Clamp(d.floor, 1, TowerFloors);
            Shards = Mathf.Max(0f, d.shards);
            Gear.Restore(d.gearLevels);

            Ready = true;
            Changed?.Invoke();
            Debug.Log($"[GameState] Nạp tiến trình: tầng {Floor}/{TowerFloors}, " +
                      $"{Shards:0} Mảnh, cấp {string.Join("/", Gear.Snapshot())}");
        }

        /// <summary>Mảnh nhận được khi dọn sạch một tầng — §5.6, trục thời gian.</summary>
        public float ShardReward(int floor) =>
            _shardBase * Mathf.Pow(1f + _shardGrowth, Mathf.Max(0, floor - 1));

        public void AddShards(float amount)
        {
            if (amount <= 0f) return;
            Shards += amount;
            Changed?.Invoke();
        }

        public bool TryUpgrade(Slot s)
        {
            if (!Ready || Gear.AtCap(s)) return false;

            float cost = Gear.NextCost(s);
            if (cost < 0f || Shards < cost) return false;

            Shards -= cost;
            Gear.Upgrade(s);
            Changed?.Invoke();
            Save();
            return true;
        }

        public void AdvanceFloor()
        {
            if (Floor >= TowerFloors) return;
            Floor++;
            Changed?.Invoke();
            Save();
        }

        public void Save() => SaveSystem.Save(new SaveData
        {
            version = 1,
            floor = Floor,
            shards = Shards,
            gearLevels = Gear.Snapshot(),
        });

        // iOS giết app trong nền mà không báo — đây là chỗ DUY NHẤT chắc chắn còn chạy.
        private void OnApplicationPause(bool paused) { if (paused && Ready) Save(); }
        private void OnApplicationQuit() { if (Ready) Save(); }
    }
}
