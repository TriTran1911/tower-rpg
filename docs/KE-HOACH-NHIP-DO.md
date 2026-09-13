# Kế hoạch chữa nhịp độ đầu game

Kết quả của một nhóm 12 agent: 4 góc chẩn đoán → 4 phương án độc lập → 3 giám khảo
chấm chéo → tổng hợp. Ba giám khảo cho cùng thứ hạng (gói phản hồi 43/50, rơi vật
phẩm 41–42/50) và cả ba đều kết luận GHÉP chứ không chọn một.

Ba phát hiện trong đây đã được kiểm chứng độc lập bằng cách đọc thẳng can-bang.xlsx
và mô phỏng lại — xem §0. Chúng lật ngược hai điều tôi đã viết sai trong DESIGN.md §7
và trong SweepRunner.cs.

---

# KẾ HOẠCH THI CÔNG — CHỮA "CHẠY VÒNG VÒNG ĐÁNH QUÁI RẤT NHÀM CHÁN"

Gốc: `/Users/trantri/development/tower-rpg` · Mọi đường dẫn dưới đây tính từ gốc này.
Xương sống: **Phương án 2 (Gói Phản Hồi), cắt còn lõi** + ghép **Mảnh rơi theo từng con (PA1)** + **nửa đầu PA4 mục 1** + **trần quét nhanh**.

---

## 0. MỘT PHÁT HIỆN PHẢI ĐỌC TRƯỚC KHI GÕ DÒNG ĐẦU TIÊN

Tôi đã **mở `docs/can-bang.xlsx` ở chế độ chỉ đọc** (không sửa một ô nào — ràng buộc 4 nguyên vẹn). Hai ô sau lật ngược hai kết luận trong hồ sơ:

| Ô | Giá trị | Nhãn trong bảng | Công thức dùng nó |
|---|---|---|---|
| `'Thông số'!B9` | **0,5** | "Tỉ lệ sát thương thực nhận — *Nhờ luật di chuyển ở mục 5.3*" | `'Kiểm chứng build'!T = P/(R×B9)` → nằm ở MẪU SỐ biên an toàn |
| `'Thông số'!B32` | **2** | "Hệ số cày lại (quét nhanh)" — chú thích: "1.0 = chỉ clear mỗi tầng một lần" | `'Đường cong tầng'!E5 = D5×B32`, `E6 = E5+D6×B32` → **ngân sách Mảnh lũy kế = 2× Mảnh leo** |
| `'Thông số'!B40` | **1,5** | Biên an toàn tối thiểu để ĐẠT | — |

**Bảng tính chưa bao giờ giả định "đứng yên ăn trọn đòn", và chưa bao giờ giả định "không farm".** Nó giả định người chơi né đúng một nửa và có gấp đôi Mảnh leo. Chú thích ở `m1-balance.csv:99-102` và `SweepRunner.cs:11-15` ("can-bang.xlsx chỉ mô hình hoá Mảnh theo việc LEO — chưa từng tính farm") là **SAI**, và cái sai đó đang che mất trần thật.

Chạy lại boss 1 (tầng 10, máu 500×1,0518⁹×1,56 = 1.228,8; dps boss 3×1,04⁹ = 4,27) với đúng cấp trang bị mà bảng tự suy ra ở `'Đường cong tầng'!G10` (ngân sách E10/4 → **cấp 7 cả bốn ô**):

| Kịch bản | DPS | Giây hạ boss | Máu | Giây sống | **Biên** |
|---|---|---|---|---|---|
| Đúng mô hình (quét tới trần 2×, né 50%) | 20,47 | 60,0 | 192,5 | 90,1 | **1,50** ✅ đúng bằng B40 |
| **Mã hôm nay** (quét vô hạn nhưng né 0%) | 20,47 | 60,0 | 192,5 | 45,1 | **0,75** ❌ |

Hai lỗi ngược chiều nhau: **quét nhanh vượt trần ở phía trên, né hỏng ở phía dưới**. Kết quả là boss 1 không thắng nổi bằng đường leo, phải farm vượt trần mới qua — đúng cái vòng bệnh hoạn mà cả bốn phương án đều ngửi thấy nhưng không ai đọc được bảng để gọi tên.

**Hệ quả cho kế hoạch:** hai việc dưới đây KHÔNG phải "đổi cân bằng", chúng là **đưa mã về đúng hai ô đã chốt** — và chúng vào Đợt 1, trước mọi thứ bóng bẩy. Làm 5 ngày juice lên trên một con boss không thắng nổi là xây nhà trên nền lún.

---

# ĐỢT 1 — 7 VIỆC, ≈ 5 NGÀY CÔNG THẬT

Thứ tự dưới đây là thứ tự thi công, không được đảo (Việc 4 mở đường cho Việc 5).

---

## VIỆC 1 — HAI GIỜ ĐẦU TIÊN: 4 SỬA MỘT DÒNG, CHƠI THỬ NGAY

Làm hết trong một buổi sáng, dựng lại scene, chơi 3 tầng. Đây là phần rẻ nhất trong toàn bộ hồ sơ.

### 1.1 · Nút TỰ ĐÁNH và QUÉT NHANH: khoá-nhưng-thấy-được
**File:** `unity/Assets/Scripts/UI/HudUI.cs` (hàm `RefreshAuto`, dòng 138-152 và `RefreshSweep`, dòng 121-136)

```csharp
private void RefreshAuto(GameState gs)
{
    if (autoButton == null) return;
    bool unlocked = auto != null && auto.IsUnlocked;

    autoButton.gameObject.SetActive(true);        // ← CŨ: SetActive(unlocked)
    autoButton.interactable = unlocked;

    if (autoLabel == null) return;
    if (!unlocked)
    {
        int need = auto != null ? auto.UnlockFloor : 20;
        int left = Mathf.Max(0, need - gs.HighestCleared);
        autoLabel.text  = $"TỰ ĐÁNH\nCÒN {left} TẦNG";
        autoLabel.color = Dim;                    // Dim đã có sẵn ở dòng 36
        return;
    }
    bool on = auto.Enabled;
    autoLabel.text  = on ? "TỰ ĐÁNH  ●" : "TỰ ĐÁNH";
    autoLabel.color = on ? Jade : Gold;
}
```
`AutoBattle.UnlockFloor` đã là `public` (AutoBattle.cs:25), `GameState.HighestCleared` cũng vậy — không phải thêm API nào.

Làm y hệt cho nút quét: luôn `SetActive(true)`; nhãn khi chưa mở `"QUÉT NHANH\nDỌN XONG TẦNG 1"`; khi hết ngân sách (sau Việc 2) `"QUÉT NHANH\nHẾT NGÂN SÁCH"`.

⚠ Nhãn 2 dòng ở fontSize 24 trong nút 144px là chật — hạ xuống 18f cho dòng phụ hoặc dùng `<size=70%>` của TMP. Kiểm bằng ảnh chụp, đừng đoán.

### 1.2 · Bỏ `RoundToInt` — lần nâng cấp đầu tiên phải đổi pixel
**File:** `unity/Assets/Scripts/Juice/DamagePopup.cs:47`

```csharp
// Trên cùng lớp:
private static readonly System.Globalization.NumberFormatInfo Vi =
    new System.Globalization.NumberFormatInfo { NumberDecimalSeparator = "," };

// Dòng 47:
_label.text = amount < _decimalBelow
            ? amount.ToString("0.0", Vi)          // 10,0 → 10,4 → 10,9
            : Mathf.RoundToInt(amount).ToString();
```
`_decimalBelow` truyền vào qua `Play(...)` từ `DamagePopupSpawner.cs:80` (đọc khoá mới `juice.popupDecimalBelow` ở `ApplyBalance`, dòng 48-53). Ngưỡng 100 để sát thương tầng cao (~600) không thành rác chữ số.

