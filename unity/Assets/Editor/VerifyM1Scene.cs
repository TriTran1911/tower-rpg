using System.Collections.Generic;
using System.Linq;
using System.Text;
using TowerRpg.Core;
using TowerRpg.Enemies;
using TowerRpg.Juice;
using TowerRpg.Player;
using TowerRpg.Progression;
using TowerRpg.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TowerRpg.EditorTools
{
    /// <summary>
    /// Mở scene M1 và soi từng tham chiếu [SerializeField]. Dựng scene bằng mã vẫn có thể
    /// nối sai — script này là bước kiểm chứng, không phải trang trí.
    ///
    /// Unity -batchmode -quit -projectPath . -executeMethod TowerRpg.EditorTools.VerifyM1Scene.Verify
    /// </summary>
    public static class VerifyM1Scene
    {
        private const string ScenePath = "Assets/Scenes/M1.unity";

        [MenuItem("Tower RPG/Kiểm scene M1")]
        public static void Verify()
        {
            var log = new StringBuilder();
            int fail = 0;

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            log.AppendLine($"scene: {scene.name}  ({scene.rootCount} đối tượng gốc)");

            // ── cây đối tượng ──────────────────────────────────────────────────────────
            log.AppendLine("\nCÂY ĐỐI TƯỢNG");
            foreach (GameObject root in scene.GetRootGameObjects())
                Dump(root.transform, log, 1);

            // ── tham chiếu ─────────────────────────────────────────────────────────────
            log.AppendLine("\nTHAM CHIẾU");
            fail += Check<FloorRunner>(log, "enemyPrefab", "playerHealth", "bossSprite");
            fail += Check<HudUI>(log, "floorNumber", "shardCount", "coreCount", "coreGroup",
                                      "bossBanner", "bossBarRoot", "bossFill", "sweep", "sweepButton", "sweepLabel",
                                      "sweepFill", "auto", "autoButton", "autoLabel", "runner");
            fail += Check<UpgradeScreen>(log, "root", "rowParent", "shardLabel",
                                              "panelSprite", "bgSprite", "cellSprite",
                                              "coreLabel", "respecButton", "respecLabel");
            fail += Check<CharacterScreen>(log, "root", "cardParent", "hintLabel",
                                                "panelSprite", "bgSprite", "cellSprite");
            fail += Check<AutoBattle>(log, "player", "joystick");
            fail += Check<PlayerAppearance>(log, "target");
            fail += Check<DamagePopupSpawner>(log, "popupPrefab");
            fail += Check<PlayerController>(log, "joystick");
            fail += Check<AutoAttack>(log, "player", "critMeter", "popups", "cameraShake");
            fail += Check<CritMeterUI>(log, "meter", "segmentRoot", "segmentPrefab");
            fail += Check<PlayerHealthUI>(log, "health", "fillImage");
            fail += Check<VirtualJoystick>(log, "touchZone", "visual", "handle", "canvas");
            fail += Check<CameraShake>(log, "target");

            // ── những thứ dễ dựng sai ──────────────────────────────────────────────────
            log.AppendLine("\nKIỂM RIÊNG");

            var joy = Object.FindFirstObjectByType<VirtualJoystick>();
            // Lấy qua THAM CHIẾU chứ không GameObject.Find — Find bỏ qua đối tượng đang tắt,
            // mà cần gạt thì đúng ra PHẢI tắt lúc chưa chạm.
            RectTransform joyVis = null;
            if (joy != null)
            {
                var jso = new SerializedObject(joy);
                joyVis = jso.FindProperty("visual")?.objectReferenceValue as RectTransform;
            }
            fail += Assert(log, "phần nhìn của cần gạt có kích thước thật",
                           joyVis != null && joyVis.rect.width > 1f,
                           joyVis == null ? "không có" : $"rect.width = {joyVis.rect.width}");
            fail += Assert(log, "cần gạt ẩn khi chưa chạm",
                           joyVis != null && !joyVis.gameObject.activeSelf,
                           joyVis == null ? "không có" : "đang hiện");

            var cv = Object.FindFirstObjectByType<CanvasScaler>();
            fail += Assert(log, "Canvas referencePixelsPerUnit = 64 (phóng 4x nguyên)",
                           cv != null && Mathf.Approximately(cv.referencePixelsPerUnit, 64f),
                           cv == null ? "không có" : cv.referencePixelsPerUnit.ToString());

            int sliced = Object.FindObjectsByType<Image>(FindObjectsSortMode.None)
                               .Count(i => i.sprite != null && i.type == Image.Type.Sliced);
            fail += Assert(log, "có ảnh 9-patch dùng sprite thật", sliced > 0, $"{sliced} ảnh");

            var rb = Object.FindFirstObjectByType<PlayerController>()?.GetComponent<Rigidbody2D>();
            fail += Assert(log, "Rigidbody2D: gravityScale = 0",
                           rb != null && Mathf.Approximately(rb.gravityScale, 0f),
                           rb == null ? "không có" : $"{rb.gravityScale}");
            fail += Assert(log, "Rigidbody2D: khoá xoay",
                           rb != null && (rb.constraints & RigidbodyConstraints2D.FreezeRotation) != 0,
                           rb == null ? "không có" : $"{rb.constraints}");

            foreach (Image img in Object.FindObjectsByType<Image>(FindObjectsSortMode.None)
                                        .Where(i => i.name is "HealthBar"))
                fail += Assert(log, $"{img.name}: Image.Type = Filled",
                               img.type == Image.Type.Filled, img.type.ToString());

            var cam = Camera.main;
            fail += Assert(log, "camera trực giao", cam != null && cam.orthographic,
                           cam == null ? "không có" : cam.orthographic.ToString());

            var sr = GameObject.Find("Floor")?.GetComponent<SpriteRenderer>();
            fail += Assert(log, "sàn: sprite + chế độ Tiled",
                           sr != null && sr.sprite != null && sr.drawMode == SpriteDrawMode.Tiled,
                           sr == null ? "không có Floor" : $"sprite={sr.sprite?.name} mode={sr.drawMode}");

            var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab");
            fail += Assert(log, "prefab Enemy có SpriteRenderer + sprite",
                           enemyPrefab != null && enemyPrefab.GetComponent<SpriteRenderer>()?.sprite != null,
                           enemyPrefab == null ? "không có" : $"{enemyPrefab.GetComponent<SpriteRenderer>()?.sprite?.name}");

            var bar = enemyPrefab != null ? enemyPrefab.GetComponent<EnemyHealthBar>() : null;
            var barSo = bar != null ? new SerializedObject(bar) : null;
            bool barOk = barSo != null &&
                         barSo.FindProperty("background").objectReferenceValue != null &&
                         barSo.FindProperty("fill").objectReferenceValue != null;
            fail += Assert(log, "prefab Enemy có thanh máu đủ 2 phần", barOk,
                           bar == null ? "không có EnemyHealthBar" : "thiếu background hoặc fill");

            // Thanh máu phải vẽ TRÊN sprite quái, nếu không nó nằm sau lưng quái và vô hình.
            var fillSr = barSo?.FindProperty("fill").objectReferenceValue as SpriteRenderer;
            int enemyOrder = enemyPrefab != null
                           ? enemyPrefab.GetComponent<SpriteRenderer>().sortingOrder : 0;
            fail += Assert(log, "thanh máu vẽ trên sprite quái",
                           fillSr != null && fillSr.sortingOrder > enemyOrder,
                           fillSr == null ? "không có" : $"{fillSr.sortingOrder} <= {enemyOrder}");

            // "Có vẽ" KHÁC "nhìn thấy được". Thanh từng render đúng mà chỉ rộng 18px vì một
            // AssetPostprocessor ép PPU 16 lên sprite trắng — mọi kiểm tra khác đều xanh.
            // Đo bề rộng THẬT theo đơn vị thế giới và so với chính con quái.
            float enemyW = enemyPrefab != null
                         ? enemyPrefab.GetComponent<SpriteRenderer>().bounds.size.x : 0f;
            float barW = fillSr != null ? fillSr.bounds.size.x : 0f;
            fail += Assert(log, "thanh máu rộng ít nhất 60% bề ngang con quái",
                           enemyW > 0f && barW >= enemyW * 0.6f,
                           $"thanh {barW:0.00} đơn vị, quái {enemyW:0.00} đơn vị");

            var popupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/DamagePopup.prefab");
            fail += Assert(log, "prefab DamagePopup có TMP_Text",
                           popupPrefab != null && popupPrefab.GetComponent<TMPro.TMP_Text>() != null,
                           popupPrefab == null ? "không có" : "ok");

            bool csv = System.IO.File.Exists("Assets/StreamingAssets/m1-balance.csv");
            fail += Assert(log, "m1-balance.csv có trong StreamingAssets", csv, csv ? "có" : "THIẾU");

            fail += Assert(log, "có GameState trong scene",
                           Object.FindFirstObjectByType<GameState>() != null, "không có");
            fail += Assert(log, "có PlayerStats trong scene",
                           Object.FindFirstObjectByType<PlayerStats>() != null, "không có");

            var up = Object.FindFirstObjectByType<UpgradeScreen>();
            var upSo = up != null ? new SerializedObject(up) : null;
            SerializedProperty icons = upSo?.FindProperty("slotIcons");
            int iconCount = 0;
            if (icons != null)
                for (int i = 0; i < icons.arraySize; i++)
                    if (icons.GetArrayElementAtIndex(i).objectReferenceValue != null) iconCount++;
            fail += Assert(log, "đủ 4 icon trang bị", iconCount == 4, $"{iconCount}/4");

            fail += Assert(log, "màn nâng cấp đóng lúc bắt đầu",
                           up != null && !(upSo.FindProperty("root").objectReferenceValue as GameObject).activeSelf,
                           "đang mở");

            // ── M3 ────────────────────────────────────────────────────────────────────
            var ch = Object.FindFirstObjectByType<CharacterScreen>();
            var chSo = ch != null ? new SerializedObject(ch) : null;
            fail += Assert(log, "màn nhân vật đóng lúc bắt đầu",
                           chSo != null &&
                           !(chSo.FindProperty("root").objectReferenceValue as GameObject).activeSelf,
                           "đang mở");

            SerializedProperty faces = chSo?.FindProperty("portraits");
            int faceCount = 0;
            if (faces != null)
                for (int i = 0; i < faces.arraySize; i++)
                    if (faces.GetArrayElementAtIndex(i).objectReferenceValue != null) faceCount++;
            fail += Assert(log, $"đủ {CharacterRoster.Count} chân dung nhân vật",
                           faceCount == CharacterRoster.Count,
                           $"{faceCount}/{CharacterRoster.Count}");

            var look = Object.FindFirstObjectByType<PlayerAppearance>();
            var lookSo = look != null ? new SerializedObject(look) : null;
            SerializedProperty looks = lookSo?.FindProperty("sprites");
            int lookCount = 0;
            if (looks != null)
                for (int i = 0; i < looks.arraySize; i++)
                    if (looks.GetArrayElementAtIndex(i).objectReferenceValue != null) lookCount++;
            fail += Assert(log, $"đủ {CharacterRoster.Count} hình người chơi",
                           lookCount == CharacterRoster.Count,
                           $"{lookCount}/{CharacterRoster.Count}");

            fail += Assert(log, "có SweepRunner trong scene",
                           Object.FindFirstObjectByType<SweepRunner>() != null, "không có");

            // Âm thanh: thiếu MỘT clip là im lặng ở đúng chỗ đó, và không gì báo cho biết —
            // SfxPlayer.Play cố ý nuốt lỗi để âm thanh không bao giờ làm hỏng một trận đấu.
            var sfxPlayer = Object.FindFirstObjectByType<SfxPlayer>();
            int clipCount = 0;
            if (sfxPlayer != null)
                for (int i = 0; i < SfxPlayer.SfxCount; i++)
                    if (sfxPlayer.ClipAt(i) != null) clipCount++;
            fail += Assert(log, $"đủ {SfxPlayer.SfxCount} clip âm thanh",
                           clipCount == SfxPlayer.SfxCount,
                           sfxPlayer == null ? "không có SfxPlayer"
                                             : $"{clipCount}/{SfxPlayer.SfxCount}");

            // Mono + nén trong RAM: mặc định của Unity là giải nén sẵn, phình RAM trên máy thật.
            int badImport = 0;
            if (sfxPlayer != null)
                for (int i = 0; i < SfxPlayer.SfxCount; i++)
                {
                    AudioClip clip = sfxPlayer.ClipAt(i);
                    if (clip == null) continue;
                    if (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip)) is not AudioImporter ai)
                        continue;
                    if (!ai.forceToMono ||
                        ai.defaultSampleSettings.loadType != AudioClipLoadType.CompressedInMemory)
                        badImport++;
                }
            fail += Assert(log, "clip âm thanh: mono + nén trong RAM",
                           badImport == 0, $"{badImport} clip sai thiết lập");

            // Nút phải BẤM ĐƯỢC. Vùng chạm cần gạt trong suốt và phủ nửa dưới màn hình;
            // nếu nó là anh em SAU một cái nút thì nó nuốt cú chạm, nút thành vô dụng.
            // Kiểm thứ tự anh em chứ KHÔNG kiểm hình học: hình học phụ thuộc tỉ lệ màn
            // hình thật (batchmode chạy ở độ phân giải khác hẳn 1080x1920), còn thứ tự
            // anh em thì đúng ở mọi máy.
            var zone = Object.FindFirstObjectByType<VirtualJoystick>();
            RectTransform zoneRt = null;
            if (zone != null)
                zoneRt = new SerializedObject(zone).FindProperty("touchZone")
                             .objectReferenceValue as RectTransform;

            int zoneOrder = zoneRt != null ? zoneRt.GetSiblingIndex() : -1;
            foreach (string n in new[] { "GearButton", "CharButton", "SweepButton", "AutoButton" })
            {
                RectTransform b = FindRect(scene, n);
                bool ok = b != null && zoneRt != null &&
                          b.parent == zoneRt.parent && b.GetSiblingIndex() > zoneOrder;
                fail += Assert(log, $"{n} nằm TRÊN vùng chạm cần gạt (bấm được)", ok,
                               b == null ? "không thấy nút"
                                         : $"thứ tự {b.GetSiblingIndex()} <= vùng chạm {zoneOrder}");
            }

            log.AppendLine(fail == 0
                ? "\n===== TẤT CẢ ĐỀU ĐẠT ====="
                : $"\n===== {fail} MỤC KHÔNG ĐẠT =====");
            Debug.Log("[VerifyM1Scene]\n" + log);

            if (fail > 0) EditorApplication.Exit(1);
        }

        /// <summary>Tìm theo tên kể cả đối tượng đang tắt — GameObject.Find bỏ qua chúng.</summary>
        private static RectTransform FindRect(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (RectTransform rt in root.GetComponentsInChildren<RectTransform>(true))
                    if (rt.name == name) return rt;
            return null;
        }

        private static bool Overlaps(RectTransform a, RectTransform b)
        {
            var ca = new Vector3[4]; var cb = new Vector3[4];
            a.GetWorldCorners(ca); b.GetWorldCorners(cb);
            var ra = new Rect(ca[0].x, ca[0].y, ca[2].x - ca[0].x, ca[2].y - ca[0].y);
            var rb = new Rect(cb[0].x, cb[0].y, cb[2].x - cb[0].x, cb[2].y - cb[0].y);
            return ra.Overlaps(rb);
        }

        private static void Dump(Transform t, StringBuilder log, int depth)
        {
            string comps = string.Join(", ", t.GetComponents<Component>()
                                              .Where(c => c != null && c is not Transform)
                                              .Select(c => c.GetType().Name));
            log.AppendLine($"  {new string(' ', depth * 2)}{t.name}" + (comps.Length > 0 ? $"  [{comps}]" : ""));
            foreach (Transform c in t) Dump(c, log, depth + 1);
        }

        private static int Check<T>(StringBuilder log, params string[] fields) where T : Component
        {
            var c = Object.FindFirstObjectByType<T>();
            if (c == null)
            {
                log.AppendLine($"  ✗ {typeof(T).Name}: KHÔNG CÓ trong scene");
                return 1;
            }

            var so = new SerializedObject(c);
            var missing = new List<string>();
            foreach (string f in fields)
            {
                SerializedProperty p = so.FindProperty(f);
                if (p == null) missing.Add($"{f}(không có trường)");
                else if (p.objectReferenceValue == null) missing.Add(f);
            }

            if (missing.Count == 0) { log.AppendLine($"  ✓ {typeof(T).Name}: {fields.Length}/{fields.Length}"); return 0; }
            log.AppendLine($"  ✗ {typeof(T).Name}: thiếu {string.Join(", ", missing)}");
            return 1;
        }

        private static int Assert(StringBuilder log, string what, bool ok, string actual)
        {
            log.AppendLine($"  {(ok ? "✓" : "✗")} {what}" + (ok ? "" : $"  -> {actual}"));
            return ok ? 0 : 1;
        }
    }
}
