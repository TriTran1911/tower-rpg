using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TowerRpg.Combat;
using TowerRpg.Core;
using TowerRpg.Enemies;
using TowerRpg.Loot;
using TowerRpg.Player;
using TowerRpg.Progression;
using TowerRpg.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TowerRpg.Tests
{
    /// <summary>
    /// MỘT PHIÊN CHƠI, chụp lại từng nhịp đáng nhìn từ tầng 1 tới lúc hạ boss.
    ///
    /// Đây không phải test — nó không phán đúng sai. Nó CHƠI rồi chụp, để con người có
    /// thứ mà nhìn. Bài học lặp năm lần trong dự án này là "nối đúng ≠ chạy đúng ≠ nhìn
    /// được", và ba lớp kiểm kia đều mù trước câu hỏi cuối.
    /// </summary>
    public class M4PhienChoi
    {
        private const int W = 540, H = 960;
        private static readonly string OutDir =
            Path.GetFullPath(Path.Combine(Application.dataPath, "../../anh-chup/phien-choi"));

        private RenderTexture _rt;
        private Camera _cam;
        private readonly StringBuilder _nhatKy = new StringBuilder();
        private int _stt;

        [UnityTest, Timeout(900000)]
        public IEnumerator Mot_phien_choi_tu_tang_1_toi_boss()
        {
            Time.timeScale = 1f;
            Directory.CreateDirectory(OutDir);
            foreach (string f in Directory.GetFiles(OutDir, "*.png")) File.Delete(f);

            SaveSystem.Delete();
            SceneManager.LoadScene("M1", LoadSceneMode.Single);
            yield return null; yield return null;

            float w = 0f;
            while ((GameState.Instance == null || !GameState.Instance.Ready) && w < 10f)
            { w += Time.deltaTime; yield return null; }

            GameState gs = GameState.Instance;
            var hp = Object.FindFirstObjectByType<PlayerHealth>();
            var ctrl = Object.FindFirstObjectByType<PlayerController>();
            var runner = Object.FindFirstObjectByType<FloorRunner>();
            var drops = Object.FindFirstObjectByType<ShardDropSpawner>();
            var upScreen = Object.FindFirstObjectByType<UpgradeScreen>();
            var chScreen = Object.FindFirstObjectByType<CharacterScreen>();
            var milestone = Object.FindFirstObjectByType<MilestoneOverlay>();
            var auto = Object.FindFirstObjectByType<AutoBattle>();

            _cam = Camera.main;
            _rt = new RenderTexture(W, H, 24);
            _cam.targetTexture = _rt;
            _cam.aspect = (float)W / H;
            foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                                       .Where(c => c.renderMode == RenderMode.ScreenSpaceOverlay))
            { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = _cam; c.planeDistance = 1f; }

            // ── 1 · vừa vào tầng 1 ───────────────────────────────────────────────
            yield return null;
            Chup("vao-tran", gs, hp, $"{EnemyRegistry.Count} quái, chưa đánh gì");

            // ── 2 · đứng đánh con đầu tiên ───────────────────────────────────────
            IDamageable muc = EnemyRegistry.Nearest(Vector3.zero, 999f);
            float tam = BalanceConfig.Instance.Get("player.attackRange");
            ctrl.transform.position = muc.Position - muc.Position.normalized * (tam * 0.55f);
            yield return Doi(2.0f);
            Chup("dang-danh", gs, hp, "đứng trong tầm, thanh máu quái vơi dần");

            // ── 3 · bắt đúng lúc quái báo hiệu ra đòn ────────────────────────────
            var quai = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)
                             .FirstOrDefault(e => e.IsAlive);
            // Bắt đúng lúc báo hiệu ĐÃ NGẢ HẲN, không phải lúc nó vừa bắt đầu: dùng cờ
            // IsTelegraphing rồi đợi tới gần cuối cửa sổ. Bản đầu dò bằng `color.g > 0.45`
            // — mà màu nền của quái vốn là TRẮNG nên điều kiện đó luôn đúng ngay khung
            // hình đầu, và ảnh chụp ra một khoảnh khắc chẳng có gì xảy ra.
            float t0 = 0f;
            float scaleMax = 0f;
            while (t0 < 5f)
            {
                t0 += Time.deltaTime;
                yield return null;
                if (quai == null || !quai.IsTelegraphing) continue;
                if (quai.transform.localScale.x > scaleMax) scaleMax = quai.transform.localScale.x;
                if (scaleMax > 1.12f) break;         // đã phồng gần hết cỡ
            }
            Chup("bao-hieu-ra-don", gs, hp, "quái sáng lên và nhích tới — 0,25s trước đòn");

            // ── 4 · viên Mảnh vừa rơi ────────────────────────────────────────────
            var xa = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)
                           .FirstOrDefault(e => e.IsAlive &&
                               Vector3.Distance(e.transform.position, ctrl.transform.position) > 4f);
            if (xa != null)
            {
                ctrl.transform.position = xa.transform.position + Vector3.down * 4f;
                yield return null;
                xa.TakeDamage(1e9f, false);
                yield return Doi(0.12f);
                ctrl.transform.position = xa.transform.position + Vector3.down * 2.3f;
                yield return null;
                Chup("manh-roi-ra", gs, hp, $"{drops.LiveCount} viên trên sàn, xác đang tan");
            }

            // ── 5 · mở màn trang bị, mua một cấp ─────────────────────────────────
            gs.AddShards(gs.ShardReward(1) * 3f);
            upScreen.Toggle();
            yield return Doi(0.3f);
            Chup("truoc-khi-nang", gs, hp, "hai con số §5.5(b) và khung Lõi");
            gs.TryUpgrade(Slot.Weapon);
            yield return Doi(0.3f);
            Chup("sau-khi-nang", gs, hp, "sát thương đổi số, cấp đổi");
            upScreen.Toggle();
            yield return Doi(0.2f);

            // ── 6 · dọn sạch tầng, banner ────────────────────────────────────────
            foreach (Enemy e in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
                if (e.IsAlive) e.TakeDamage(1e9f, false);
            yield return Doi(0.45f);
            Chup("don-sach-tang", gs, hp, "banner + số Mảnh đang chạy");

            // ── 7 · leo tới cửa boss ─────────────────────────────────────────────
            while (gs.Floor < 9) gs.AdvanceFloor();
            yield return null;
            hp.TakeDamage(hp.MaxHp * 0.85f);      // tới cửa boss với máu thấp
            yield return Doi(0.2f);
            Chup("truoc-cua-boss", gs, hp, $"tầng 9, máu {hp.Fraction * 100:F0}%");

            EnemyRegistry.ClearAll();
            float t1 = 0f;
            while (gs.Floor < 10 && t1 < 12f) { t1 += Time.deltaTime; yield return null; }
            yield return Doi(0.6f);
            Chup("vao-tang-boss", gs, hp, $"máu hồi về {hp.Fraction * 100:F0}% — cửa boss");

            // ── 8 · đánh boss ────────────────────────────────────────────────────
            Enemy boss = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)
                               .FirstOrDefault(e => e.IsBoss && e.IsAlive);
            if (boss != null)
            {
                ctrl.transform.position = boss.transform.position + Vector3.down * 1.6f;
                boss.TakeDamage(boss.HealthFraction * 1e5f * 0.45f, false);
                yield return Doi(0.4f);
                Chup("dang-danh-boss", gs, hp, "thanh máu boss trên đỉnh màn hình");

                boss.TakeDamage(1e9f, false);
                yield return Doi(0.5f);
                Chup("man-cot-moc", gs, hp, $"hạ boss — {gs.Cores} Lõi, màn cột mốc");
                if (milestone != null && milestone.IsOpen)
                {
                    yield return new WaitForSecondsRealtime(0.8f);
                    milestone.Dong();
                }
                yield return Doi(0.3f);
            }

            // ── 9 · nhân vật mới đã mở ───────────────────────────────────────────
            if (chScreen != null)
            {
                chScreen.Toggle();
                yield return Doi(0.3f);
                Chup("nhan-vat-moi", gs, hp, $"hạ {gs.BossesKilled} boss — mở nhân vật");
                chScreen.Toggle();
            }

            // ── 10 · sang chương 2: bảng nền và loại quái đổi ────────────────────
            int moiChuong = BalanceConfig.Instance.GetInt("tower.floorsPerChapter");
            while (gs.Floor <= moiChuong + 4) gs.AdvanceFloor();
            EnemyRegistry.ClearAll();
            float t2 = 0f;
            while (EnemyRegistry.Count == 0 && t2 < 10f)
            { t2 += Time.unscaledDeltaTime; yield return null; }
            yield return Doi(0.4f);
            ctrl.transform.position = Vector3.zero;
            yield return null;
            Chup("chuong-2", gs, hp, $"tầng {gs.Floor} — bảng nền và quái của chương 2");

            _cam.targetTexture = null;
            Time.timeScale = 1f;
            Debug.Log("\n╔══ NHẬT KÝ PHIÊN CHƠI ═══════════════════════════\n" + _nhatKy);
            SaveSystem.Delete();

            Assert.Greater(_stt, 8, "phiên chơi phải đi được hết cung đường");
        }

        /// <summary>
        /// Chờ bằng giây THẬT, không phải giây game.
        ///
        /// MilestoneOverlay đặt Time.timeScale = 0 và chờ người chạm — đúng như thiết kế.
        /// Nhưng mọi vòng chờ đếm bằng Time.deltaTime sẽ ĐỨNG IM VĨNH VIỄN lúc đó, vì
        /// deltaTime bằng 0. Lần đầu nó treo bảng bấm giờ; lần này nó treo phiên chơi 900
        /// giây rồi mới chịu bỏ cuộc. Bản thân game KHÔNG sai — nó dừng là đúng ý — nhưng
        /// bất cứ thứ gì tự động chờ nó thì phải dùng đồng hồ không bị đóng băng.
        /// </summary>
        private IEnumerator Doi(float giay)
        {
            float moc = Time.unscaledTime;
            while (Time.unscaledTime - moc < giay) yield return null;
            yield return null;
        }

        private void Chup(string ten, GameState gs, PlayerHealth hp, string ghiChu)
        {
            _stt++;
            string file = $"{_stt:00}-{ten}";
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = _rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(OutDir, file + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
            RenderTexture.active = prev;

            _nhatKy.AppendLine($"  {file,-26} tầng {gs.Floor,2} · {gs.Shards,9:N0} Mảnh · " +
                               $"máu {hp.Fraction * 100,3:F0}% · {ghiChu}");
        }
    }
}
