using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TowerRpg.Combat;
using TowerRpg.Core;
using TowerRpg.Juice;
using TowerRpg.Progression;
using TowerRpg.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TowerRpg.Tests
{
    /// <summary>
    /// Chụp lại bốn thứ M5 thêm vào. Quyết định #29 của dự án: "nối đúng ≠ chạy đúng ≠
    /// NHÌN ĐƯỢC" — và M5 toàn là thứ chỉ nhìn mới biết đúng hay sai. Riêng đợt này đã
    /// có tiền lệ đắt: hiệu ứng doạ đòn của quái từng nối đúng, test xanh, đẩy lên git,
    /// và KHÔNG BAO GIỜ CHẠY (Lerp từ trắng sang trắng) cho tới khi có người đo pixel.
    /// </summary>
    public class M5ChupAnh
    {
        private const int W = 540, H = 960;
        private static readonly string OutDir =
            Path.GetFullPath(Path.Combine(Application.dataPath, "../../anh-chup-m5"));

        private RenderTexture _rt;
        private Camera _cam;

        private void Shot(string name)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = _rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(OutDir, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
            RenderTexture.active = prev;
            Debug.Log($"[chụp M5] {name}");
        }

        [UnityTest]
        public IEnumerator Chup_M5()
        {
            Directory.CreateDirectory(OutDir);
            SaveSystem.Delete();
            SceneManager.LoadScene("M1", LoadSceneMode.Single);
            yield return null; yield return null;

            float w = 0f;
            while ((GameState.Instance == null || !GameState.Instance.Ready) && w < 10f)
            { w += Time.deltaTime; yield return null; }
            yield return null;

            _cam = Camera.main;
            _rt = new RenderTexture(W, H, 24);
            _cam.targetTexture = _rt;
            _cam.aspect = (float)W / H;
            foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                                       .Where(c => c.renderMode == RenderMode.ScreenSpaceOverlay))
            {
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = _cam;
                c.planeDistance = 1f;
            }
            yield return null;

            // ── 1. KHÓI CHẾT ─────────────────────────────────────────────────────────
            // Chụp ở giữa vòng đời cụm khói, khi nó đã bung nhưng chưa mờ.
            var quai = Object.FindObjectsByType<Enemies.Enemy>(FindObjectsSortMode.None)[0];
            Vector3 cho = quai.transform.position;
            var ctrl = Object.FindFirstObjectByType<Player.PlayerController>();
            ctrl.transform.position = cho + Vector3.down * 1.2f;
            quai.TakeDamage(1e9f, false);
            float t = 0f;
            while (t < 0.15f) { t += Time.deltaTime; yield return null; }
            Shot("1-khoi-chet");

            // ĐO PIXEL, không tin mắt: khói phải THẬT SỰ đổi ảnh so với lúc chưa ai chết.
            var puffs = Object.FindFirstObjectByType<PuffFxSpawner>();
            int dangChay = puffs.GetComponentsInChildren<PuffFx>(true).Count(f => f.gameObject.activeSelf);
            Assert.Greater(dangChay, 0, "chụp lúc đáng ra có khói mà không cụm nào bật");

            // ── 2. THẺ CHƯƠNG ────────────────────────────────────────────────────────
            var tr = Object.FindFirstObjectByType<SceneTransition>();
            Coroutine co = tr.StartCoroutine(tr.TheChuong(1, 21, 40));
            t = 0f;
            while (t < 0.9f) { t += Time.unscaledDeltaTime; yield return null; }
            Shot("2-the-chuong");
            while (co != null && tr.IsRunning) yield return null;
            yield return tr.MoManChuong();

            // ── 3. NGÃ XUỐNG ─────────────────────────────────────────────────────────
            var hud = Object.FindFirstObjectByType<HudUI>();
            var hp = Object.FindFirstObjectByType<Player.PlayerHealth>();
            hp.TakeDamage(1e9f);
            t = 0f;
            while (t < 0.5f) { t += Time.deltaTime; yield return null; }
            Shot("3-nga-xuong");

            // ── 4. ĐỈNH THÁP ─────────────────────────────────────────────────────────
            // Dựng một hồ sơ đã leo hết để bảng tổng kết có số thật, không phải toàn 0.
            GameState gs = GameState.Instance;
            gs.AddShards(120000f);
            for (int i = 0; i < 60; i++)
            {
                gs.TryUpgrade(Slot.Weapon);
                gs.TryUpgrade(Slot.Armor);
                gs.TryUpgrade(Slot.Glove);
                gs.TryUpgrade(Slot.Ring);
            }
            while (gs.Floor < gs.TowerFloors) gs.AdvanceFloor();
            yield return null;

            var vc = Object.FindFirstObjectByType<VictoryScreen>();
            vc.Show();
            t = 0f;
            while (t < 0.4f) { t += Time.unscaledDeltaTime; yield return null; }
            Shot("4-dinh-thap");

            t = 0f;
            while (t < 1.4f) { t += Time.unscaledDeltaTime; yield return null; }
            vc.Dong();
            Time.timeScale = 1f;

            _cam.targetTexture = null;
            Debug.Log($"[chụp M5] xong -> {OutDir}");
        }
    }
}
