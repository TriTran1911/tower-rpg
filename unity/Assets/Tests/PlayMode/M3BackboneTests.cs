using System.Collections;
using NUnit.Framework;
using TowerRpg.Combat;
using TowerRpg.Core;
using TowerRpg.Enemies;
using TowerRpg.Juice;
using TowerRpg.Player;
using TowerRpg.Progression;
using TowerRpg.UI;
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

        /// <summary>
        /// Dọn sạch một tầng như FloorRunner làm thật: cộng Mảnh TRƯỚC rồi mới đánh dấu.
        /// Chỉ gọi MarkCleared là mô phỏng thiếu — từ Việc 2, quét còn cần NGÂN SÁCH, mà
        /// ngân sách sinh ra từ Mảnh leo được chứ không từ cái dấu đã-dọn.
        /// </summary>
        private void ClearFloor(int floor)
        {
            _gs.AddShards(_gs.ShardReward(floor));
            _gs.MarkCleared(floor);
        }

        [UnityTest]
        public IEnumerator Quet_nhanh_chi_mo_o_tang_DA_don_sach()
        {
            Assert.IsFalse(_gs.CanSweep(1), "chưa dọn tầng nào thì chưa quét được");

            ClearFloor(1);
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

        // ── Việc 2: hai ô đã lệch so với can-bang.xlsx ────────────────────────────

        [UnityTest]
        public IEnumerator Quai_ngoai_tam_thi_hoi_chieu_DONG_BANG()
        {
            var hp = Object.FindFirstObjectByType<PlayerHealth>();
            var ctrl = Object.FindFirstObjectByType<PlayerController>();
            Assert.IsNotNull(hp); Assert.IsNotNull(ctrl);

            float range = BalanceConfig.Instance.Get("enemy.attackRange");
            float interval = 1f / BalanceConfig.Instance.Get("enemy.attacksPerSecond");

            // Đứng THẬT XA, lâu hơn nhiều lần một nhịp đánh của quái.
            ctrl.transform.position = new Vector3(0f, -60f, 0f);
            yield return new WaitForSeconds(interval * 3f);
            hp.ResetHealth();
            yield return null;

            float full = hp.Fraction;
            Assert.AreEqual(1f, full, 0.001f, "ở xa thì không được mất máu");

            // Bước vào sát một con quái. ĐÂY LÀ CHỖ LỖI CŨ NẰM: hồi chiêu từng tụt âm
            // sâu trong lúc ở xa, nên vừa vào tầm là ăn đòn NGAY khung hình đó.
            IDamageable near = EnemyRegistry.Nearest(Vector3.zero, 999f);
            Assert.IsNotNull(near, "cần ít nhất một con quái");
            ctrl.transform.position = near.Position - Vector3.up * (range * 0.5f);

            // Ngay sau khi vào tầm, phải còn được một khoảng ân huệ bằng gần một nhịp.
            yield return new WaitForSeconds(interval * 0.4f);
            Assert.AreEqual(1f, hp.Fraction, 0.001f,
                "vừa vào tầm đã ăn đòn ngay — hồi chiêu quái vẫn chạy lúc ở ngoài tầm, "
                + "tức là di chuyển KHÔNG né được gì và §5.3 mất hết ý nghĩa không gian");

            // ...nhưng đứng đủ lâu thì vẫn phải ăn đòn, nếu không là thành bất tử.
            yield return new WaitForSeconds(interval * 1.5f);
            Assert.Less(hp.Fraction, 1f, "đứng trong tầm đủ lâu thì PHẢI mất máu");
        }

        [UnityTest]
        public IEnumerator Quet_nhanh_dung_lai_khi_het_ngan_sach()
        {
            float mult = BalanceConfig.Instance.Get("sweep.totalMult");

            ClearFloor(1);
            yield return null;

            float climbed = _gs.ShardsClimbed;
            Assert.Greater(climbed, 0f, "test này cần người chơi đã leo được ít nhất một tầng");

            float budget = _gs.SweepBudgetLeft;
            Assert.AreEqual(climbed * (mult - 1f), budget, 1f,
                            "trần quét phải đúng (hệ số - 1) lần Mảnh đã leo");
            Assert.IsTrue(_gs.CanSweep(1), "còn ngân sách thì quét được");

            // Tiêu sạch ngân sách.
            _gs.AddShards(budget, fromSweep: true);
            yield return null;

            Assert.AreEqual(0f, _gs.SweepBudgetLeft, 0.01f, "phải hết ngân sách");
            Assert.IsFalse(_gs.CanSweep(1),
                "ĐÂY LÀ TRẦN CỦA §5.6: hết ngân sách thì quét phải dừng, nếu không quét "
                + "thành máy in Mảnh vô hạn và toàn bộ đường cong chi phí mất nghĩa");

            // Leo thêm thì trần tự nới ra — quét là NÉN THỜI GIAN, không phải nguồn thứ hai.
            _gs.AddShards(1000f);
            yield return null;
            Assert.Greater(_gs.SweepBudgetLeft, 0f, "leo thêm phải nới được trần");
            Assert.IsTrue(_gs.CanSweep(1));
        }

        [UnityTest]
        public IEnumerator Tong_Manh_khong_bao_gio_vuot_he_so_cay_lai()
        {
            float mult = BalanceConfig.Instance.Get("sweep.totalMult");
            for (int f = 1; f <= 5; f++) ClearFloor(f);
            yield return null;

            // Cố tình quét tham lam hơn ngân sách rất nhiều lần.
            for (int i = 0; i < 50; i++)
            {
                float left = _gs.SweepBudgetLeft;
                if (left <= 0f) break;
                _gs.AddShards(Mathf.Min(_gs.ShardReward(5), left), fromSweep: true);
            }
            yield return null;

            float total = _gs.ShardsClimbed + _gs.ShardsSwept;
            Assert.LessOrEqual(total, _gs.ShardsClimbed * mult + 1f,
                $"tổng Mảnh vượt {mult}x Mảnh leo — đúng ô 'Thông số'!B32 bị phá");
        }

        [UnityTest]
        public IEnumerator Mau_nen_PHAI_tang_theo_tang_da_qua()
        {
            var hp = Object.FindFirstObjectByType<PlayerHealth>();
            Assert.IsNotNull(hp);
            yield return null;

            float atFloor1 = hp.MaxHp;
            Assert.Greater(atFloor1, 0f);

            // Leo lên tầng 10 mà KHÔNG mở màn nâng cấp, KHÔNG mua Giáp — đúng cách một
            // người chơi mới đi tới boss đầu tiên.
            while (_gs.Floor < 10) _gs.AdvanceFloor();
            yield return null; yield return null;

            float growth = BalanceConfig.Instance.Get("player.hpPerFloor");
            float want = atFloor1 * Mathf.Pow(1f + growth, 9);

            // VAN AN TOÀN #4 CỦA §5.8. PlayerHealth cache _maxHp; nếu quên gọi lại Rescale
            // khi đổi tầng thì van này im lặng không chạy và người chơi đánh boss 1 bằng
            // máu của tầng 1 — đo được biên 0,51 thay vì 0,75.
            Assert.AreEqual(want, hp.MaxHp, want * 0.01f,
                $"máu tối đa ở tầng 10 phải là {want:F1} (gấp {Mathf.Pow(1f + growth, 9):F3} lần "
                + $"tầng 1) nhưng đang là {hp.MaxHp:F1} — van an toàn #4 của §5.8 không chạy");
        }

        [UnityTest]
        public IEnumerator Moi_boss_phai_dat_bien_an_toan_toi_thieu()
        {
            yield return null;
            BalanceConfig b = BalanceConfig.Instance;

            // Cấp trang bị mà 'Đường cong tầng'!G của can-bang.xlsx kỳ vọng ở mỗi tầng boss.
            var expected = new (int floor, int gear)[] { (10, 7), (20, 13), (30, 19), (40, 25) };
            const float MinMargin = 1.5f;     // ô 'Thông số'!B40

            for (int i = 0; i < expected.Length; i++)
            {
                (int floor, int gear) = expected[i];
                if (!b.Has($"boss.hpMult{i + 1}")) continue;

                float Pow(string key, int lv) => Mathf.Pow(1f + b.Get(key), lv - 1);

                float dmg = b.Get("player.attackDamage") * Pow("gear.weapon.perLevel", gear);
                float aps = b.Get("player.attacksPerSecond") * Pow("gear.glove.perLevel", gear);
                float critMul = Pow("gear.ring.perLevel", gear) * b.Get("crit.multiplier");
                float pdps = dmg * aps * (1f + (critMul - 1f) / b.Get("crit.meterSize"));

                float php = b.Get("player.maxHp")
                          * Mathf.Pow(1f + b.Get("player.hpPerFloor"), floor - 1)
                          * Pow("gear.armor.perLevel", gear);

                float bhp = b.Get("enemy.hpFloor1")
                          * Mathf.Pow(1f + b.Get("enemy.hpGrowth"), floor - 1)
                          * b.Get($"boss.hpMult{i + 1}");
                float bdps = b.Get("enemy.dpsFloor1")
                           * Mathf.Pow(1f + b.Get("enemy.dpsGrowth"), floor - 1);

                float margin = (php / bdps) / (bhp / pdps);

                // ĐÂY LÀ RÀNG BUỘC KHIẾN GAME KẾT THÚC ĐƯỢC. Nó từng hỏng theo BA cách cùng
                // lúc và không test nào thấy: (1) enemy.dpsFloor1 giao 3 thay vì B7xB9 = 1,5,
                // (2) van an toàn #4 của §5.8 không chạy vì PlayerHealth cache máu tối đa,
                // (3) quét nhanh vô hạn che mất triệu chứng bằng Mảnh thừa mứa.
                Assert.GreaterOrEqual(margin, MinMargin,
                    $"boss {i + 1} (tầng {floor}, trang bị cấp {gear}): biên {margin:F2} < {MinMargin}. "
                    + "Thời gian sống ngắn hơn thời gian giết -> KHÔNG THẮNG NỔI bằng đường leo. "
                    + "Kiểm enemy.dpsFloor1, player.hpPerFloor và boss.hpMult trong m1-balance.csv.");
            }
        }

        // ── Việc 3: âm thanh ──────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Am_thanh_du_clip_va_khong_bao_gio_nem_loi()
        {
            var sfx = Object.FindFirstObjectByType<SfxPlayer>();
            Assert.IsNotNull(sfx, "không có SfxPlayer trong scene");
            yield return null;

            for (int i = 0; i < SfxPlayer.SfxCount; i++)
                Assert.IsNotNull(sfx.ClipAt(i), $"thiếu clip cho Sfx thứ {i} ({(Sfx)i})");

            // SfxPlayer.Play cố ý NUỐT mọi lỗi — âm thanh là trang trí, nó không có quyền
            // làm hỏng một trận đấu. Test này khoá tính chất đó: gọi bậy kiểu gì cũng không nổ.
            for (int i = 0; i < SfxPlayer.SfxCount; i++) SfxPlayer.Play((Sfx)i);
            SfxPlayer.Play((Sfx)(-1));
            SfxPlayer.Play((Sfx)999);
            SfxPlayer.Play(Sfx.Hit, -5f);
            SfxPlayer.Play(Sfx.Hit, 99f);
            yield return null;

            // KHÔNG dừng ở "không ném lỗi" — đó là cái bẫy đã cắn dự án này ba lần:
            // nối đúng KHÁC chạy đúng. Kiểm rằng một AudioSource THẬT SỰ đang phát.
            SfxPlayer.Play(Sfx.Hit);
            yield return null;

            var sources = sfx.GetComponentsInChildren<AudioSource>();
            Assert.AreEqual(8, sources.Length, "phải có 8 nguồn quay vòng");

            int playing = 0;
            foreach (AudioSource s in sources) if (s.isPlaying) playing++;
            Debug.Log($"[âm thanh] {sources.Length} nguồn, {playing} đang phát sau khi gọi Play");

            Assert.Greater(playing, 0,
                "gọi Play xong mà KHÔNG nguồn nào phát — clip đã nối, không lỗi nào ném ra, "
                + "nhưng game vẫn câm. Kiểm AudioListener trong scene và clip có bị null không.");
        }

        [UnityTest]
        public IEnumerator Tieng_bi_danh_co_hoi_de_khong_thanh_nhieu()
        {
            var sfx = Object.FindFirstObjectByType<SfxPlayer>();
            Assert.IsNotNull(sfx);
            float cd = BalanceConfig.Instance.Get("audio.playerHitCooldown");
            Assert.Greater(cd, 0f, "phải có hồi cho tiếng bị đánh — 6 quái đánh liên tục "
                                 + "mà không chặn thì thành tiếng nhiễu liên tục");

            // Bắn dồn dập: không được ném lỗi, và hồi phải chặn bớt.
            for (int i = 0; i < 50; i++) SfxPlayer.Play(Sfx.PlayerHit);
            yield return null;
            Assert.Pass($"50 lần liên tiếp với hồi {cd}s — không ném lỗi");
        }

        // ── Việc 4: thanh máu quái + khoảnh khắc chết ─────────────────────────────

        [UnityTest]
        public IEnumerator Quai_chet_thi_GO_DANG_KY_NGAY_du_xac_con_dang_tan()
        {
            yield return null;
            int before = EnemyRegistry.Count;
            Assert.Greater(before, 0, "cần quái để giết");

            var target = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)[0];
            float deathSeconds = BalanceConfig.Instance.Get("juice.enemyDeathSeconds");

            target.TakeDamage(1e9f, false);      // giết tức khắc

            // ĐÂY LÀ TÍNH CHẤT LÀM HOẠT ẢNH CHẾT TỐN 0 GIÂY THỜI GIAN CHƠI.
            // FloorRunner chờ Count == 0 và AutoAttack hỏi Nearest — cả hai đọc registry.
            // Nếu đợi Destroy xong mới gỡ thì mỗi con cộng thêm 0,22s vào thời lượng tầng.
            Assert.AreEqual(before - 1, EnemyRegistry.Count,
                "quái phải rời registry NGAY trong khung hình chết, không đợi xác tan xong");
            Assert.IsFalse(target.IsAlive, "phải đọc là đã chết ngay");

            // ...nhưng cái xác vẫn còn đó để nhìn.
            Assert.IsTrue(target != null, "xác phải còn trong khung hình vừa chết");

            yield return new WaitForSeconds(deathSeconds + 0.3f);
            Assert.IsTrue(target == null, $"xác phải biến mất sau {deathSeconds}s");
        }

        [UnityTest]
        public IEnumerator Thanh_mau_quai_an_khi_day_va_hien_khi_trung_don()
        {
            yield return null; yield return null;
            var enemy = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)[0];
            var bar = enemy.GetComponent<EnemyHealthBar>();
            Assert.IsNotNull(bar, "prefab quái phải có EnemyHealthBar");

            var renderers = enemy.GetComponentsInChildren<SpriteRenderer>(true);
            SpriteRenderer fill = System.Array.Find(renderers, r => r.name == "HpFill");
            Assert.IsNotNull(fill, "thiếu HpFill");

            yield return null;
            Assert.IsFalse(fill.enabled,
                "quái máu đầy mà đã hiện thanh — một tầng vừa bày sẽ có 6 thanh đầy vô nghĩa");

            enemy.TakeDamage(1f, false);
            yield return null; yield return null;

            Assert.IsTrue(fill.enabled, "trúng đòn rồi thì thanh máu phải hiện ra");
            Assert.Less(enemy.HealthFraction, 1f);

            // VƠI TỪ PHẢI SANG TRÁI, không teo đều hai bên. Mép TRÁI của ruột phải đứng yên
            // trùng mép trái của nền; chỉ mép phải được di chuyển. Bản đầu co giãn nhầm
            // đối tượng nên ruột teo quanh tâm như viên thuốc — mọi test khác vẫn xanh.
            SpriteRenderer bg = System.Array.Find(renderers, r => r.name == "HpBg");
            Assert.IsNotNull(bg, "thiếu HpBg");

            enemy.TakeDamage(enemy.HealthFraction * 0.5f * 1000f, false);
            yield return null; yield return null;

            float leftGap = Mathf.Abs(fill.bounds.min.x - bg.bounds.min.x);
            float rightGap = Mathf.Abs(fill.bounds.max.x - bg.bounds.max.x);
            Assert.Less(leftGap, 0.05f,
                $"mép TRÁI của ruột lệch {leftGap:F3} đơn vị khỏi mép trái nền — thanh đang "
                + "teo về giữa thay vì vơi từ phải sang trái");
            Assert.Greater(rightGap, leftGap,
                "mép PHẢI phải lùi vào khi mất máu");
        }

        [UnityTest]
        public IEnumerator Thanh_mau_boss_theo_dung_mau_boss()
        {
            var runner = Object.FindFirstObjectByType<FloorRunner>();
            Assert.IsNotNull(runner);

            while (_gs.Floor < 9) _gs.AdvanceFloor();
            yield return null;
            EnemyRegistry.ClearAll();

            float t = 0f;
            while ((!runner.InBossFight || EnemyRegistry.Count == 0) && t < 10f)
            { t += Time.deltaTime; yield return null; }

            Assert.IsTrue(runner.InBossFight, "phải vào được trận boss");
            Assert.AreEqual(1f, runner.BossHealthFraction, 0.02f, "boss vừa bày phải đầy máu");

            Enemy boss = System.Array.Find(
                Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None), e => e.IsBoss && e.IsAlive);
            Assert.IsNotNull(boss);
            boss.TakeDamage(boss.HealthFraction > 0f ? 1e6f : 1f, false);
            yield return null;

            // Boss mất 75-193 giây; không có vạch tiến trình thì người chơi không biết
            // mình đang thắng hay đang phí thời gian.
            Assert.AreEqual(0f, runner.BossHealthFraction, 0.001f,
                            "boss chết thì thanh phải về 0");
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
            Assert.AreEqual(_gs.ShardsClimbed, d.shardsClimbed, 1f, "Mảnh-đã-leo phải được lưu");
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
