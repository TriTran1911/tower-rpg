using System.Collections;
using NUnit.Framework;
using TowerRpg.Combat;
using TowerRpg.Core;
using TowerRpg.Enemies;
using TowerRpg.Player;
using TowerRpg.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TowerRpg.Tests
{
    /// <summary>
    /// M3: xương sống — boss, Lõi, đột phá, tẩy điểm, quét nhanh, tự đánh, đổi nhân vật.
    ///
    /// Mấy test ở đây khoá đúng những chỗ mà một lần "dọn dẹp code" vô tình sẽ phá:
    /// Lõi chỉ rơi lần đầu, tích k×(1/k) không đổi, và trần cấp phải khôi phục đúng thứ tự.
    /// </summary>
    public class M3BackboneTests
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

        // ── Boss và Lõi ───────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Tang_boss_dung_moi_10_tang()
        {
            Assert.IsFalse(_gs.IsBossFloor(1),  "tầng 1 không phải tầng boss");
            Assert.IsFalse(_gs.IsBossFloor(9),  "tầng 9 không phải tầng boss");
            Assert.IsTrue (_gs.IsBossFloor(10), "tầng 10 PHẢI là tầng boss");
            Assert.IsTrue (_gs.IsBossFloor(20), "tầng 20 PHẢI là tầng boss");
            Assert.AreEqual(1, _gs.BossIndex(10), "tầng 10 là boss số 1");
            Assert.AreEqual(4, _gs.BossIndex(40), "tầng 40 là boss số 4");
            yield break;
        }

        [UnityTest]
        public IEnumerator Boss_chi_rot_Loi_LAN_DAU_tien()
        {
            Assert.AreEqual(0, _gs.Cores, "người chơi mới phải có 0 Lõi");

            Assert.IsTrue(_gs.AwardBoss(10), "hạ boss 1 lần đầu phải được nhận");
            int first = _gs.Cores;
            Assert.Greater(first, 0, "hạ boss lần đầu phải rơi Lõi");

            // ĐÂY LÀ TRỤ CỦA §5.6: quét lại boss cũ KHÔNG được cho thêm Lõi, nếu không
            // Lõi hết hữu hạn và toàn bộ trò chơi phân bổ mất nghĩa.
            Assert.IsFalse(_gs.AwardBoss(10), "hạ lại boss 1 KHÔNG được nhận nữa");
            Assert.AreEqual(first, _gs.Cores, "quét lại boss cũ không được thêm Lõi");

            Assert.IsTrue(_gs.AwardBoss(20), "boss 2 là con mới, phải được nhận");
            Assert.AreEqual(first * 2, _gs.Cores, "hai boss phải cho gấp đôi Lõi");
            yield break;
        }

        [UnityTest]
        public IEnumerator Tang_boss_bay_DUNG_MOT_con_va_no_la_boss()
        {
            yield return null;
            int normalCount = EnemyRegistry.Count;
            Assert.Greater(normalCount, 1, "tầng thường phải có nhiều hơn một con");

            // Đi tới sát tầng boss rồi dọn sạch — FloorRunner sẽ bày tầng 10.
            while (_gs.Floor < 9) _gs.AdvanceFloor();
            yield return null;

            EnemyRegistry.ClearAll();                  // giả lập dọn sạch tầng 9
            float t = 0f;
            while (_gs.Floor < 10 && t < 8f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(10, _gs.Floor, "phải lên tới tầng 10");

            t = 0f;
            while (EnemyRegistry.Count == 0 && t < 8f) { t += Time.deltaTime; yield return null; }

            Assert.AreEqual(1, EnemyRegistry.Count, "tầng boss chỉ bày ĐÚNG MỘT con");

            Enemy[] alive = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            Enemy bossEnemy = System.Array.Find(alive, e => e.IsAlive);
            Assert.IsNotNull(bossEnemy, "phải có con quái còn sống ở tầng boss");
            Assert.IsTrue(bossEnemy.IsBoss, "con ở tầng boss phải được đánh dấu IsBoss");
        }

        // ── Đột phá ───────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Dot_pha_nang_tran_va_go_duoc_buc_tuong_M2()
        {
            int baseCap = _gs.Gear.MaxLevel;
            _gs.AddShards(10_000_000f);
            yield return null;

            // Nâng tới kịch trần nền — đây đúng là bức tường mà M2 dừng lại ở đó.
            while (_gs.TryUpgrade(Slot.Weapon)) { }
            Assert.IsTrue(_gs.Gear.AtCap(Slot.Weapon), "phải chạm trần");
            Assert.AreEqual(baseCap, _gs.Gear.Level(Slot.Weapon), "trần nền phải là maxLevel");
            Assert.IsFalse(_gs.TryUpgrade(Slot.Weapon),
                           "BỨC TƯỜNG M2: còn Mảnh mà không nâng được nữa");

            // Không có Lõi thì vẫn tắc.
            Assert.IsFalse(_gs.TryBreakthrough(Slot.Weapon), "chưa có Lõi thì không đột phá được");

            _gs.AwardBoss(10);
            Assert.IsTrue(_gs.TryBreakthrough(Slot.Weapon), "có Lõi rồi thì đột phá được");

            // ĐÂY LÀ CÂU TRẢ LỜI CỦA M3 CHO M2.
            Assert.Greater(_gs.Gear.CapOf(Slot.Weapon), baseCap, "đột phá phải nâng trần");
            Assert.IsTrue(_gs.TryUpgrade(Slot.Weapon), "qua trần rồi phải nâng tiếp được");
        }

        [UnityTest]
        public IEnumerator Dot_pha_tieu_dung_so_Loi_theo_thang_1_2_3()
        {
            _gs.AwardBoss(10); _gs.AwardBoss(20); _gs.AwardBoss(30); _gs.AwardBoss(40);
            yield return null;

            int before = _gs.Cores;
            Assert.AreEqual(1, _gs.Gear.NextTierCost(Slot.Weapon), "cổng 1 tốn 1 Lõi");
            _gs.TryBreakthrough(Slot.Weapon);
            Assert.AreEqual(before - 1, _gs.Cores, "phải trừ đúng 1 Lõi");

            Assert.AreEqual(2, _gs.Gear.NextTierCost(Slot.Weapon), "cổng 2 tốn 2 Lõi");
            _gs.TryBreakthrough(Slot.Weapon);
            Assert.AreEqual(before - 3, _gs.Cores, "1 + 2 = 3 Lõi đã tiêu");
            Assert.AreEqual(3, _gs.Gear.CoresSpent(), "CoresSpent phải khớp");
        }

        [UnityTest]
        public IEnumerator Loi_tieu_vao_o_nao_thi_dong_cua_o_kia()
        {
            _gs.AwardBoss(10);                         // đúng 3 Lõi
            int cores = _gs.Cores;
            yield return null;

            // Dồn hết vào Vũ khí: 1 + 2 = 3 Lõi cho hai bậc.
            Assert.IsTrue(_gs.TryBreakthrough(Slot.Weapon));
            Assert.IsTrue(_gs.TryBreakthrough(Slot.Weapon));
            Assert.AreEqual(0, _gs.Cores, "tiêu hết 3 Lõi");

            // §5.6: tiêu rồi là hết, ô khác phải chịu.
            Assert.IsFalse(_gs.TryBreakthrough(Slot.Armor),
                           "hết Lõi thì KHÔNG đột phá ô khác được — đó là ý nghĩa của lựa chọn");
            Assert.AreEqual(2, _gs.Gear.Tier(Slot.Weapon));
            Assert.AreEqual(0, _gs.Gear.Tier(Slot.Armor));
            Assert.AreEqual(3, cores, "một boss cho 3 Lõi");
        }

        // ── Tẩy điểm ──────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Tay_diem_hoan_100_phan_tram_Loi_va_cat_cap_vuot_tran()
        {
            _gs.AwardBoss(10);
            _gs.TryBreakthrough(Slot.Weapon);
            _gs.AddShards(10_000_000f);
            yield return null;

            while (_gs.TryUpgrade(Slot.Weapon)) { }
            int overCap = _gs.Gear.Level(Slot.Weapon);
            Assert.Greater(overCap, _gs.Gear.MaxLevel, "phải đang ở cấp vượt trần nền");

            int coresBefore = _gs.Cores;
            float shardsBefore = _gs.Shards;
            float cost = _gs.RespecCost();

            Assert.IsTrue(_gs.TryRespec(), "đủ Mảnh thì tẩy điểm được");

            Assert.AreEqual(coresBefore + 1, _gs.Cores, "phải hoàn 100% Lõi đã tiêu");
            Assert.AreEqual(shardsBefore - cost, _gs.Shards, 1f, "phải trừ đúng giá tẩy điểm");
            Assert.AreEqual(0, _gs.Gear.Tier(Slot.Weapon), "bậc phải về 0");
            Assert.AreEqual(_gs.Gear.MaxLevel, _gs.Gear.Level(Slot.Weapon),
                            "cấp vượt trần phải bị CẮT về trần nền — không hoàn Mảnh");
        }

        [UnityTest]
        public IEnumerator Tay_diem_lan_sau_dat_gap_doi()
        {
            _gs.AwardBoss(10);
            _gs.TryBreakthrough(Slot.Weapon);
            _gs.AddShards(10_000_000f);
            yield return null;

            float first = _gs.RespecCost();
            Assert.IsTrue(_gs.TryRespec());

            _gs.TryBreakthrough(Slot.Armor);
            float second = _gs.RespecCost();
            Assert.AreEqual(first * 2f, second, first * 0.01f,
                            "lần tẩy sau phải đắt gấp đôi — quyết định vẫn có sức nặng");
        }

        // ── Nhân vật §5.5b ────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Doi_nhan_vat_KHONG_doi_suc_manh()
        {
            _gs.AddShards(100000f);
            for (int i = 0; i < 5; i++) _gs.TryUpgrade(Slot.Weapon);
            _gs.AwardBoss(10); _gs.AwardBoss(20); _gs.AwardBoss(30); _gs.AwardBoss(40);
            yield return null;

            float baseline = _st.Damage * _st.MaxHp;

            // ĐÂY LÀ TÍNH CHẤT MÀ CẢ §5.5b DỰA VÀO: biên an toàn tỉ lệ với TÍCH
            // sát thương × máu. Nhân một thừa số lên k và chia thừa số kia cho k thì
            // tích không đổi — nên bảng hệ số boss ở §5.10 không phải tính lại.
            for (int i = 0; i < CharacterRoster.Count; i++)
            {
                Assert.IsTrue(_gs.TrySetCharacter(i) || _gs.CharacterIndex == i,
                              $"phải đổi được sang nhân vật {i}");
                yield return null;

                Assert.AreEqual(baseline, _st.Damage * _st.MaxHp, baseline * 0.001f,
                    $"nhân vật {CharacterRoster.Name(i)} (k={CharacterRoster.DamageMult(i):0.00}) " +
                    "làm đổi tích sát thương×máu — §5.5b vỡ, phải cân bằng lại toàn bộ bảng boss");
            }

            // ...nhưng hai thừa số RIÊNG LẺ thì phải khác nhau thật, nếu không đổi nhân
            // vật chẳng có cảm giác gì.
            _gs.TrySetCharacter(2); yield return null;   // Sát thủ, k cao
            float glassDmg = _st.Damage;
            _gs.TrySetCharacter(4); yield return null;   // Tăng, k thấp
            Assert.Less(_st.Damage, glassDmg * 0.5f, "Tăng phải đánh yếu hơn Sát thủ rõ rệt");
            Assert.Greater(_st.MaxHp, 0f);
        }

        [UnityTest]
        public IEnumerator Nhan_vat_khoa_cho_toi_khi_ha_du_boss()
        {
            Assert.IsTrue(CharacterRoster.IsUnlocked(0, 0), "nhân vật đầu phải có sẵn");
            Assert.IsFalse(CharacterRoster.IsUnlocked(1, 0), "nhân vật 2 phải khoá khi chưa hạ boss");
            Assert.IsFalse(_gs.TrySetCharacter(1), "không đổi sang nhân vật đang khoá được");

            _gs.AwardBoss(10);
            yield return null;

            Assert.IsTrue(CharacterRoster.IsUnlocked(1, _gs.BossesKilled), "hạ boss 1 mở nhân vật 2");
            Assert.IsTrue(_gs.TrySetCharacter(1), "mở rồi thì đổi được");
            Assert.AreEqual(1, _gs.CharacterIndex);
        }

        // ── Quét nhanh và tự đánh ─────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Quet_nhanh_chi_mo_o_tang_DA_don_sach()
        {
            Assert.IsFalse(_gs.CanSweep(1), "chưa dọn tầng nào thì chưa quét được");

            _gs.MarkCleared(1);
            yield return null;

            Assert.IsTrue(_gs.CanSweep(1), "dọn xong tầng 1 thì quét được tầng 1");
            Assert.IsFalse(_gs.CanSweep(2), "tầng 2 chưa dọn thì chưa quét được");
        }

        [UnityTest]
        public IEnumerator Tu_danh_khoa_cho_toi_moc_tang_20()
        {
            var auto = Object.FindFirstObjectByType<AutoBattle>();
            Assert.IsNotNull(auto, "không có AutoBattle trong scene");

            Assert.IsFalse(auto.IsUnlocked, "chưa tới mốc thì tự đánh phải khoá");
            Assert.IsFalse(auto.Toggle(), "khoá thì bật không được");

            _gs.MarkCleared(auto.UnlockFloor);
            yield return null;

            Assert.IsTrue(auto.IsUnlocked, $"dọn xong tầng {auto.UnlockFloor} phải mở tự đánh");
            Assert.IsTrue(auto.Toggle(), "mở rồi thì bật được");
            Assert.IsTrue(auto.Enabled);
        }

        // ── Save ──────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Save_giu_du_Loi_bac_dot_pha_va_nhan_vat()
        {
            _gs.AwardBoss(10);
            _gs.TryBreakthrough(Slot.Glove);
            _gs.TrySetCharacter(1);
            _gs.AddShards(10_000_000f);
            yield return null;

            while (_gs.TryUpgrade(Slot.Glove)) { }
            int lvl = _gs.Gear.Level(Slot.Glove);
            int cores = _gs.Cores;
            _gs.Save();

            SaveData d = SaveSystem.Load();
            Assert.AreEqual(SaveData.CurrentVersion, d.version, "phải lưu ở version hiện tại");
            Assert.AreEqual(cores, d.cores, "Lõi còn lại phải được lưu");
            Assert.AreEqual(1, d.bossesKilled, "số boss đã hạ phải được lưu");
            Assert.AreEqual(1, d.gearTiers[(int)Slot.Glove], "bậc đột phá phải được lưu");
            Assert.AreEqual(1, d.characterIndex, "nhân vật đang dùng phải được lưu");

            // Bẫy thứ tự: Restore kẹp cấp theo TRẦN, mà trần phụ thuộc bậc. Nếu khôi phục
            // cấp trước bậc thì người chơi mở lại game là mất sạch cấp trên trần nền.
            Assert.Greater(lvl, _gs.Gear.MaxLevel, "test này chỉ có nghĩa khi cấp vượt trần nền");
            Assert.AreEqual(lvl, d.gearLevels[(int)Slot.Glove], "cấp vượt trần phải được lưu nguyên");
        }
    }
}