Chuỗi popup theo cấp Vũ khí 1→10 đi từ `10,10,11,11,12,12,13,14,14,15` thành `10,0 · 10,4 · 10,9 · 11,4 · 11,9 · 12,4 · 13,0 · 13,5 · 14,2 · 14,8` — **mỗi lần mua đều đổi pixel**. Không đụng một chữ số cân bằng nào: ràng buộc 3 khoá CON SỐ 4,4%, không khoá cách in nó ra.

### 1.3 · Thanh chí mạng đếm đúng
**File:** `unity/Assets/Scripts/UI/CritMeterUI.cs:46` — `Build(size)` → `Build(Mathf.Max(2, size - 1))`

Đã đối chiếu `CritMeter.cs:23,26`: `Fill01 = charge/(size-1)`, `IsReady` khi `charge >= size-1`, charge chạy 0..4. Với **4 vạch**: `lit = Floor(charge/4 × 4) = charge` → 0/1/2/3, và `IsReady` ép `lit = 4`. Hết nhảy cóc 3→5, hết vạch ma.
**KHÔNG** dùng bản `charge/size` (đề xuất ở PA4 mục 7): lúc thanh sẵn sàng `Fill01` chỉ bằng 0,8, vạch cuối không bao giờ sáng — tệ hơn hiện trạng.
`crit.meterSize` trong CSV **giữ nguyên 5** → `M1LoopTests.cs:68` vẫn xanh.

