# Tower RPG

Game mobile 2D, nhìn từ trên xuống, chơi một tay trên màn hình dọc. Leo 100 tầng, đánh quái,
nâng cấp trang bị. Đang ở mốc M1 — vòng lặp chiến đấu chạy được và đã kiểm chứng tự động.

**Unity 6000.0.83f1 LTS · URP 2D · C#**

![Chuỗi khung hình game M1 đang chạy](docs/anh-m1.png)

---

## Nguyên tắc nền: không có yếu tố ngẫu nhiên ở bất kỳ đâu

Không gacha, không hòm đồ, không tỉ lệ rơi — **kể cả chí mạng cũng không phải xác suất**.
Chí mạng dùng *thanh dồn*: cứ đúng 5 đòn thì đòn thứ 5 chắc chắn chí mạng, thanh hiển thị
đầy dần dưới chân nhân vật.

Điều đó giữ được toàn bộ khoái cảm của đòn chí mạng, nhưng đổi lại ba thứ: chơi quanh nó được
(lùi lại chờ thanh đầy rồi dồn vào lúc boss hở sườn), không bao giờ gây ức chế vì xui, và
**tính được bằng bảng tính** — nên cân bằng được bằng số thay vì bằng cảm giác.

## Luật cốt lõi

> Di chuyển thì an toàn nhưng không gây sát thương.
> Đứng yên thì gây sát thương nhưng ăn đòn.

Không có nút tấn công. Nhân vật tự đánh kẻ gần nhất, **và chỉ khi đang đứng yên**. Một luật
duy nhất tạo ra toàn bộ sức căng — tầm đánh của quái cố ý lớn hơn tầm đánh của người chơi,
nên vào được vị trí đánh cũng là vào vị trí ăn đòn.

## Ba tầng đọc

| | Màu | |
|---|---|---|
| Thế giới | xám trung tính | nền, không tranh sự chú ý |
| Quái | **đỏ** | mối đe doạ |
| Nhân vật | **lam ngọc** | bạn, màu duy nhất không ai khác có |

Không phải chuyện thẩm mỹ mà là thứ bậc đọc trên màn hình dọc nhỏ: bạn phải theo dõi được
vị trí của chính mình *trong lúc đứng yên ăn đòn*. Năm chương đổi sắc nền, hai tầng còn lại
giữ nguyên suốt 100 tầng.

---

## Điều đáng đọc nhất ở đây không phải code

[`docs/DESIGN.md`](docs/DESIGN.md) — 570 dòng, **21 quyết định** đều ghi kèm phương án đã loại
và lý do loại. Cùng [`docs/can-bang.xlsx`](docs/can-bang.xlsx), bảng tính kiểm chứng toàn bộ
đường cong độ khó.

Tài liệu ghi thẳng những chỗ thiết kế **hỏng**, vì giấu đi thì người sau phải tự khám phá lại:

- Điều kiện *"mọi build hợp lý đều qua được tầng 100"* là **bất khả thi về mặt đại số** —
  đã thay bằng một quy tắc kiểm chứng được, không phải bằng cách chỉnh số cho đẹp.
- *"Hai hướng xây dựng đối lập"* **chưa bao giờ đúng**. Quét 420.000 cấu hình: cứu được hướng
  này *hoặc* hướng kia, **không bao giờ cả hai** — vì hai build là ảnh gương của nhau.
- **Ô Nhẫn là ô đổ rác**: 84 phép hoán vị Lõi, 0 lần có lợi.

## Quy tắc cứng: không một con số cân bằng nào nằm trong `.cs`

Tất cả ở [`m1-balance.csv`](unity/Assets/StreamingAssets/m1-balance.csv). Sửa file, chạy lại,
không cần biên dịch. Thiếu một khoá là game báo lỗi đỏ và **không khởi động trận đấu**, thay vì
âm thầm chạy với số 0.

---

## Chạy thử

```bash
cd unity
U=/Applications/Unity/Hub/Editor/6000.0.83f1/Unity.app/Contents/MacOS/Unity

# dựng lại scene M1 từ đầu
"$U" -batchmode -quit -projectPath . -executeMethod TowerRpg.EditorTools.BuildM1Scene.Build -logFile -

# soi 18 tham chiếu + 10 mục dễ hỏng
"$U" -batchmode -quit -projectPath . -executeMethod TowerRpg.EditorTools.VerifyM1Scene.Verify -logFile -

# 9 test PlayMode chạy game thật
"$U" -runTests -batchmode -projectPath . -testPlatform PlayMode -testResults ket-qua.xml -logFile -
```

Bốn trong chín test khoá chặt luật cốt lõi: đứng yên thì đánh **và** ăn đòn, di chuyển thì quái
không mất một điểm máu nào, đúng 5 đòn một chí mạng, và thanh chí mạng **không** reset khi
di chuyển. Sửa gì làm chúng đỏ thì đừng sửa test — sửa mã, hoặc thừa nhận đã đổi thiết kế.

## Cấu trúc

```
docs/            DESIGN.md · can-bang.xlsx · M1-dung-scene.md
tools/           doi-bang-mau.py — sinh mọi biến thể màu từ bản gốc
art-source/      Ninja Adventure bản gốc (CC0) — ngoài Unity, không bị import
unity/Assets/
  Scripts/       16 file mã game
  Editor/        dựng scene · kiểm scene
  Tests/         9 test PlayMode
  Art/           bản đã đổi màu + 5 nền theo chương
  StreamingAssets/m1-balance.csv
```

## Ghi công

Đồ hoạ và âm thanh: **[Ninja Adventure Asset Pack](https://pixel-boy.itch.io/ninja-adventure-asset-pack)**
của Pixel-boy & AAA, giấy phép **CC0 1.0**. Bảng màu đã được xử lý lại bằng `tools/doi-bang-mau.py`;
bản gốc giữ nguyên trong `art-source/`.

CC0 không bắt buộc ghi công — ghi ở đây vì họ xứng đáng.
