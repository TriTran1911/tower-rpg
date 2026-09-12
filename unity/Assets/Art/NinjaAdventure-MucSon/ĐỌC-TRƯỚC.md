# Ninja Adventure — bộ asset đã chốt

Tác giả: **Pixel-boy & AAA** · https://pixel-boy.itch.io/ninja-adventure-asset-pack
Bản tải: 29/03/2026 · 89 MB · 2.234 file
Giấy phép: **CC0 1.0 Universal** (xem `LICENSE.txt` kèm theo)

CC0 nghĩa là: dùng thương mại được, sửa được, **đẩy lên repo công khai được**, không bắt buộc
ghi công. Đây là lý do chính nó thắng hai lựa chọn trả phí 39,99 đô — cả hai đều cấm phát tán
file art thô, tức phải `.gitignore` toàn bộ thư mục này nếu muốn public repo portfolio.

*(Ghi công không bắt buộc nhưng nên có — tác giả đáng được nhắc trong phần credit của game.)*

---

## Có gì (đã đếm trên máy, không phải theo trang bán)

| | Số thật | Trang bán ghi |
|---|---|---|
| Quái thường | **66** con, 7 nhóm chủ đề | "30+" |
| Boss | **20** mục | "9" |
| Tileset | **19** file | 8 |
| Nhạc / SFX / jingle | **41 / 132 / 15** | "37 / 100+" |
| Hiệu ứng FX | 105 file | "30+" |

**7 nhóm quái theo chủ đề** — cần 5 cho 5 chương (§5.1), nên dư 2:

| Nhóm | Số con |
|---|---|
| thú | 14 |
| bò sát / rồng | 12 |
| thuỷ sinh | 12 |
| côn trùng / thực vật | 11 |
| quỷ / nguyên tố | 8 |
| undead / hồn | 6 |
| slime | 4 |

**Sàn đấu trường** (§5.1 cần, tưởng không có): `TilesetFloor` · `TilesetFloorB` ·
`TilesetFloorDetail` · `TilesetDungeon`.

**Nhân vật người chơi** `Actor/CharacterAnimated/NinjaGreen/Separate/` — đầy đủ:
Attack · Dead · Hit · Idle · Walk · Roll · Jump · Climb · Swim · Push · Item · Pickup.

---

## ⚠️ Ba thiếu sót đã biết — đọc trước khi lập lịch

### 1. Quái thường KHÔNG có hoạt ảnh đánh và KHÔNG có hoạt ảnh chết

Mỗi con chỉ có `Faceset.png` + `SpriteSheet.png` **64×64** = 4 hướng × 4 frame **đi bộ**, hết.

**Cách lấp, dùng đồ có sẵn trong chính bộ này:**
- Quái đánh → phát một trong 8 hiệu ứng ở `FX/Attack/` tại vị trí quái *(CircularSlash, Claw,
  ClawDouble, Cut, CutDouble, CutX, SlashCurved, SlashDoubleCurved)*
- Quái chết → `FX/Smoke/SmokeCircular/` hoặc `FX/Elemental/Explosion/` + sprite mờ dần

Chấp nhận được, nhưng phải biết trước: quái chết sẽ là **phụt khói rồi biến mất**, không phải
ngã xuống. Ở mốc M5 có thể thấy chưa đã.

### 2. Boss có Idle + Walk + Hit, nhưng cũng không có hoạt ảnh chết
Dùng chung cách lấp ở trên, với FX cỡ lớn hơn.

### 3. Chỉ có icon vũ khí
`Items/Weapons/` có **45 icon**. **Không có giáp, găng, nhẫn** — ba trong bốn ô của §5.5.

Phải tự vẽ hoặc mua một bộ icon nhỏ khác tác giả. Đây là **ngoại lệ hợp lý với quyết định #13**:
icon 16×16 nằm trong khung UI, không bao giờ đứng cạnh sprite nhân vật trên cùng một khung hình,
nên rủi ro lệch phong cách gần như bằng không. Ghép **tileset** hay **quái** khác tác giả mới là
thứ #13 cấm — và cấm đúng.

---

## Lưu ý kỹ thuật khi nhập vào Unity

Sprite gốc **16×16**, nhỏ nhất trong các ứng viên. Bắt buộc:

- **Filter Mode = Point (no filter)** — để Bilinear là nhoè hết
- **Compression = None** — nén sẽ làm bẩn từng điểm ảnh
- **Pixels Per Unit = 16**
- Phóng theo **bội số nguyên** (3x / 4x), không bao giờ phóng lẻ
- **HUD và số sát thương bay lên phải dùng font khác** — font pixel 5px của bộ này ở cỡ gốc
  sẽ không đọc được trên màn hình dọc

Chưa ai kiểm bộ này trên **màn hình dọc điện thoại thật**. Việc đó thuộc mốc M1, làm sớm.
