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

        // ── M3 ────────────────────────────────────────────────────────────────────
        public int Cores { get; private set; }
        public int BossesKilled { get; private set; }
        public int HighestCleared { get; private set; }
        public int Respecs { get; private set; }
        public int CharacterIndex { get; private set; }

        // Hũ Mảnh của tầng đang chơi dở — FloorRunner sở hữu logic, GameState chỉ chở đi lưu.
        public int PaidFloor { get; private set; } = -1;
        public float PaidShards { get; private set; }

        public int BossEvery { get; private set; } = 10;

        public event Action Changed;        // tiến trình đổi — giao diện nghe cái này
        public event Action<int> CharacterChanged;   // đổi nhân vật — chỉ hình dạng đổi

        private float _shardBase, _shardGrowth, _respecBase, _respecGrowth, _sweepTotalMult;
        private int _corePerBoss;

        /// <summary>Mảnh kiếm được bằng cách LEO (dọn tầng). Mẫu số của trần quét.</summary>
        public float ShardsClimbed { get; private set; }

        /// <summary>Mảnh kiếm được bằng QUÉT NHANH.</summary>
        public float ShardsSwept { get; private set; }

        /// <summary>
        /// Còn quét thêm được bao nhiêu Mảnh — ô 'Thông số'!B32 = 2 của can-bang.xlsx.
        /// Tổng Mảnh cả đời bị chặn ở totalMult lần Mảnh-đã-leo, nên quét là CÁCH NÉN
        /// THỜI GIAN chứ không phải nguồn Mảnh thứ hai. Leo thêm thì trần tự nới ra.
        /// </summary>
        public float SweepBudgetLeft =>
            Mathf.Max(0f, ShardsClimbed * (_sweepTotalMult - 1f) - ShardsSwept);

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
            BossEvery    = Mathf.Max(1, b.GetInt("tower.bossEvery"));
            _corePerBoss = Mathf.Max(0, b.GetInt("core.perBoss"));
            _respecBase   = b.Get("respec.costBase");
            _respecGrowth = b.Get("respec.costGrowth");
            _sweepTotalMult = Mathf.Max(1f, b.Get("sweep.totalMult"));

            CharacterRoster.Configure(b);

            // Bậc đột phá phải khôi phục TRƯỚC cấp: Restore kẹp cấp theo trần, mà trần
            // phụ thuộc bậc. Ngược thứ tự là người chơi mất sạch cấp trên 10 khi mở lại game.
            Gear.Configure(b);
            SaveData d = SaveSystem.Load();
            Gear.RestoreTiers(d.gearTiers);
            Gear.Restore(d.gearLevels);

            Floor  = Mathf.Clamp(d.floor, 1, TowerFloors);
            Shards = Mathf.Max(0f, d.shards);
            Cores  = Mathf.Max(0, d.cores);
            BossesKilled   = Mathf.Max(0, d.bossesKilled);
            HighestCleared = Mathf.Clamp(d.highestCleared, 0, TowerFloors);
            Respecs        = Mathf.Max(0, d.respecs);
            ShardsSwept    = Mathf.Max(0f, d.shardsSwept);
            PaidFloor      = d.paidFloor;
            PaidShards     = Mathf.Max(0f, d.paidShards);

            // Save v2 trở về trước không tách nguồn Mảnh; SaveSystem đánh dấu -1 để đây
            // dựng lại từ số tầng đã dọn. Làm ở đây chứ không ở SaveSystem vì cần
            // ShardReward, mà nó phụ thuộc số liệu cân bằng vừa nạp xong ở trên.
            if (d.shardsClimbed < 0f)
            {
                float rebuilt = 0f;
                for (int f = 1; f <= HighestCleared; f++) rebuilt += ShardReward(f);
                ShardsClimbed = rebuilt;
                Debug.Log($"[GameState] Dựng lại Mảnh-đã-leo cho save cũ: {rebuilt:N0} " +
                          $"từ {HighestCleared} tầng -> trần quét {SweepBudgetLeft:N0}.");
            }
            else ShardsClimbed = d.shardsClimbed;
            CharacterIndex = CharacterRoster.IsUnlocked(d.characterIndex, BossesKilled)
                           ? d.characterIndex : 0;

            Ready = true;
            Changed?.Invoke();
            CharacterChanged?.Invoke(CharacterIndex);
            Debug.Log($"[GameState] Nạp tiến trình: tầng {Floor}/{TowerFloors}, " +
                      $"{Shards:0} Mảnh, cấp {string.Join("/", Gear.Snapshot())}, " +
                      $"bậc {string.Join("/", Gear.TierSnapshot())}, {Cores} Lõi, " +
                      $"{BossesKilled} boss, nhân vật {CharacterRoster.Name(CharacterIndex)}");
        }

        /// <summary>Mảnh nhận được khi dọn sạch một tầng — §5.6, trục thời gian.</summary>
        public float ShardReward(int floor) =>
            _shardBase * Mathf.Pow(1f + _shardGrowth, Mathf.Max(0, floor - 1));

        /// <param name="fromSweep">
        /// Quét nhanh phải khai báo, vì nó tiêu vào TRẦN chứ không nới trần. Quên cờ này
        /// là quét tự cấp ngân sách cho chính nó và trần mất tác dụng hoàn toàn.
        /// </param>
        public void AddShards(float amount, bool fromSweep = false)
        {
            if (amount <= 0f) return;
            Shards += amount;
            if (fromSweep) ShardsSwept += amount;
            else           ShardsClimbed += amount;
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

        /// <summary>Tầng này có boss không — §5.10. Cứ BossEvery tầng một con.</summary>
        public bool IsBossFloor(int floor) => floor > 0 && floor % BossEvery == 0;

        /// <summary>Thứ tự boss của một tầng, bắt đầu từ 1. Tầng thường trả về 0.</summary>
        public int BossIndex(int floor) => IsBossFloor(floor) ? floor / BossEvery : 0;

        /// <summary>
        /// Hạ boss xong. Lõi CHỈ rơi ở lần giết đầu tiên — quét lại không cho thêm.
        /// Đó là điều làm Lõi hữu hạn tuyệt đối, nền móng của toàn bộ §5.6.
        /// </summary>
        public bool AwardBoss(int floor)
        {
            int index = BossIndex(floor);
            if (index <= 0 || index <= BossesKilled) return false;   // đã lấy rồi

            BossesKilled = index;
            Cores += _corePerBoss;
            Changed?.Invoke();
            Save();
            Debug.Log($"[GameState] Hạ boss {index} lần đầu: +{_corePerBoss} Lõi (còn {Cores}), " +
                      $"mở nhân vật {CharacterRoster.Name(index)}.");
            return true;
        }

        /// <summary>Đột phá một ô: tiêu Lõi để nâng trần cấp 10 bậc.</summary>
        public bool TryBreakthrough(Slot s)
        {
            if (!Ready || Gear.AtMaxTier(s)) return false;

            int cost = Gear.NextTierCost(s);
            if (cost < 0 || Cores < cost) return false;

            Cores -= cost;
            Gear.Breakthrough(s);
            Changed?.Invoke();
            Save();
            return true;
        }

        /// <summary>Mảnh cho lần tẩy điểm kế tiếp — đắt gấp đôi sau mỗi lần (§5.8 van 2).</summary>
        public float RespecCost() => _respecBase * Mathf.Pow(1f + _respecGrowth, Respecs);

        /// <summary>
        /// Tẩy điểm: hoàn 100% Lõi, tốn RẤT NHIỀU Mảnh. Cấp vượt trần nền bị cắt về 10 và
        /// KHÔNG hoàn Mảnh — nếu hoàn thì tẩy điểm thành nút "chơi lại miễn phí" và quyết
        /// định phân bổ mất sạch sức nặng mà §5.6 dựa vào.
        /// </summary>
        public bool TryRespec()
        {
            if (!Ready) return false;

            int refund = Gear.CoresSpent();
            if (refund <= 0) return false;              // chưa tiêu Lõi thì không có gì để tẩy

            float cost = RespecCost();
            if (Shards < cost) return false;

            Shards -= cost;
            Cores  += refund;
            Gear.ResetTiers();
            Respecs++;

            Changed?.Invoke();
            Save();
            Debug.Log($"[GameState] Tẩy điểm lần {Respecs}: hoàn {refund} Lõi, tốn {cost:N0} Mảnh.");
            return true;
        }

        /// <summary>Quét nhanh mở cho tầng đã dọn sạch ít nhất một lần (§5.2), và còn ngân sách.</summary>
        public bool CanSweep(int floor) =>
            floor >= 1 && floor <= HighestCleared && SweepBudgetLeft > 0f;

        /// <summary>Tự động chiến đấu mở khi đã dọn tới tầng mốc.</summary>
        public bool AutoUnlocked(int atFloor) => HighestCleared >= atFloor;

        /// <summary>Đổi nhân vật — §5.5b. Miễn phí, không hồi chiêu, không mất gì.</summary>
        public bool TrySetCharacter(int index)
        {
            if (!Ready || index == CharacterIndex) return false;
            if (!CharacterRoster.IsUnlocked(index, BossesKilled)) return false;

            CharacterIndex = index;
            CharacterChanged?.Invoke(index);
            Changed?.Invoke();
            Save();
            return true;
        }

        /// <summary>FloorRunner báo lại hũ Mảnh để lần Save tới ghi đúng.</summary>
        public void SetFloorPot(int floor, float paid)
        {
            PaidFloor = floor;
            PaidShards = Mathf.Max(0f, paid);
        }

        public void MarkCleared(int floor)
        {
            if (floor <= HighestCleared) return;
            HighestCleared = floor;
            Changed?.Invoke();
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
            version = SaveData.CurrentVersion,
            floor = Floor,
            shards = Shards,
            gearLevels = Gear.Snapshot(),
            gearTiers = Gear.TierSnapshot(),
            cores = Cores,
            bossesKilled = BossesKilled,
            highestCleared = HighestCleared,
            respecs = Respecs,
            characterIndex = CharacterIndex,
            shardsClimbed = ShardsClimbed,
            shardsSwept = ShardsSwept,
            paidFloor = PaidFloor,
            paidShards = PaidShards,
        });

        // iOS giết app trong nền mà không báo — đây là chỗ DUY NHẤT chắc chắn còn chạy.
        private void OnApplicationPause(bool paused) { if (paused && Ready) Save(); }
        private void OnApplicationQuit() { if (Ready) Save(); }
    }
}
