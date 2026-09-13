using System.Collections;
using NUnit.Framework;
using TowerRpg.Combat;
using TowerRpg.Core;
using TowerRpg.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TowerRpg.Tests
{
    /// <summary>
    /// Chạy thật scene M1 và kiểm bằng hành vi, không phải bằng tham chiếu.
    ///
    /// VerifyM1Scene chứng minh scene NỐI ĐÚNG. Bộ test này chứng minh nó CHẠY ĐÚNG —
    /// hai chuyện khác nhau. Đây là chỗ duy nhất kiểm được luật cốt lõi §5.3 và §5.4
    /// mà không cần người ngồi bấm.
    ///
    /// Chạy không giao diện:
    ///   Unity -runTests -batchmode -projectPath . -testPlatform PlayMode \
    ///         -testResults ket-qua.xml
    /// </summary>
    public class M1LoopTests
    {
        private const string SceneName = "M1";
        private const float LoadTimeout = 10f;

        private PlayerController _ctrl;
        private PlayerHealth    _health;
        private CritMeter       _meter;
        private AutoAttack      _attack;
        private VirtualJoystick _joystick;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            yield return null;
            yield return null;

            // BalanceConfig nạp CSV bằng coroutine — phải chờ, không được giả định
            float t = 0f;
            while ((BalanceConfig.Instance == null || !BalanceConfig.Instance.IsLoaded) && t < LoadTimeout)
            {
                t += Time.deltaTime;
                yield return null;
            }

            yield return null;   // để GameBootstrap kịp sinh quái

            _ctrl     = Object.FindFirstObjectByType<PlayerController>();
            _health   = Object.FindFirstObjectByType<PlayerHealth>();
            _meter    = Object.FindFirstObjectByType<CritMeter>();
            _attack   = Object.FindFirstObjectByType<AutoAttack>();
            _joystick = Object.FindFirstObjectByType<VirtualJoystick>();
        }

        // ── 1. nền móng ───────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Nap_duoc_so_lieu_can_bang()
        {
            Assert.IsNotNull(BalanceConfig.Instance, "không có BalanceConfig trong scene");
            Assert.IsTrue(BalanceConfig.Instance.IsLoaded, "CSV chưa nạp xong");
            Assert.IsFalse(BalanceConfig.Instance.LoadFailed,
                           "nạp CSV THẤT BẠI — thiếu khoá hoặc thiếu file");
            Assert.AreEqual(5, BalanceConfig.Instance.GetInt("crit.meterSize"));
            yield break;
        }

        [UnityTest]
        public IEnumerator Sinh_dung_so_quai()
        {
            int want = BalanceConfig.Instance.GetInt("enemy.count");
            Assert.AreEqual(want, EnemyRegistry.Count, $"phải có {want} quái lúc bắt đầu");
            yield break;
        }

        [UnityTest]
        public IEnumerator Bat_dau_o_giua_thi_an_toan()
        {
            // Quái ở bán kính 3.5; tầm đánh quái 2.8 — người chơi đứng giữa phải KHÔNG bị đánh.
            float hp0 = _health.Fraction;
            int quai0 = EnemyRegistry.Count;

            yield return new WaitForSeconds(2f);

            Assert.AreEqual(hp0, _health.Fraction, 0.001f,
                            "đứng giữa đấu trường mà vẫn mất máu — tầm đánh quái đang quá xa");
            Assert.AreEqual(quai0, EnemyRegistry.Count,
                            "quái chết dù người chơi ngoài tầm — tầm đánh người chơi đang quá xa");
        }

        // ── 2. luật cốt lõi §5.3 ──────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Dung_yen_trong_tam_thi_danh_va_giet_duoc_quai()
        {
            int before = EnemyRegistry.Count;
            TeleportNextToNearestEnemy();

            yield return new WaitForSeconds(6f);

            Assert.Less(EnemyRegistry.Count, before,
                        "đứng yên trong tầm 6 giây mà không giết được con nào — §5.3 hỏng");
        }

        [UnityTest]
        public IEnumerator Dung_yen_trong_tam_thi_AN_DON()
        {
            TeleportNextToNearestEnemy();
            float hp0 = _health.Fraction;

            yield return new WaitForSeconds(4f);

            Assert.Less(_health.Fraction, hp0,
                        "đứng yên trong tầm quái mà không mất máu — đứng yên phải có GIÁ (§5.3)");
        }

        [UnityTest]
        public IEnumerator Di_chuyen_thi_KHONG_danh()
        {
            TeleportNextToNearestEnemy();
            yield return null;

            PushJoystick(new Vector2(1f, 0f));
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.IsTrue(_ctrl.IsMoving, "đẩy cần gạt mà IsMoving vẫn false — cần gạt không nhận");

            int before = EnemyRegistry.Count;
            float hpBefore = HpOfNearestEnemy();

            yield return new WaitForSeconds(3f);   // vẫn giữ cần gạt

            Assert.AreEqual(before, EnemyRegistry.Count,
                            "đang di chuyển mà vẫn giết được quái — LUẬT CỐT LÕI §5.3 HỎNG");
            Assert.AreEqual(hpBefore, HpOfNearestEnemy(), 0.001f,
                            "đang di chuyển mà quái vẫn mất máu — §5.3 hỏng");

            ReleaseJoystick();
        }

        // ── 3. thanh chí mạng §5.4 ────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Chi_mang_dung_nhip_cu_5_don_mot_lan()
        {
            _attack.enabled = false;                 // tự điều khiển nhịp, không để AutoAttack chen vào
            yield return null;

            int size = BalanceConfig.Instance.GetInt("crit.meterSize");
            for (int round = 1; round <= 4; round++)
            {
                for (int i = 1; i < size; i++)
                    Assert.IsFalse(_meter.RegisterAttack(),
                                   $"vòng {round}, đòn {i}: KHÔNG được chí mạng");

                Assert.IsTrue(_meter.RegisterAttack(),
                              $"vòng {round}, đòn {size}: PHẢI chí mạng");
            }
        }

        [UnityTest]
        public IEnumerator Thanh_chi_mang_KHONG_reset_khi_di_chuyen()
        {
            _attack.enabled = false;
            yield return null;

            int size = BalanceConfig.Instance.GetInt("crit.meterSize");
            for (int i = 1; i < size; i++) _meter.RegisterAttack();   // nạp gần đầy
            Assert.IsTrue(_meter.IsReady, "nạp đủ size-1 đòn thì thanh phải sẵn sàng");

            PushJoystick(new Vector2(1f, 0f));
            yield return new WaitForSeconds(1.5f);
            ReleaseJoystick();
            yield return new WaitForFixedUpdate();

            Assert.IsTrue(_meter.IsReady,
                          "chạy vòng quanh xong thanh chí mạng bị reset — " +
                          "chiến thuật chữ ký của §5.4 (lùi lại, chờ, dồn vào lúc boss hở sườn) đã mất");
            Assert.IsTrue(_meter.RegisterAttack(), "đòn ngay sau khi dừng phải là chí mạng");
        }

        // ── trợ giúp ──────────────────────────────────────────────────────────────────

        /// <summary>Dịch người chơi tới sát con quái gần nhất, trong tầm đánh của cả hai.</summary>
        private void TeleportNextToNearestEnemy()
        {
            IDamageable near = EnemyRegistry.Nearest(Vector3.zero, 999f);
            Assert.IsNotNull(near, "không còn quái nào để tới gần");

            float range = BalanceConfig.Instance.Get("player.attackRange");
            Vector3 dir = (near.Position - Vector3.zero).normalized;
            _ctrl.transform.position = near.Position - dir * (range * 0.5f);
        }

        /// <summary>Máu THẬT của con gần nhất, không phải chỉ sống/chết.</summary>
        private float HpOfNearestEnemy()
        {
            IDamageable near = EnemyRegistry.Nearest(_ctrl.transform.position, 999f);
            return near is Enemies.Enemy e ? e.HealthFraction : -1f;
        }

        /// <summary>
        /// Giả lập kéo cần gạt ĐỘNG: nhấn xuống một điểm, rồi kéo sang điểm khác.
        /// Cần gạt động hiện ra NGAY TẠI điểm nhấn, nên nhấn và kéo cùng một chỗ
        /// cho độ lệch bằng 0 — phải tách hai điểm ra.
        /// </summary>
        private void PushJoystick(Vector2 dir)
        {
            var zone = _joystick.GetComponent<RectTransform>();
            Vector2 down = new Vector2(zone.position.x, zone.position.y);

            _joystick.OnPointerDown(new PointerEventData(EventSystem.current) { position = down });

            // kéo ra xa đủ để vượt vùng chết (player.moveDeadzone)
            var visual = new SerializedObjectProxy(_joystick).Visual;
            float radius = visual != null ? visual.rect.width * 0.5f * visual.lossyScale.x : 100f;
            Vector2 drag = down + dir.normalized * radius;

            _joystick.OnDrag(new PointerEventData(EventSystem.current) { position = drag });
        }

        /// <summary>Đọc trường private 'visual' của cần gạt mà không cần đổi tầm vực.</summary>
        private readonly struct SerializedObjectProxy
        {
            private readonly VirtualJoystick _j;
            public SerializedObjectProxy(VirtualJoystick j) => _j = j;

            public RectTransform Visual
            {
                get
                {
                    var f = typeof(VirtualJoystick).GetField("visual",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    return f?.GetValue(_j) as RectTransform;
                }
            }
        }

        private void ReleaseJoystick() =>
            _joystick.OnPointerUp(new PointerEventData(EventSystem.current));
    }
}
