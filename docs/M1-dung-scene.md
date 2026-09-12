# M1 — Vòng lặp sống

Mốc M1 của [DESIGN.md](../docs/DESIGN.md) §9.6. Tồn tại để trả lời **đúng một câu hỏi**:

> Vòng lặp *đứng-yên-để-đánh* có vui không?

Nếu câu trả lời là không, mọi con số trong `can-bang.xlsx` đều vô nghĩa.

**Không có trong M1:** giao diện nâng cấp · tầng · boss · Lõi · save · quét nhanh · auto-battle.
Số trong `m1-balance.csv` là **số bịa cho dễ chơi**, không phải số đã cân bằng.

> ⚠️ **Quái CÓ đánh lại.** Bản nháp đầu của khung này không cho quái gây sát thương — khi đó
> đứng yên là chiến thuật trội tuyệt đối và M1 không thể trả lời câu hỏi của chính nó.
> Sức căng nằm ở hai con số trong CSV: `player.attackRange` (2.0) **nhỏ hơn**
> `enemy.attackRange` (2.8). Vào được tầm đánh của mình cũng là vào tầm ăn đòn.
> **Đảo ngược hai số này là giết sạch M1.**

---

## 0. Scene đã dựng sẵn — không phải làm tay

Toàn bộ mục 1 và 2 dưới đây **đã được dựng bằng mã và kiểm chứng tự động**. Mở
`Assets/Scenes/M1.unity` rồi bấm Play là chạy. Giữ lại phần hướng dẫn tay để bạn hiểu
scene gồm những gì, và để dựng lại nếu cần.

| Công cụ | Menu | Làm gì |
|---|---|---|
| `BuildM1Scene.cs` | `Tower RPG ▸ Dựng scene M1` | Dựng lại toàn bộ scene từ đầu, ghi đè |
| `VerifyM1Scene.cs` | `Tower RPG ▸ Kiểm scene M1` | Soi 18 tham chiếu + 10 mục dễ hỏng |
| `M1LoopTests.cs` | `Window ▸ General ▸ Test Runner` | 8 test PlayMode chạy game thật |

**Chạy không cần mở Unity:**

```bash
cd unity
U=/Applications/Unity/Hub/Editor/6000.0.83f1/Unity.app/Contents/MacOS/Unity
"$U" -batchmode -quit -projectPath . -executeMethod TowerRpg.EditorTools.BuildM1Scene.Build  -logFile -
"$U" -batchmode -quit -projectPath . -executeMethod TowerRpg.EditorTools.VerifyM1Scene.Verify -logFile -
"$U" -runTests -batchmode -projectPath . -testPlatform PlayMode -testResults ket-qua.xml -logFile -
```

### Tám test PlayMode chứng minh được gì

| Test | Chứng minh |
|---|---|
| `Nap_duoc_so_lieu_can_bang` | CSV nạp xong, không thiếu khoá |
| `Sinh_dung_so_quai` | Đúng 6 quái theo `enemy.count` |
| `Bat_dau_o_giua_thi_an_toan` | Đứng giữa thì không ai đánh ai — tầm đánh đặt đúng |
| `Dung_yen_trong_tam_thi_danh_va_giet_duoc_quai` | **§5.3** — đứng yên thì đánh, và giết được |
| `Dung_yen_trong_tam_thi_AN_DON` | **§5.3** — đứng yên có GIÁ, máu tụt thật |
| `Di_chuyen_thi_KHONG_danh` | **§5.3** — di chuyển thì quái không mất một điểm máu nào |
| `Chi_mang_dung_nhip_cu_5_don_mot_lan` | **§5.4** — đúng 5 đòn một chí mạng, kiểm 4 vòng liên tiếp |
| `Thanh_chi_mang_KHONG_reset_khi_di_chuyen` | **§5.4** — chạy vòng quanh xong, đòn kế vẫn là chí mạng |

### Chụp ảnh game đang chạy

`M1CaptureTest.cs` vào Play thật và chụp 5 khung hình ra `anh-chup/`:

```bash
cd unity && "$U" -runTests -batchmode -projectPath . -testPlatform PlayMode \
  -testFilter "TowerRpg.Tests.M1CaptureTest" -testResults cap.xml -logFile -
```

Nó nằm trong bộ test PlayMode vì đó là **đường duy nhất** vào được Play mode không giao diện —
gọi `EditorApplication.EnterPlaymode()` từ `-executeMethod` thì Unity treo, đã thử.
Cũng đừng dùng `WaitForEndOfFrame` trong đó: batchmode không kích hoạt nó.

Ảnh chụp bắt được hai lỗi bố cục mà 8 test xanh **không hề phát hiện**: cần gạt nằm chen vào
giữa màn hình, và camera hẹp tới mức quái ở rìa vòng bị cắt mép. Test kiểm hành vi; muốn biết
game *trông* thế nào thì phải nhìn.

Bốn test cuối là **luật cốt lõi của cả game**. Nếu sau này sửa gì làm chúng đỏ, đừng sửa test —
sửa mã, hoặc thừa nhận là đã đổi thiết kế.

---

## 1. Tạo project

Unity Hub → New project → **2D (URP)** → **6000.0.83f1** → đặt tại `tower-rpg/unity`.

Chép toàn bộ `unity/Assets/` vào `unity/Assets/`.

Rồi làm ba việc bắt buộc:

1. **Window → TextMeshPro → Import TMP Essential Resources** *(không có bước này thì số sát thương không hiện)*
2. **Project Settings → Player → Resolution and Presentation → Default Orientation = Portrait**
3. **Unity → Settings → External Tools → External Script Editor = Visual Studio Code**

