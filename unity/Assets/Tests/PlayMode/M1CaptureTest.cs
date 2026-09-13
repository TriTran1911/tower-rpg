using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TowerRpg.Combat;
using TowerRpg.Core;
using TowerRpg.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TowerRpg.Tests
{
    /// <summary>
    /// Chạy scene M1 thật rồi chụp lại nhiều khung hình — để NHÌN thấy game chạy,
    /// không chỉ đọc test xanh.
    ///
    /// Nằm trong bộ test PlayMode vì đó là đường DUY NHẤT vào được Play mode không giao diện:
    /// gọi EditorApplication.EnterPlaymode() từ -executeMethod thì Unity treo.
    ///
    /// Ảnh ra ở anh-chup/ ngoài thư mục Assets (để Unity khỏi import lại).
    /// </summary>
    public class M1CaptureTest
    {
        private const int W = 540, H = 960;              // 9:16, đúng màn hình dọc
        private static readonly string OutDir =
            Path.GetFullPath(Path.Combine(Application.dataPath, "../../anh-chup"));

        private static readonly (float at, string name)[] Shots =
        {
            (0.3f, "1-vao-tran"),
            (1.5f, "2-dang-danh"),
            (3.0f, "3-giua-tran"),
            (5.0f, "4-sau-5s"),
            (8.0f, "5-sau-8s"),
        };

        [UnityTest]
        public IEnumerator Chup_anh_van_hanh()
        {
            Directory.CreateDirectory(OutDir);
            // Chỉ xoá ảnh CỦA MÌNH — dọn cả thư mục sẽ nuốt luôn ảnh ghép tay để trong đó.
            foreach ((float _, string n) in Shots)
            {
                string old = Path.Combine(OutDir, n + ".png");
                if (File.Exists(old)) File.Delete(old);
            }

            SceneManager.LoadScene("M1", LoadSceneMode.Single);
            yield return null; yield return null;

            float wait = 0f;
            while ((BalanceConfig.Instance == null || !BalanceConfig.Instance.IsLoaded) && wait < 10f)
            { wait += Time.deltaTime; yield return null; }
            yield return null;

            Assert.IsFalse(BalanceConfig.Instance.LoadFailed, "nạp số liệu thất bại");
            Assert.Greater(EnemyRegistry.Count, 0, "không có quái nào");

            Camera cam = Camera.main;
            Assert.IsNotNull(cam, "không có camera");

            var rt = new RenderTexture(W, H, 24);
            cam.targetTexture = rt;
            cam.aspect = (float)W / H;

            // Canvas Overlay không vẽ vào targetTexture — đổi sang ScreenSpaceCamera để chụp được UI
            foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                                       .Where(c => c.renderMode == RenderMode.ScreenSpaceOverlay))
            {
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = 1f;
            }

            // đưa người chơi vào sát quái — đứng giữa thì không có gì xảy ra (đúng thiết kế)
            var ctrl = Object.FindFirstObjectByType<PlayerController>();
            IDamageable near = EnemyRegistry.Nearest(Vector3.zero, 999f);
            float range = BalanceConfig.Instance.Get("player.attackRange");
            ctrl.transform.position = near.Position - near.Position.normalized * (range * 0.55f);

            var hp = Object.FindFirstObjectByType<PlayerHealth>();
            var meter = Object.FindFirstObjectByType<CritMeter>();

            float t = 0f;
            foreach ((float at, string name) in Shots)
            {
                while (t < at) { t += Time.deltaTime; yield return null; }
                // KHÔNG dùng WaitForEndOfFrame: batchmode không kích hoạt nó.
                // Camera có targetTexture nên khung hình đã vẽ xong ở frame trước.
                yield return null;

                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(OutDir, name + ".png"), tex.EncodeToPNG());
                Object.Destroy(tex);
                RenderTexture.active = prev;

                Debug.Log($"[chụp] {name}  t={t:0.0}s  quái={EnemyRegistry.Count}  " +
                          $"máu={hp.Fraction:0.00}  thanhCM={meter.Fill01:0.00}");
            }

            cam.targetTexture = null;
            Assert.AreEqual(Shots.Length, Directory.GetFiles(OutDir, "*.png").Length,
                            "thiếu ảnh");
        }
    }
}
