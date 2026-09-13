using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
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
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TowerRpg.EditorTools
{
    /// <summary>
    /// Dựng toàn bộ scene M1 bằng mã. Chạy lại được nhiều lần — luôn ghi đè Assets/Scenes/M1.unity.
    ///
    /// VÌ SAO DỰNG BẰNG MÃ: file .unity là YAML đầy GUID và fileID tham chiếu chéo; viết tay
    /// gần như chắc chắn hỏng. Dựng bằng mã thì kiểm chứng được và tái lập được.
    ///
    /// Chạy trong editor:  menu  Tower RPG ▸ Dựng scene M1
    /// Chạy không giao diện:
    ///   Unity -batchmode -quit -projectPath . -executeMethod TowerRpg.EditorTools.BuildM1Scene.Build
    /// </summary>
    public static class BuildM1Scene
    {
        private const string Art     = "Assets/Art/NinjaAdventure-MucSon";
        private const string Chapter = "Assets/Art/Chapters/1-nen-da";
        private const string ScenePath  = "Assets/Scenes/M1.unity";
        private const string PrefabDir  = "Assets/Prefabs";

        private const int Cell = 16;              // kích thước ô của bộ asset

        // Hệ thiết kế giao diện — docs/GIAO-DIEN.md §2
        private const int   UiRefW = 1080, UiRefH = 1920;  // độ phân giải thiết kế
        private const float UiPpu  = 64f;   // sprite PPU 16 × 64/16 = phóng ĐÚNG 4x nguyên
        private const int   Unit   = 32;    // đơn vị khoảng cách
        private const int   Edge   = 64;    // lề an toàn
        private const int   Touch  = 144;   // vùng chạm tối thiểu (Android 48dp ≈ 132px)

        // Tầng giao diện: ẤM, KHÔNG đổi theo chương (docs/GIAO-DIEN.md §1)
        private static readonly Color UiPaper = new Color(0.91f, 0.88f, 0.81f);
        private static readonly Color UiGold  = new Color(0.91f, 0.70f, 0.29f);
        private static readonly Color UiJade  = new Color(0.28f, 0.81f, 0.70f);
        // Màu chữ đã kiểm tương phản WCAG trên NỀN GỖ TỐI (70,64,46):
        // giấy 7,87 · vàng 5,40 · ngọc 5,33 · mờ 4,61 · son 4,67 — đều qua 4,5:1.
        // Trên NỀN GỖ SÁNG (243,140,76) chỉ có MỰC đạt (7,41), nên nút sáng dùng chữ mực.
        private static readonly Color UiDim   = new Color(0.71f, 0.67f, 0.63f);
        private static readonly Color UiCinnabar = new Color(0.89f, 0.61f, 0.58f);  // son — Lõi
        private static readonly Color UiInk   = new Color(0.10f, 0.09f, 0.08f);     // chữ trên gỗ sáng
        private const float ArenaSize = 14f;      // bề rộng sàn, đơn vị Unity
        private const float OrthoSize = 7.2f;     // bề rộng 8.1 đơn vị: đủ chỗ cho quái ở bán kính 3.5
                                          // cộng nửa sprite, không bị cắt mép

        [MenuItem("Tower RPG/Dựng scene M1")]
        public static void Build()
        {
            EnsureTmpResources();

            // Cả 5 nhân vật lấy từ CÙNG một nguồn Actor/Character (ô 16px) thay vì
            // CharacterAnimated (ô 32px, chỉ có mỗi NinjaGreen). Đã đo: phần vẽ thật là
            // 15x15 pixel ở cả hai nguồn, nên đổi nguồn KHÔNG đổi kích thước trên màn hình —
            // chỉ bỏ đi phần đệm rỗng. Nhờ vậy 5 nhân vật chắc chắn cùng cỡ với nhau.
            Sprite[] charSprites = new Sprite[CharacterRoster.Count];
            for (int i = 0; i < CharacterRoster.Count; i++)
                charSprites[i] = SliceAndGet(
                    $"{Art}/Actor/Character/{CharacterRoster.ArtFolders[i]}/SpriteSheet.png", Cell, 0);

            Sprite player = charSprites[0];
            Sprite enemy  = SliceAndGet($"{Art}/Actor/Monster/Skull/SpriteSheet.png", Cell, 0);

            Sprite floor  = SliceAndGet($"{Chapter}/Tilesets/TilesetFloor.png", Cell, 23, fullRect: true);

            if (player == null || enemy == null || floor == null)
            {
                Debug.LogError("[BuildM1Scene] Thiếu sprite, dừng. " +
                               $"player={player} enemy={enemy} floor={floor}");
                return;
            }
            for (int i = 0; i < CharacterRoster.Count; i++)
                if (charSprites[i] == null)
                {
                    Debug.LogError($"[BuildM1Scene] Thiếu sprite nhân vật {i} " +
                                   $"({CharacterRoster.ArtFolders[i]}), dừng.");
                    return;
                }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── Camera ────────────────────────────────────────────────────────────────
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = OrthoSize;
            cam.backgroundColor = new Color(0.06f, 0.06f, 0.07f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<AudioListener>();
            var shake = camGo.AddComponent<CameraShake>();

            // ── Sàn đấu trường ────────────────────────────────────────────────────────
            var floorGo = new GameObject("Floor");
            var floorSr = floorGo.AddComponent<SpriteRenderer>();
            floorSr.sprite = floor;
            floorSr.drawMode = SpriteDrawMode.Tiled;
            floorSr.size = new Vector2(ArenaSize, ArenaSize);
            floorSr.sortingOrder = -100;

            // ── Bootstrap ─────────────────────────────────────────────────────────────
            var bootGo   = new GameObject("Bootstrap");
            var balance  = bootGo.AddComponent<BalanceConfig>();
            var state    = bootGo.AddComponent<GameState>();
            var pstats   = bootGo.AddComponent<PlayerStats>();
            var runner   = bootGo.AddComponent<FloorRunner>();
            var popups   = bootGo.AddComponent<DamagePopupSpawner>();
            var sweep    = bootGo.AddComponent<SweepRunner>();
            var sfx      = bootGo.AddComponent<SfxPlayer>();
            var drops    = bootGo.AddComponent<ShardDropSpawner>();
            var slashes  = bootGo.AddComponent<SlashFxSpawner>();
            var music    = bootGo.AddComponent<AudioDirector>();

            // Pool thứ hai cho Lõi tím của boss — tách riêng vì nó dùng prefab khác và
            // KHÔNG được nối vào lớp kế toán hũ Mảnh của FloorRunner.
            var coreDropsGo = new GameObject("CoreDrops");
            coreDropsGo.transform.SetParent(bootGo.transform, false);
            var coreDrops = coreDropsGo.AddComponent<ShardDropSpawner>();

            // ── Người chơi ────────────────────────────────────────────────────────────
            var playerGo = new GameObject("Player");
            var psr = playerGo.AddComponent<SpriteRenderer>();
            psr.sprite = player;
            psr.sortingOrder = 10;
            var body = playerGo.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.bodyType = RigidbodyType2D.Dynamic;
            var ctrl   = playerGo.AddComponent<PlayerController>();
            var health = playerGo.AddComponent<PlayerHealth>();
            var meter  = playerGo.AddComponent<CritMeter>();
            var attack = playerGo.AddComponent<AutoAttack>();
            var autoBattle = playerGo.AddComponent<AutoBattle>();
            var anim   = playerGo.AddComponent<PlayerAnimator>();

            // thanh chí mạng ngay dưới chân nhân vật (§5.4) — CHIA VẠCH, không liền mạch
            var critCanvasGo = new GameObject("CritBarCanvas");
            critCanvasGo.transform.SetParent(playerGo.transform, false);
            critCanvasGo.transform.localPosition = new Vector3(0f, -0.78f, 0f);
            critCanvasGo.transform.localScale = Vector3.one * 0.01f;
            var critCanvas = critCanvasGo.AddComponent<Canvas>();
            critCanvas.renderMode = RenderMode.WorldSpace;
            critCanvas.sortingOrder = 20;
            var critRt = critCanvasGo.GetComponent<RectTransform>();
            critRt.sizeDelta = new Vector2(150f, 20f);

            Image critBg = MakeImage("CritBg", critCanvasGo.transform, new Color(0.06f, 0.05f, 0.04f, 0.85f));
            critBg.rectTransform.anchorMin = Vector2.zero;
            critBg.rectTransform.anchorMax = Vector2.one;
            critBg.rectTransform.offsetMin = new Vector2(-4f, -4f);
            critBg.rectTransform.offsetMax = new Vector2(4f, 4f);

            var segRootGo = new GameObject("Segments", typeof(RectTransform));
            segRootGo.transform.SetParent(critCanvasGo.transform, false);
            var segRoot = segRootGo.GetComponent<RectTransform>();
            segRoot.anchorMin = Vector2.zero;
            segRoot.anchorMax = Vector2.one;
            segRoot.offsetMin = segRoot.offsetMax = Vector2.zero;

            // mẫu vạch — CritMeterUI nhân bản nó ra đúng crit.meterSize cái
            Image segPrefab = MakeImage("SegmentPrefab", segRoot, Color.white);
            segPrefab.rectTransform.sizeDelta = new Vector2(20f, 20f);
            segPrefab.gameObject.SetActive(false);

            var critUi = critCanvasGo.AddComponent<CritMeterUI>();

            // ── Giao diện màn hình — docs/GIAO-DIEN.md ────────────────────────────────
            // Viền 9-patch = 6, KHÔNG phải 5. Đã đo từng pixel: vùng giữa chỉ đồng màu
            // (243,140,76) từ cột 6 trở vào. Đặt 5 thì cột bevel lọt vào vùng bị kéo giãn,
            // và trên nút rộng nó nhoè thành một vệt nâu chiếm ~20% bề ngang.
            // Lỗi này có từ M2 và không test nào bắt được — chỉ nhìn ảnh chụp mới thấy.
            Sprite panelSp = UiSprite("Theme/Theme Wood/nine_path_panel.png", 6);
            Sprite bgSp    = UiSprite("Theme/Theme Wood/nine_path_bg.png", 4);
            Sprite heartSp = UiSprite("Receptacle/IconHeart.png");

            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // 64 / sprite PPU 16 = phóng ĐÚNG 4x nguyên. Lệch số này là pixel art nhoè.
            canvas.referencePixelsPerUnit = UiPpu;
            // Sprite thế giới có sortingOrder tới 10; giao diện phải vượt hẳn lên trên.
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(UiRefW, UiRefH);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = UiPpu;
            canvasGo.AddComponent<GraphicRaycaster>();

            // khung số tầng, góc trên-trái
            Image floorPanel = UiImage("FloorPanel", canvasGo.transform, panelSp);
            Anchor(floorPanel.rectTransform, new Vector2(0f, 1f), new Vector2(Edge, -Edge), new Vector2(240, 128));
            floorPanel.rectTransform.pivot = new Vector2(0f, 1f);

            var floorLabel = UiText("FloorLabel", floorPanel.transform, "TẦNG", 26f, UiGold);
            Anchor(floorLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(200, 30));
            var floorNum = UiText("FloorNumber", floorPanel.transform, "01", 50f, UiPaper);
            Anchor(floorNum.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(200, 56));

            // thanh máu — khung gỗ 9-patch + ruột đầy vơi
            Image hpFrame = UiImage("HealthFrame", canvasGo.transform, panelSp);
            int hpX = Edge + 240 + Unit, hpW = UiRefW - hpX - Edge;
            Anchor(hpFrame.rectTransform, new Vector2(0f, 1f), new Vector2(hpX, -Edge), new Vector2(hpW, 128));
            hpFrame.rectTransform.pivot = new Vector2(0f, 1f);

            Image hpTrack = UiImage("HealthTrack", hpFrame.transform, bgSp);
            Anchor(hpTrack.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(28f, -6f), new Vector2(hpW - 112, 48));
            hpTrack.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            Image hpFill = UiImage("HealthBar", hpTrack.transform, null, new Color(0.80f, 0.24f, 0.26f), false);
            hpFill.rectTransform.anchorMin = new Vector2(0f, 0f);
            hpFill.rectTransform.anchorMax = new Vector2(1f, 1f);
            hpFill.rectTransform.offsetMin = new Vector2(6f, 6f);
            hpFill.rectTransform.offsetMax = new Vector2(-6f, -6f);
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;
            hpFill.fillOrigin = (int)Image.OriginHorizontal.Left;

            Image heart = UiImage("Heart", hpFrame.transform, heartSp, null, false);
            Anchor(heart.rectTransform, new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(56, 56));
            heart.rectTransform.pivot = new Vector2(0f, 0.5f);

            var hpText = UiText("HealthText", hpTrack.transform, "120 / 120", 30f, UiPaper);
            hpText.rectTransform.anchorMin = Vector2.zero;
            hpText.rectTransform.anchorMax = Vector2.one;
            hpText.rectTransform.offsetMin = hpText.rectTransform.offsetMax = Vector2.zero;

            var hpUi = canvasGo.AddComponent<PlayerHealthUI>();

            // ô Mảnh, ngay dưới thanh máu
            Image shardPanel = UiImage("ShardPanel", canvasGo.transform, panelSp);
            Anchor(shardPanel.rectTransform, new Vector2(0f, 1f),
                   new Vector2(Edge, -(Edge + 128 + Unit)), new Vector2(360, 96));
            shardPanel.rectTransform.pivot = new Vector2(0f, 1f);
            var shardLbl = UiText("ShardLabel", shardPanel.transform, "Mảnh", 24f, UiGold,
                                  TextAlignmentOptions.Left);
            Anchor(shardLbl.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(140, 28));
            shardLbl.rectTransform.pivot = new Vector2(0f, 1f);
            var shardVal = UiText("ShardCount", shardPanel.transform, "0", 34f, UiPaper,
                                  TextAlignmentOptions.Right);
            Anchor(shardVal.rectTransform, new Vector2(1f, 1f), new Vector2(-30f, -46f), new Vector2(300, 40));
            shardVal.rectTransform.pivot = new Vector2(1f, 1f);

            // ô Lõi, cạnh ô Mảnh. Ẩn tới khi hạ boss đầu tiên — HudUI lo việc đó.
            Image corePanel = UiImage("CorePanel", canvasGo.transform, panelSp);
            Anchor(corePanel.rectTransform, new Vector2(0f, 1f),
                   new Vector2(Edge + 360 + Unit, -(Edge + 128 + Unit)), new Vector2(260, 96));
            corePanel.rectTransform.pivot = new Vector2(0f, 1f);
            var coreLbl = UiText("CoreLabel", corePanel.transform, "Lõi", 24f, UiCinnabar,
                                 TextAlignmentOptions.Left);
            Anchor(coreLbl.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -20f), new Vector2(120, 28));
            coreLbl.rectTransform.pivot = new Vector2(0f, 1f);
            var coreVal = UiText("CoreCount", corePanel.transform, "0", 34f, UiPaper,
                                 TextAlignmentOptions.Right);
            Anchor(coreVal.rectTransform, new Vector2(1f, 1f), new Vector2(-28f, -46f), new Vector2(200, 40));
            coreVal.rectTransform.pivot = new Vector2(1f, 1f);

            // biển BOSS giữa màn, chỉ hiện ở tầng boss
            var bossBanner = UiText("BossBanner", canvasGo.transform, "BOSS", 56f, UiCinnabar);
            Anchor(bossBanner.rectTransform, new Vector2(0.5f, 1f),
                   new Vector2(0f, -(Edge + 128 + Unit + 96 + Unit)), new Vector2(900, 70));
            bossBanner.gameObject.SetActive(false);

            // thanh máu boss, ngay dưới biển BOSS
            var bossBarGo = new GameObject("BossBar", typeof(RectTransform));
            bossBarGo.transform.SetParent(canvasGo.transform, false);
            var bbRt = bossBarGo.GetComponent<RectTransform>();
            bbRt.anchorMin = new Vector2(0.5f, 1f); bbRt.anchorMax = new Vector2(0.5f, 1f);
            bbRt.pivot = new Vector2(0.5f, 1f);
            bbRt.anchoredPosition = new Vector2(0f, -(Edge + 128 + Unit + 96 + Unit + 74));
            bbRt.sizeDelta = new Vector2(UiRefW - Edge * 2, 44);

            Image bossTrack = UiImage("Track", bossBarGo.transform, bgSp);
            bossTrack.rectTransform.anchorMin = Vector2.zero;
            bossTrack.rectTransform.anchorMax = Vector2.one;
            bossTrack.rectTransform.offsetMin = bossTrack.rectTransform.offsetMax = Vector2.zero;

            Image bossFill = UiImage("Fill", bossTrack.transform, null, UiCinnabar, false);
            bossFill.rectTransform.anchorMin = Vector2.zero;
            bossFill.rectTransform.anchorMax = Vector2.one;
            bossFill.rectTransform.offsetMin = new Vector2(6f, 6f);
            bossFill.rectTransform.offsetMax = new Vector2(-6f, -6f);
            bossFill.type = Image.Type.Filled;
            bossFill.fillMethod = Image.FillMethod.Horizontal;
            bossFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            bossBarGo.SetActive(false);

            // banner sự kiện: dọn sạch tầng / hạ boss. Đặt ở 1/3 trên màn hình — đủ cao để
            // không che đấu trường, đủ thấp để nằm trong vùng mắt đang nhìn khi đánh nhau.
            // Neo TRÁI và thu bề ngang lại: cột nút bên phải bắt đầu ở x = 832, còn banner
            // căn giữa rộng 952 thì chiếm tới x = 1016 và chui xuống dưới nút QUÉT NHANH.
            // Nhìn ảnh chụp mới thấy — kích thước "cả bề ngang trừ hai lề" nghe rất hợp lý
            // cho tới lúc có một cột nút chiếm mất bên phải.
            Image bannerBg = UiImage("EventBanner", canvasGo.transform, bgSp);
            bannerBg.rectTransform.anchorMin = bannerBg.rectTransform.anchorMax = new Vector2(0f, 1f);
            bannerBg.rectTransform.pivot = new Vector2(0f, 1f);
            bannerBg.rectTransform.anchoredPosition = new Vector2(Edge, -(UiRefH * 0.32f));
            bannerBg.rectTransform.sizeDelta = new Vector2(UiRefW - Edge - (Edge + Touch + 40 + Unit), 96);

            var eventBanner = UiText("Label", bannerBg.transform, "", 38f, UiGold);
            eventBanner.rectTransform.anchorMin = Vector2.zero;
            eventBanner.rectTransform.anchorMax = Vector2.one;
            eventBanner.rectTransform.offsetMin = eventBanner.rectTransform.offsetMax = Vector2.zero;
            bannerBg.gameObject.SetActive(false);

            var hudUi = canvasGo.AddComponent<HudUI>();

            // nút mở màn nâng cấp — góc trên-phải, vùng chạm đủ lớn
            var gearBtnGo = new GameObject("GearButton", typeof(RectTransform));
            gearBtnGo.transform.SetParent(canvasGo.transform, false);
            var gbRt = gearBtnGo.GetComponent<RectTransform>();
            gbRt.anchorMin = gbRt.anchorMax = new Vector2(1f, 1f);
            gbRt.pivot = new Vector2(1f, 1f);
            gbRt.anchoredPosition = new Vector2(-Edge, -(Edge + 128 + Unit));
            gbRt.sizeDelta = new Vector2(Touch + 40, Touch);
            var gbImg = gearBtnGo.AddComponent<Image>();
            gbImg.sprite = panelSp;
            gbImg.type = Image.Type.Sliced;
            var gearBtn = gearBtnGo.AddComponent<Button>();
            gearBtn.targetGraphic = gbImg;
            var gbTxt = UiText("Label", gearBtnGo.transform, "TRANG\nBỊ", 28f, UiInk);
            gbTxt.rectTransform.anchorMin = Vector2.zero;
            gbTxt.rectTransform.anchorMax = Vector2.one;
            gbTxt.rectTransform.offsetMin = gbTxt.rectTransform.offsetMax = Vector2.zero;
            gbTxt.textWrappingMode = TextWrappingModes.Normal;

            // Ba nút M3 xếp dọc dưới nút trang bị. Đáy nút cuối ở -848, vùng chạm cần gạt
            // bắt đầu ở -864 — cố ý không chồng lên nhau, chạm nút không thành ra đi bộ.
            (Button charBtn, TMP_Text charTxt) = SideButton("CharButton", canvasGo.transform, panelSp,
                                                          "NHÂN\nVẬT", -392, UiInk);
            (Button sweepBtn, TMP_Text sweepTxt) = SideButton("SweepButton", canvasGo.transform, panelSp,
                                                          "QUÉT NHANH", -552, UiInk);
            (Button autoBtn, TMP_Text autoTxt)   = SideButton("AutoButton", canvasGo.transform, panelSp,
                                                          "TỰ ĐÁNH", -712, UiInk);

            // vạch tiến trình của lượt quét, chạy dọc đáy nút quét
            Image sweepFill = UiImage("SweepFill", sweepBtn.transform, null, UiJade, false);
            sweepFill.rectTransform.anchorMin = new Vector2(0f, 0f);
            sweepFill.rectTransform.anchorMax = new Vector2(1f, 0f);
            sweepFill.rectTransform.pivot = new Vector2(0f, 0f);
            sweepFill.rectTransform.offsetMin = new Vector2(12f, 10f);
            sweepFill.rectTransform.offsetMax = new Vector2(-12f, 0f);
            sweepFill.rectTransform.sizeDelta = new Vector2(sweepFill.rectTransform.sizeDelta.x, 10f);
            sweepFill.type = Image.Type.Filled;
            sweepFill.fillMethod = Image.FillMethod.Horizontal;
            sweepFill.fillAmount = 0f;

            // ── MÀN HÌNH NÂNG CẤP ────────────────────────────────────────────────────
            var upGo = new GameObject("UpgradeScreen", typeof(RectTransform));
            upGo.transform.SetParent(canvasGo.transform, false);
            var upRt = upGo.GetComponent<RectTransform>();
            upRt.anchorMin = Vector2.zero; upRt.anchorMax = Vector2.one;
            upRt.offsetMin = upRt.offsetMax = Vector2.zero;

            var dimImg = UiImage("Dim", upGo.transform, null, new Color(0.04f, 0.03f, 0.03f, 0.92f), false);
            dimImg.rectTransform.anchorMin = Vector2.zero;
            dimImg.rectTransform.anchorMax = Vector2.one;
            dimImg.rectTransform.offsetMin = dimImg.rectTransform.offsetMax = Vector2.zero;
            dimImg.raycastTarget = true;      // chặn chạm xuyên xuống game

            var upHead = UiText("Title", upGo.transform, "TRANG BỊ", 44f, UiGold);
            Anchor(upHead.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -340f), new Vector2(600, 60));

            var upShards = UiText("Shards", upGo.transform, "0 Mảnh", 32f, UiPaper);
            Anchor(upShards.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -396f), new Vector2(600, 44));

            var upCores = UiText("Cores", upGo.transform, "0 Lõi", 32f, UiCinnabar);
            Anchor(upCores.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -434f), new Vector2(600, 44));

            var upCrit = UiText("Crit", upGo.transform, "", 24f, UiJade);
            Anchor(upCrit.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -466f), new Vector2(900, 34));

            var rowParentGo = new GameObject("Rows", typeof(RectTransform));
            rowParentGo.transform.SetParent(upGo.transform, false);
            var rowParent = rowParentGo.GetComponent<RectTransform>();
            rowParent.anchorMin = new Vector2(0f, 1f);
            rowParent.anchorMax = new Vector2(1f, 1f);
            rowParent.pivot = new Vector2(0.5f, 1f);
            rowParent.offsetMin = new Vector2(Edge, 0f);
            rowParent.offsetMax = new Vector2(-Edge, 0f);
            // Ô Mảnh/Lõi của HUD kết thúc ở -320, nên tiêu đề phải bắt đầu từ -340 trở
            // xuống. Bốn dòng 220+14 = 936px từ -490 thì hết ở -1426; nút tẩy điểm ở -1496.
            // Đổi bất kỳ số nào ở đây là phải tính lại RowH/Pad trong UpgradeScreen.
            rowParent.anchoredPosition = new Vector2(0f, -510f);
            rowParent.sizeDelta = new Vector2(0f, 1200f);

            // nút tẩy điểm — §5.8 van 2. Đặt DƯỚI bốn dòng, không lẫn vào chúng: đây là
            // hành động huỷ, không phải một ô trang bị thứ năm.
            var respecGo = new GameObject("Respec", typeof(RectTransform));
            respecGo.transform.SetParent(upGo.transform, false);
            var rsRt = respecGo.GetComponent<RectTransform>();
            rsRt.anchorMin = new Vector2(0f, 0f); rsRt.anchorMax = new Vector2(1f, 0f);
            rsRt.pivot = new Vector2(0.5f, 0f);
            rsRt.offsetMin = new Vector2(Edge, 0f); rsRt.offsetMax = new Vector2(-Edge, 0f);
            rsRt.anchoredPosition = new Vector2(0f, Edge + 40 + Touch + Unit);
            rsRt.sizeDelta = new Vector2(0f, Touch);
            var rsImg = respecGo.AddComponent<Image>();
            rsImg.sprite = panelSp; rsImg.type = Image.Type.Sliced;
            var respecBtn = respecGo.AddComponent<Button>();
            respecBtn.targetGraphic = rsImg;
            var respecTxt = UiText("Label", respecGo.transform, "TẨY ĐIỂM", 28f, UiInk);
            respecTxt.rectTransform.anchorMin = Vector2.zero;
            respecTxt.rectTransform.anchorMax = Vector2.one;
            respecTxt.rectTransform.offsetMin = respecTxt.rectTransform.offsetMax = Vector2.zero;

            var closeGo = new GameObject("Close", typeof(RectTransform));
            closeGo.transform.SetParent(upGo.transform, false);
            var cRt = closeGo.GetComponent<RectTransform>();
            cRt.anchorMin = cRt.anchorMax = new Vector2(0.5f, 0f);
            cRt.pivot = new Vector2(0.5f, 0f);
            cRt.anchoredPosition = new Vector2(0f, Edge + 40);
            cRt.sizeDelta = new Vector2(400, Touch);
            var cImg = closeGo.AddComponent<Image>();
            cImg.sprite = panelSp; cImg.type = Image.Type.Sliced;
            var closeBtn = closeGo.AddComponent<Button>();
            closeBtn.targetGraphic = cImg;
            var cTxt = UiText("Label", closeGo.transform, "ĐÓNG", 32f, UiInk);
            cTxt.rectTransform.anchorMin = Vector2.zero; cTxt.rectTransform.anchorMax = Vector2.one;
            cTxt.rectTransform.offsetMin = cTxt.rectTransform.offsetMax = Vector2.zero;

            // Lưu vào scene ở trạng thái ĐÓNG — cùng lý do với cần gạt: ẩn trong Start()
            // thì nó vẫn loé lên một khung hình khi tải.
            upGo.SetActive(false);

            var upScreen = canvasGo.AddComponent<UpgradeScreen>();
            gearBtn.onClick.AddListener(upScreen.Toggle);
            closeBtn.onClick.AddListener(upScreen.Toggle);

            // ── MÀN HÌNH NHÂN VẬT §5.5b ──────────────────────────────────────────────
            var chGo = new GameObject("CharacterScreen", typeof(RectTransform));
            chGo.transform.SetParent(canvasGo.transform, false);
            var chRt = chGo.GetComponent<RectTransform>();
            chRt.anchorMin = Vector2.zero; chRt.anchorMax = Vector2.one;
            chRt.offsetMin = chRt.offsetMax = Vector2.zero;

            var chDim = UiImage("Dim", chGo.transform, null, new Color(0.04f, 0.03f, 0.03f, 0.92f), false);
            chDim.rectTransform.anchorMin = Vector2.zero;
            chDim.rectTransform.anchorMax = Vector2.one;
            chDim.rectTransform.offsetMin = chDim.rectTransform.offsetMax = Vector2.zero;
            chDim.raycastTarget = true;

            var chHead = UiText("Title", chGo.transform, "NHÂN VẬT", 44f, UiGold);
            Anchor(chHead.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -340f), new Vector2(600, 60));

            var chHint = UiText("Hint", chGo.transform, "", 22f, UiDim);
            Anchor(chHint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -404f), new Vector2(920, 68));
            chHint.textWrappingMode = TextWrappingModes.Normal;

            var chRowsGo = new GameObject("Cards", typeof(RectTransform));
            chRowsGo.transform.SetParent(chGo.transform, false);
            var chRows = chRowsGo.GetComponent<RectTransform>();
            chRows.anchorMin = new Vector2(0f, 1f); chRows.anchorMax = new Vector2(1f, 1f);
            chRows.pivot = new Vector2(0.5f, 1f);
            chRows.offsetMin = new Vector2(Edge, 0f); chRows.offsetMax = new Vector2(-Edge, 0f);
            chRows.anchoredPosition = new Vector2(0f, -490f);
            chRows.sizeDelta = new Vector2(0f, 1200f);

            var chCloseGo = new GameObject("Close", typeof(RectTransform));
            chCloseGo.transform.SetParent(chGo.transform, false);
            var ccRt = chCloseGo.GetComponent<RectTransform>();
            ccRt.anchorMin = ccRt.anchorMax = new Vector2(0.5f, 0f);
            ccRt.pivot = new Vector2(0.5f, 0f);
            ccRt.anchoredPosition = new Vector2(0f, Edge + 40);
            ccRt.sizeDelta = new Vector2(400, Touch);
            var ccImg = chCloseGo.AddComponent<Image>();
            ccImg.sprite = panelSp; ccImg.type = Image.Type.Sliced;
            var chCloseBtn = chCloseGo.AddComponent<Button>();
            chCloseBtn.targetGraphic = ccImg;
            var ccTxt = UiText("Label", chCloseGo.transform, "ĐÓNG", 32f, UiInk);
            ccTxt.rectTransform.anchorMin = Vector2.zero; ccTxt.rectTransform.anchorMax = Vector2.one;
            ccTxt.rectTransform.offsetMin = ccTxt.rectTransform.offsetMax = Vector2.zero;

            chGo.SetActive(false);   // như màn nâng cấp: lưu ở trạng thái ĐÓNG

            var chScreen = canvasGo.AddComponent<CharacterScreen>();
            charBtn.onClick.AddListener(chScreen.Toggle);
            chCloseBtn.onClick.AddListener(chScreen.Toggle);

            // ── MÀN HÌNH CỘT MỐC BOSS ────────────────────────────────────────────────
            var msGo = new GameObject("MilestoneOverlay", typeof(RectTransform));
            msGo.transform.SetParent(canvasGo.transform, false);
            var msRt = msGo.GetComponent<RectTransform>();
            msRt.anchorMin = Vector2.zero; msRt.anchorMax = Vector2.one;
            msRt.offsetMin = msRt.offsetMax = Vector2.zero;

            Image msDim = UiImage("Dim", msGo.transform, null, new Color(0.03f, 0.02f, 0.02f, 0.94f), false);
            msDim.rectTransform.anchorMin = Vector2.zero;
            msDim.rectTransform.anchorMax = Vector2.one;
            msDim.rectTransform.offsetMin = msDim.rectTransform.offsetMax = Vector2.zero;
            msDim.raycastTarget = true;      // nuốt chạm, và chính nó là nút "chạm để tiếp"

            var msTitle = UiText("Title", msGo.transform, "", 58f, UiGold);
            Anchor(msTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(960, 80));
            var msDetail = UiText("Detail", msGo.transform, "", 40f, UiPaper);
            Anchor(msDetail.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(960, 130));
            msDetail.textWrappingMode = TextWrappingModes.Normal;
            var msHint = UiText("Hint", msGo.transform, "", 28f, UiDim);
            Anchor(msHint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -160f), new Vector2(960, 40));

            msGo.SetActive(false);
            var milestone = canvasGo.AddComponent<MilestoneOverlay>();

            // ── CẦN GẠT ĐỘNG ─────────────────────────────────────────────────────────
            // Vùng chạm phủ nửa dưới màn hình; cần gạt hiện ra ngay nơi ngón đặt xuống.
            var zoneGo = new GameObject("JoystickZone", typeof(RectTransform));
            zoneGo.transform.SetParent(canvasGo.transform, false);
            var zone = zoneGo.GetComponent<RectTransform>();
            zone.anchorMin = new Vector2(0f, 0f);
            zone.anchorMax = new Vector2(1f, 0.55f);
            zone.offsetMin = zone.offsetMax = Vector2.zero;
            var zoneImg = zoneGo.AddComponent<Image>();
            zoneImg.color = new Color(0f, 0f, 0f, 0f);   // trong suốt nhưng vẫn nhận chạm
            zoneImg.raycastTarget = true;

            Image joyVisual = UiImage("JoystickVisual", zoneGo.transform, bgSp,
                                      new Color(1f, 1f, 1f, 0.28f));
            joyVisual.rectTransform.anchorMin = joyVisual.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            joyVisual.rectTransform.sizeDelta = new Vector2(300, 300);

            Image joyHandle = UiImage("Handle", joyVisual.transform, panelSp,
                                      new Color(1f, 1f, 1f, 0.60f));
            joyHandle.rectTransform.anchorMin = joyHandle.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            joyHandle.rectTransform.sizeDelta = new Vector2(Touch, Touch);
            joyHandle.rectTransform.anchoredPosition = Vector2.zero;

            // Lưu vào scene ở trạng thái ẨN — nếu không nó loé lên một khung hình khi tải.
            joyVisual.gameObject.SetActive(false);

            var joystick = zoneGo.AddComponent<VirtualJoystick>();

            // Vùng chạm cần gạt là LỚP DƯỚI CÙNG, luôn luôn. Nó phủ nửa dưới màn hình và
            // trong suốt, nên nếu nó là anh em SAU một cái nút thì nó nuốt cú chạm và nút
            // thành ra bấm không được — người chơi bấm QUÉT NHANH lại hoá ra đi bộ.
            // Ở 1080x1920 các nút chỉ cách vùng chạm 8px; đổi sang màn hình lùn hơn là
            // chúng chồng lên nhau ngay. Đặt xuống đáy thì hình học có sai cũng không hỏng.
            zoneGo.transform.SetAsFirstSibling();

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();

            // ── Prefab ────────────────────────────────────────────────────────────────
            Directory.CreateDirectory(PrefabDir);
            GameObject enemyPrefab = MakeEnemyPrefab(enemy);
            GameObject popupPrefab = MakePopupPrefab();
            GameObject shardPrefab = MakeShardPrefab("GemYellow", UiGold, "ShardPickup");
            GameObject corePrefab  = MakeShardPrefab("GemPurple", UiCinnabar, "CorePickup");
            GameObject slashPrefab = MakeSlashPrefab();

            // ── Nối tham chiếu ────────────────────────────────────────────────────────
            Wire(runner,   ("enemyPrefab", enemyPrefab.GetComponent<Enemy>()),
                           ("arenaCentre", null), ("playerHealth", health),
                           ("drops", drops), ("coreDrops", coreDrops),
                           ("floorRenderer", floorSr));
            WireChuongVaBoss(runner);
            Wire(drops,     ("pickupPrefab", shardPrefab.GetComponent<ShardPickup>()));
            Wire(coreDrops, ("pickupPrefab", corePrefab.GetComponent<ShardPickup>()));
            Wire(slashes,   ("prefab", slashPrefab.GetComponent<SlashFx>()));
            Wire(music,     ("runner", runner));
            WireMusic(music, "Musics/1 - Adventure Begin.ogg", "Musics/17 - Fight.ogg");
            Wire(popups,   ("popupPrefab", popupPrefab.GetComponent<DamagePopup>()));
            Wire(ctrl,     ("joystick", joystick));
            Wire(attack,   ("player", ctrl), ("critMeter", meter), ("popups", popups),
                           ("cameraShake", shake), ("slashes", slashes), ("animator", anim));
            Wire(anim,     ("target", psr), ("controller", ctrl));
            Wire(milestone, ("root", msGo), ("title", msTitle), ("detail", msDetail),
                            ("hint", msHint), ("shake", shake));
            Wire(critUi,   ("meter", meter), ("segmentRoot", segRoot), ("segmentPrefab", segPrefab));
            Wire(hpUi,     ("health", health), ("fillImage", hpFill), ("label", hpText));
            Wire(joystick, ("touchZone", zone), ("visual", joyVisual.rectTransform),
                           ("handle", joyHandle.rectTransform), ("canvas", canvas));
            Wire(shake,    ("target", camGo.transform));
            Wire(autoBattle, ("player", ctrl), ("joystick", joystick));
            Wire(hudUi,    ("floorNumber", floorNum), ("shardCount", shardVal),
                           ("coreCount", coreVal), ("coreGroup", corePanel.gameObject),
                           ("bossBanner", bossBanner),
                           ("bossBarRoot", bossBarGo), ("bossFill", bossFill),
                           ("sweep", sweep), ("sweepButton", sweepBtn),
                           ("sweepLabel", sweepTxt), ("sweepFill", sweepFill),
                           ("auto", autoBattle), ("autoButton", autoBtn), ("autoLabel", autoTxt),
                           ("runner", runner), ("eventBanner", eventBanner),
                           ("gearButton", gearBtn), ("gearLabel", gbTxt), ("charLabel", charTxt),
                           ("eventBannerRoot", bannerBg.gameObject), ("milestone", milestone),
                           ("cameraShake", shake));
            Wire(upScreen, ("root", upGo), ("rowParent", rowParent), ("shardLabel", upShards),
                           ("coreLabel", upCores), ("critLabel", upCrit), ("respecButton", respecBtn),
                           ("respecLabel", respecTxt));
            Wire(chScreen, ("root", chGo), ("cardParent", chRows), ("hintLabel", chHint));

            // ── ÂM THANH ─────────────────────────────────────────────────────────────
            // Thứ tự PHẢI khớp enum Sfx. Chọn bằng số đo, không bằng cảm tính — xem chú thích.
            WireClips(sfx, new[]
            {
                "Sounds/Hit & Impact/Hit1.wav",      // Hit       0,34s · đục (sắc 0,065)
                "Sounds/Whoosh & Slash/Slash.wav",   // Crit      0,34s · SẮC (0,448) — khác hẳn Hit
                "Sounds/Hit & Impact/Impact.wav",    // EnemyDie  0,24s · ngắn nhất nhóm Impact
                "Sounds/Bonus/Coin.wav",             // Pickup    0,29s · chờ Việc 5 gọi
                "Jingles/Success1.wav",              // FloorClear 0,45s · NGẮN NHẤT trong 15 jingle
                "Jingles/LevelUp1.wav",              // BossDown  1,18s · hiếm nên dài được
                "Sounds/Bonus/PowerUp1.wav",         // Upgrade   0,47s
                "Sounds/Hit & Impact/Hit5.wav",      // PlayerHit 0,33s · phát ở vol 0,4
            });

            // sprite dùng chung + 4 icon trang bị cho màn nâng cấp
            var upSo = new SerializedObject(upScreen);
            upSo.FindProperty("panelSprite").objectReferenceValue = panelSp;
            upSo.FindProperty("bgSprite").objectReferenceValue = bgSp;
            upSo.FindProperty("cellSprite").objectReferenceValue =
                UiSprite("Theme/Theme Wood/inventory_cell.png", 4);
            SerializedProperty icons = upSo.FindProperty("slotIcons");
            icons.arraySize = 4;
            string[] iconPaths =
            {
                "Skill Icon/Spell/Cut.png",
                "Skill Icon/Items & Weapon/Armor.png",
                "Skill Icon/Job & Action/Punch.png",
                "Skill Icon/Items & Weapon/Ring.png",
            };
            for (int i = 0; i < 4; i++)
                icons.GetArrayElementAtIndex(i).objectReferenceValue = UiSprite(iconPaths[i]);
            upSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(upScreen);

            // màn nhân vật: sprite khung + 5 chân dung Faceset
            var chSo = new SerializedObject(chScreen);
            chSo.FindProperty("panelSprite").objectReferenceValue = panelSp;
            chSo.FindProperty("bgSprite").objectReferenceValue = bgSp;
            chSo.FindProperty("cellSprite").objectReferenceValue =
                UiSprite("Theme/Theme Wood/inventory_cell.png", 4);
            SerializedProperty faces = chSo.FindProperty("portraits");
            faces.arraySize = CharacterRoster.Count;
            for (int i = 0; i < CharacterRoster.Count; i++)
                faces.GetArrayElementAtIndex(i).objectReferenceValue =
                    ActorSprite($"Actor/Character/{CharacterRoster.ArtFolders[i]}/Faceset.png");
            chSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(chScreen);

            // Hoạt ảnh: Idle/Walk/Attack x 4 hướng x 5 nhân vật. File 64x16 = bốn khung
            // 16x16 nằm ngang, thứ tự xuống/trái/phải/lên.
            var animSo = new SerializedObject(anim);
            foreach (string bo in new[] { "idle", "walk", "attack" })
            {
                SerializedProperty arr = animSo.FindProperty(bo);
                arr.arraySize = CharacterRoster.Count * PlayerAnimator.Huong;
                string file = bo == "idle" ? "Idle" : bo == "walk" ? "Walk" : "Attack";
                for (int ch = 0; ch < CharacterRoster.Count; ch++)
                    for (int d = 0; d < PlayerAnimator.Huong; d++)
                        arr.GetArrayElementAtIndex(ch * PlayerAnimator.Huong + d)
                           .objectReferenceValue = SliceAndGet(
                               $"{Art}/Actor/Character/{CharacterRoster.ArtFolders[ch]}/SeparateAnim/{file}.png",
                               Cell, d);
            }
            animSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(anim);


            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            AddSceneToBuildSettings(ScenePath);

            Debug.Log($"[BuildM1Scene] XONG -> {ScenePath}\n" +
                      $"  sprite: nhân vật {player.name}, quái {enemy.name}, sàn {floor.name}\n" +
                      $"  prefab: {PrefabDir}/Enemy.prefab, {PrefabDir}/DamagePopup.prefab");
        }

        // ── trợ giúp ──────────────────────────────────────────────────────────────────

        /// <summary>Nút dọc bên phải HUD: nền 9-patch + chữ, vùng chạm đủ 144px.</summary>
        private static (Button, TextMeshProUGUI) SideButton(string name, Transform parent,
                                                            Sprite panel, string label,
                                                            float y, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-Edge, y);
            rt.sizeDelta = new Vector2(Touch + 40, Touch);

            var img = go.AddComponent<Image>();
            img.sprite = panel;
            img.type = Image.Type.Sliced;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            // 20f chứ không phải 24f: nhãn khoá là HAI DÒNG ("TỰ ĐÁNH / CÒN 17 TẦNG")
            // trong nút cao 144px. Ở 24f dòng dưới bị cắt — đã kiểm bằng ảnh chụp.
            TextMeshProUGUI t = UiText("Label", go.transform, label, 20f, colour);
            t.rectTransform.anchorMin = Vector2.zero;
            t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
            t.textWrappingMode = TextWrappingModes.Normal;
            return (btn, t);
        }

        /// <summary>Sprite diễn viên/chân dung — một ảnh nguyên, không cắt lưới.</summary>
        private static Sprite ActorSprite(string rel)
        {
            string path = $"{Art}/{rel}";
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti)
            {
                Debug.LogError($"[BuildM1Scene] Không thấy sprite: {path}");
                return null;
            }
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>
        /// Nạp clip theo đúng thứ tự enum Sfx và ép thiết lập import hợp lý cho mobile.
        ///
        /// Mặc định của Unity là Decompress On Load — 147 file 44,1kHz stereo giải nén sẵn
        /// vào RAM là hàng chục MB cho những tiếng dài chưa tới nửa giây. Compressed In
        /// Memory + Force To Mono cắt gần hết chỗ đó mà tai không phân biệt được, vì âm
        /// thanh đã đặt spatialBlend = 0 (2D) nên vế stereo vốn không dùng tới.
        /// </summary>
        private static void WireClips(SfxPlayer player, string[] relativePaths)
        {
            var so = new SerializedObject(player);
            SerializedProperty arr = so.FindProperty("clips");
            arr.arraySize = relativePaths.Length;

            for (int i = 0; i < relativePaths.Length; i++)
            {
                string path = $"{Art}/Audio/{relativePaths[i]}";

                if (AssetImporter.GetAtPath(path) is AudioImporter ai)
                {
                    var s = ai.defaultSampleSettings;
                    s.loadType = AudioClipLoadType.CompressedInMemory;
                    s.compressionFormat = AudioCompressionFormat.Vorbis;
                    s.quality = 0.7f;
                    s.preloadAudioData = true;    // clip ngắn: nạp sẵn để đòn đầu không trễ
                    ai.defaultSampleSettings = s;
                    ai.forceToMono = true;
                    EditorUtility.SetDirty(ai);
                    ai.SaveAndReimport();
                }

                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) Debug.LogError($"[BuildM1Scene] Không thấy clip: {path}");
                arr.GetArrayElementAtIndex(i).objectReferenceValue = clip;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(player);
        }

        private static Image MakeImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = name == "Joystick";
            return img;
        }

        private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static GameObject MakeEnemyPrefab(Sprite sprite)
        {
            var go = new GameObject("Enemy");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 5;
            go.AddComponent<Enemy>();

            // ── thanh máu ────────────────────────────────────────────────────────────
            Sprite white = WhitePixelSprite();
            SpriteRenderer bar = BarPart(go.transform, "HpBg", white,
                                         new Color(0.10f, 0.08f, 0.07f, 0.9f), 6);
            bar.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            bar.transform.localScale = new Vector3(0.95f, 0.14f, 1f);

            // Ruột đặt pivot TRÁI bằng cách lệch nửa bề rộng: co localScale.x thì nó vơi
            // từ phải sang trái như thanh máu thật, thay vì co đều về giữa.
            var pivot = new GameObject("HpPivot");
            pivot.transform.SetParent(go.transform, false);
            pivot.transform.localPosition = new Vector3(-0.475f, 0.55f, 0f);

            SpriteRenderer fill = BarPart(pivot.transform, "HpFill", white,
                                          new Color(0.80f, 0.24f, 0.26f), 7);
            fill.transform.localPosition = new Vector3(0.465f, 0f, 0f);
            fill.transform.localScale = new Vector3(0.93f, 0.10f, 1f);

            var hb = go.AddComponent<EnemyHealthBar>();
            var hbSo = new SerializedObject(hb);
            hbSo.FindProperty("background").objectReferenceValue = bar;
            hbSo.FindProperty("fill").objectReferenceValue = fill;
            hbSo.FindProperty("fillPivot").objectReferenceValue = pivot.transform;
            hbSo.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{PrefabDir}/Enemy.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>
        /// Năm chương và mười boss của M4.
        ///
        /// Chương chỉ đổi SÀN và LOẠI QUÁI. Màu quái vẫn đỏ, nhân vật vẫn lam ngọc suốt
        /// 100 tầng — quyết định #23: người chơi học thứ bậc đọc MỘT LẦN rồi dùng mãi.
        /// Thư mục Chapters/ đã sinh sẵn năm bảng nền, cùng cấu trúc 51 file, chỉ khác màu.
        /// </summary>
        private static void WireChuongVaBoss(FloorRunner runner)
        {
            string[] chuong = { "1-nen-da", "2-chieu-giay", "3-nang-dong",
                                "4-cham-dem", "5-suong-lech" };
            // Một loại quái cho mỗi chương, chọn theo chủ đề bảng nền.
            string[] quai = { "Skull", "Bamboo", "Flam", "BlueBat", "Spirit" };

            // Mười boss, mười hình, không con nào lặp. Ô vuông khác nhau nên kèm kích thước.
            (string ten, int o)[] boss =
            {
                ("GiantFrog2", 40), ("GiantRacoon", 60),          // chương 1
                ("GiantBamboo", 62), ("GiantBamboo2", 62),        // chương 2
                ("GiantFlam", 50), ("GiantRacoonGold", 60),       // chương 3
                ("DemonCyclop", 50), ("TenguBlue", 68),           // chương 4
                ("GiantBlueSamurai", 48), ("TenguRed", 82),       // chương 5
            };

            var so = new SerializedObject(runner);

            SerializedProperty san = so.FindProperty("chapterFloors");
            san.arraySize = chuong.Length;
            SerializedProperty conQuai = so.FindProperty("chapterEnemies");
            conQuai.arraySize = chuong.Length;
            for (int i = 0; i < chuong.Length; i++)
            {
                san.GetArrayElementAtIndex(i).objectReferenceValue =
                    SliceAndGet($"Assets/Art/Chapters/{chuong[i]}/Tilesets/TilesetFloor.png",
                                Cell, 23, fullRect: true);
                conQuai.GetArrayElementAtIndex(i).objectReferenceValue =
                    SliceAndGet($"{Art}/Actor/Monster/{quai[i]}/SpriteSheet.png", Cell, 0);
            }

            SerializedProperty bs = so.FindProperty("bossSprites");
            bs.arraySize = boss.Length;
            for (int i = 0; i < boss.Length; i++)
                bs.GetArrayElementAtIndex(i).objectReferenceValue =
                    SliceAndGet($"{Art}/Actor/Boss/{boss[i].ten}/Idle.png", boss[i].o, 0);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(runner);
        }

        private static SpriteRenderer BarPart(Transform parent, string name, Sprite sprite,
                                              Color colour, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = colour;
            sr.sortingOrder = order;      // sprite quái = 5, số sát thương = 30
            return sr;
        }

        /// <summary>
        /// Sinh một sprite trắng 4x4 vào Assets/Art/Generated/. Bộ asset CC0 không có sẵn
        /// hình chữ nhật trơn nào, mà thanh máu chỉ cần đúng thế — tô màu bằng
        /// SpriteRenderer.color. Sinh một lần rồi tái dùng, không ghi đè nếu đã có.
        /// </summary>
        private static Sprite WhitePixelSprite()
        {
            // NGOÀI Assets/Art/ MỘT CÁCH CÓ CHỦ Ý. PixelArtImportSettings là một
            // AssetPostprocessor ép spritePixelsPerUnit = 16 cho MỌI texture dưới Art/,
            // và nó chạy SAU khi hàm này đặt PPU 4 — nên sprite thành 0,25 đơn vị và thanh
            // máu nhỏ đúng 4 lần, còn khoảng 18x2 pixel trên màn hình. Test vẫn xanh,
            // verifier vẫn xanh, fill.enabled vẫn true; chỉ nhìn ảnh chụp mới thấy.
            const string dir = "Assets/Generated";
            const string path = dir + "/white.png";

            if (!File.Exists(path))
            {
                Directory.CreateDirectory(dir);
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var px = new Color32[16];
                for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
                tex.SetPixels32(px);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }

            if (AssetImporter.GetAtPath(path) is TextureImporter ti)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.filterMode = FilterMode.Point;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.spritePixelsPerUnit = 4f;   // 4x4 pixel -> đúng 1 đơn vị Unity
                EditorUtility.SetDirty(ti);
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Viên Mảnh rơi ra từ quái. GemYellow 14x14, một ảnh nguyên nên không cắt lưới.</summary>
        private static GameObject MakeShardPrefab(string gem, Color tint, string name)
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ActorSpriteAt($"Items/Resource/{gem}.png");
            sr.sortingOrder = 7;              // trên sàn (-100) và trên quái (5)

            // NHUỘM VÀNG. Tên file là "GemYellow" nhưng bảng màu Mực & Son đã remap nó
            // thành XÁM (175,180,184) vì rule_for() xếp Items/ vào tầng "thế giới" — và
            // một viên xám nằm trên sàn xám thì gần như vô hình. Đã đo từng pixel mới thấy.
            //
            // Mảnh KHÔNG thuộc tầng thế giới: nó là PHẦN THƯỞNG, cùng tầng đọc với giao
            // diện — tông ấm, không đổi theo chương (quyết định #23). Nhãn "Mảnh" trên HUD
            // đã là vàng; viên rơi ra phải cùng màu thì người chơi mới nối được hai thứ.
            sr.color = tint;
            go.AddComponent<ShardPickup>();

            string path = $"{PrefabDir}/{name}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>Vệt chém — Cut/SpriteSheet.png 128x32 = bốn khung 32x32.</summary>
        private static GameObject MakeSlashPrefab()
        {
            var go = new GameObject("SlashFx");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 20;             // trên quái (5) và thanh máu (7)
            var fx = go.AddComponent<SlashFx>();

            var so = new SerializedObject(fx);
            so.FindProperty("view").objectReferenceValue = sr;
            SerializedProperty frames = so.FindProperty("frames");
            frames.arraySize = 4;
            for (int i = 0; i < 4; i++)
                frames.GetArrayElementAtIndex(i).objectReferenceValue =
                    SliceAndGet($"{Art}/FX/Attack/Cut/SpriteSheet.png", 32, i);
            if (frames.GetArrayElementAtIndex(0).objectReferenceValue is Sprite s0) sr.sprite = s0;
            so.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{PrefabDir}/SlashFx.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>
        /// Nhạc nền phải là STREAMING, ngược hẳn với hiệu ứng. Một bài 2-3 phút giải nén
        /// vào RAM là hàng chục MB cho đúng một thứ đang phát; còn hiệu ứng thì ngắn và
        /// phát liên tục nên mới nạp sẵn (xem WireClips).
        /// </summary>
        private static void WireMusic(AudioDirector director, string normal, string boss)
        {
            AudioClip Nap(string rel)
            {
                string path = $"{Art}/Audio/{rel}";
                if (AssetImporter.GetAtPath(path) is AudioImporter ai)
                {
                    var s = ai.defaultSampleSettings;
                    s.loadType = AudioClipLoadType.Streaming;
                    s.compressionFormat = AudioCompressionFormat.Vorbis;
                    s.quality = 0.6f;
                    s.preloadAudioData = false;
                    ai.defaultSampleSettings = s;
                    ai.forceToMono = false;      // nhạc giữ stereo
                    EditorUtility.SetDirty(ai);
                    ai.SaveAndReimport();
                }
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) Debug.LogError($"[BuildM1Scene] Không thấy nhạc: {path}");
                return clip;
            }

            var so = new SerializedObject(director);
            so.FindProperty("normalTrack").objectReferenceValue = Nap(normal);
            so.FindProperty("bossTrack").objectReferenceValue = Nap(boss);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(director);
        }

        /// <summary>Như ActorSprite nhưng nhận đường dẫn ngoài thư mục Actor/.</summary>
        private static Sprite ActorSpriteAt(string rel)
        {
            string path = $"{Art}/{rel}";
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti)
            {
                Debug.LogError($"[BuildM1Scene] Không thấy sprite: {path}");
                return null;
            }
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static GameObject MakePopupPrefab()
        {
            var go = new GameObject("DamagePopup");
            var tmp = go.AddComponent<TextMeshPro>();     // bản 3D, KHÔNG phải bản UI
            tmp.text = "0";
            tmp.fontSize = 4f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.GetComponent<RectTransform>().sizeDelta = new Vector2(4f, 2f);
            tmp.sortingOrder = 30;
            go.AddComponent<DamagePopup>();
            string path = $"{PrefabDir}/DamagePopup.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void Wire(Object target, params (string field, Object value)[] pairs)
        {
            var so = new SerializedObject(target);
            foreach ((string field, Object value) in pairs)
            {
                SerializedProperty p = so.FindProperty(field);
                if (p == null)
                {
                    Debug.LogError($"[BuildM1Scene] {target.GetType().Name} không có trường '{field}'");
                    continue;
                }
                p.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        /// <summary>
        /// Nạp một sprite giao diện, đặt viền 9-patch nếu có.
        /// Viền phải đặt ở TextureImporter rồi reimport — không đặt được lúc chạy.
        /// </summary>
        private static Sprite UiSprite(string rel, int border = 0)
        {
            string path = $"{Art}/Ui/{rel}";
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti)
            {
                Debug.LogError($"[BuildM1Scene] Không thấy sprite giao diện: {path}");
                return null;
            }

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;

            var st = new TextureImporterSettings();
            ti.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect;   // bắt buộc cho Image.Type.Sliced
            st.spriteBorder = border > 0
                ? new Vector4(border, border, border, border)
                : Vector4.zero;
            ti.SetTextureSettings(st);

            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Chữ trong giao diện. Dùng TextMeshPro UI.</summary>
        private static TextMeshProUGUI UiText(string name, Transform parent, string content,
                                              float size, Color colour,
                                              TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = content;
            t.fontSize = size;
            t.color = colour;
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }

        /// <summary>Ảnh giao diện dùng sprite thật; 9-patch khi có viền.</summary>
        private static Image UiImage(string name, Transform parent, Sprite sprite,
                                     Color? tint = null, bool sliced = true)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = sliced && sprite != null && sprite.border != Vector4.zero
                       ? Image.Type.Sliced : Image.Type.Simple;
            img.color = tint ?? Color.white;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Cắt texture thành lưới ô vuông rồi trả về sprite thứ index.</summary>
        private static Sprite SliceAndGet(string path, int cell, int index, bool fullRect = false)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti)
            {
                Debug.LogError($"[BuildM1Scene] Không tìm thấy texture: {path}");
                return null;
            }

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) { Debug.LogError($"[BuildM1Scene] Không nạp được: {path}"); return null; }

            int cols = tex.width / cell, rows = tex.height / cell;
            string baseName = Path.GetFileNameWithoutExtension(path);

            var meta = new List<SpriteMetaData>(cols * rows);
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    meta.Add(new SpriteMetaData
                    {
                        name = $"{baseName}_{row * cols + col}",
                        // toạ độ texture tính từ ĐÁY, nên lật hàng để index 0 là ô trên-trái
                        rect = new Rect(col * cell, tex.height - (row + 1) * cell, cell, cell),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    });
                }
            }

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Multiple;
            ti.spritesheet = meta.ToArray();

            if (fullRect)
            {
                var s = new TextureImporterSettings();
                ti.ReadTextureSettings(s);
                s.spriteMeshType = SpriteMeshType.FullRect;   // cần cho SpriteDrawMode.Tiled
                ti.SetTextureSettings(s);
            }

            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();

            string want = $"{baseName}_{index}";
            Sprite found = AssetDatabase.LoadAllAssetsAtPath(path)
                                        .OfType<Sprite>()
                                        .FirstOrDefault(s => s.name == want);
            if (found == null)
                Debug.LogError($"[BuildM1Scene] Không thấy sprite '{want}' trong {path} " +
                               $"(lưới {cols}x{rows})");
            return found;
        }

        /// <summary>
        /// TextMeshPro cần bộ tài nguyên nền (font atlas, shader), không có thì chữ không hiện.
        /// Nhập thẳng từ .unitypackage nằm sẵn trong gói com.unity.ugui — tin cậy hơn gọi
        /// TMP_PackageResourceImporter qua reflection, vì tên kiểu đổi theo phiên bản Unity.
        /// </summary>
        private static void EnsureTmpResources()
        {
            if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
            {
                Debug.Log("[BuildM1Scene] TMP Essentials đã có, bỏ qua.");
                return;
            }

            string[] found = Directory.GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage",
                                                SearchOption.AllDirectories);
            if (found.Length == 0)
            {
                Debug.LogWarning("[BuildM1Scene] Không tìm thấy TMP Essential Resources.unitypackage. " +
                                 "Vào Window ▸ TextMeshPro ▸ Import TMP Essential Resources bằng tay.");
                return;
            }

            AssetDatabase.ImportPackage(found[0], false);
            AssetDatabase.Refresh();
            Debug.Log($"[BuildM1Scene] Đã nhập TMP Essentials từ {found[0]}");
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var list = EditorBuildSettings.scenes.ToList();
            if (list.Any(s => s.path == path)) return;
            list.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
