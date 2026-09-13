using System.Collections;
using NUnit.Framework;
using TowerRpg.Combat;
using TowerRpg.Core;
using TowerRpg.Player;
using TowerRpg.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TowerRpg.Tests
{
    /// <summary>
    /// M2: tiến trình — rơi Mảnh, nâng cấp trang bị, lên tầng, save/load.
    /// Xoá save trước mỗi test để luôn bắt đầu từ người chơi mới.
    /// </summary>
    public class M2ProgressionTests
    {
        private GameState _gs;
        private PlayerStats _st;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            SceneManager.LoadScene("M1", LoadSceneMode.Single);
            yield return null; yield return null;

            float t = 0f;
            while ((GameState.Instance == null || !GameState.Instance.Ready) && t < 10f)
            { t += Time.deltaTime; yield return null; }
            yield return null;

            _gs = GameState.Instance;
            _st = PlayerStats.Instance;
        }

        [TearDown]
        public void TearDown() => SaveSystem.Delete();

        [UnityTest]
        public IEnumerator Nguoi_choi_moi_bat_dau_o_tang_1_cap_1()
        {
            Assert.IsNotNull(_gs, "không có GameState");
            Assert.AreEqual(1, _gs.Floor, "người chơi mới phải ở tầng 1");
            Assert.AreEqual(0f, _gs.Shards, 0.01f, "người chơi mới phải có 0 Mảnh");
            for (int i = 0; i < Equipment.SlotCount; i++)
                Assert.AreEqual(1, _gs.Gear.Level((Slot)i), $"ô {(Slot)i} phải ở cấp 1");
            yield break;
        }

        [UnityTest]
        public IEnumerator Don_sach_tang_thi_nhan_Manh_va_len_tang()
        {
            int floor0 = _gs.Floor;
            float want = _gs.ShardReward(floor0);

            EnemyRegistry.ClearAll();                 // giả lập dọn sạch
            yield return new WaitForSeconds(2.5f);    // qua clearDelay của FloorRunner

            Assert.GreaterOrEqual(_gs.Shards, want * 0.99f,
                                  $"dọn sạch tầng {floor0} phải nhận ~{want:0} Mảnh");
            Assert.AreEqual(floor0 + 1, _gs.Floor, "phải lên tầng kế");
        }

        [UnityTest]
        public IEnumerator Nang_cap_tru_dung_so_Manh_va_tang_chi_so()
        {
            _gs.AddShards(100000f);
            yield return null;

            float before = _gs.Shards;
            float cost = _gs.Gear.NextCost(Slot.Weapon);
            float dmg0 = _st.Damage;

            Assert.IsTrue(_gs.TryUpgrade(Slot.Weapon), "phải nâng cấp được khi đủ Mảnh");

            Assert.AreEqual(before - cost, _gs.Shards, 0.01f, "phải trừ đúng số Mảnh");
            Assert.AreEqual(2, _gs.Gear.Level(Slot.Weapon), "cấp phải tăng đúng 1");
            Assert.Greater(_st.Damage, dmg0, "sát thương phải tăng theo trang bị");
        }

        [UnityTest]
        public IEnumerator Khong_du_Manh_thi_khong_nang_duoc()
        {
            Assert.AreEqual(0f, _gs.Shards, 0.01f);
            Assert.IsFalse(_gs.TryUpgrade(Slot.Weapon), "0 Mảnh mà vẫn nâng được");
            Assert.AreEqual(1, _gs.Gear.Level(Slot.Weapon));
            yield break;
        }

        [UnityTest]
        public IEnumerator Cham_tran_cap_thi_dung_lai_du_con_Manh()
        {
            _gs.AddShards(10_000_000f);
            yield return null;

            int max = _gs.Gear.MaxLevel;
            for (int i = 0; i < max + 5; i++) _gs.TryUpgrade(Slot.Weapon);

            Assert.AreEqual(max, _gs.Gear.Level(Slot.Weapon), $"phải dừng đúng ở trần {max}");
            Assert.IsTrue(_gs.Gear.AtCap(Slot.Weapon));
            Assert.AreEqual(-1f, _gs.Gear.NextCost(Slot.Weapon), "chạm trần thì không còn giá");
            Assert.Greater(_gs.Shards, 0f,
                           "ĐÂY LÀ BỨC TƯỜNG CỦA M2: còn Mảnh mà không nâng được nữa — cần Lõi (M3)");
        }

        [UnityTest]
        public IEnumerator Giap_tang_cap_thi_mau_toi_da_tang()
        {
            _gs.AddShards(100000f);
            yield return null;

            float hp0 = _st.MaxHp;
            _gs.TryUpgrade(Slot.Armor);
            Assert.Greater(_st.MaxHp, hp0, "nâng Giáp mà máu tối đa không tăng");
        }

        [UnityTest]
        public IEnumerator Save_va_nap_lai_giu_nguyen_tien_trinh()
        {
            _gs.AddShards(50000f);
            _gs.TryUpgrade(Slot.Weapon);
            _gs.TryUpgrade(Slot.Weapon);
            _gs.TryUpgrade(Slot.Ring);
            _gs.AdvanceFloor();
            _gs.Save();

            int floor = _gs.Floor;
            float shards = _gs.Shards;
            int[] gear = _gs.Gear.Snapshot();

            SceneManager.LoadScene("M1", LoadSceneMode.Single);
            yield return null; yield return null;
            float t = 0f;
            while ((GameState.Instance == null || !GameState.Instance.Ready) && t < 10f)
            { t += Time.deltaTime; yield return null; }

            GameState after = GameState.Instance;
            Assert.AreEqual(floor, after.Floor, "tầng không được đổi sau khi nạp lại");
            Assert.AreEqual(shards, after.Shards, 0.01f, "Mảnh không được đổi");
            CollectionAssert.AreEqual(gear, after.Gear.Snapshot(), "cấp trang bị không được đổi");
        }

        [UnityTest]
        public IEnumerator Quai_manh_len_theo_tang()
        {
            // Chỉ số quái §5.7 phải tăng theo tầng, nếu không thì leo tháp vô nghĩa.
            var e1 = Object.FindFirstObjectByType<Enemies.Enemy>();
            Assert.IsNotNull(e1, "tầng 1 phải có quái");
            float hp1 = e1.HealthFraction;   // luôn 1.0 lúc mới sinh — ta so máu TUYỆT ĐỐI qua reward

            float r1 = _gs.ShardReward(1);
            float r5 = _gs.ShardReward(5);
            Assert.Greater(r5, r1, "Mảnh thưởng phải tăng theo tầng");
            Assert.AreEqual(1f, hp1, 0.001f);
            yield break;
        }
    }
}
