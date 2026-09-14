using System.Collections.Generic;
using System.Linq;
using System.Text;
using TowerRpg.Core;
using TowerRpg.Enemies;
using TowerRpg.Juice;
using TowerRpg.Loot;
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
            fail += Check<FloorRunner>(log, "enemyPrefab", "playerHealth", "drops", "floorRenderer",
                                            "transition");
            fail += Check<ShardDropSpawner>(log, "pickupPrefab");
            fail += Check<HudUI>(log, "floorNumber", "shardCount", "coreCount", "coreGroup",
                                      "bossBanner", "bossBarRoot", "bossFill", "sweep", "sweepButton", "sweepLabel",
                                      "sweepFill", "auto", "autoButton", "autoLabel", "runner",
                                      "eventBanner", "eventBannerRoot", "cameraShake",
                                      "gearButton", "gearLabel", "charButton", "charLabel",
                                      "upgrade", "character", "milestone", "victory");
            fail += Check<UpgradeScreen>(log, "root", "rowParent", "shardLabel",
                                              "panelSprite", "bgSprite", "cellSprite",
                                              "coreLabel", "respecButton", "respecLabel",
                                              "hudInfo", "auto", "closeButton");
            fail += Check<CharacterScreen>(log, "root", "cardParent", "hintLabel",
                                                "panelSprite", "bgSprite", "cellSprite",
                                                "hudInfo", "closeButton");
            fail += Check<AutoBattle>(log, "player", "joystick");
            fail += Check<PlayerAnimator>(log, "target", "controller");
            fail += Check<SlashFxSpawner>(log, "prefab");
            fail += Check<AudioDirector>(log, "normalTrack", "bossTrack", "runner");
            fail += Check<MilestoneOverlay>(log, "root", "title", "detail", "hint", "shake");
            fail += Check<VictoryScreen>(log, "root", "title", "stats", "hint", "shake", "endTheme");
            fail += Check<SceneTransition>(log, "veil", "chapterNo", "chapterName", "chapterRange");
            fail += Check<PuffFxSpawner>(log, "prefab");
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

            // ── M5 ────────────────────────────────────────────────────────────────────
            // Tấm phủ chuyển cảnh KHÔNG ĐƯỢC ăn chạm. Nó phủ kín màn hình và sống suốt
            // cả lượt chơi; để raycastTarget = true là nuốt mọi cú chạm, kể cả lúc trong
            // suốt — đúng kiểu lỗi mà vùng chạm cần gạt đã gây ra một lần ở M2.
            var trans = Object.FindFirstObjectByType<SceneTransition>();
            Image veilImg = null;
            if (trans != null)
                veilImg = new SerializedObject(trans).FindProperty("veil").objectReferenceValue as Image;
            fail += Assert(log, "tấm phủ chuyển cảnh không chặn chạm",
                           veilImg != null && !veilImg.raycastTarget,
                           veilImg == null ? "không có" : "raycastTarget đang bật");
            fail += Assert(log, "tấm phủ chuyển cảnh bắt đầu trong suốt",
                           veilImg != null && veilImg.color.a < 0.01f,
                           veilImg == null ? "không có" : $"alpha = {veilImg.color.a}");
            fail += Assert(log, "tấm phủ chuyển cảnh phủ kín màn hình",
                           veilImg != null && veilImg.rectTransform.anchorMin == Vector2.zero
                                           && veilImg.rectTransform.anchorMax == Vector2.one,
                           veilImg == null ? "không có" : "không neo bốn góc");

            // Màn đỉnh tháp PHẢI lưu ở trạng thái TẮT: bật sẵn là nó che màn hình ngay
            // khung hình đầu tiên của trò chơi, trước cả khi ai leo được tầng nào.
            var vic = Object.FindFirstObjectByType<VictoryScreen>();
            GameObject vicRoot = vic != null
                ? new SerializedObject(vic).FindProperty("root").objectReferenceValue as GameObject : null;
            fail += Assert(log, "màn đỉnh tháp lưu ở trạng thái tắt",
                           vicRoot != null && !vicRoot.activeSelf,
                           vicRoot == null ? "không có" : "đang bật");

            AudioSource endSrc = vic != null
                ? new SerializedObject(vic).FindProperty("endTheme").objectReferenceValue as AudioSource : null;
            fail += Assert(log, "nhạc kết có clip và KHÔNG lặp",
                           endSrc != null && endSrc.clip != null && !endSrc.loop && !endSrc.playOnAwake,
                           endSrc == null ? "không có" : $"clip={endSrc.clip?.name} loop={endSrc.loop}");

            // Prefab khói: sáu khung, không khung nào rỗng. Thiếu một khung là hoạt ảnh
            // nhảy cóc mà không ai báo lỗi.
            var puffPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PuffFx.prefab");
            var puffFx = puffPrefab != null ? puffPrefab.GetComponent<PuffFx>() : null;
            int khungDu = 0;
            if (puffFx != null)
            {
                SerializedProperty khungKhoi = new SerializedObject(puffFx).FindProperty("frames");
                for (int i = 0; i < khungKhoi.arraySize; i++)
                    if (khungKhoi.GetArrayElementAtIndex(i).objectReferenceValue != null) khungDu++;
            }
            fail += Assert(log, "prefab khói đủ 6 khung hình", khungDu == 6, $"{khungDu}/6");

            // Banner phải TỰ CO. Không có nó thì mọi câu dài hơn tấm gỗ 736px đều tràn
            // ra sàn — âm thầm, không lỗi, chỉ nhìn ảnh chụp mới thấy.
            var hudM5 = Object.FindFirstObjectByType<HudUI>();
            TMPro.TMP_Text bannerTxt = hudM5 != null
                ? new SerializedObject(hudM5).FindProperty("eventBanner").objectReferenceValue as TMPro.TMP_Text
                : null;
            fail += Assert(log, "chữ banner tự co cho vừa tấm gỗ",
                           bannerTxt != null && bannerTxt.enableAutoSizing && bannerTxt.fontSizeMin >= 20f,
                           bannerTxt == null ? "không có"
                                             : $"autoSize={bannerTxt.enableAutoSizing} min={bannerTxt.fontSizeMin}");

            // Màn phủ của màn đỉnh tháp phải ĐỤC HẲN. Ở alpha 0,97 đo được vẫn còn 17%
            // tín hiệu sRGB lọt qua — pha trộn làm trong không gian tuyến tính rồi mới
            // mã hoá, nên "gần 1" không hề gần đục.
            Image vcDimImg = null;
            if (vic != null)
                foreach (Image i in vic.GetComponentsInChildren<Image>(true))
                    if (i.name == "Dim") vcDimImg = i;
            fail += Assert(log, "màn đỉnh tháp che kín (alpha = 1)",
                           vcDimImg != null && vcDimImg.color.a >= 0.999f,
                           vcDimImg == null ? "không có" : $"alpha = {vcDimImg.color.a}");

            // ── BA TẦNG ĐỌC (quyết định #23) ─────────────────────────────────────────
            fail += KiemBaTangDoc(log);

            // Cụm HUD sống phải là một hộp RIÊNG, đủ bốn khung, và KHÔNG ăn chạm —
            // nó trải kín màn hình, ăn raycast là nuốt sạch mọi cú chạm của trò chơi.
            var manNang = Object.FindFirstObjectByType<UpgradeScreen>();
            RectTransform hudBox = manNang != null
                ? new SerializedObject(manNang).FindProperty("hudInfo").objectReferenceValue as RectTransform
                : null;
            fail += Assert(log, "cụm HUD sống không ăn chạm",
                           hudBox != null && hudBox.GetComponent<Graphic>() == null,
                           hudBox == null ? "không có" : "có Graphic nên chặn chạm");
            int duKhung = 0;
            if (hudBox != null)
                foreach (Transform t in hudBox)
                    if (t.name is "FloorPanel" or "HealthFrame" or "ShardPanel" or "CorePanel") duKhung++;
            fail += Assert(log, "cụm HUD sống đủ 4 khung (tầng·máu·Mảnh·Lõi)",
                           duKhung == 4, $"{duKhung}/4");

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

            var shardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ShardPickup.prefab");
            var shardSr = shardPrefab != null ? shardPrefab.GetComponent<SpriteRenderer>() : null;
            fail += Assert(log, "prefab viên Mảnh có sprite",
                           shardSr != null && shardSr.sprite != null,
                           shardPrefab == null ? "không có prefab" : "thiếu sprite");

            // Cùng bài học với thanh máu: "có sprite" KHÁC "nhìn thấy được". Một
            // AssetPostprocessor hay một PPU sai là nó teo còn vài pixel mà mọi kiểm
            // tra khác vẫn xanh. Đo bề rộng THẬT theo đơn vị thế giới.
            float shardW = shardSr != null ? shardSr.bounds.size.x : 0f;
            fail += Assert(log, "viên Mảnh to nhìn thấy được (>= 0,5 đơn vị)",
                           shardW >= 0.5f, $"{shardW:0.00} đơn vị");

            fail += Assert(log, "viên Mảnh vẽ trên sàn và trên quái",
                           shardSr != null && shardSr.sortingOrder > 5,
                           shardSr == null ? "không có" : $"{shardSr.sortingOrder}");

            // Viên Mảnh phải NỔI trên nền sàn. Sprite gốc tên "GemYellow" nhưng bảng màu
            // Mực & Son remap nó thành xám — trùng tông với sàn, và không kiểm tra nào
            // khác phát hiện được vì nó vẫn render đúng, vẫn đủ to, vẫn đúng thứ tự vẽ.
            var floorSr2 = GameObject.Find("Floor")?.GetComponent<SpriteRenderer>();
            bool warmEnough = shardSr != null &&
                              shardSr.color.r > shardSr.color.b + 0.2f;   // ngả ấm rõ rệt
            fail += Assert(log, "viên Mảnh nhuộm tông ấm để không hoà vào sàn", warmEnough,
                           shardSr == null ? "không có" : $"màu {shardSr.color}");

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


            // Hoạt ảnh: 3 bộ x 5 nhân vật x 4 hướng = 60 sprite. Thiếu một cái là nhân
            // vật đứng hình ở đúng hướng đó và không gì báo cho biết.
            var pAnim = Object.FindFirstObjectByType<PlayerAnimator>();
            var animSo2 = pAnim != null ? new SerializedObject(pAnim) : null;
            int animCo = 0, animCan = 0;
            if (animSo2 != null)
                foreach (string bo in new[] { "idle", "walk", "attack" })
                {
                    SerializedProperty arr = animSo2.FindProperty(bo);
                    if (arr == null) continue;
                    animCan += arr.arraySize;
                    for (int i = 0; i < arr.arraySize; i++)
                        if (arr.GetArrayElementAtIndex(i).objectReferenceValue != null) animCo++;
                }
            fail += Assert(log, "đủ sprite hoạt ảnh nhân vật (3 bộ x 5 x 4 hướng)",
                           animCan > 0 && animCo == animCan, $"{animCo}/{animCan}");

            // M4: thiếu một hình boss là tầng đó hiện con quái thường phóng to, và không
            // gì báo cho biết. Thiếu một bảng nền là cả 20 tầng của chương đó dùng nền cũ.
            var fr = Object.FindFirstObjectByType<FloorRunner>();
            var frSo = fr != null ? new SerializedObject(fr) : null;
            foreach ((string ten, int can) in new[]
                     { ("bossSprites", 10), ("chapterFloors", 5), ("chapterEnemies", 5) })
            {
                SerializedProperty arr = frSo?.FindProperty(ten);
                int co = 0;
                if (arr != null)
                    for (int i = 0; i < arr.arraySize; i++)
                        if (arr.GetArrayElementAtIndex(i).objectReferenceValue != null) co++;
                fail += Assert(log, $"đủ {can} {ten}", co == can, $"{co}/{can}");
            }

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

        // ─────────────────────────────────────────────────────────────────────────────
        // BA TẦNG ĐỌC — thế giới xám · quái ĐỎ · nhân vật LAM (quyết định #23, §5.5b)
        //
        // VÌ SAO KIỂM Ở ĐÂY chứ không ở test PlayMode: đây là tính chất của FILE ART,
        // không phải hành vi lúc chạy. Và texture trong bản build cố ý KHÔNG bật
        // Read/Write (bật là nhân đôi RAM texture trên máy yếu), nên GetPixels32 lúc
        // chạy sẽ ném lỗi. Trong Editor thì đọc thẳng byte của file PNG là xong.
        //
        // VÌ SAO PHẢI CÓ: quy tắc lam ĐÃ TỒN TẠI trong tools/doi-bang-mau.py từ đầu,
        // và đã TỰ TẮT TRONG IM LẶNG suốt từ M3 — rule_for() chỉ tô cho
        // Actor/CharacterAnimated, còn M3 đổi nguồn nhân vật sang Actor/Character.
        // Không lớp kiểm nào bắt được: mã đúng, tham chiếu đủ, 82 test xanh, ảnh chụp
        // vẫn ra một nhân vật — chỉ là nhân vật đó cùng màu với cái sàn.
        // ─────────────────────────────────────────────────────────────────────────────

        private static bool LaLam(Color32 c) => c.g - c.r > 40 && c.b - c.r > 30;
        private static bool LaDo(Color32 c)  => c.r - c.g > 40 && c.r - c.b > 40;

        /// <summary>Pixel thân: đục và không phải viền tối (viền thì sắc nào cũng tối).</summary>
        private static List<Color32> ThanPng(string assetPath, int cell = 16)
        {
            var ra = new List<Color32>();
            string full = System.IO.Path.GetFullPath(assetPath);
            if (!System.IO.File.Exists(full)) return ra;

            var tex = new Texture2D(2, 2);
            if (!ImageConversion.LoadImage(tex, System.IO.File.ReadAllBytes(full))) return ra;

            // Chỉ ô đầu tiên — đó là khung game thật sự bày ra (SliceAndGet index 0).
            int w = Mathf.Min(cell, tex.width), h = Mathf.Min(cell, tex.height);
            Color32[] all = tex.GetPixels32();
            for (int y = tex.height - h; y < tex.height; y++)
                for (int x = 0; x < w; x++)
                {
                    Color32 c = all[y * tex.width + x];
                    if (c.a > 200 && c.r + c.g + c.b > 150) ra.Add(c);
                }
            Object.DestroyImmediate(tex);
            return ra;
        }

        private static int TyLe(List<Color32> px, System.Func<Color32, bool> hop) =>
            px.Count == 0 ? -1 : px.FindAll(c => hop(c)).Count * 100 / px.Count;

        private static int KiemBaTangDoc(StringBuilder log)
        {
            const string Art = "Assets/Art/NinjaAdventure-MucSon";
            int fail = 0;

            // NHÂN VẬT — cả năm, vì §5.5b cho đổi. Tô đúng mỗi người đầu thì người chơi
            // mở khoá xong, đổi sang người thứ hai, và tự tắt mất tầng đọc thứ ba.
            foreach (string ten in Progression.CharacterRoster.ArtFolders)
            {
                var px = ThanPng($"{Art}/Actor/Character/{ten}/SpriteSheet.png");
                int t = TyLe(px, LaLam);
                fail += Assert(log, $"nhân vật {ten}: thân LAM",
                               t >= 60,
                               t < 0 ? "không đọc được file"
                                     : $"chỉ {t}% pixel thân đạt sắc lam — kiểm rule_for() "
                                       + "trong tools/doi-bang-mau.py có tô cho Actor/Character không");
            }

            // QUÁI — năm loại của năm chương. Hai tầng đọc chỉ có nghĩa khi CẢ HAI đúng.
            foreach (string ten in new[] { "Skull", "Bamboo", "Flam", "BlueBat", "Spirit" })
            {
                var px = ThanPng($"{Art}/Actor/Monster/{ten}/SpriteSheet.png");
                int t = TyLe(px, LaDo);
                fail += Assert(log, $"quái {ten}: thân ĐỎ", t >= 60,
                               t < 0 ? "không đọc được file" : $"chỉ {t}% đạt sắc đỏ");
            }

            return fail;
        }

    }
}