**Sprite tạm:** chưa cần asset pack. Dùng `Assets → Create → 2D → Sprites → Circle` cho nhân vật
và `Square` cho quái. M1 không quan tâm đến hình thức.

---

## 2. Dựng scene

```
Main Camera                + CameraShake            target = Main Camera
Bootstrap                  + BalanceConfig, GameBootstrap, EnemySpawner, DamagePopupSpawner
Player                     + Rigidbody2D, SpriteRenderer, PlayerController,
                             PlayerHealth, CritMeter, AutoAttack
  └ CritBarCanvas          Canvas (World Space), Scale 0.01, Pos Y = -0.7
      └ CritFill           Image + CritMeterUI
Canvas                     Screen Space – Overlay
  ├ Joystick               Image + VirtualJoystick      neo góc dưới-trái, Width=Height=280
  │   └ Handle             Image                        Width=Height=110
  └ HealthBar              Image + PlayerHealthUI       neo trên cùng
EventSystem
```

**Rigidbody2D của Player:** `Gravity Scale = 0` · `Freeze Rotation Z = ✓` · `Body Type = Dynamic`

**CritFill và HealthBar (Image):** `Image Type = Filled` · `Fill Method = Horizontal` · `Fill Origin = Left`

> ⚠️ **Joystick phải có Width/Height cụ thể, không dùng neo kéo giãn.** Với neo kéo giãn thì
> bán kính bằng 0 và cần gạt chết câm. Script sẽ báo lỗi đỏ nếu bạn dựng sai.

### Prefab cần tạo

| Prefab | Gồm | Ghi chú |
|---|---|---|
| `Enemy` | SpriteRenderer + script `Enemy` | Kéo vào `EnemySpawner.enemyPrefab` |
| `DamagePopup` | **TextMeshPro - Text** (bản 3D, KHÔNG phải bản UI) + script `DamagePopup` | Kéo vào `DamagePopupSpawner.popupPrefab` |

### Bảng kéo thả — làm đủ, thiếu ô nào script sẽ báo lỗi đỏ

| Script | Trường | Kéo vào |
|---|---|---|
| GameBootstrap | balance · spawner · playerHealth | Bootstrap · Bootstrap · Player |
| EnemySpawner | enemyPrefab · arenaCentre | prefab Enemy · *(để trống = gốc toạ độ)* |
| DamagePopupSpawner | popupPrefab | prefab DamagePopup |
| PlayerController | joystick | Joystick |
| AutoAttack | player · critMeter · popups · cameraShake | Player · Player · Bootstrap · Main Camera |
| CritMeterUI | meter · fillImage | Player · CritFill |
| PlayerHealthUI | health · fillImage | Player · HealthBar |
| **VirtualJoystick** | **background · handle · canvas** | **Joystick · Handle · Canvas** |
| CameraShake | target | Main Camera |

---

## 3. Chạy thử — kiểm đúng năm điều

1. **Di chuyển thì KHÔNG đánh.** Giữ cần gạt → số sát thương ngừng hiện. Thả ra → đánh lại.
2. **Hồi chiêu cũng dừng khi di chuyển.** Nhấp-nhả cần gạt liên tục phải làm sát thương
   **giảm rõ rệt**. Nếu vẫn đánh đủ nhịp thì luật §5.3 đã mất răng.
3. **Cứ đúng 5 đòn thì 1 đòn chí mạng.** Đếm tay. Không bao giờ 4, không bao giờ 6.
4. **Thanh chí mạng KHÔNG reset khi di chuyển.** Đánh 4 đòn → chạy vòng quanh → đứng lại →
   đòn kế tiếp phải là chí mạng ngay. Đây là chiến thuật chữ ký của §5.4.
5. **Đứng yên phải thấy đau.** Máu tụt khi đứng trong tầm quái. Không thấy tụt nghĩa là
   `enemy.attackRange` đang nhỏ hơn `player.attackRange` — sửa CSV.

---

## 4. Quy tắc cứng (§9.2)

> Không một con số cân bằng nào được viết trong `.cs`.

Tất cả nằm trong `Assets/StreamingAssets/m1-balance.csv`. Sửa file đó rồi chạy lại — không cần
biên dịch. Thiếu một khoá là `BalanceConfig` báo lỗi đỏ và **không khởi động trận đấu**, thay vì
âm thầm chạy với số 0.

Giá trị trong `[SerializeField]` là **thẩm mỹ** (màu sắc, tỉ lệ phóng, tần số rung) — đúng chỗ.

> **Vì sao mọi script đọc số qua `BalanceConfig.TryUse(...)` chứ không gọi thẳng `Get()`:**
> trên Android, StreamingAssets nằm trong APK nên phải đọc bất đồng bộ. Gọi `Get()` trong
> `Start()` sẽ nhận về **0 cho mọi chỉ số** — nhân vật đứng im, quái đứng im, không một đòn nào
> được đánh. Trong Editor lỗi này **hoàn toàn vô hình**. Đừng bỏ `TryUse` đi.

---

## 5. Xong M1 thì làm gì

Trả lời câu hỏi ở đầu file. **Nếu không vui, dừng lại và sửa ở đây** — đừng đi tiếp sang M2.

Chỗ để vặn, theo thứ tự đáng thử: `enemy.damage` · `player.attacksPerSecond` ·
`crit.meterSize` · `crit.multiplier` · khoảng cách giữa `player.attackRange` và
`enemy.attackRange`. Vòng lặp này đứng hay sụp ở nhịp giữa *đứng yên ăn đòn* và
*chạy đi cho an toàn*.