### 1.4 · Chớp trúng đòn: TRẮNG, 0,10 giây
**File:** `unity/Assets/Scripts/Enemy/Enemy.cs:18-19`
```csharp
[SerializeField] private Color hitFlashColor = Color.white;   // CŨ (1; 0,4; 0,4)
[SerializeField] private float hitFlashSeconds = 0.10f;       // CŨ 0.06
```
Bảng màu "Mực & Son" (quyết định #18/#19) làm toàn bộ quái ĐỎ; nhân đỏ nhạt lên đỏ trong 3,6 khung hình là vô hình. `Enemy.prefab` do `BuildM1Scene.MakeEnemyPrefab` sinh bằng `AddComponent<Enemy>()` nên nó lấy giá trị mặc định của class — đổi default rồi **dựng lại scene** là xong.

---

## VIỆC 2 — VÁ HAI Ô ĐANG LỆCH SO VỚI BẢNG TÍNH (≈ 0,75 ngày)

### 2.1 · Ngoài tầm thì hồi chiêu quái ĐÓNG BĂNG (B9 = 0,5)
**File:** `unity/Assets/Scripts/Enemy/Enemy.cs:77-84` — **đảo thứ tự hai khối**, đúng 4 dòng:

```csharp
// ĐỐI XỨNG với AutoAttack.cs:58 — người chơi di chuyển thì hồi chiêu đứng yên,
// quái ngoài tầm thì hồi chiêu cũng đứng yên. Sát thương thực nhận vì thế TỈ LỆ
// với thời gian đứng trong tầm — đúng ô 'Thông số'!B9 = 0,5 của can-bang.xlsx.
float sqr = (player.transform.position - transform.position).sqrMagnitude;
if (sqr > _attackRange * _attackRange) return;      // ← LÊN TRƯỚC

_attackCooldown -= Time.deltaTime;                  // ← XUỐNG SAU
if (_attackCooldown > 0f) return;

player.TakeDamage(_damage);
_attackCooldown = _attackInterval;
```

**Vì sao đây là vá lỗi chứ không phải buff:** hiện tại dòng 77 trừ hồi chiêu bất kể người chơi ở đâu, dòng 81 ra ngoài tầm thì `return` mà không reset → hồi chiêu âm sâu → quái vung **ngay khung hình** người chơi bước vào lại → lùi ra dưới 1,25 giây né được **đúng 0 đòn**. Sau khi sửa, sát thương nhận tỉ lệ thuận với thời gian trong tầm; ở một tầng thường 48 giây thì người chơi đi bộ ~15% thời gian → nhận ~85%, còn boss thì tuỳ tay. Mô hình muốn 50%; đây là bước đầu tiên về phía đó.

**TUYỆT ĐỐI KHÔNG** làm hai biến thể sau, cả hai đều là nới biên thật:
- ❌ reset `_attackCooldown = _attackInterval` khi ra ngoài tầm (tặng một cửa sổ ân huệ đầy mỗi lần quay lại)
- ❌ luật "ra khỏi tầm trong lúc vung thì đòn TRƯỢT" (PA4, cửa sổ 0,35s) — đó là hoàn tiền, không phải né

### 2.2 · Trần quét nhanh = đúng 2× ngân sách leo (B32 = 2)
**Files:** `unity/Assets/Scripts/Progression/GameState.cs`, `SweepRunner.cs`, `SaveData.cs`, `SaveSystem.cs`, `m1-balance.csv`

Hiện `SweepRunner.Loop` (dòng 58-78) là `while (CanSweep)` vô hạn, chạy coroutine **song song** với FloorRunner: 400 Mảnh / 3 giây ở tầng 1 = 133 Mảnh/giây so với 9,6 khi đánh tay — gấp 13,9 lần. Chiến lược tối ưu từ phút đầu là ngừng chơi.

Cài **ngân sách**, không phải hồi chiêu:

```csharp
// GameState.cs
public float ShardsClimbed { get; private set; }   // Mảnh kiếm từ LEO (tầng + rơi đồ)
public float ShardsSwept   { get; private set; }   // Mảnh kiếm từ QUÉT
private float _sweepTotalMult;                     // = b.Get("sweep.totalMult") = 2

public float SweepBudgetLeft =>
    Mathf.Max(0f, ShardsClimbed * (_sweepTotalMult - 1f) - ShardsSwept);

public void AddShards(float amount, bool fromSweep = false)
{
    if (amount <= 0f) return;
    Shards += amount;
    if (fromSweep) ShardsSwept += amount; else ShardsClimbed += amount;
    Changed?.Invoke();
}

public bool CanSweep(int floor) =>
    floor >= 1 && floor <= HighestCleared && SweepBudgetLeft > 0f;
```
```csharp
// SweepRunner.Loop, thay khối dòng 70-74:
int floor = TargetFloor;
float budget = GameState.Instance.SweepBudgetLeft;
if (budget <= 0f) break;
float reward = Mathf.Min(GameState.Instance.ShardReward(floor), budget);
GameState.Instance.AddShards(reward, fromSweep: true);
```

**Save:** `SaveData.CurrentVersion` 2 → **3**, thêm `public float shardsClimbed; public float shardsSwept;`. Di trú bản cũ trong `SaveSystem`: nếu `version < 3` thì dựng lại `shardsClimbed = Σ ShardReward(1..highestCleared)` (một vòng for) và `shardsSwept = 0`, ghi `Debug.Log`. Không có bước này thì người chơi cũ mở game lên thấy nút quét chết cứng.

**Vì sao không lệch biên:** `'Đường cong tầng'!E = D × B32` với `B32 = 2`, và cột `G` (cấp trang bị kỳ vọng) được suy ra từ chính `E/4`. Mô hình **luôn** cho người chơi gấp đôi Mảnh leo. Mã hiện giao **vô hạn** — tức đang nằm ngoài mô hình theo hướng quá nhiều. Siết về 2× là đưa mã **về** đúng ô đã chốt, không phải nerf tôi bịa ra. `sweep.seconds` giữ nguyên 3.

**Kèm theo:** sửa chú thích sai ở `m1-balance.csv:99-102` và `SweepRunner.cs:11-15`, dán thẳng số ô `'Thông số'!B32 = 2` vào đó để lần sau không ai kết luận ngược.

---

## ~~VIỆC 3 — ÂM THANH~~ ✅ XONG

> **Đã cài, 37/37 test, 39 mục kiểm scene.** `SfxPlayer.cs` với 8 nguồn quay vòng, nối đủ
> 7 chỗ. Kiểm end-to-end: gọi `Play` xong thì **8/8 nguồn thật sự đang phát** — không dừng
> ở "không ném lỗi".
>
> **CÒN LẠI CHO BẠN, VÀ KHÔNG AI LÀM THAY ĐƯỢC: nghe thử.** Tôi không nghe được. 8 clip
> dưới đây chọn bằng SỐ ĐO (độ dài, đỉnh, độ sắc phổ), không phải bằng tai:
>
> | Sự kiện | Clip | Vì sao chọn (số đo) |
> |---|---|---|
> | Trúng đòn | `Hit & Impact/Hit1.wav` | 0,34s · độ sắc 0,065 = đục, tiếng "thịch" |
> | Chí mạng | `Whoosh & Slash/Slash.wav` | 0,34s · độ sắc **0,448** — khác hẳn đòn thường |
> | Quái chết | `Hit & Impact/Impact.wav` | 0,24s — ngắn nhất nhóm Impact |
> | Nhặt Mảnh | `Bonus/Coin.wav` | 0,29s · chưa ai gọi, chờ Việc 5 |
> | Dọn tầng | `Jingles/Success1.wav` | 0,45s — **ngắn nhất trong 15 jingle** |
> | Hạ boss | `Jingles/LevelUp1.wav` | 1,18s — hiếm nên dài được |
> | Nâng cấp | `Bonus/PowerUp1.wav` | 0,47s |
> | Bị đánh | `Hit & Impact/Hit5.wav` | 0,33s · phát ở âm lượng 0,4 |
>
> Đổi clip nào không ưng: sửa đúng một dòng trong mảng ở `BuildM1Scene.WireClips` rồi dựng
> lại scene. Thư mục còn 139 file chưa dùng.
>
> **Chưa làm:** nhạc nền (41 file `.ogg`) — Đợt 2, và phải đặt Load Type *Streaming*.

### Nội dung gốc

`grep -rniE "AudioSource|AudioClip|PlayOneShot"` trên `Scripts/` + `Editor/` = **0 dòng**. `AudioListener` đã nằm sẵn ở `BuildM1Scene.cs:104`. 147 file .wav + 41 .ogg CC0 đã có giấy phép, chưa dùng một file nào.

**File mới:** `unity/Assets/Scripts/Juice/SfxPlayer.cs` (~60 dòng)
- `public enum Sfx { Hit, Crit, EnemyDie, Pickup, FloorClear, BossDown, Upgrade, PlayerHit }`
- `[SerializeField] private AudioClip[] clips;` — chỉ mục theo đúng thứ tự enum
- 8 `AudioSource` con quay vòng (`_next = (_next+1) % 8`). **Không dùng `PlayClipAtPoint`** — nó cấp phát một GameObject mỗi lần.
- `public static void Play(Sfx id, float vol = 1f)` với `_instance` static; âm lượng nền đọc từ `audio.sfxVolume`.
- `PlayerHit` phải có hồi 0,15 giây (`_lastHitAt`), nếu không 6 con quái đánh liên tục sẽ thành tiếng nhiễu.

**Nối (mỗi chỗ một dòng):**

| Chỗ | File · dòng | Clip |
|---|---|---|
| Trúng đòn / chí mạng | `Player/AutoAttack.cs` sau dòng 79 | `Sounds/Hit & Impact/Hit1.wav` · `Sounds/Whoosh & Slash/Slash.wav` |
| Quái chết | `Enemy/Enemy.cs` (khối chết mới ở Việc 4) | `Sounds/Hit & Impact/Impact.wav` |
| Nhặt Mảnh | `Loot/ShardPickup.cs` (Việc 5) | `Sounds/Bonus/Coin.wav` |
| Dọn sạch tầng | `Progression/FloorRunner.cs` sau dòng 107 | `Jingles/Success1.wav` |
| Hạ boss | `Progression/FloorRunner.cs` sau dòng 100 | `Jingles/LevelUp1.wav` |
| Nâng cấp | `UI/UpgradeScreen.cs` (chỗ gọi `TryUpgrade` thành công) | `Sounds/Bonus/PowerUp1.wav` |
| Bị đánh | `Player/PlayerHealth.cs:62` | `Sounds/Hit & Impact/Hit5.wav` vol 0,4 |

Tất cả đường dẫn trên đã kiểm là **có thật** trong `unity/Assets/Art/NinjaAdventure-MucSon/Audio/`.

**Dựng scene:** `BuildM1Scene.cs` — `var sfx = bootGo.AddComponent<SfxPlayer>();` cạnh dòng 121-122. `Wire()` hiện chỉ nhận `Object` đơn nên viết thêm một hàm `WireClips(SfxPlayer, string[] paths)` dùng `SerializedObject.FindProperty("clips")` + `arraySize` + `GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(p)`.

**Import settings (bắt buộc, nếu không phình RAM trên máy thật):** SFX → *Load Type: Compressed In Memory*, *Force To Mono*, quality ~70%. Nhạc nền `.ogg` để Đợt 2 và phải là *Streaming*.

⚠ **Cái bẫy thời gian lớn nhất của cả kế hoạch:** nghe thử 147 clip. **Hẹn giờ 60 phút, chọn 8 clip, chốt, cấm quay lại cãi nhau với chính mình.**
⚠ `AssetDatabase.LoadAssetAtPath` chỉ chạy trong Editor — ổn vì scene dựng lúc thiết kế; nếu sau này cần nạp lúc chạy thì phải đổi sang Resources/Addressables. Ghi làm nợ kỹ thuật.

---

## ~~VIỆC 4 — THANH MÁU QUÁI + KHOẢNH KHẮC CHẾT~~ ✅ XONG

> **40/40 test, 42 mục kiểm scene.** Thanh máu từng con quái (ẩn khi đầy, cam khi dưới 30%),
> thanh máu boss trên đỉnh màn hình, và xác quái co/giãn/mờ trong 0,22 giây.
>
> **Hai lỗi chỉ ảnh chụp mới bắt được, cả hai đều qua lọt mọi test:**
> 1. `white.png` sinh vào `Assets/Art/Generated/` — mà `PixelArtImportSettings` là một
>    `AssetPostprocessor` ép `spritePixelsPerUnit = 16` cho MỌI texture dưới `Art/`, chạy SAU
>    khi hàm sinh đặt PPU 4. Thanh máu nhỏ đúng **4 lần**, còn 18×2 pixel. `fill.enabled`
>    vẫn `true`, verifier vẫn xanh. Sửa: sinh ra `Assets/Generated/` cho ngoài tầm tay nó.
> 2. Ruột thanh **teo về giữa như viên thuốc** thay vì vơi từ phải sang trái — tôi dựng đúng
>    một pivot ở mép trái rồi vẫn co giãn đứa con thay vì co giãn chính pivot.
>
> Đã thêm hai lớp kiểm để không tái phạm: verifier đo **bề rộng THẬT theo đơn vị thế giới**
> (≥ 60% bề ngang con quái), và một test đòi mép trái của ruột trùng mép trái của nền.
>
> `juice.enemyDeathSeconds = 0,22` **tốn 0 giây thời gian chơi**: quái gỡ khỏi
> `EnemyRegistry` ngay trong khung hình chết, nên `FloorRunner` và `Nearest` thấy nó chết tức
> thì — cái xác đang tan chỉ là pixel. Có test khoá đúng tính chất này.

### Nội dung gốc

**Đây là món đắt giá nhất trong toàn bộ 30 mục của bốn phương án**, vì nó là cách DUY NHẤT biến +4,4%/cấp — thứ ràng buộc 3 khoá cứng — thành thứ **đếm được**: trên popup, Vũ khí cấp 1→2 đổi "10" thành "10"; trên một thanh máu, cùng 4,4% đó đổi **8 đòn thành 7 đòn**.

### 4.1 · `unity/Assets/Scripts/UI/EnemyHealthBar.cs` (mới, ~60 dòng)
- Hai `SpriteRenderer` con (`Bg`, `Fill`), **không dùng Canvas thế giới** — 6-20 canvas mỗi tầng là phí.
- `Fill` đặt pivot trái, `localScale.x = enemy.HealthFraction` mỗi `LateUpdate`. `HealthFraction` đã có sẵn ở `Enemy.cs:40` và chú thích của chính nó ghi "dùng cho thanh máu quái ở mốc sau".
- **Ẩn tới khi con đó trúng đòn đầu tiên** (`HealthFraction < 1f`), để một tầng vừa bày không có 6 thanh đầy vô nghĩa.
- `sortingOrder = 6` (sprite quái 5, popup 30). Đặt ở `y = +0,55` so với tâm quái; với boss thì `transform.localScale *= bossScale` ở `FloorRunner.cs:166` sẽ kéo cả thanh — chấp nhận được, boss to thì thanh to.

**Sprite trắng 1 pixel:** bộ asset không có sẵn. Trong `BuildM1Scene` thêm hàm sinh một lần: `Texture2D` 4×4 trắng → `EncodeToPNG` → ghi ra `Assets/Art/Generated/white.png` → `AssetDatabase.ImportAsset` → `TextureImporter.textureType = Sprite` → `LoadAssetAtPath<Sprite>`. Đúng khuôn mà dự án đang dùng cho `SliceAndGet`.

**Gắn vào prefab:** `BuildM1Scene.MakeEnemyPrefab` (dòng 627-638), thêm hai object con trước khi `SaveAsPrefabAsset`.

### 4.2 · Thanh máu boss trên đỉnh màn hình
- `FloorRunner`: lưu `private Enemy _bossRef;` lúc spawn boss (dòng 159-169), thêm
  `public float BossHealthFraction => _bossRef != null && _bossRef.IsAlive ? _bossRef.HealthFraction : 0f;`
- `HudUI`: thêm `[SerializeField] private Image bossFill;` cạnh dòng 21, cập nhật trong `Update()` khi `runner.InBossFight` (không dùng `Refresh()` vì nó chỉ chạy theo sự kiện). Dựng ở `BuildM1Scene` cạnh `bossBanner`.
- Với boss 75-193 giây, đây là **điều kiện để trận đấu chịu đựng nổi**.

### 4.3 · Quái chết có một khoảnh khắc
**File:** `unity/Assets/Scripts/Enemy/Enemy.cs:96` — `if (_hp <= 0f) Destroy(gameObject);` thay bằng:

```csharp
if (_hp > 0f) return;

_hp = 0f;
_armed = false;
EnemyRegistry.Unregister(this);            // ← NGAY trong khung hình này. Bắt buộc.
SfxPlayer.Play(Sfx.EnemyDie);
_drops?.Drop(transform.position, _shardValue);   // Việc 5
StartCoroutine(DieVisual());               // co ngang/giãn dọc + alpha→0 trong juice.enemyDeathSeconds rồi Destroy
```

**MẸO QUYẾT ĐỊNH — unregister ngay:** `FloorRunner.cs:90` (`while EnemyRegistry.Count > 0`) và `EnemyRegistry.Nearest` (đã lọc `!IsAlive` ở dòng 44) thấy quái chết **tức thì**, nên hoạt ảnh chết 0,22 giây **tốn 0 giây thời gian chơi** và `M1LoopTests` đo `Count` giảm vẫn xanh.
`OnDestroy()` ở dòng 100 **giữ nguyên** — `Remove` gọi hai lần là vô hại.
**TUYỆT ĐỐI KHÔNG** đặt lời gọi `Drop` trong `OnDestroy`: `EnemyRegistry.ClearAll()` (dòng 57-64) huỷ quái bằng `Destroy` ở **mọi** lần `SpawnFloor` (`FloorRunner.cs:124`), kể cả đường `Retry` dòng 185 — đặt ở đó là biến nút chết thành máy in Mảnh.

---

## ~~VIỆC 5 — MẢNH RƠI TỪ TỪNG CON QUÁI~~ ✅ XONG

> **45/45 test, 47 mục kiểm scene.** Bước 2 của vòng lặp ở `DESIGN.md:15` đã quay lại:
> *"đánh quái → rơi vật phẩm cố định → nâng cấp trang bị"*. Nhịp trả thưởng từ
> **1 lần/49 giây xuống 1 lần/8 giây** — ảnh chụp cho thấy HUD hiện "Mảnh 67" ngay
> giữa tầng, đúng 400/6 = phần của một con quái.
>
> **Bất biến trung tâm được giữ:** mỗi lượt tầng vẫn trả đúng `ShardReward(tầng)`. Mảnh
> rơi là cách CHIA NHỎ khoản cũ, không phải nguồn mới — nên `can-bang.xlsx` không phải
> chạy lại một dòng. Cả bốn đường rò đã bịt và có test đi qua từng đường **bằng cách giết
> quái THẬT**, không dùng `ClearAll` như 30 test cũ.
>
> **Ba lỗi tìm được trong lúc làm:**
> 1. `BalanceConfig.TryUse` gọi từ `Awake()` thay vì `Start()` — **45/45 test hỏng cùng
>    một dòng**. Đây là quy tắc chính dự án này đã ghi từ M2 và tôi vừa tái phạm.
> 2. `GemYellow.png` sau khi áp bảng Mực & Son là **màu XÁM** (175,180,184), nằm trên sàn
>    xám thì gần như vô hình. `rule_for()` xếp `Items/` vào tầng "thế giới", nhưng Mảnh là
>    **phần thưởng** — cùng tầng đọc với giao diện. Nhuộm vàng cùng tông nhãn "Mảnh".
> 3. Viên bị hút quá nhanh nên không ảnh nào chụp được nó — phải thêm một mốc chụp cố tình
>    đứng xa mới NHÌN được.
>
> **Tự khai một nới lỏng:** giết 5/6 con rồi chết thì giữ ~83% phần thưởng tầng, trong khi
> trước đây chết = 0 Mảnh. `_paid` chặn ở đúng một phần thưởng mỗi lượt nên không thành lỗ
> farm, nhưng nó là nới lỏng thật.

### Nội dung gốc

Đây là **bước 2 của vòng lặp năm bước ở `DESIGN.md:15`** ("đánh quái → rơi vật phẩm *cố định* → nâng cấp trang bị") đã bị xoá khỏi bản cài, là ý tưởng gốc của chủ dự án, và là câu trả lời **đúng nguyên văn** cho "tôi không thấy rơi vật phẩm". Nhịp trả thưởng đi từ 1 lần/49 giây xuống 1 lần/8 giây.

### 5.1 · `unity/Assets/Scripts/Loot/ShardPickup.cs` (mới, ~110 dòng)
Vòng đời **xác định tuyệt đối**, không một lời gọi `Random` nào:
bật ra khỏi xác theo cung **hướng về phía người chơi** (`loot.popDistance`, không random hướng, không random lượng) → rơi xuống → nhấp nhô tại chỗ → người chơi vào `loot.magnetRadius` thì hút về với `loot.flySpeed` → chạm `loot.pickupRadius` thì `GameState.AddShards(value)` + `SfxPlayer.Play(Sfx.Pickup)` + bắn `Collected(value)` → trả về pool.
Lấy người chơi qua `PlayerHealth.Current` (đã static sẵn, `PlayerHealth.cs:16`) — không cần nối tham chiếu.
Có `CollectNow(float speedMult)` cho lệnh dọn cuối tầng.

**Hình học đã kiểm:** lúc quái gục, người chơi cách xác ≤ `player.attackRange` = 2,0. `popDistance` 1,1 hướng về người chơi → viên rơi cách chân ~0,9 → **nằm sẵn trong `magnetRadius` 1,8 → hút ngay, không phải đi một bước nào**. Tổng chuỗi bật-bay-keng-nhảy-số ≈ 0,5-0,6 giây, **rơi đúng vào khoảng 1,0 giây chết cứng sau mỗi lần di chuyển** (`AutoAttack.cs:58` đóng băng hồi chiêu lúc đi bộ). Lấp đúng cái lỗ, không tạo lỗ mới.

**Rơi đồ là thứ NHÌN THẤY, không phải thứ TƯƠNG TÁC.** Bán kính hút nhỏ để "bắt đi nhặt thật" sẽ khiến người chơi không bao giờ đi qua viên Mảnh trong lối chơi bình thường (họ đi theo cung TRONG của vòng tròn bán kính 3,5), mọi viên dồn về lượt dọn cuối tầng, và ta quay lại đúng chỗ cũ với đồ hoạ đẹp hơn.

### 5.2 · `unity/Assets/Scripts/Loot/ShardDropSpawner.cs` (mới, ~90 dòng)
Sao chép **nguyên khuôn** `Juice/DamagePopupSpawner.cs:23-73` (ObjectPool + Prewarm + `BalanceConfig.TryUse` — đã chạy đúng). Thêm:
- `List<ShardPickup> _live`
- `public event Action<float> Collected`
- `public void Drop(Vector3 pos, float value)`
- `public void FlushAll()` — cộng ngay giá trị mọi viên còn sống rồi trả hết về pool
- `public void DiscardAll()` — trả về pool **KHÔNG cộng** (dùng khi bày lại tầng sau khi chết)

Prewarm 16, maxSize 64 (bằng popup).

### 5.3 · Lớp kế toán "hũ Mảnh" trong `FloorRunner` — phần quan trọng nhất
**File:** `unity/Assets/Scripts/Progression/FloorRunner.cs` (sửa dòng 103-107, 122-171, 180-186)

```
_paidFloor, _paid : tầng đang tính sổ, và Mảnh ĐÃ VÀO VÍ cho tầng đó trong cả lượt

SpawnFloor(floor):
    if (floor != _paidFloor) { _paidFloor = floor; _paid = 0f; }   // lượt tầng mới
    drops.DiscardAll();                                            // viên cũ trên sàn: bỏ, không cộng
    _pot   = Mathf.Max(0f, ShardReward(floor) - _paid);            // phần còn NỢ
    _share = _pot / count;                                         // boss: count=1 → thả trọn
    ... e.Initialise(hpEach, dmgEach, _rate, range, boss, _share, drops)

drops.Collected += v => _paid += v;                                // nối một lần ở Configure

Dọn sạch (dòng 103-107):
    drops.FlushAll();                                              // Collected bắn → _paid tăng
    float rest = Mathf.Max(0f, ShardReward(floor) - _paid);
    GameState.AddShards(rest); _paid += rest;                      // sai số dấu phẩy động đổ về đây
    MarkCleared(floor); Save(); FloorCleared?.Invoke(floor, ShardReward(floor));

AdvanceFloor → tầng mới → _paid tự reset ở SpawnFloor
```

**Tổng Mảnh mỗi lượt tầng bằng ĐÚNG `ShardReward(floor)` theo CẤU TRÚC**, không theo may rủi làm tròn → toàn bộ 420.000 cấu hình của `can-bang.xlsx` không phải chạy lại một dòng.

**Ba đường rò phải bịt (30 test hiện có KHÔNG đi qua đường nào trong ba đường này — chúng dùng `ClearAll`, quái không thật sự chết):**
1. ❌ `Drop` trong `OnDestroy` → `ClearAll()` biến mỗi lần bày lại tầng thành in tiền.
2. ❌ Thiếu `_paid` → chết-rồi-thử-lại thành cách farm nhanh nhất game.
3. ❌ **Đường thứ ba, chưa phương án nào khai:** `Retry()` (dòng 180-186) gọi `SpawnFloor` → `ClearAll()` chỉ dọn **quái**, không dọn `ShardPickup` đang nằm trên sàn. Vì vậy `DiscardAll()` ở đầu `SpawnFloor` là **bắt buộc**.
4. **Đường thứ tư:** thoát app giữa tầng thì `_paid` mất → lượt sau hũ đầy lại. Bịt bằng cách lưu `_paid` + `_paidFloor` vào `SaveData` v3 (hai trường, cùng lần tăng version với Việc 2.2).

**Tự khai một nới lỏng nhỏ:** giết 5/6 con rồi chết thì người chơi giữ ~83% phần thưởng tầng, trong khi hôm nay chết = 0 Mảnh. `_paid` chặn ở đúng một phần thưởng tầng mỗi lượt nên không thành lỗ farm — nhưng nó là nới lỏng thật, phải biết.

### 5.4 · Prefab + nối dây
**File:** `unity/Assets/Editor/BuildM1Scene.cs`
- `MakeShardPickupPrefab()` theo khuôn `MakeEnemyPrefab` (dòng 627-638), sprite `ActorSprite("Items/Resource/GemYellow.png")` — **đã kiểm là có thật**, 14×14, một ảnh nguyên nên không cần cắt lưới. `sortingOrder = 7`.
- `var drops = bootGo.AddComponent<ShardDropSpawner>();` cạnh dòng 121-122
- `Wire(drops, ("pickupPrefab", ...))` và thêm `("drops", drops)` vào khối `Wire(runner, ...)` dòng 483-486

⚠ **Rối mắt:** `magnetRadius` 1,8 phải đủ lớn để không quá 2-3 viên nằm trên sàn cùng lúc ở lối chơi bình thường. Bắt buộc kiểm bằng ảnh chụp (Việc 7), không đoán — bài học quyết định #29: "nối đúng ≠ chạy đúng ≠ nhìn được".

---

## VIỆC 6 — ĐÍCH ĐẾN CỦA VIÊN MẢNH: HUD CHẠY SỐ + BANNER DỌN TẦNG (≈ 0,5 ngày)

`FloorRunner.cs:107` bắn `FloorCleared` và **grep toàn repo cho thấy KHÔNG MỘT AI NGHE** (`HudUI.cs:45` chỉ nghe `FloorStarted`). Viên Mảnh phải hạ cánh vào đâu đó, nếu không đường bay kết thúc trong hư không.

- `unity/Assets/Scripts/UI/HudUI.cs:103` — `shardCount.text = $"{gs.Shards:N0}"` đổi thành nội suy giá trị hiển thị chạy tới giá trị thật trong ~0,25 giây + một nháy vàng ngắn trên nhãn (~15 dòng).
- `HudUI.Start()` dòng 43-47 — thêm `runner.FloorCleared += OnFloorCleared;` và một hàm cạnh `OnBossDefeated` (dòng 74): banner **"TẦNG 01 XONG · +400 MẢNH"** 1,0 giây + `Sfx.FloorClear`. Layout dựng ở `BuildM1Scene` cạnh `bossBanner`.
- `OnBossDefeated` (dòng 74-78) hiện là **một dòng `Debug.Log`** — tối thiểu đổi thành banner "+3 LÕI · MỞ NHÂN VẬT MỚI" 2 giây + `Sfx.BossDown` + `cameraShake.Shake()`. Màn hình cột mốc riêng để Đợt 2.

**KHÔNG** đổi thời điểm hay số lượng Mảnh — chỉ làm cho lần cộng vốn đã có trở nên nhìn thấy được.

---

## VIỆC 7 — NGHIỆM THU (≈ 0,5 ngày)

Dựng lại scene · chạy 30 test cũ + 5 test mới · chụp ảnh · **chơi thật 10 tầng có bấm giờ**. Chi tiết ở mục KIỂM CHỨNG bên dưới.

---

# ĐỢT 2 — LÀM SAU, NẾU CÒN SỨC (theo đúng thứ tự này)

| # | Việc | File | Công |
|---|---|---|---|
| 1 | **Báo hiệu ra đòn của quái** — 0,25s trước khi đánh, sprite sáng lên và nhích về phía người chơi. Sau Việc 2.1 thì hồi chiêu chỉ chạy khi trong tầm nên **không cần phép kiểm tầm riêng** nữa. **Hiển thị thuần, KHÔNG có luật trượt.** | `Enemy/Enemy.cs` (+12 dòng chỉ đọc) | 0,5 ngày |
| 2 | **Vệt chém** khi ra đòn — `FX/Attack/Cut/SpriteSheet.png` (128×32 = 4 khung), sinh tại vị trí mục tiêu qua pool FX | `Player/AutoAttack.cs` + `Juice/FxSpawner.cs` | 0,5 ngày |
| 3 | **Hai con số của §5.5(b)** trên màn nâng cấp — `DESIGN.md:226-228` ghi rõ cả nhãn: **"5 đòn ≈ 0,95 s đánh liên tục"**, KHÔNG được viết "giữ yên liên tục". Và đổi `×1.04` thành `Sát thương 10,0 → 10,4` | `UI/UpgradeScreen.cs` | 0,5 ngày |
| 4 | **Nhạc nền** — `Musics/1 - Adventure Begin.ogg`, đổi sang `17 - Fight.ogg` khi `runner.InBossFight`. Import *Streaming* | `Juice/AudioDirector` | 0,5 ngày |
| 5 | **Boss rơi 3 viên Lõi tím** (`Items/Resource/GemPurple.png`) bay về ô Lõi — dùng lại đúng hệ thống của Việc 5. Lõi VẪN do `AwardBoss` cấp trong khung hình đó, viên bay chỉ là lời loan báo | `FloorRunner` + `ShardDropSpawner` | 0,25 ngày |
| 6 | **Màn hình cột mốc boss** — overlay tối, rung mạnh, chạm để tiếp | `UI/MilestoneOverlay.cs` (mới) | 1 ngày |
| 7 | **Khung Lõi "18 / 30 cả game"** + năm ô cổng đột phá (`GIAO-DIEN.md:78-83`, hiện 0/3) | `UI/UpgradeScreen.cs` | 0,5 ngày |
| 8 | **Hoạt ảnh đánh + lật mặt nhân vật** — `Actor/Character/<tên>/SeparateAnim/Attack.png` (64×16 = 4 hướng) | `Player/PlayerAnimator.cs` (mới) | 1 ngày |

**Một quyết định CHỜ SỐ LIỆU, không quyết bây giờ:** hồi đầy máu khi bước vào tầng boss. Bảng tính dùng **máu ĐẦY** (`'Kiểm chứng build'!P = baseHp×(1+hpPerFloor)^99×armor`, không trừ máu hiện tại), mà `ResetHealth()` chỉ được gọi lúc chết (`FloorRunner.cs:184`) — nên về nguyên tắc hồi đầy là đúng mô hình. **Nhưng** sau Việc 2.1, lượng máu mất mỗi tầng đã giảm gần một nửa, người chơi có thể tới tầng 10 với 60-70% máu thay vì 16-40%. **Đo trước (chỉ tiêu bên dưới), sửa sau.** Ba lớp buff sinh tồn chồng lên nhau mà không nhân ra một lần là lỗ hổng lớn nhất của PA4 — đừng lặp lại.

---

# CỐ Ý KHÔNG LÀM (và vì sao)

| Không làm | Vì sao |
|---|---|
| **`enemy.count` 6 → 12 hoặc 15** (PA1 mục 10, PA3 mục 2) | Bảng tính chỉ mô hình TỔNG, chưa bao giờ mô hình HÌNH HỌC. Đứng cách con A 2,0 thì con kề bên ở 3,04 > `enemy.attackRange` 2,8 → hiện luôn **đúng một** con với tới. 15 con (cách 24°) thì **năm** con với tới: sát thương nhận thật đi từ 0,50 lên 1,00/giây. Đây là đổi độ khó **ngoài mô hình** → phải đo tay lại → mở đúng cái vòng "sửa-thử-sai-sửa" mà ràng buộc 5 không cho phép. Và không nới `spawnRadius` để bù được: ortho 7,2 trên màn 9:16 cho nửa bề ngang 4,05, mà 3,5 + nửa sprite 0,5 = 4,00 — đã sát mép (`BuildM1Scene.cs:58`). |
| **`player.attackDamage` 10→5 kèm `attacksPerSecond` 1,0→2,0** (PA3 mục 1) | Chứng minh bất biến D×S=10 là **đúng tuyệt đối** (`PlayerStats` khớp `'Kiểm chứng build'!N5 = K×L×M`), tôi không cãi. Nhưng: (a) B12/B13 là hai ô bảng tính **dẫn chiếu trực tiếp**, đổi chúng là phải mở bảng xác nhận; (b) con số sát thương nhỏ đi làm người chơi **cảm thấy yếu đi** — đi ngược đúng chỗ dư địa "cảm giác mạnh lên" mà cả bối cảnh chỉ ra. Giữ lại làm **van dự phòng**: nếu sau buổi chơi thử vẫn thấy 8 giây là dài, đây là cần gạt rẻ nhất để kéo, và phải kéo **cùng lúc** cả hai số. |
| **`player.splashRatio` 0,5** (PA4 mục 3) | Chạm hai con cùng lúc = 1,5 lần sát thương mỗi đòn. Để có 1,5 lần bằng đường chính thống phải leo `1,044ⁿ = 1,5` → **n ≈ 10 cấp Vũ khí, cho không, ngay từ tầng 1**. Ràng buộc 3 nén 9% xuống 4,4% chính là để ngăn việc này. Cộng thêm tầng thường tụt xuống ~31 giây, dưới dải 40-60s của §5.1. |
| **Bảng trọng số máu/dps cho quái** (PA4 mục 4) | Sát thương thực nhận là `Σ(thời gian giết con i × dps con i)`, không phải `Σdps`. Máu [0,5 0,5 1 1 1,5 1,5] **đối nghịch** dps [1,5 1,5 1 1 0,5 0,5] cho `Σ(hp×dps) = 5,0` so với 6,0 khi đồng đều — **giảm 17% sát thương nhận** trước khi người chơi ra một quyết định khôn ngoan nào. |
| **Cửa sổ né 0,35 giây / luật đòn TRƯỢT** (PA4 mục 1, nửa sau) | Nửa đầu (đóng băng hồi chiêu) là vá lỗi; nửa sau là hoàn tiền, tức nới biên. Đã làm nửa đầu ở Việc 2.1. |
| **Sửa `AutoAttack.cs:58`** (cho hồi chiêu chạy lúc di chuyển) | Rất hấp dẫn vì nó xoá ngay 1 giây màn hình chết. Nhưng né một đòn quái thường chỉ tốn 0,356 giây đi bộ, ngắn hơn hồi chiêu 1,0 giây → người chơi khéo né **sạch** mọi đòn mà không mất một sát thương nào. Đó đúng là "làm cho việc đứng yên hết rủi ro" — **vi phạm ràng buộc 2**. Một giây trống đó được lấp bằng **thông tin và vật thể** (viên Mảnh bay, thanh máu, tiếng động), không phải bằng cách gỡ luật. |
| **Tăng bất kỳ `gear.*.perLevel` nào** | Ràng buộc 3, cứng nhất. |
| **Sửa `can-bang.xlsx`** | Ràng buộc 4. Tôi chỉ **đọc**. |
| **Hạ `auto.unlockFloor`** | Xem mục dưới. |
| **Cài DOTween** (`DESIGN.md:624` có nhắc) | `CameraShake.cs` đã chứng minh coroutine tween là đủ. Thêm một gói là thêm một mớ asmdef và một rủi ro không cần thiết. |
| **Chia đợt quái** (`enemy.waves`) | Đúng lời hứa §5.1 và tôi tin nó hay, nhưng nó là 30 dòng trong `FloorRunner` + một khoá CSV + đo tay lại biên sống sót. Để **Đợt 3**, sau khi biết bảy việc trên đã đủ vui hay chưa. |

---

# `unity/Assets/StreamingAssets/m1-balance.csv` — CHÍNH XÁC NHỮNG GÌ ĐỔI

## Sửa khoá cũ: **KHÔNG MỘT KHOÁ NÀO.**
`gear.*.perLevel` · `enemy.hpFloor1` · `enemy.hpGrowth` · `enemy.dpsFloor1` · `enemy.dpsGrowth` · `enemy.count` · `enemy.attacksPerSecond` · `enemy.attackRange` · `player.attackDamage` · `player.attacksPerSecond` · `player.attackRange` · `player.maxHp` · `player.hpPerFloor` · `shard.perFloor1` · `shard.growth` · `gear.costBase` · `gear.costGrowth` · `crit.meterSize` · `crit.multiplier` · `boss.hpMult1-4` · `boss.count` · `boss.attackRangeMult` · `core.*` · `auto.unlockFloor` · `sweep.seconds` — **giữ nguyên từng chữ số**.

## Thêm khoá mới (chỉ THÊM, nối vào cuối file)

```
# ── QUÉT NHANH: TRẦN NGÂN SÁCH ───────────────────────────────────────────────
# SỬA CHÚ THÍCH CŨ Ở TRÊN: can-bang.xlsx CÓ mô hình farm — ô 'Thông số'!B32 = 2
# ("Hệ số cày lại"), và 'Đường cong tầng'!E = D × B32, tức ngân sách Mảnh lũy kế
# LUÔN là gấp đôi Mảnh leo. Cột cấp trang bị kỳ vọng (G) cũng suy từ E/4.
# Mã đang giao VÔ HẠN (133 Mảnh/giây so với 9,6 khi đánh tay) — nằm ngoài mô hình.
sweep.totalMult,2,Tổng Mảnh cả đời <= hệ số này x Mảnh kiếm từ LEO. = 'Thông số'!B32

# ── RƠI VẬT PHẨM §1 bước 2 — nhịp và thẩm mỹ. TỔNG Mảnh mỗi tầng KHÔNG ĐỔI ────
# Mỗi con mang đúng ShardReward(tầng)/enemy.count. Cố định tuyệt đối, không tỉ lệ
# rớt, không quay số. Viên chưa nhặt được dọn trọn lúc dọn sạch tầng.
loot.popSeconds,0.35,Thời gian viên Mảnh bật ra khỏi xác theo cung
loot.popDistance,1.1,Bật VỀ PHÍA người chơi bao xa — hướng xác định, không random
loot.magnetRadius,1.8,Người chơi trong bán kính này thì viên bắt đầu hút về
loot.flySpeed,9,Tốc độ bay về người chơi
loot.pickupRadius,0.35,Chạm mức này là ăn
loot.bobAmplitude,0.08,Biên độ nhấp nhô lúc nằm chờ
loot.bobSpeed,3,Nhịp nhấp nhô
loot.flushSpeedMult,2.5,Dọn nốt viên còn lại lúc hết tầng thì bay nhanh gấp này

# ── HIỆU ỨNG (thêm vào nhóm juice.* có sẵn) ──────────────────────────────────
# RÀNG BUỘC PHẢI GIỮ: juice.enemyDeathSeconds KHÔNG cộng vào thời lượng tầng, vì
# quái được gỡ khỏi EnemyRegistry ngay khi máu về 0 (xem Enemy.cs).
juice.enemyDeathSeconds,0.22,Hoạt ảnh chết của quái trước khi huỷ
juice.popupDecimalBelow,100,Dưới ngưỡng này thì hiện một chữ số thập phân (10,0 → 10,4)
audio.sfxVolume,0.7,Âm lượng hiệu ứng
audio.musicVolume,0.35,Âm lượng nhạc nền (Đợt 2)
```

## Lý do không lệch biên — từng khoá

- **`sweep.totalMult,2`** — không lệch vì nó **chính là** `'Thông số'!B32 = 2`. Mã hiện giao vô hạn, tức đang lệch theo hướng quá nhiều; đây là siết **về** mô hình. Sau khi siết, cấp trang bị đạt được ở tầng 10 khớp `'Đường cong tầng'!G10` = cấp 7, và biên boss 1 = 1,50 = đúng `'Thông số'!B40`.
- **`loot.*`** — tổng Mảnh mỗi lượt tầng bằng **đúng** `ShardReward(floor)` theo cấu trúc (hũ `_pot` + `FlushAll` + `_paid`). Chỉ **thời điểm** trả đổi, không phải **lượng**. Đây là `m1-balance.csv` (file chạy của game), không phải `can-bang.xlsx`; và §9.2 **bắt buộc** mọi con số phải nằm ở đây thay vì viết cứng trong .cs — để chúng trong C# mới là vi phạm.
- **`juice.*` / `audio.*`** — chính CSV tự khai khối này là "thẩm mỹ, không phải cân bằng". Không xuất hiện trong bất kỳ công thức nào của bảng tính.
- **`Enemy.cs` thứ tự hồi chiêu (Việc 2.1)** — không phải khoá CSV, và nó **giảm** sát thương thực nhận về phía `B9 = 0,5` mà bảng tính vốn đã giả định. Hiện mã giao 100%, tức tầng boss đang **khắc nghiệt gấp đôi** bảng tính.

---

# NÚT TỰ ĐÁNH: GIỮ MỐC 20, CHỈ CHO NHÌN THẤY

**Trả lời thẳng: KHÔNG hạ mốc. Chỉ đổi `SetActive(unlocked)` thành hiện-mờ-có-đếm-ngược.**

Ba lý do, theo thứ tự sức nặng:

1. **Hạ mốc không chữa bệnh, nó chỉ đổi tư thế nằm.** Nếu 7 giây giết một con quái không có phản hồi nào thay đổi, thì mở auto ở tầng 1 chỉ khiến người chơi **ngồi xem** 7 giây không phản hồi mà không cần cầm máy. Vòng lặp không hay hơn, chỉ là người chơi rời tay ra khỏi nó nhanh hơn. Thứ đang thiếu là phản hồi và nhịp trả thưởng — đó là Việc 3, 4, 5.
2. **§5.2 mua mốc 20 bằng một lập luận thật**, và `AutoBattle.cs:11-17` viết rõ auto **cố ý làm ngu** (không né, không chọn mục tiêu, không canh thanh chí mạng) để "tay người chơi luôn nhỉnh hơn máy" và để §1 ("khoảng cách do chăm chỉ") không sụp. Hạ mốc là mở van đó sớm, và van đó là trục chính của thiết kế.
3. **Câu hỏi của chủ dự án là "khi nào mới có", không phải "sao chưa có".** Game đã có sẵn câu trả lời (tầng 20) và **không chỗ nào trên màn hình nói ra**. Một ô xám ghi "TỰ ĐÁNH · CÒN 12 TẦNG" biến 16 phút chờ thành 16 phút **đếm ngược có mục tiêu**; một ô trống biến 16 phút đó thành đi lang thang. Chi phí: 10 phút. Giá trị nhịp độ: rất lớn.

**Nhưng ghi nhận một mâu thuẫn thật trong tài liệu và chốt cách xử:** `DESIGN.md:108-110` ước lượng mốc 20 là "25-35 phút" nhưng 20 tầng × 40-60s chỉ ra 13-20 phút, còn §3 đã chốt "phiên chơi 5-15 phút". Đo thật: ~16-18 phút. Tức mốc auto **dài hơn cả một phiên chơi dài nhất mà chính tài liệu giả định**.

→ **Quyết định chờ số liệu, có tiêu chí cụ thể:** nếu ở bảng bấm giờ (Việc 7) **hai phiên chơi thử liên tiếp kết thúc trước tầng 20**, thì hạ `auto.unlockFloor` từ 20 xuống **10** — không xuống 1. Lý do chọn 10: đó là boss đầu tiên, tức mốc "đã chứng minh biết chơi", trùng với mốc nhận Lõi và mở nhân vật, nên nó gom cả ba phần thưởng vào một khoảnh khắc thay vì rải mỏng. Và vì `AutoBattle` cố ý ngu, mở sớm hơn vẫn không phá §1. **Đây là đổi một khoá CSV, không đụng một công thức nào của bảng tính** (`auto.unlockFloor` không xuất hiện trong `can-bang.xlsx`).

**Về nút QUÉT NHANH**, nói rõ luôn cho khỏi hiểu nhầm: sau Việc 2.2 nó **không còn là nút bỏ qua game**, nó là **ngân sách nghỉ tay** — leo được bao nhiêu thì quét thêm được bấy nhiêu, trần đúng 2× như bảng tính chốt. Nhãn phải nói ra điều đó: `"QUÉT NHANH\nCÒN 2.400"`.

---

# KIỂM CHỨNG — ĐO CÁI GÌ, CHỤP ẢNH GÌ, TEST GÌ

Bài học `quyết định #29` là "nối đúng ≠ chạy đúng ≠ nhìn được"; §8 còn một nấc thứ tư mà M1 đã bỏ qua trong im lặng: **"nhìn được ≠ chơi thấy vui"**. 30 test xanh không phát hiện được một giây nào trong 45 giây trống rỗng của một tầng. Nên có ba tầng kiểm, và tầng thứ ba mới là tầng quyết định.

## A. Năm test tự động mới
**File:** `unity/Assets/Tests/PlayMode/M4FeedbackTests.cs` (mới)

1. **`Tong_Manh_mot_luot_tang_bang_dung_ShardReward`** — giết từng con một qua `TakeDamage`, nhặt hết, dọn sạch; assert `GameState.Shards` tăng **đúng** `ShardReward(floor)`, sai số ≤ 0,01.
2. **`Chet_giua_tang_roi_thu_lai_khong_sinh_them_Manh`** — giết 3/6 con, ép `PlayerHealth` về 0, chờ `Retry`, dọn sạch lượt sau; assert tổng vẫn **đúng** `ShardReward(floor)`. *(Đây là lỗ do chính đề xuất này sinh ra — hôm nay chết = 0 Mảnh.)*
3. **`Quai_chet_thi_Count_giam_NGAY_trong_khung_hinh_do`** — khoá bất biến của hoạt ảnh chết. Đọc `EnemyRegistry.Count` ngay sau lời gọi `TakeDamage` kết liễu, **không `yield`**.
4. **`Quet_nhanh_khong_vuot_tran_2x`** — bơm `HighestCleared`, bật quét chạy 60 giây giả lập; assert `ShardsSwept <= ShardsClimbed * (sweep.totalMult - 1)`.
5. **`Ngoai_tam_thi_khong_don_don`** — đứng trong tầm 4 giây rồi ra ngoài 4 giây, so với đứng trong tầm 8 giây: máu mất ở ca một ≈ **một nửa** ca hai, sai số một đòn.

**Và chạy lại 30 test cũ.** Hai chỗ phải để mắt: `M1LoopTests.cs:107` (cho 14 giây để giết một con — vẫn dư) và `M1LoopTests.cs:102-105` (chú thích "500/6 ≈ 83 máu... ~7 giây" — vẫn đúng vì ta không đổi `enemy.count`).

## B. Ảnh chụp — mở rộng `M1CaptureTest`
**File:** `unity/Assets/Tests/PlayMode/M1CaptureTest.cs`, mảng `Shots` (dòng 30-37). Thêm các mốc:

| Ảnh | Phải nhìn thấy gì |
|---|---|
| `8-thanh-mau-quai` | thanh máu trên đầu con đang bị đánh, vơi rõ |
| `9-khung-quai-chet` | đúng khung hoạt ảnh chết (bắt ở ~0,1s sau đòn kết liễu) |
| `10-manh-dang-bay` | 1-3 viên vàng trên đường bay — **nếu thấy ≥ 5 viên nằm sàn thì `magnetRadius` sai** |
| `11-nut-auto-khoa` | nút TỰ ĐÁNH xám, nhãn "CÒN 12 TẦNG" đọc được, không tràn |
| `12-quet-het-ngan-sach` | nút quét mờ + nhãn hết ngân sách |
| `13-thanh-chi-mang-4-vach` | 4 vạch, sáng 3 → không có cú nhảy cóc |

## C. Bảng bấm giờ — chơi tay 10 tầng, và đây mới là tiêu chí
Ghi tay 6 cột: `tầng · giây/tầng · số Mảnh cuối tầng · HP% lúc vào tầng · khoảng lặng dài nhất (giây) · ghi chú cảm giác`.

**Chỉ tiêu nghiệm thu — đạt/không đạt, không cãi:**

| # | Đo | Hôm nay | Ngưỡng ĐẠT |
|---|---|---|---|
| 1 | Nhịp trả thưởng (giây giữa hai lần nhận Mảnh) | 48-54 s | **≤ 10 s** |
| 2 | Khoảng lặng dài nhất (không số, không tiếng, không vật thể) | 3,4 s trên vòng 8,4 s | **≤ 2,0 s** |
| 3 | Số kênh phản hồi cho một con quái chết | 0 | **≥ 3** (hoạt ảnh chết · tiếng · viên Mảnh bay + số nhảy) |
| 4 | Lần nâng cấp đầu tiên có đổi pixel trên màn hình không | KHÔNG ("10"→"10") | **CÓ** (10,0 → 10,4) |
| 5 | Số đòn để giết một con quái có **nhìn thấy được** không | không có thanh máu | **CÓ**, và giảm 8→7 sau ~2 cấp Vũ khí |
| 6 | HP% lúc bước vào tầng 10 | 16-40% | **≥ 50%** (nếu không đạt → xét lại mục "hồi máu trước boss" ở Đợt 2) |
| 7 | Thắng boss tầng 10 bằng đường leo + quét **trong trần 2×** | không (biên 0,75) | **CÓ**, biên đo được ≥ 1,3 (mục tiêu mô hình 1,50) |
| 8 | Thời lượng một tầng thường | 48-54 s | **giữ trong 40-60 s** của §5.1 |
| 9 | Mỗi phiên chơi thử kết thúc ở tầng nào | — | ghi lại; **< 20 hai lần liên tiếp → hạ `auto.unlockFloor` về 10** |

**Chỉ tiêu số 10, không đo được bằng máy và là chỉ tiêu thật:** chơi liền 15 phút, đặt máy xuống, tự trả lời một câu — **"có vui không"**. Đó đúng là tiêu chí mà `DESIGN.md:562` giao cho M1 và đã bị đổi lặng lẽ thành "30 test xanh" ở quyết định #21. Nếu câu trả lời vẫn là không, **DỪNG, đừng đi tiếp Đợt 2** — kéo cần gạt dự phòng (D×S 10/1 → 5/2 kèm `enemy.waves`) rồi đo lại.

---

# NẾU HỤT THỜI GIAN — THỨ TỰ CẮT

Cắt từ dưới lên, không bao giờ cắt ngược:

1. Cắt **Việc 6** (HUD chạy số + banner) → còn 4,5 ngày. Mất đích đến của viên Mảnh, nhưng viên Mảnh vẫn bay và vẫn kêu.
2. Cắt **Việc 5** (rơi đồ) → còn 3 ngày. **Mất câu trả lời đúng nguyên văn cho chủ dự án** — chỉ cắt nếu thật sự bí.
3. **KHÔNG BAO GIỜ** cắt Việc 1 (2 giờ), Việc 2 (hai lỗ cân bằng), Việc 3 (âm thanh), Việc 4 (thanh máu + chết).

Bản tối thiểu chạy được là **Việc 1 + 2 + 3 + 4 ≈ 2,5 ngày**, và tự nó đã đổi: game hết câm, quái có thanh máu vơi dần và chết thành một sự kiện, nâng cấp đổi pixel, nút auto trả lời được câu hỏi của chính nó, boss tầng 10 lần đầu tiên thắng được bằng đường leo.