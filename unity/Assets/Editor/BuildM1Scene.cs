using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using TowerRpg.Core;
using TowerRpg.Enemies;
using TowerRpg.Juice;
using TowerRpg.Player;
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
        private const float ArenaSize = 14f;      // bề rộng sàn, đơn vị Unity
        private const float OrthoSize = 7.2f;     // bề rộng 8.1 đơn vị: đủ chỗ cho quái ở bán kính 3.5
                                          // cộng nửa sprite, không bị cắt mép

        [MenuItem("Tower RPG/Dựng scene M1")]
        public static void Build()
        {
            EnsureTmpResources();

            Sprite player = SliceAndGet($"{Art}/Actor/CharacterAnimated/NinjaGreen/Separate/Idle.png", 32, 0);
            Sprite enemy  = SliceAndGet($"{Art}/Actor/Monster/Skull/SpriteSheet.png", Cell, 0);
            Sprite floor  = SliceAndGet($"{Chapter}/Tilesets/TilesetFloor.png", Cell, 23, fullRect: true);

            if (player == null || enemy == null || floor == null)
            {
                Debug.LogError("[BuildM1Scene] Thiếu sprite, dừng. " +
                               $"player={player} enemy={enemy} floor={floor}");
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
            var spawner  = bootGo.AddComponent<EnemySpawner>();
            var popups   = bootGo.AddComponent<DamagePopupSpawner>();
            var boot     = bootGo.AddComponent<GameBootstrap>();

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

            // thanh chí mạng ngay dưới chân nhân vật (§5.4)
            var critCanvasGo = new GameObject("CritBarCanvas");
            critCanvasGo.transform.SetParent(playerGo.transform, false);
            critCanvasGo.transform.localPosition = new Vector3(0f, -0.75f, 0f);
            critCanvasGo.transform.localScale = Vector3.one * 0.01f;
            var critCanvas = critCanvasGo.AddComponent<Canvas>();
            critCanvas.renderMode = RenderMode.WorldSpace;
            critCanvas.sortingOrder = 20;
            var critRt = critCanvasGo.GetComponent<RectTransform>();
            critRt.sizeDelta = new Vector2(120f, 14f);

            Image critBg   = MakeImage("CritBg",   critCanvasGo.transform, new Color(0f, 0f, 0f, 0.55f));
            critBg.rectTransform.sizeDelta = new Vector2(120f, 14f);
            Image critFill = MakeImage("CritFill", critCanvasGo.transform, new Color(0.55f, 0.75f, 1f));
            critFill.rectTransform.sizeDelta = new Vector2(120f, 14f);
            critFill.type = Image.Type.Filled;
            critFill.fillMethod = Image.FillMethod.Horizontal;
            critFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            critFill.fillAmount = 0f;
            var critUi = critCanvasGo.AddComponent<CritMeterUI>();

            // ── Giao diện màn hình ────────────────────────────────────────────────────
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);   // màn hình dọc, §3
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // thanh máu — nửa còn lại của luật §5.3 phải nhìn thấy được
            Image hpBg = MakeImage("HealthBg", canvasGo.transform, new Color(0f, 0f, 0f, 0.6f));
            Anchor(hpBg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(760f, 46f));
            Image hpFill = MakeImage("HealthBar", canvasGo.transform, new Color(0.85f, 0.25f, 0.3f));
            Anchor(hpFill.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(760f, 46f));
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;
            hpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            var hpUi = canvasGo.AddComponent<PlayerHealthUI>();

            // cần gạt ảo — PHẢI có kích thước cụ thể, neo kéo giãn làm bán kính = 0
            Image joyBg = MakeImage("Joystick", canvasGo.transform, new Color(1f, 1f, 1f, 0.16f));
            Anchor(joyBg.rectTransform, new Vector2(0f, 0f), new Vector2(70f, 90f), new Vector2(400f, 400f));
            Image joyHandle = MakeImage("Handle", joyBg.transform, new Color(1f, 1f, 1f, 0.42f));
            joyHandle.rectTransform.anchorMin = joyHandle.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            joyHandle.rectTransform.anchoredPosition = Vector2.zero;
            joyHandle.rectTransform.sizeDelta = new Vector2(160f, 160f);
            var joystick = joyBg.gameObject.AddComponent<VirtualJoystick>();

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();

            // ── Prefab ────────────────────────────────────────────────────────────────
            Directory.CreateDirectory(PrefabDir);
            GameObject enemyPrefab = MakeEnemyPrefab(enemy);
            GameObject popupPrefab = MakePopupPrefab();

            // ── Nối tham chiếu ────────────────────────────────────────────────────────
            Wire(boot,     ("balance", balance), ("spawner", spawner), ("playerHealth", health));
            Wire(spawner,  ("enemyPrefab", enemyPrefab.GetComponent<Enemy>()), ("arenaCentre", null));
            Wire(popups,   ("popupPrefab", popupPrefab.GetComponent<DamagePopup>()));
            Wire(ctrl,     ("joystick", joystick));
            Wire(attack,   ("player", ctrl), ("critMeter", meter), ("popups", popups), ("cameraShake", shake));
            Wire(critUi,   ("meter", meter), ("fillImage", critFill));
            Wire(hpUi,     ("health", health), ("fillImage", hpFill));
            Wire(joystick, ("background", joyBg.rectTransform), ("handle", joyHandle.rectTransform), ("canvas", canvas));
            Wire(shake,    ("target", camGo.transform));

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
            string path = $"{PrefabDir}/Enemy.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
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
