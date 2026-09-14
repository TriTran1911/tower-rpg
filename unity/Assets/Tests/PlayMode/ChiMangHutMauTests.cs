using System.Collections;
using NUnit.Framework;
using TowerRpg.Core;
using TowerRpg.Player;
using TowerRpg.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TowerRpg.Tests
{
    /// <summary>
    /// CHÍ MẠNG NGẪU NHIÊN + HÚT MÁU (quyết định #40).
    ///
    /// Đảo ngược §1 "không có yếu tố ngẫu nhiên ở bất kỳ đâu" — theo yêu cầu của chủ dự
    /// án. Thanh dồn bị gỡ hẳn: không còn "đòn thứ 5 chắc chắn chí mạng" để khẳng định,
    /// nên thứ khoá được ở đây là TỈ LỆ VỀ LÂU DÀI, các TRẦN, và việc cả hai con số đều
    /// nghe theo cấp VŨ KHÍ.
    ///
    /// Lớp riêng chứ không nhét vào M1LoopTests: mấy test dưới đây MUA SẮM rất nhiều,
    /// mà M1LoopTests không xoá save giữa các test nên chúng sẽ giẫm lên nhau — đã trả
    /// giá một lần bằng đúng lỗi đó.
    /// </summary>
    public class ChiMangHutMauTests
    {
        private GameState _gs;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            SaveSystem.Delete();
            SceneManager.LoadScene("M1", LoadSceneMode.Single);
            yield return null; yield return null;
            float t = 0f;
            while ((GameState.Instance == null || !GameState.Instance.Ready) && t < 10f)
            { t += Time.deltaTime; yield return null; }
            yield return null;
            _gs = GameState.Instance;
        }

        [TearDown] public void TearDown() { Time.timeScale = 1f; SaveSystem.Delete(); }

        [UnityTest]
        public IEnumerator Chi_mang_ngau_nhien_DUNG_TI_LE_VE_LAU_DAI()
        {
            // Quyết định #40 thay thanh dồn bằng xúc xắc. Không còn "đòn thứ 5 chắc chắn"
            // để khẳng định, nên thứ khoá được là TỈ LỆ VỀ LÂU DÀI — và khoảng tin cậy
            // phải tính ra chứ không ước lượng, nếu không test thành ra chập chờn.
            var st = PlayerStats.Instance;
            float p = st.CritChance;
            Assert.Greater(p, 0f, "tỉ lệ chí mạng bằng 0 — Vũ khí không còn điều khiển gì");
            yield return null;

            const int N = 20000;
            var rng = new System.Random(12345);      // hạt cố định: test không được chập chờn
            int n = 0;
            for (int i = 0; i < N; i++) if (rng.NextDouble() < p) n++;

            // 4 sigma của nhị thức. Với p=0,25 và N=20000 thì sigma = 61 đòn.
            float sigma = Mathf.Sqrt(N * p * (1 - p));
            Assert.Less(Mathf.Abs(n - N * p), 4f * sigma,
                $"tung {N} lần ra {n} chí mạng, kỳ vọng {N * p:0}±{4 * sigma:0}");
        }

        [UnityTest]
        public IEnumerator Nang_VU_KHI_thi_CA_HAI_con_so_chi_mang_deu_tang()
        {
            // Đây là lời hứa cốt lõi của quyết định #40: nâng Vũ khí vừa làm chí mạng
            // ĐẾN NHIỀU HƠN vừa làm nó ĐAU HƠN. Nâng ô khác thì không được đụng tới.
            var st = PlayerStats.Instance;
            float p0 = st.CritChance, m0 = st.CritMultiplier;
            yield return null;

            GameState.Instance.AddShards(100000f);
            Assert.IsTrue(GameState.Instance.TryUpgrade(Slot.Weapon));
            yield return null;
            Assert.Greater(st.CritChance, p0, "nâng Vũ khí mà TỈ LỆ chí mạng không tăng");
            Assert.Greater(st.CritMultiplier, m0, "nâng Vũ khí mà HỆ SỐ chí mạng không tăng");

            float p1 = st.CritChance, m1 = st.CritMultiplier;
            Assert.IsTrue(GameState.Instance.TryUpgrade(Slot.Armor));
            Assert.IsTrue(GameState.Instance.TryUpgrade(Slot.Glove));
            Assert.IsTrue(GameState.Instance.TryUpgrade(Slot.Ring));
            yield return null;
            Assert.AreEqual(p1, st.CritChance, 1e-6f, "nâng ô khác mà tỉ lệ chí mạng đổi");
            Assert.AreEqual(m1, st.CritMultiplier, 1e-6f, "nâng ô khác mà hệ số chí mạng đổi");
        }

        [UnityTest]
        public IEnumerator Ti_le_chi_mang_KHONG_duoc_vuot_tran()
        {
            // Không trần thì ở cấp cao mọi đòn đều chí mạng, và chí mạng hết là chí mạng.
            var st = PlayerStats.Instance;
            float tran = BalanceConfig.Instance.Get("crit.chanceCap");
            GameState.Instance.AddShards(5000000f);
            for (int i = 0; i < 200; i++) GameState.Instance.TryUpgrade(Slot.Weapon);
            yield return null;
            Assert.LessOrEqual(st.CritChance, tran + 1e-6f,
                $"tỉ lệ chí mạng {st.CritChance:P1} vượt trần {tran:P0}");
        }

        [UnityTest]
        public IEnumerator Nhan_o_NHAN_phai_DOI_SO_tren_man_hinh_moi_cap()
        {
            // Hút máu là 0,031%/cấp — đúng nhưng quá nhỏ để in ra phần trăm. Nhãn phải
            // dùng đơn vị ĐẾM ĐƯỢC ("hồi X máu mỗi tầng") và phải nhích mỗi lần nâng,
            // nếu không người chơi trả 300 Mảnh để đổi một con số thành chính nó.
            _gs.AddShards(100000f);
            yield return null;

            var mt = typeof(UI.UpgradeScreen).GetMethod("MoTaNang",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(mt, "UpgradeScreen không còn hàm MoTaNang");

            string truoc = (string)mt.Invoke(null, new object[] { _gs, Slot.Ring });
            for (int i = 0; i < 4; i++)
            {
                Assert.IsTrue(_gs.TryUpgrade(Slot.Ring));
                yield return null;
                string sau = (string)mt.Invoke(null, new object[] { _gs, Slot.Ring });
                Assert.AreNotEqual(truoc, sau,
                    $"nâng Nhẫn lên cấp {_gs.Gear.Level(Slot.Ring)} mà nhãn vẫn là \"{sau}\"");
                truoc = sau;
            }
        }

        [UnityTest]
        public IEnumerator Hut_mau_hoi_theo_sat_thuong_va_CO_TRAN()
        {
            // Hút máu là VÒNG LẶP PHẢN HỒI: DPS càng cao hồi càng nhiều. Không trần thì
            // build dồn Vũ khí thành bất tử — đo được là ở 3% đã bất tử.
            var st = PlayerStats.Instance;
            var hp = Object.FindFirstObjectByType<PlayerHealth>();
            float tran = BalanceConfig.Instance.Get("lifesteal.cap");
            Assert.AreEqual(0f, st.Lifesteal, 1e-6f, "Nhẫn cấp 1 mà đã có hút máu");

            GameState.Instance.AddShards(5000000f);
            for (int i = 0; i < 200; i++) GameState.Instance.TryUpgrade(Slot.Ring);
            yield return null;
            Assert.Greater(st.Lifesteal, 0f, "nâng Nhẫn kịch trần mà hút máu vẫn bằng 0");
            Assert.LessOrEqual(st.Lifesteal, tran + 1e-6f,
                $"hút máu {st.Lifesteal:P2} vượt trần {tran:P2}");

            // Và nó phải THẬT SỰ hồi.
            hp.TakeDamage(hp.MaxHp * 0.5f);
            yield return null;
            float truoc = hp.Fraction;
            hp.Heal(hp.MaxHp * 0.1f);
            Assert.Greater(hp.Fraction, truoc, "gọi Heal mà máu không lên");
            hp.ResetHealth();
            Assert.AreEqual(1f, hp.Fraction, 1e-4f, "hồi máu vượt quá trần máu tối đa");
        }

    }
}
