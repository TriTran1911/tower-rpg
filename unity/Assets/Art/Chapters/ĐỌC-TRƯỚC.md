# Năm bảng nền theo chương

**Thư mục sinh tự động. Đừng sửa tay.** Nguồn: `art-source/NinjaAdventure/Backgrounds/`

```bash
python3 tools/doi-bang-mau.py <slug> art-source/NinjaAdventure \
        unity/Assets/Art/Chapters/<slug> --only Backgrounds
```

Chỉ sinh lại `Backgrounds/` — **quái đỏ và nhân vật lam ngọc giữ nguyên suốt 100 tầng**,
lấy từ `../NinjaAdventure-MucSon/`. Người chơi học thứ bậc đọc một lần rồi dùng mãi; đổi nó
theo chương là phá luôn giá trị của hệ thống. Cả 5 chương chỉ tốn 2,4 MB.

| # | Chương | Tầng | Sàn | L* | Ý |
|---|---|---|---|---|---|
| 1 | Nền Đá | 1–20 | `#787378` | 49,0 | Đá xám, phần tháp do người xây |
| 2 | Chiếu & Giấy Dầu | 21–40 | `#827f5a` | 52,6 | Chiếu, giấy dầu ám khói, gỗ sẫm |
| 3 | Nắng Đồng | 41–60 | `#cbcc9a` | 80,9 | Mất mái, nắng gắt — chói nhất, giữa tháp |
| 4 | Chàm Đêm | 61–80 | `#696e9f` | 47,8 | Chàm thẫm, đã trên tầng mây |
| 5 | Sương Lệch | 81–100 | `#ddd5de` | 86,1 | Loãng, nhạt, không thuộc thế giới này |

Độ sáng đi so le **49 → 53 → 81 → 48 → 86**, nên không chương nào trùng cảm giác với chương
liền trước. Hai cú lật lớn rơi đúng vào tầng 41 (bừng sáng) và tầng 61 (sập tối).

---

## Số liệu kiểm chứng — đo trên TILE THẬT

| Ràng buộc | Ngưỡng | Thực tế | |
|---|---|---|---|
| ΔE tới quái đỏ | ≥ 45 | **66,2** | ✅ dư 21 |
| ΔE tới nhân vật lam | ≥ 45 | **44,2** | ⚠️ thiếu 0,8 *(chương 3)* |
| ΔE giữa hai chương | ≥ 25 | **24,3** | ⚠️ thiếu 0,7 *(chương 1↔2)* |

Hai thiếu sót đều **dưới một đơn vị** và nằm ở ngưỡng do chính ta đặt ra, không phải ngưỡng
tri giác. Dừng ở đây là có chủ ý: tiếp tục vặn nữa là lợi ích giảm dần trên một thứ thuần
thẩm mỹ, trong khi mốc M1 còn chưa chạy lần nào.

## Bài học: đừng kiểm ở chỗ không có pixel

Bản thiết kế đầu được kiểm rất kỹ tại **mốc giữa dải** (t = 0,50) và đạt mọi ràng buộc.
Nhưng khi render ra ảnh thật thì ba cặp chương rơi xuống ΔE 13–17.

Lý do: pixel của tile sàn hầu hết **nằm ở đoạn sáng** (t ≈ 0,85–1,0), nơi cả năm bảng đều
hội tụ về gần trắng. Mốc giữa đẹp nhưng **không có pixel nào ở đó**.

Cách sửa đã dùng: kéo hai mốc sáng (0,90 và 1,00) về phía sắc của chương, rồi giãn độ sáng
giữa các cặp còn đụng nhau. Từ ΔE 13,2 lên 24,3.

> **Mọi lần chỉnh bảng màu sau này đều phải render ra ảnh rồi đo lại trên tile thật.**
> Số ở mốc giữa không nói lên điều gì về thứ người chơi nhìn thấy.

## Còn để mở

- **UI và FX vẫn đổi màu theo bảng nền.** Hiện `rule_for()` xếp mọi thứ ngoài
  `CharacterAnimated`/`Monster`/`Boss` vào tầng "thế giới". Nếu muốn thanh máu và số sát
  thương giữ một màu suốt 100 tầng thì phải tách chúng ra khỏi quy tắc đó.
- **Mỗi chương một tileset gốc khác nhau.** Bộ asset có 16 tileset (Dungeon, House, Field,
  Water, Towers, Nature, Desert…). Dùng tileset khác nhau *cộng* sắc riêng sẽ cho khác biệt
  mạnh hơn nhiều so với chỉ đổi màu một tile. Đã thử, chưa áp.
- Chưa kiểm trên **màn hình điện thoại thật ngoài nắng**. Chương 3 và 5 sáng, có thể chói.
