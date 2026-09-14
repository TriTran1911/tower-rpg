# Thiết kế game — Tower RPG (tên tạm)

> Tài liệu thiết kế, viết trước khi code.
> Trạng thái: **đã chốt thiết kế và engine (Unity 6 + C#)**.
> Số liệu cân bằng đã kiểm chứng bằng `can-bang.xlsx` — xem mục 5.7, 5.8 và quyết định #15.
> Ngày: 2026-09-12

---

## 1. Tóm tắt hiểu biết

Game RPG 2D mobile, chơi một mình, **hoàn toàn offline**. Vòng lặp cốt lõi:

```
đánh quái → rơi vật phẩm (cố định) → nâng cấp trang bị → mạnh hơn → lên tầng cao hơn
```

**Nguyên tắc nền, không được vi phạm:** không có yếu tố may rủi ở bất kỳ đâu.
Không gacha, không hòm đồ, không tỉ lệ rơi, không tỉ lệ chí mạng.
Mọi kết quả đều xác định và tính toán được.

**Mục đích:** sản phẩm portfolio. Ràng buộc số một là **phải hoàn thành**.
Ưu tiên hoàn thiện và bóng bẩy hơn là sâu và nhiều.

**Cái hay của game đến từ đánh đổi, không đến từ xác suất.**
Tài nguyên luôn thiếu so với nhu cầu; người chơi buộc phải chọn tiêu vào đâu,
và mỗi lựa chọn đóng lại một cánh cửa khác.

---

## 2. Không làm (non-goals)

Những thứ dưới đây bị loại bỏ **có chủ ý**, không phải vì quên:

| Loại bỏ | Lý do |
|---|---|
| Backend / server | Offline hoàn toàn; không chi phí vận hành |
| IAP, quảng cáo | Portfolio không cần doanh thu |
| PvP, guild, chat, bảng xếp hạng | Đội chi phí gấp nhiều lần, không phục vụ mục tiêu |
| Gacha, hòm đồ, tỉ lệ rơi | Trái nguyên tắc nền |
| **Đội hình** nhiều nhân vật cùng đánh | Buộc phải bỏ §5.3 — sáu nhân vật thì không đặt vị trí từng người trên màn hình dọc được, nó thành auto-battle. *(Đổi nhân vật thì KHÁC và ĐƯỢC PHÉP — xem §5.5b)* |
| Cốt truyện, lồng tiếng, cutscene | Ngốn thời gian, không chứng minh năng lực kỹ thuật |
| Thiết kế màn chơi (level design) | Đấu trường kín thay thế; xem mục 5.1 |

---

## 3. Giả định

1. **Nền tảng:** build Android + iOS; test chính trên iOS.
2. **Màn hình dọc**, chơi một tay.
3. **Phiên chơi 5–15 phút**, chơi ngắt quãng.
4. **Trục "nạp tiền" chỉ tồn tại trên giấy** — thiết kế đường cong kinh tế và
   chứng minh nó cân bằng, nhưng không tích hợp IAP.
5. **Save lưu trên máy**, chấp nhận người chơi có thể sửa. Không có gì để mất.
6. **Ngôn ngữ hiển thị: tiếng Anh.**
7. **Mốc thực tế: 3 tháng cho bản chơi được trọn vẹn**, mở rộng sau.
8. **Đồ họa: đã chốt — Ninja Adventure** (Pixel-boy & AAA), giấy phép **CC0 1.0**.
   Nằm tại `art-source/NinjaAdventure/`, kèm `ĐỌC-TRƯỚC.md` ghi đầy đủ nội dung
   và ba thiếu sót đã biết. 66 quái (7 nhóm chủ đề), 20 boss, 19 tileset, 105 FX.
   **Phong cách Đông Á** (samurai, tengu, kappa, oni) — đây là cửa một chiều, tháp sẽ không
   phải fantasy phương Tây.
9. **Nhạc và âm thanh: đã chốt** — cùng bộ trên, cũng CC0: 41 nhạc, 132 SFX, 15 jingle.
10. **Bảng màu: đã đổi — "Mực & Son"**, sinh tự động tại
    `unity/Assets/Art/NinjaAdventure-MucSon/`. Ba tầng đọc: thế giới **xám**,
    quái **đỏ**, nhân vật **lam ngọc**. Bản gốc giữ nguyên làm đầu vào cho biến thể sau.
11. **Năm nền theo chương: đã chốt** — `unity/Assets/Art/Chapters/`, chỉ 2,4 MB vì chỉ
    sinh lại `Backgrounds/`. Nền Đá → Chiếu & Giấy Dầu → Nắng Đồng → Chàm Đêm → Sương Lệch.
    Quái và nhân vật **giữ nguyên cả 100 tầng**.

---

## 4. Yêu cầu phi chức năng

| Hạng mục | Yêu cầu |
|---|---|
| Hiệu năng | 60fps ổn định trên máy tầm trung đời 2020 |
| Quy mô dữ liệu | Một người chơi, save vài trăm KB |
| Bảo mật | Không áp dụng — không dữ liệu cá nhân, không mạng, không tiền |
| Độ tin cậy | **Hỏng save là rủi ro thật duy nhất.** Bắt buộc ghi nguyên tử + giữ 1 bản backup |
| Bảo trì | Làm một mình, sẽ có lúc nghỉ vài tuần. **Toàn bộ số liệu cân bằng phải nằm ngoài code** |

---

## 5. Thiết kế

### 5.1 Cấu trúc tháp

**100 tầng, chia 5 chương × 20 tầng.** Mỗi chương dùng một bộ tile và một nhóm quái
riêng — đây là cách xử lý điểm yếu đơn điệu của cấu trúc tháp. Người chơi bước sang
tầng 21 và thấy cả thế giới đổi màu: phần thưởng thị giác không tốn dòng code nào.

**Một tầng:** đấu trường kín, 5–8 quái theo đợt, khoảng 40–60 giây.

**Mỗi 10 tầng có một boss.** Boss là cổng chặn thật sự và là nơi **duy nhất** rơi ra
tài nguyên hiếm. Toàn bộ đường cong tiến trình neo vào đây.

**Không có thiết kế màn chơi.** 100 tầng chỉ là 100 cấu hình dạng
*"đợt nào, quái gì, bao nhiêu con"* — dữ liệu thuần túy trong một file.
Đây là quyết định tiết kiệm công sức lớn nhất của toàn bộ dự án.

### 5.2 Hai cơ chế tự động

| Cơ chế | Mở khi nào | Tác dụng |
|---|---|---|
| **Quét nhanh** | Clear một tầng lần đầu | Tầng đó bấm một nút là nhận thưởng, không đánh lại. Khiến việc farm không thành cực hình. **Có hồi 3 giây** — xem quyết định #27 |
| **Tự động chiến đấu** | Hoàn thành chương 1 (tầng 20) | Nhân vật tự đánh tầng mới. Cột mốc lớn — cần màn hình chúc mừng riêng |

**Vì sao mốc auto là tầng 20:** đủ lâu để người chơi hiểu hệ thống bằng tay và thấy
việc đánh tay có ý nghĩa, đủ sớm để chưa kịp chán. Với 40–60 giây/tầng, đây là
khoảng **25–35 phút** chơi tay đầu tiên — đúng ngưỡng chú ý của người chơi mobile.

### 5.3 Chiến đấu

**Nhìn từ trên xuống, màn hình dọc, đấu trường kín.**

**Điều khiển: một ngón tay.** Cần gạt ảo bên trái để di chuyển. Không có nút tấn công —
nhân vật tự đánh kẻ địch gần nhất, **nhưng chỉ khi đứng yên**.

> **Luật cốt lõi:** Di chuyển thì an toàn nhưng không gây sát thương.
> Đứng yên thì gây sát thương nhưng ăn đòn.

Một luật duy nhất tạo ra toàn bộ chiều sâu của giai đoạn đánh tay. Không cần thêm
hệ thống nào, sức căng đã có sẵn.

### 5.4 Chí mạng xác định — thanh dồn

Thay vì "20% cơ hội chí mạng": mỗi đòn đánh nạp một thanh; thanh đầy thì
**đòn kế tiếp chắc chắn chí mạng**. Thanh hiển thị ngay dưới nhân vật.

Cùng một khoái cảm — số to, màu vàng, rung màn hình — nhưng không có xác suất nào.
Tốt hơn xác suất ở ba điểm:

1. **Chơi quanh được.** Thanh gần đầy mà boss sắp ra đòn? Lùi lại, chờ, rồi dồn
   chí mạng vào lúc boss hở sườn. Kỹ năng thật, xuất hiện đúng ở giai đoạn đánh tay.
2. **Không bao giờ gây ức chế.** Không có chuyện "xui 10 đòn liền rồi chết".
   Mọi thất bại đều truy được về quyết định.
3. **Tính toán được.** Ngồi tính chính xác DPS trên bảng tính — cân bằng dễ hơn bội phần.

Ghép với luật 5.3: muốn dồn đủ thanh để tung chí mạng vào boss, phải **đứng yên
đủ lâu** — đúng lúc nguy hiểm nhất.

### 5.5 Trang bị

| Ô | Chỉ số | Vai trò thật (đo được, không phải ý định) |
|---|---|---|
| Vũ khí | Sát thương mỗi đòn (+4,4%/cấp) | Trục sức mạnh. Đóng góp trần 20→60: **x5,60** |
| Giáp | Máu tối đa + hút máu (+4,4%/cấp) | **Nguồn máu duy nhất**, nên là trục lựa chọn duy nhất. **x5,60** |
| Găng | Tốc độ đánh (+3,4%/cấp) | Trục **nhịp tay**. x3,87 |
| Nhẫn | Hệ số chí mạng (+3,4%/cấp) | Trục **cảm giác**. x2,40 — thấp nhất |

**Chỉ bốn ô. Không thêm.**

> **Mục này mô tả sự thật đo được của mô hình, không mô tả ý định.** Bản trước hứa "hai
> hướng xây dựng đối lập thật sự". Lời hứa đó đã được chứng minh là **không thể giữ**.
> Thà nói thật còn hơn để người sau đi cân lại số một cách vô vọng.

#### Vì sao không có "hai hướng đối lập cân sức"

Biên chiến thắng tỉ lệ với tích `sát thương × máu hiệu dụng`. Trong tích đó, **ba ô
Vũ khí / Găng / Nhẫn cùng đổ vào một thừa số; chỉ Giáp đổ vào thừa số kia.** Bốn ô không
phải hai cặp đối lập — chúng là **một trục, cộng ba cách tiêu tiền dọc trục đó**.

Gốc rễ sâu hơn, và là mệnh đề **đại số** chứ không phải chuyện chỉnh số:

**`(15,10,3,1)` chính là ảnh gương của `(1,3,10,15)`.** Ở tầng 100 hai build có vector cấp
`[20,30,50,60]` và `[60,50,30,20]`, đảo nhau khít. Với độ dốc mỗi ô là `s₀..s₃`:

> `log biên(bùng nổ) − log biên(bền bỉ) = 40(s₃−s₀) + 20(s₂−s₁)`

Cứu **bùng nổ** đòi Nhẫn dốc hơn Vũ khí. Cứu **bền bỉ** đòi điều ngược lại.
**Hai điều kiện loại trừ nhau với mọi bộ số.**

Đã quét **420.000 cấu hình** (14 cách gán bốn ô vào hai trục × 30.000 bộ độ dốc 0,5–14%/cấp):

| | Số cấu hình thoát |
|---|---|
| Bùng nổ một mình không bị trội (tầng 50/70/100) | 18.387 (4,38%) |
| Bền bỉ một mình không bị trội | 18.289 (4,35%) |
| **Cả hai cùng lúc** | **0 (0,0000%)** |

Mỗi hướng riêng lẻ cứu được. **Giao của hai điều kiện là rỗng.**

Điều này đúng với mọi ô có đường cong "%/cấp cố định" — tức đúng với toàn bộ §5.7. Phá nó
**bắt buộc** phải cho ít nhất một ô đường cong **phi mũ**, tức một cơ chế mới. Đã dựng thử
và đo: cơ chế rẻ nhất làm được điều đó đẩy thời gian giết boss tầng 100 từ 200 giây lên
**trung vị 48 phút, chậm nhất 4,8 giờ** — phá §5.1 và §3 — mà hai hướng **vẫn chỉ đạt 65%
biên của build dàn đều**. Không-bị-trội **không đồng nghĩa** cạnh tranh được.

Nói cho chính xác: mệnh đề bất khả thi đúng cho **§5.7 như đang viết**, không phải cho mọi
thiết kế. Nếu sau này chấp nhận trả cái giá trên thì con đường vẫn mở.

#### Lựa chọn thật nằm ở đâu

**(a) Bao nhiêu Lõi vào Giáp. Năm nấc. Đây là quyết định duy nhất có sức nặng.**

| Lõi vào Giáp | Build mạnh nhất của nấc | Sát thương | Máu hiệu dụng | Biên so với tối ưu |
|---|---|---|---|---|
| 1 | (15,1,10,3) | 1.245 | 17.704 | 59% |
| 3 | (15,3,6,6) | 1.092 | 27.239 | 80% |
| **6** | **(15,6,6,3)** | **888** | **41.910** | **100%** |
| **10** | **(10,10,6,3)** | **577** | **64.484** | **100%** |
| **15** | **(6,15,6,3)** | **375** | **99.215** | **100%** |

Ba nấc 6 / 10 / 15 có biên **bằng nhau đến sai số dấu phẩy động**. Đó là lựa chọn đúng nghĩa:
giết nhanh mà mỏng, hay chậm mà dày, **cùng khả năng thắng**, sát thương chênh **2,4 lần**
giữa hai đầu. Hai nấc 1 và 3 là lựa chọn **dở** — tài liệu nói thẳng thay vì gọi chúng là
"một phong cách chơi".

> ⚠️ **Ba nấc bằng nhau vì `dmgL == hpL` (cùng 4,403%), không vì may mắn.** Hoán vị Lõi giữa
> Vũ khí và Giáp cho biên y hệt trên **52/52** build. Đây là **lựa chọn cân sức duy nhất còn
> lại của thiết kế**, và nó là lưỡi dao: §5.7 tự khai số của nó sẽ chỉnh hàng chục lần.
> **Khoá `dmgL == hpL` thành bất biến trong file dữ liệu, kèm comment và một test tự động.**

**(b) Găng và Nhẫn quyết định trò chơi §5.3 *chơi ra sao*, không quyết định *mạnh cỡ nào*.**

| Tầng 100 | 5 đòn mất bao lâu | Hệ số chí mạng | % sát thương ở đòn chí mạng |
|---|---|---|---|
| Găng 10 / Nhẫn 15 | **0,95 s** | 14,7x | **79%** |
| Dàn đều (6/6) | 1,34 s | 7,5x | 65% |
| Găng 3 / Nhẫn 1 | **1,87 s** | 3,8x | **49%** |

Cùng khả năng qua boss, hai người chơi hai trò khác nhau: một người dồn gần bốn phần năm sát
thương vào một đòn mỗi 0,95 giây; người kia ăn đều và đợi 1,87 giây. Khác biệt về **nhịp tay**
— có thật, cảm được, **không tốn một con số cân bằng nào**.

Hai con số này phải hiện trên màn hình trang bị (~10 dòng C#, mốc M2, đọc từ file dữ liệu
theo §9.2). **Nhãn phải là "5 đòn ≈ 0,95 s đánh liên tục", KHÔNG được viết "giữ yên liên tục"**
— §5.4 cho phép lùi lại chờ mà không mất thanh dồn; nhãn sai sẽ dạy người chơi ngược luật.

Phần khó nghe: bảng (b) mô tả rìa, không mô tả build nên chọn. Bốn trong năm build trên biên
Pareto ở tầng 100 đều có Găng 6 / Nhẫn 3. Muốn chạm cột 0,95 s phải trả **67% biên**.

**(c) Ba boss đầu không có lựa chọn nào cả.** Đến hết tầng 31, ngân sách Mảnh chưa đẩy được ô
nào chạm trần, nên **cả 52 phân bổ có chỉ số giống hệt nhau** — chênh biên đúng **1,0000** ở
boss 1, 2, 3. Chỉ số bắt đầu phân biệt từ **tầng 32**. Hệ quả trực tiếp của thang cổng §5.6,
không phải lỗi — nhưng UI phải trung thực, đừng để màn hình đột phá gào lên "quyết định trọng
đại" ở boss 1 khi nó chưa là gì cả.

#### 5.5b Đổi nhân vật — sưu tầm mà không phá gì

**Vẫn là MỘT nhân vật trên màn hình.** Đây không phải đội hình: không ghép ba người bổ trợ
nhau, không tương khắc. §5.3 sống nguyên vẹn vì bạn vẫn chỉ điều khiển vị trí của chính mình.

| Luật | |
|---|---|
| **Mở khoá theo mốc** | Giết boss lần đầu → mở một nhân vật. 10 boss = 10 nhân vật. Xác định, không quay số |
| **Trang bị dùng chung** | Bốn ô, Mảnh, Lõi thuộc về **người chơi**, không thuộc nhân vật. Đổi người không mất gì |
| **Chỉ đổi hình dạng, không đổi sức mạnh** | Mỗi nhân vật nhân sát thương lên `k`, chia máu cho `k` |
| **Đổi tự do giữa các tầng** | Không tốn tài nguyên, không chờ hồi |

**Vì sao luật thứ ba quan trọng đến thế:** biên an toàn tỉ lệ với **tích** `sát thương × máu`
(xem §5.10). Nhân một thừa số lên `k` và chia thừa số kia cho `k` thì tích **không đổi** —
nên biên không đổi một chữ số nào.

Đã kiểm: 5 nhân vật × 5 build × 10 boss, tất cả cho **1,364485 → 904,373632**, trùng khít
tới 6 chữ số thập phân.

> **Thêm bao nhiêu nhân vật cũng không đụng một dòng nào của `can-bang.xlsx`.**

Nhưng chơi thì khác hẳn — tại boss cuối, build dàn đều:

| Nhân vật | `k` | Giết boss | Sống được | Kiểu chơi |
|---|---|---|---|---|
| Sát thủ | 1,80 | **87s** | 320s | Giết chớp nhoáng, sai một nhịp là chết |
| Kiếm sĩ | 1,35 | 116s | 426s | |
| Cân bằng | 1,00 | 157s | 575s | Trung dung |
| Vệ binh | 0,70 | 224s | 822s | |
| Tăng | 0,50 | **314s** | **1.151s** | Chậm mà chắc, tha thứ sai lầm |

Bảng tính nói chúng bằng nhau. **Tay người chơi thì không** — nhân vật mỏng chết vì một lần
đứng lì quá lâu, nhân vật dày tha thứ sai lầm. Đó là kỹ năng, không phải sức mạnh, nên nó
không phá nguyên tắc "khoảng cách do chăm chỉ" ở §1.

**Đồ hoạ có sẵn:** `art-source/NinjaAdventure/Actor/Character/` có **89 nhân vật đủ hoạt ảnh**
(Attack · Dead · Idle · Walk + hai chiêu Special) — Samurai, Knight, Tengu, Vampire, Skeleton,
Monk, Ninja đủ màu. Cùng tác giả, cùng phong cách, nên **quyết định #13 không bị phá**.

**Thuộc mốc M3**, cùng chỗ với boss và Lõi, vì mở khoá gắn với lần giết boss đầu tiên.

#### ⚠️ Vấn đề chưa xử lý: Nhẫn là ô đổ rác

Hoán vị Lõi ở tầng 100, mỗi phép chuyển một bậc từ ô này sang ô kia, tính theo biên:

| Thêm một bậc Lõi vào | Lợi | Hoà | Hại |
|---|---|---|---|
| Vũ khí | 56 | 28 | **0** |
| Giáp | 56 | 28 | **0** |
| Găng | 28 | 0 | 56 |
| **Nhẫn** | **0** | **0** | **84** |

**Không một lần nào trong 84 phép đo mà thêm Lõi vào Nhẫn làm tăng biên.** Đó là định nghĩa
một ô đổ rác, và nó **vi phạm §5.6** ("mỗi Lõi tiêu đi là một cánh cửa đóng lại"). Găng cũng
sai nhiều hơn đúng. Người chơi tính đúng chỉ đổ Lõi vào Vũ khí và Giáp — mà hai ô đó lại hoán
đổi được cho nhau.

Ghi thẳng thay vì giấu: **thiết kế hiện có bốn ô nhưng chỉ một quyết định Lõi thật sự** —
dồn hay dàn — nhân với một lựa chọn nhãn miễn phí. Đã thử cân bằng lại bằng thuần số
(Găng 4,4%/cấp, Nhẫn 5,6%/cấp): Nhẫn hết chết và hai hướng có biên bằng nhau, **nhưng** tiến
trình vọt từ 59x lên 154x và hệ số máu boss lên 13,8 — phá §5.11. Để mở ở §7, **không tự chốt**.

### 5.6 Kinh tế tài nguyên

| Tài nguyên | Nguồn | Tính chất | Trục |
|---|---|---|---|
| **Mảnh** | Quái thường | Vô hạn | *Thời gian* — cày nhiều thì có nhiều |
| **Lõi** | **Chỉ boss, chỉ lần giết đầu tiên** | Hữu hạn tuyệt đối | *Lựa chọn* |

Quét lại boss **không** cho Lõi.

**Con số buộc phải chọn:**

- Mỗi trang bị đi từ cấp 1 → **60**.
- Mốc **10/20/30/40/50** là cổng chặn cứng, phải dùng Lõi để đột phá; mỗi lần đột phá nâng trần thêm 10 cấp.
  *(Bản thảo đầu ghi cấp tối đa 50 — sai. Năm cổng, mỗi cổng +10 cấp, thì trần cuối phải là 60.
  Ba con số 15 / 60 / 30 vẫn giữ nguyên.)*
- Chi phí đột phá: **1 → 2 → 3 → 4 → 5 Lõi**.
- Nâng một món lên tối đa tốn **15 Lõi**. Bốn món tối đa tốn **60 Lõi**.
- **Cả game chỉ có 30 Lõi** (10 boss × 3).

Toàn bộ thiết kế gói trong ba con số đó. Người chơi **không bao giờ** max được mọi thứ —
chỉ đủ cho hai món, hoặc dàn đều thì không món nào vượt mốc 30. Mỗi Lõi tiêu đi là
một cánh cửa đóng lại. Không có xác suất nào, nhưng có sức nặng thật.

### 5.7 Đường cong sức mạnh

**Nguyên tắc duy nhất:** người chơi cày bình thường phải luôn đi trước đường cong
quái một chút.

Con số khởi điểm (sẽ tinh chỉnh hàng chục lần):

| Đại lượng | Giá trị đã kiểm chứng |
|---|---|
| Máu quái mỗi tầng | **+5,2%** |
| Sát thương quái mỗi tầng | **+4,0%** |
| Máu nền người chơi mỗi tầng đã qua | **+4,5%** |
| Vũ khí / Giáp — mỗi cấp | **+4,4%** |
| Găng / Nhẫn — mỗi cấp | **+3,4%** |

Hai tốc độ của người chơi đều là con số ban đầu (9% và 7%) **chia đôi theo hình học**,
nên tỉ lệ 9:7 giữa hai cặp ô được bảo toàn tuyệt đối. Đây không phải con số tuỳ tiện:
xem §5.11 để biết vì sao buộc phải nén.

Máu quái +5,2% là giá trị **lớn nhất** còn giữ được biên tầng thường ở mức 1,60 cho
build yếu nhất — giải ngược từ ràng buộc, không phải chọn tay.

Bản thảo đầu ghi "người chơi nhỉnh hơn 8% mỗi tầng" — con số đó sai. 8% mỗi tầng luỹ kế
qua 100 tầng là **hơn 2.000 lần**, tức game trở nên vô nghĩa từ giữa chặng.

⚠️ **Khoảng cách giữa máu quái (+5,2%) và sát thương quái (+4,0%) nay rất hẹp** — bản
thảo đầu để 9% và 4%. Mối đe doạ của tháp không còn thuần tuý là "quái dày máu" mà cân
bằng giữa dày máu và đánh đau. Sinh tồn được gánh chủ yếu bởi **máu nền +4,5%/tầng**
chứ không phải bởi Giáp; đó cũng là van an toàn số 4 ở §5.8.

**Ba con số này bắt buộc nằm trong file dữ liệu, không nằm trong code.**

### 5.8 Chống bế tắc vĩnh viễn ⚠️

**Đây là rủi ro nghiêm trọng nhất của thiết kế.**

Lõi hữu hạn tuyệt đối + mốc đột phá chặn cứng = người chơi có thể tự đưa mình vào
trạng thái **không thể thắng**. Ai đó dồn hết 30 Lõi vào Nhẫn và Găng, đến tầng 80
phát hiện mình quá mong manh, và không có đường lùi. Họ gỡ game.
Với sản phẩm portfolio, một người xem demo gặp đúng tình huống này là hỏng cả buổi.

**Ba van an toàn, cần cả ba:**

1. **Điều kiện thiết kế (đã sửa sau khi dựng mô hình):**

   > *Mọi phân bổ đầu tư ít nhất một bậc đột phá vào **cả bốn ô** đều qua được tầng 100.*

   Bản thảo đầu viết "bất kỳ hướng xây dựng hợp lý nào" — điều kiện đó **bất khả thi về
   mặt toán học**. Nếu nhịp độ phẳng và người chơi chạm cấp trần đúng ở tầng cuối, thì
   build bỏ trắng một ô không thể theo kịp với *bất kỳ* bộ tham số nào; đó là hệ quả đại số,
   không phải chuyện chỉnh số.

   Quét toàn bộ **62 phân bổ tiêu hết 30 Lõi**, đánh giá tại cả 10 boss với bộ tham số
   đã chốt: **53 phân bổ qua được, 9 phân bổ không qua.** Cả 9 đều bỏ trắng ít nhất một ô:
   `0/0/15/15`, `0/10/10/10`, `0/15/0/15`, `0/15/15/0`, `10/0/10/10`, `10/10/0/10`,
   `15/0/0/15`, `15/0/15/0`, `15/15/0/0`. Tất cả do van số 2 (tẩy điểm) xử lý.

   **Cả 52 phân bổ có ≥1 Lõi ở cả bốn ô đều qua được cả 10 boss**, biên thấp nhất 1,50.
   Điều kiện trên vẫn đúng tuyệt đối.

   *(Bản thảo trước ghi "đúng một phân bổ không qua" — con số đó tính ở tầng 100 với quái
   thường, không phải ở boss, và với tham số cũ. Sau khi nén tăng trưởng, con số là 9.)*
2. **Tẩy điểm:** rút toàn bộ Lõi ra và phân bổ lại, **hoàn 100%, nhưng tốn rất
   nhiều Mảnh**. Người cày chăm sửa được sai lầm; người lười phải sống với nó.
   Quyết định vẫn có sức nặng, nhưng sức nặng tính bằng thời gian, không bằng
   việc mất tài khoản.
3. **Boss thử lại vô hạn, không mất gì.** Không vé thách đấu, không giới hạn lượt.
   Thất bại chỉ tốn thời gian — thứ người chơi sẵn lòng bỏ ra.
4. **Máu nền tăng theo tầng đã qua, độc lập với Giáp** (+4,5%/tầng — xem bảng §5.7). Van thứ tư, phát hiện
   khi dựng mô hình: không có nó, build nhẹ Giáp chết tức khắc ở tầng cao và điều kiện trên
   không bao giờ thỏa.

#### Ghi chú M3 — thời gian hạ boss đã đo được

Cài xong mới đo được bằng số thật. Với tháp 40 tầng và dàn trang bị dồn sát thương:

| Boss | Tầng | Máu | Thời gian hạ |
|---|---|---|---|
| 1 | 10 | 1.229 | 75s |
| 2 | 20 | 2.872 | 112s |
| 3 | 30 | 6.760 | 158s |
| 4 | 40 | 15.912 | 193s |

**Nằm trong dải đã thiết kế, không phải lỗi.** §5.1 lấy mốc 200 giây cho boss tầng 100,
và bảng §5.5b liệt kê từ 87s (Sát thủ) tới 314s (Tăng). Boss là bài kiểm tra sức bền —
dài hơn hẳn quái thường là đúng chủ ý. Đã suýt "sửa" cho nhanh lại trước khi đọc lại §5.1.

### 5.10 Độ khó nằm ở boss, không ở quái thường

Hệ quả không tránh được của việc cấp trần Lõi phải có ý nghĩa: quái thường **sẽ** dễ dần
ở giữa game. Đó không phải lỗi cần sửa — đã có quét nhanh nên tầng thường vốn chỉ là
nguồn tài nguyên.

Vì vậy **boss có đường cong máu riêng**. Đã chốt, không còn phải chỉnh tay:

| Boss | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
|---|---|---|---|---|---|---|---|---|---|---|
| Hệ số máu | 1,56 | 2,20 | 3,14 | 4,42 | 4,62 | 4,31 | 4,07 | 3,45 | 2,36 | 1,50 |

Hệ số sát thương boss để nguyên **1,0** ở cả mười con. Lý do: trong công thức biên,
hai hệ số máu và sát thương chỉ xuất hiện dưới dạng **tích** — chúng là đúng một bậc
tự do. Thêm trục thứ hai không mua được gì, chỉ thêm một cột phải chỉnh tay.

Bộ hệ số này neo build **yếu nhất** đúng 1,50 ở mọi boss; build mạnh nhất chạm **5,15**.
Máu boss tuyệt đối tăng đơn điệu từ 1.229 đến 111.287, và mọi hệ số đều ≥ 1,0 nên boss
luôn dày máu hơn quái thường cùng tầng.

**Ghi chú mô hình — Mảnh là tài nguyên chung.** Các con số trên tính với giả định: ô đã chạm
trần Lõi không nhận Mảnh nữa, phần đó dồn sang ô còn lại. Bảng tính đầu chia cứng Mảnh 1/4
cho mỗi ô ở mọi tầng — kể cả ô đã chạm trần, điều người chơi không bao giờ làm vì màn hình
nâng cấp không cho. Sửa lại **không tốn một dòng code Unity nào** (nó chỉ mô tả đúng hành vi
sẵn có) và cải thiện mọi chỉ số: ca ngoài dải 3 → **0**, dải biên 1,50–5,78 → **1,50–5,15**,
build bền bỉ bị áp đảo 22/10/1 → **0/3/1**, tiến trình giữ nguyên **59,13x**, vẫn **0/520**
build bị khoá.

Vẫn là **xấp xỉ**: nó giả định người chơi chia Mảnh đều cho các ô chưa chạm trần. Người chơi
thật có thể dồn hết vào một ô.

### 5.11 Vì sao phải nén tăng trưởng mỗi cấp

Đây là ràng buộc cứng nhất của toàn bộ thiết kế, và nó chỉ lộ ra khi dựng mô hình.

Hệ số máu boss chỉ **tịnh tiến** cả cụm build lên xuống — nó không thu hẹp được khoảng
cách giữa chúng. Mà khoảng cách ấy sinh từ *cấp trần chênh nhau × tăng trưởng mỗi cấp*.
Với tham số ban đầu (9%/cấp, trần chênh 40 cấp), build mạnh nhất hơn build yếu nhất
**11,7 lần** ở boss 7. Không hệ số boss nào sửa được điều đó.

Nén tăng trưởng là **cách duy nhất**. Nhưng nó đánh đổi trực tiếp với cảm giác mạnh lên:

| Tăng trưởng mỗi cấp | Chênh lệch build | Dải biên đạt được | Tiến trình DPS qua 100 tầng |
|---|---|---|---|
| 9% / 7% (ban đầu) | 11,7x | 1,5 – 17,6 | 5.090x |
| 5,9% / 4,6% | 5,6x | 1,5 – 8,4 | 252x |
| **4,4% / 3,4% (đã chọn)** | **3,9x** | **1,5 – 5,8** | **59x** |
| 2,9% / 2,3% | 2,6x | 1,5 – 3,9 | 14x |
| 1,7% / 1,4% | 1,8x | 1,5 – 2,7 | 5x |

Chọn 4,4%/3,4% là chọn **giữ cảm giác tiến trình** (sát thương đi từ 10 lên ~600) và
chấp nhận build tối ưu thấy boss chỉ ở mức dễ chịu (biên 5,8) thay vì bị chặn nghẹt.
Siết dải xuống 1,5–3,0 sẽ khiến sát thương chỉ đi từ 10 lên 24 sau cả trăm tầng —
với một RPG mà vòng lặp cốt lõi là *"đánh quái → mạnh hơn"*, đó là đường cong chết.

**Thang cổng đột phá không bị đụng.** Trần 10/20/30/40/50/60, chi phí 1-2-3-4-5, tổng 30 Lõi
— §5.6 và quyết định #7 còn nguyên từng chữ. Phương án thay thế (nâng trần nền 10→40) đã bị
loại vì nó khiến Lõi vô hình suốt 70% game.

### 5.9 Thiết kế kiếm tiền (trên giấy)

Nếu sau này thương mại hóa: **bán Mảnh, không bao giờ bán Lõi.**

Tiền mua được *tốc độ*, không mua được *sức mạnh*. Người nạp đến tầng 50 nhanh hơn,
nhưng ở cùng tầng 50 họ không mạnh hơn người cày — và vẫn phải đối mặt đúng những
lựa chọn đau đầu đó.

Đây là điểm nên nói thành lời trong hồ sơ: nó cho thấy hiểu cách kiếm tiền mà không
thao túng người chơi.

---

## 6. Nhật ký quyết định

| # | Quyết định | Phương án khác đã cân nhắc | Lý do chọn |
|---|---|---|---|
| 1 | Mục đích: portfolio | Học cho vui; thử nghiệm thương mại | Định hình mọi ràng buộc còn lại: *phải xong* > *phải sâu* |
| 2 | Offline hoàn toàn, không backend | Leaderboard; PvP bất đối xứng; guild | Portfolio không cần doanh thu → không cần so sánh xã hội → không cần server |
| 3 | Combat: đánh tay trước, auto sau | Idle thuần; hành động toàn phần; turn-based | Học hệ thống bằng tay; mở auto thành cột mốc phần thưởng |
| 4 | Cấu trúc: leo tháp | Nhiều khu vực (B); run-based (C) | A có khả năng hoàn thành cao nhất. C **xung đột** với nguyên tắc không ngẫu nhiên (roguelite sinh ra để chứa ngẫu nhiên) |
| 5 | Cái hay = đánh đổi phân bổ tài nguyên | Kỹ năng tay; cảm giác số tăng; khám phá nội dung | Giữ được hồi hộp mà không cần xác suất; khớp với trục "chăm chỉ / nạp tiền" |
| 6 | **Giữ chí mạng, nhưng dạng thanh dồn** | Bỏ hẳn chí mạng; chí mạng theo xác suất thật | Giữ trọn khoái cảm chí mạng **và** nguyên tắc không ngẫu nhiên. Còn thêm được kỹ năng canh thời điểm ở giai đoạn đánh tay |
| 7 | 4 ô trang bị, tổng 30 Lõi | Nhiều ô hơn; Lõi vô hạn | Ba con số (15 / 60 / 30) tự động ép chuyên môn hóa mà không cần luật phụ |
| 8 | Bán Mảnh, không bán Lõi | Bán cả hai; bán lượt quét | Tiền mua tốc độ chứ không mua sức mạnh — luận điểm đạo đức mạnh cho portfolio |
| 9 | Đấu trường kín, không level design | Màn có địa hình; map mở | Tiết kiệm công sức lớn nhất toàn dự án; biến 100 tầng thành dữ liệu |
| 10 | Di chuyển = không đánh | Có nút tấn công riêng | Một luật tạo sẵn sức căng; cộng hưởng hoàn hảo với thanh chí mạng |
| 11 | Cho phép tẩy điểm, tốn Mảnh | Không cho tẩy; tẩy miễn phí | Van chống bế tắc vĩnh viễn, mà không làm quyết định mất sức nặng |
| 12 | Số liệu cân bằng để ngoài code | Hằng số trong code | Sẽ chỉnh hàng chục lần; và sẽ có lúc nghỉ vài tuần rồi quay lại |
| 13 | Một bộ asset pack duy nhất | Tự vẽ; ghép nhiều pack; 3D low-poly | Không chuyên vẽ; ghép pack là cách nhanh nhất khiến game trông nghiệp dư |
| 14 | **Engine: Unity 6 + C#** | Godot 4 + GDScript (khuyến nghị ban đầu của tài liệu); Flame/Flutter; dựng thử M1 trên cả hai | Nền C# sẵn có từ .NET dùng lại được ngay. Unity được nhận diện rộng hơn Godot nhiều lần trên thị trường tuyển dụng — với sản phẩm portfolio đây là lợi ích thật, không phải cảm tính. **Đánh đổi chấp nhận:** editor nặng, vòng lặp sửa–chạy chậm hơn, build iOS lâu hơn |
| 15 | **Sửa điều kiện 5.8 theo kết quả mô hình** | Giữ nguyên điều kiện cũ; siết Lõi chặt hơn; nới trần cấp | Điều kiện cũ bất khả thi về mặt toán học. Quét 62 phân bổ cho quy tắc thay thế rõ ràng và kiểm chứng được: **>= 1 Lõi ở cả bốn ô thì luôn qua**. Kèm theo: cấp trần 50→60, tách đường cong sát thương quái khỏi máu quái, thêm van an toàn thứ tư, dồn độ khó về boss |
| 16 | **Nén tăng trưởng mỗi cấp xuống một nửa (9%/7% → 4,4%/3,4%), giữ nguyên toàn bộ thang cổng đột phá** | Nâng trần nền 10→40 *(loại: Lõi vô hình tới tầng 72)*; gộp bốn tốc độ về một số *(loại: Vũ khí và Găng thành cùng một chỉ số)*; thêm soft-cap sát thương cho boss *(loại: phá §5.3 và §5.4)*; nén mạnh hơn nữa xuống 1,7%/1,4% *(loại: tiến trình chỉ còn 5x)* | Hệ số boss chỉ tịnh tiến cả cụm, không thu hẹp được chênh lệch 11,7x giữa các build — nén tăng trưởng là cách duy nhất. Chọn mức chia đôi vì nó giữ được tiến trình 59x (cảm giác mạnh lên còn thật) mà vẫn đưa mọi build hợp lệ vào dải 1,5–5,8. §5.7 tự khai số của nó là tạm và sẽ chỉnh hàng chục lần; §5.6 là trụ cột nên không đụng. Kiểm chứng: 0 build hợp lệ bị khoá, 52/52 qua cả 10 boss |
| 17 | **Viết lại §5.5 theo sự thật đo được, thay vì thêm cơ chế để cứu "hai hướng đối lập"** | Hút máu chí mạng cho Nhẫn; chuyển một ô sát thương sang sinh tồn (2 chọi 2, cơ chế khiên); chí mạng mạnh riêng ở boss | Quét 420.000 cấu hình: cứu được bùng nổ **hoặc** bền bỉ, **không bao giờ cả hai** — hai build là ảnh gương của nhau nên hai điều kiện loại trừ nhau. Cơ chế rẻ nhất phá được ràng buộc đẩy thời gian giết boss cuối lên trung vị 48 phút và hai hướng vẫn chỉ đạt 65% biên build dàn đều. Cái giá lớn hơn thứ mua về; dự án làm một mình và ràng buộc số một là PHẢI HOÀN THÀNH. Lấy kèm: sửa mô hình Mảnh-dùng-chung (0 dòng code, cải thiện mọi chỉ số) |
| 18 | **Bộ asset: Ninja Adventure (CC0)** | ElvGames Rogue Adventure 39,99$ *(10 boss, nhưng nhiều khả năng chỉ 1 hướng và **cấm đẩy art lên repo công khai**)*; ElvGames Fantasy Dreamland *(loại: 0 boss)*; CraftPix Premium *(loại: ~105 pack nhiều hoạ sĩ, đúng cái bẫy #13)*; Dungeons&Pixels 5,99$ *(loại: chỉ 3 nhóm quái)* | Đã TẢI VỀ ĐẾM THẬT, không tin trang bán: 66 quái / 7 nhóm chủ đề (cần 5), 20 boss, 19 tileset — đều nhiều hơn quảng cáo. **CC0 là điểm tách bạch quyết định**: hai lựa chọn trả phí đều cấm phát tán art thô, tức phải .gitignore thư mục art nếu muốn public repo — mất mát thật với một sản phẩm PORTFOLIO mà người xem muốn đọc code. Giá 0đ cũng nghĩa là chọn sai thì mất thời gian chứ không mất tiền. Kèm theo: đóng luôn câu hỏi nhạc/âm thanh (41 nhạc + 132 SFX, cùng CC0). **Đánh đổi đã biết và chấp nhận:** quái thường chỉ có hoạt ảnh đi (4 hướng × 4 frame), không có đánh và không có chết — lấp bằng FX có sẵn trong chính bộ này |
| 19 | **Đổi bảng màu sang "Mực & Son" — ba tầng đọc** | Giữ nguyên bảng gốc *(loại: nhân vật màu ô liu trên sàn cam, không nổi)*; "Tháp Đêm" xám tím lạnh *(loại: mọi loài quái thành cùng một khối xám, mất khả năng phân biệt)*; "Huyết Nguyệt" đỏ tím *(loại: sàn đỏ + quái đỏ, tương phản kém và mỏi mắt)*; "Chu Sa" sơn mài ấm *(loại giữa chừng: bảng gốc vốn đã cam ấm nên dải ấm gần như không đổi được gì)* | Không phải chuyện thẩm mỹ mà là **thứ bậc đọc**, phục vụ §3 (màn hình dọc nhỏ) và §5.3 (phải theo dõi được vị trí của chính mình trong lúc đứng yên ăn đòn). Thế giới xám trung tính, quái đỏ, nhân vật lam ngọc — ba tầng, không lẫn. Sắc đỏ lệch nhẹ theo tên loài nên các loài vẫn phân biệt được. **Lỗi đã sửa trong quá trình:** quy tắc đầu chỉ giữ những màu vốn đã đỏ, làm 41/66 quái chìm vào nền — đổi sang quy tắc theo thư mục (vai trò) thì 66/66 nổi được. Sinh lại bất cứ lúc nào bằng `tools/doi-bang-mau.py`; bản gốc giữ nguyên làm đầu vào |
| 20 | **Năm nền theo chương, chỉ đổi tầng thế giới** | Sinh lại toàn bộ art cho mỗi chương *(loại: 5 × 124 MB, và làm UI/HUD đổi màu theo chương)*; giữ một nền chung cho cả tháp *(loại: §5.1 cần 5 chương khác nhau về thị giác)* | Chỉ `Backgrounds/` là chương-riêng — 2,4 MB cho cả 5. Hai tầng quái đỏ và nhân vật lam giữ nguyên suốt 100 tầng vì người chơi học thứ bậc đọc một lần rồi dùng mãi. **Phát hiện quan trọng trong quá trình:** bản thiết kế đầu đạt mọi ràng buộc khi đo ở *mốc giữa dải* nhưng render ra thì ba cặp chương rơi xuống ΔE 13–17 — vì pixel tile sàn nằm ở đoạn sáng, nơi năm bảng hội tụ. Mốc giữa đẹp nhưng không có pixel nào ở đó. Sau khi kéo hai mốc sáng và giãn độ sáng: ΔE 24,3. Còn hai thiếu sót dưới 1 đơn vị, chấp nhận. **Cũng sửa luôn một lỗi thật:** cờ `giu_do` của bản Mực & Son giữ nguyên pixel đỏ của lớp thế giới, làm đèn lồng và lửa trong tile đỏ trùng khít màu quái (ΔE 1,0) — đã bỏ |
| 21 | **Dựng scene M1 bằng mã, kèm 8 test PlayMode** | Dựng tay theo hướng dẫn *(loại: 18 ô kéo-thả, sai một ô là luật §5.3 hỏng trong im lặng và không ai biết)*; viết thẳng file .unity *(loại: YAML đầy GUID và fileID chéo, viết tay gần như chắc chắn hỏng)*; chỉ kiểm tham chiếu, không chạy thử *(loại: nối đúng KHÁC chạy đúng)* | `BuildM1Scene.cs` dựng lại được bất cứ lúc nào; `VerifyM1Scene.cs` soi 18 tham chiếu + 10 mục từng hỏng trong đợt review; `M1LoopTests.cs` chạy game thật headless. Bốn test khoá chặt §5.3 và §5.4 — đứng yên thì đánh và ăn đòn, di chuyển thì quái không mất một điểm máu nào, đúng 5 đòn một chí mạng, thanh không reset khi di chuyển. **8/8 đạt trong 18,3 giây.** Kèm theo: tách mã game ra asmdef riêng (`TowerRpg`, `TowerRpg.Editor`, `TowerRpg.Tests.PlayMode`) — bắt buộc vì asmdef không tham chiếu được Assembly-CSharp, và cũng làm biên dịch nhanh hơn |
| 22 | **Đổi nhân vật (§5.5b), KHÔNG phải đội hình** | Đội hình 3-6 người cùng đánh *(loại: buộc bỏ §5.3, vì không đặt vị trí từng người trên màn hình dọc được — nó thành auto-battle, và đó là đổi thể loại chứ không phải thêm tính năng)*; gacha rút nhân vật *(loại: trái nguyên tắc nền)*; mỗi nhân vật một bộ trang bị riêng *(loại: nhân số ô cân bằng lên 10 lần, phạt người chơi thử nghiệm)* | Người dùng muốn **cảm giác mở khoá**, không phải chiến thuật ghép đội — hỏi rõ trước khi thiết kế. Lời giải: một nhân vật trên màn hình như cũ, trang bị dùng chung, nhân vật chỉ đổi HÌNH DẠNG chỉ số (nhân sát thương `k`, chia máu `k`). Biên an toàn tỉ lệ với TÍCH hai thừa số nên `k` triệt tiêu — **kiểm 5 nhân vật × 5 build × 10 boss, trùng khít tới 6 chữ số thập phân.** Chi phí: 0 cân bằng, 0 đồ hoạ (89 nhân vật có sẵn, cùng tác giả), ~150 dòng code. **Một nhận định sai đã sửa:** trước đó tôi kết luận bộ asset chỉ có 1 nhân vật đủ hoạt ảnh — sai, vì chỉ nhìn thư mục `CharacterAnimated` mà bỏ qua `Character` |
| 23 | **Hệ giao diện: tầng đọc THỨ TƯ, tông ấm, không đổi theo chương** | Phong cách công nghiệp *(loại: kim loại và đinh tán lệch tông với sprite ninja — đúng cái bẫy quyết định #13)*; để giao diện trong tầng "thế giới" như hiện tại *(loại: thanh máu sẽ ngả ô-liu ở chương 3 và ngả tím ở chương 5)*; tự vẽ giao diện mới *(loại: bộ asset đã có 43 file Theme Wood + 62 icon, cùng tác giả)* | Người chơi học giao diện MỘT LẦN rồi dùng suốt 100 tầng — cùng lý do quái luôn đỏ và nhân vật luôn lam. Tông ấm gỗ/vàng đồng là khoảng trống duy nhất còn lại (xám đã là thế giới, đỏ là quái, lam là nhân vật). Lưới 32px, lề 64px, vùng chạm 144px (Android 48dp ≈ 132px). **Hai quyết định đáng nói:** thanh chí mạng chia đúng 5 vạch để ĐẾM được thay vì ước lượng — đó là toàn bộ lý do chọn thanh dồn thay vì xác suất; và cần gạt ĐỘNG hiện ra nơi ngón chạm, vì bản M1 đặt cố định góc trái mà phần lớn người cầm máy tay phải. Chi tiết ở `docs/GIAO-DIEN.md` |
| 24 | **M2 dừng ĐÚNG ở bức tường trần cấp** | Cho Lõi vào luôn M2 *(loại: M3 mất lý do tồn tại, và đột phá là hệ thống lớn cần cân bằng riêng)*; bỏ trần cấp ở M2 *(loại: người chơi nâng vô hạn, không bao giờ cảm thấy cần Lõi)* | Với đường cong Mảnh thật, người chơi chạm trần cấp 10 vào khoảng tầng 18–20 rồi **không nâng được nữa dù còn thừa Mảnh**. Đó chính xác là câu hỏi mà M3 trả lời. Có một test khoá điều này: `Cham_tran_cap_thi_dung_lai_du_con_Manh`. Kiểm bằng bảng tính: 42–53 giây mỗi tầng (khớp §5.1), biên an toàn 1,5–1,9 |
| 25 | **Tháp M3 nâng 20 → 40 tầng, vì nếu không thì Lõi VÔ NGHĨA** | Giữ 20 tầng *(loại: đo được chênh build đúng **×1,00** — nâng trần mở ra một cấp không ai mua nổi)*; 30 tầng *(×1,85, có tác dụng nhưng chưa tới)*; 50 tầng *(×5,90, nhưng dài quá cho một mốc)* | Ở 20 tầng người chơi chỉ dư **836 Mảnh** mà cấp 10→11 tốn **1.097**. Đột phá nâng trần rồi vẫn không mua nổi cấp kế — cả hệ thống Lõi thành đồ trang trí, và không test nào phát hiện được vì mọi hàm đều chạy đúng. Đo lại theo độ sâu: 20 tầng ×1,00 · 30 tầng ×1,85 · **40 tầng ×3,30** · 50 tầng ×5,90. Chọn 40 vì ×3,30 đã gần trọn dải ×3,9 mà §5.11 chốt cho cả 100 tầng — tức M3 chạy được gần hết biên độ của hệ thống, đúng tiêu chí "toàn bộ hệ thống đã đủ". **Bài học: hệ thống chạy đúng ≠ hệ thống có tác dụng.** Chỉ mô phỏng kinh tế mới thấy |
| 26 | **Một nút, hai nghĩa: chưa chạm trần thì NÂNG (Mảnh), chạm trần thì ĐỘT PHÁ (Lõi)** | Hai nút riêng cạnh nhau *(loại: nút đột phá nằm đó xám ngoét suốt 10 cấp đầu, và lúc cần thì người chơi đã quen mắt bỏ qua)*; một màn hình đột phá riêng *(loại: bức tường ở màn này, lối ra ở màn kia — người chơi phải tự đoán ra là có lối)* | Bức tường và lối thoát khỏi bức tường phải nằm **đúng một chỗ**. Chạm trần là nút tự đổi chữ, đổi sắc nền sang son, đổi đơn vị giá từ Mảnh sang Lõi — không phải đi tìm. Phân biệt bằng **sắc nền** chứ không bằng màu chữ, vì chữ trên gỗ sáng bắt buộc phải là mực mới đọc nổi (xem #28) |
| 27 | **Quét nhanh có hồi 3 giây, không phải tức thời** | Tức thời, bấm bao nhiêu lần cũng được *(loại: Mảnh vô hạn, toàn bộ đường cong chi phí ở §5.6 mất nghĩa)*; chỉ quét được một lần mỗi tầng *(loại: không giải quyết được việc farm, tức là không làm đúng việc §5.2 giao)* | §5.2 nói quét nhanh bấm lại được, nhưng `can-bang.xlsx` **chỉ mô hình hoá Mảnh theo việc LEO, chưa bao giờ tính farm**. Đây là lỗ hổng của mô hình chứ không phải của §5.2. 3 giây so với ~45 giây đánh tay vẫn là **nén 15 lần** — đủ để "farm không thành cực hình" mà không thủng kinh tế. Ghi vào §7 vì con số 3 chưa có mô hình đỡ lưng |
| 28 | **Chữ trên gỗ sáng phải là MỰC — đo tương phản chứ không ướm mắt** | Giữ chữ giấy cho đồng bộ với HUD *(loại: đo được **1,85:1**, dưới xa mức 4,5:1 và thực tế là không đọc được)* | `nine_path_panel.png` có **ruột cam sáng (243,140,76)**. 9-patch kéo giãn phần ruột, nên panel nhỏ trông tối (viền chiếm hết) còn panel to trông sáng chói — cùng một sprite, hai kết quả ngược nhau. Chốt: mảng lớn dùng `nine_path_bg` (tối, chữ giấy 7,87:1), nút nhỏ giữ `nine_path_panel` (cam) nhưng chữ là mực (7,41:1). Kéo sáng luôn `dim` 2,94→4,61 và `son` 2,07→4,67. **Kèm theo, một lỗi có từ M2:** viền 9-patch đặt 5 trong khi ruột chỉ đồng màu từ cột 6 — cột bevel lọt vào vùng kéo giãn và nhoè thành vệt nâu chiếm ~20% bề ngang mọi nút rộng. Sửa 5→6 |
| 29 | **Ba lớp kiểm vẫn để lọt, phải NHÌN ẢNH mới thấy** | Tin vào 30 test xanh + 37 mục kiểm scene *(loại: cả ba lỗi dưới đây đều lọt qua sạch sẽ)* | Lần thứ ba trong dự án này: **nối đúng ≠ chạy đúng ≠ nhìn được**. Ba lỗi chỉ ảnh chụp mới bắt được: (1) ô icon bị đẩy xuống dưới nền dòng nên **biến mất hoàn toàn** — tham chiếu vẫn đủ 4/4 nên verifier báo đạt; (2) dòng Nhẫn **đè lên nút tẩy điểm 144px**, thuần số học mà không ai kiểm; (3) dấu `÷` không có trong font dự phòng của TMP nên hiện thành `+`, làm câu "máu ÷k" đọc ra **ngược hẳn nghĩa**. Đã thêm hai ảnh chụp tự động (`6-nang-cap` dựng đúng cảnh chạm trần, `7-nhan-vat`) vào bộ test |
| 30 | **`enemy.dpsFloor1` 3 → 1,5: gộp sẵn tỉ lệ thực nhận vào con số mã đọc** | Bỏ `boss.attackRangeMult` tôi tự thêm ở M3 *(đo được chỉ lên 1,13 chứ không tới 1,50, và biến việc qua boss thành bài kiểm tra KỸ NĂNG — cấn với §1 "khoảng cách do chăm chỉ")*; cho quái nhịp vung tay có luật trượt *(loại: dải cửa sổ dùng được chỉ rộng 0,10 giây, và W < 0,43s biến nó thành cái bẫy kéo biên xuống 0,21)*; sửa 66 ô Excel cho B9 = 1 *(loại: đụng can-bang.xlsx, phá ràng buộc 4, mà kết quả y hệt)* | `can-bang.xlsx` tách sát thương quái làm hai ô — `B7 = 3` *("nếu người chơi đứng yên hoàn toàn")* và `B9 = 0,5` *("tỉ lệ thực nhận — nhờ luật di chuyển §5.3")* — nhưng công thức biên `T5 = P5/(R5×B9)` chỉ dùng chúng dưới dạng **tích 1,5**. Mã không có ô B9 nào nên giao thẳng 3, tức **gấp đôi** thứ bảng đang báo. Cộng với van #4 không chạy, cả bốn boss rơi xuống biên **0,51 / 0,33 / 0,21 / 0,14** — tức **game không ai kết thúc được**, vi phạm thẳng ràng buộc số một, mà 33 test vẫn xanh và không ai thấy. Sửa một dòng CSV là mã giao đúng con số bảng **đang hiển thị**: không đụng `can-bang.xlsx` (ràng buộc 4 nguyên vẹn), không đụng hệ số máu boss (§5.10 nguyên vẹn), không chạy lại kiểm 52/52. **Đánh đổi đã biết và chấp nhận:** tầng thường cũng dễ đi một nửa — chấp nhận được vì bảng chưa bao giờ tính biên tầng thường (`'Đường cong tầng'` không có cột sống sót) và §5.10 tự nói quái thường dễ dần là có chủ ý. **Bài học lớn nhất của cả dự án:** ba hệ thống cùng hỏng một lúc mà triệu chứng bị che — quét nhanh vô hạn phát Mảnh thừa mứa nên không ai chạm tới bức tường boss để phát hiện ra nó |
| 31 | **Dừng M4 để sửa nhịp độ 5 phút đầu — bảy việc, và bốn lỗi cân bằng lộ ra giữa đường** | Làm tiếp M4 nội dung *(loại: §8 giao tiêu chí M1 là "thấy vui hoặc không vui — nếu không vui, DỪNG LẠI VÀ SỬA Ở ĐÂY", và chủ dự án vừa nói thẳng là chán)*; chỉ thêm hiệu ứng *(loại: không chữa được nhịp trả thưởng 1 lần/49 giây)* | Người chơi phản hồi *"chạy vòng vòng đánh quái rất nhàm chán, không thấy rơi vật phẩm hay chế độ auto"*. Đo ra: 7 giây đứng yên để giết MỘT con, nâng cấp +4,4% in ra màn hình vẫn là `"10"`, **không hề có rơi vật phẩm**, nút tự đánh `SetActive(false)` suốt 16 phút, **không một dòng mã âm thanh** trong khi 188 file CC0 nằm không, quái chết là `Destroy()` trong im lặng tuyệt đối. Nhóm 12 agent chẩn đoán rồi 3 giám khảo chấm chéo: gói phản hồi 43/50, rơi vật phẩm 41–42/50, cả ba đều kết luận GHÉP. **Nhưng thứ đắt giá nhất lại là bốn lỗi CÂN BẰNG mà đợt sửa này vô tình đào lên** — xem quyết định #30 và §7. Kết quả đo được sau bảy việc: nhịp trả thưởng **48–54 s → 8,4 s**, khoảng lặng dài nhất **3,4 s → 1,5 s**, kênh phản hồi khi quái chết **0 → 3**, và boss tầng 10 **lần đầu tiên trong đời dự án hạ được bằng đường leo**. 54 test PlayMode, 47 mục kiểm scene |
| 32 | **Tự động hoá luôn "bảng bấm giờ chơi tay 10 tầng"** | Ghi tay 6 cột như kế hoạch giao *(loại một phần: người vẫn phải trả lời chỉ tiêu 10, nhưng 5 cột kia thì máy ghi chính xác hơn và lặp lại được)* | `M4BangBamGio.cs` bật TỰ ĐÁNH, mua nâng cấp tham lam mỗi tầng, leo 10 tầng, in bảng. **Hai lỗi của chính bộ đo phải sửa trước khi tin số của nó:** bản đầu quên mua nâng cấp nên đánh boss bằng trang bị **cấp 1** (300 giây, 0 Mảnh — số sai không phải vì game sai mà vì bộ đo không chơi như người chơi); và cột "Mảnh nhận" đo **số dư ví** nên ra số ÂM khi bộ đo tiêu tiền, còn nhịp trả thưởng gộp cả tầng boss (một trận 151 giây, đúng một phần thưởng) làm trung bình vọt lên 10,9 thay vì 8,4. **Một phát hiện ngoài dự tính:** HP tụt đều 100 → 9 % suốt tầng 1–9 rồi nhảy lên 89 % — cú nhảy đó là **một lần chết**, vì `ResetHealth()` là nguồn hồi máu DUY NHẤT trong game. Tức **hiện tại chết có lợi**. Đó đúng là số liệu mà mục "hồi máu trước tầng boss" của Đợt 2 đang chờ |
| 33 | **M4: 100 tầng · 5 chương · 10 boss — và quét nhanh hoá ra CHỊU LỰC** | Giữ 40 tầng *(loại: §8 giao M4 là "chơi hết được từ đầu đến cuối")*; thêm tầng mà không kiểm biên từng boss *(loại: `Biên theo boss` của bảng tính neo vào trang bị CUỐI GAME nên về cấu trúc không thể phát hiện bức tường ở boss sớm)* | `tower.floors` 40 → 100, thêm `boss.hpMult5-10`, năm bảng nền đổi theo chương (thư mục `Chapters/` sinh sẵn từ M1), năm loại quái theo chủ đề chương, **mười boss mười hình không con nào lặp**. Quái vẫn ĐỎ và nhân vật vẫn LAM NGỌC suốt 100 tầng — quyết định #23 nguyên vẹn. **Mô phỏng leo thật**: cả 10 boss đạt biên ≥ 1,50, trang bị cuối 40/40/40/40 bậc 3333 — khớp đúng `MIN(40, …)` trong công thức cấp kỳ vọng của bảng tính. **Nhưng bỏ quét nhanh đi thì kẹt ở boss 2, 3, 4** (0,99 / 0,91 / 0,92): ô `B32 = 2` không phải giả định cho tiện, nó **chịu lực**. Test biên mở rộng từ 4 lên **10 boss** kèm mệnh đề chống sót: số boss kiểm được phải bằng `tower.floors / tower.bossEvery` |
| 34 | **M5: game lần đầu biết mình đã kết thúc — và một test đỗ nhờ may suốt bốn mốc** | Bỏ qua M5 để thêm nội dung *(loại: chính §8 cảnh báo "M5 là phần dev hay bỏ qua nhất và cũng là phần người xem portfolio đánh giá đầu tiên")*; chỉ thêm hiệu ứng cho đẹp *(loại: thứ M5 thiếu nhất không phải hiệu ứng mà là KẾT THÚC)* | **Tầng 100 trước M5 không có kết thúc**: `FloorRunner` ghi một dòng `Debug.Log` còn sót chữ "M3" rồi `continue` — bày lại đúng tầng đó, mãi mãi. Người leo hai tiếng rưỡi nhận đúng thứ họ nhận ở tầng 99. Thêm **màn đỉnh tháp** (bảng bảy dòng số người chơi tự làm ra, nhạc `8 - End Theme`, chỉ hiện MỘT LẦN cả đời một file save, suy từ `HighestCleared` nên sống qua việc tắt app) và **chuyển cảnh hai cấp** (chớp tối 0,3 s giữa tầng thường vì nó xảy ra 95 lần; đen hẳn + tên chương 2 s ở đổi chương vì nó xảy ra đúng 4 lần). Thêm **khói chết** (giải mục §7 treo từ M1), **tiếng ngã xuống** `GameOver2.wav` — chọn vì 1,50 s ĐÚNG BẰNG `deathDelay`, ba bài GameOver kia còn đang kêu "thua" lúc tầng đã bày lại xong. **Ba lỗi cũ lộ ra:** boss cuối bật CẢ HAI overlay chồng nhau (cùng đặt `timeScale = 0`, cùng bắn một khung hình); `AudioDirector` cấp phát một mảng MỖI KHUNG HÌNH và fade bằng `deltaTime` nên nhạc boss treo nguyên âm lượng suốt lúc người chơi đọc màn chúc mừng; và **ngã xuống là khoảnh khắc CÂM NHẤT trong game** — 1,5 giây đứng im, không tiếng, không chữ, trong khi luật "chết không mất gì" có từ M1 và **chưa bao giờ hiện ra một chữ nào**. **Thứ đắt nhất lại là một test:** bốn test rơi vật phẩm đỗ **nhờ may** từ Việc 5 — scene có HAI `ShardDropSpawner` (Mảnh và Lõi) và chúng gọi `FindFirstObjectByType`; thêm ĐÚNG MỘT component vào `Bootstrap` là thứ tự đảo, cả bốn hỏng cùng lúc. Cùng hệt cái bẫy `FindFirstObjectByType<Canvas>()` của phiên chơi thử — tức lần thứ hai, và lần này nó đã âm thầm sai suốt bốn mốc. Giờ hỏi thẳng `FloorRunner` nó đang dùng bộ nào |
| 35 | **Tầng đọc thứ ba đã tắt suốt ba mốc — và không lớp kiểm nào thấy** | Tô lam bằng tint trên SpriteRenderer lúc chạy *(loại: tint là phép NHÂN, tô lên sprite xám thì ra lam xỉn và tối đi; và nó giấu mất nguyên nhân thật)*; chấp nhận vì "nhân vật vẫn phân biệt được bằng dáng" *(loại: §5.3 bắt người chơi ĐỨNG YÊN ăn đòn — lúc đó dáng không đổi, chỉ có màu phân biệt được)* | Quy tắc lam **chưa bao giờ thiếu** — `hero_tint` (hue 0,47 sat 0,62) nằm trong `tools/doi-bang-mau.py` từ đầu và chạy đúng. Nhưng `rule_for()` chỉ nhận `Actor/CharacterAnimated`, còn M3 (§5.5b, 5 nhân vật) đổi nguồn sang `Actor/Character` vì đó là thư mục DUY NHẤT có đủ 5 người cùng cỡ ô 16px. Từ khoảnh khắc đó, hero_tint chỉ còn tô cho một thư mục **game không đọc nữa**, và cả 5 nhân vật rơi vào nhánh `world` — **bị xám hoá cùng với cái sàn họ đang đứng**. Đo ΔE trên sprite thật, chương 1: **12,6 · 70% pixel thân chìm dưới ΔE 25**; sau khi sửa **49,0 · 0%**. Cả 10 tổ hợp (5 chương × 2 tầng đọc) giờ đạt ΔE 37–66 và chìm 0–1%. **Bộ kiểm màu mới bắt thêm một lỗi thứ hai ngay lần chạy đầu:** `Spirit` (quái chương 5) chỉ 49% sắc đỏ và **ΔE 14,1 với 51% chìm** vào sàn "Sương Lệch" — 20 tầng cuối, đúng chỗ §5.1 tự viết là "quái đỏ và nhân vật lam là hai thứ duy nhất còn màu thật". Hạ trần sáng của `threat_tint` 0,86 → 0,66 (*quái không bao giờ được nhạt*) đưa nó lên 41,5 / 0% mà bốn con kia xê dịch không quá 0,2. **Bài học không phải về màu mà về lớp kiểm:** 82 test PlayMode, 64 mục kiểm scene và bốn đợt xem ảnh chụp đều KHÔNG thấy, vì mã đúng, tham chiếu đủ, ảnh chụp vẫn ra một nhân vật — chỉ là nhân vật đó cùng màu với sàn. Thứ bắt được nó là **đo ΔE giữa hai tầng đọc**, giờ đã khoá thành 10 mục trong `VerifyM1Scene` |
| 36 | **"Không nâng cấp giữa chừng được à?" — làm được từ M2, game chưa bao giờ nói** | Cho bảng TRANG BỊ dừng trò chơi (`timeScale = 0`) *(loại: NGƯỢC HẲN điều chủ dự án xin — họ muốn vừa thu thập vừa nâng, dừng game là chặn đúng thứ đó)*; thu bảng lại còn nửa trên để chừa cần gạt *(loại: đấu trường nằm đúng dải giữa màn hình dọc, thu kiểu gì cũng che mất nó)* | Chủ dự án hỏi *"tôi không thể điều chỉnh trang bị ở các tầng khác được hay sao?"*. **Đo A/B trước khi sửa gì**: bật tự đánh, mở bảng, ngồi 30 giây — `+1 tầng · +400 Mảnh · nhân vật đi 11,8 đơn vị`; không mở bảng — `+1 tầng · +400 Mảnh · 11,9 đơn vị`. **Giống hệt nhau.** Nút TRANG BỊ chưa bao giờ khoá (`HudUI:369` ghi rõ *"nền LUÔN sáng: cánh cửa này chưa bao giờ khoá"*), `Toggle()` không hề đụng `timeScale`, và nhãn Mảnh trong bảng vẫn chạy sống qua `GameState.Changed`. Tức khả năng ĐÃ CÓ TỪ M2 — thứ thiếu là **tấm phủ 0,92 che kín màn hình nên không có cách nào biết**. Cùng họ với hai nút câm của gói giao tiếp: một tính năng không nói ra thì bằng không tồn tại. Sửa: gom bốn khung "đang xảy ra chuyện gì" (tầng · máu · Mảnh · Lõi) vào một hộp `HudInfo` rồi **nhấc lên TRÊN tấm phủ** khi mở bảng, trả về đúng chỗ cũ khi đóng (để nguyên trên cùng là thẻ chương và màn đỉnh tháp bị HUD đè). Tấm phủ nhạt từ 0,92 xuống 0,84 để thấy trận đánh qua khe giữa các hàng. Nhãn Mảnh nói thẳng: *"37.362 Mảnh · tầng 21, tự đánh vẫn chạy"* — và **chỉ nói khi tự đánh thật sự bật**, vì tắt tự đánh thì tấm phủ chặn cần gạt nên người chơi đứng im, hứa "vẫn chạy" là nói dối. Hai test khoá lại: một bắt `timeScale` phải bằng 1 và Mảnh phải tăng sau 25 giây trong bảng, một bắt HUD phải nổi lên trên rồi trả về chỗ cũ |
| 37 | **Hai màn hình lớn nhất game CHƯA BAO GIỜ mở được bằng ngón tay** | Tin bốn lớp kiểm chứng đang có *(loại: cả bốn đều đi vòng qua đúng chỗ hỏng — xem cột lý do)*; dùng `UnityEventTools.AddPersistentListener` trong bộ dựng *(loại: API chỉ có trong Editor, và kiểu hỏng của nó vẫn im lặng; nối lúc chạy thì không có gì để hỏng)* | Chủ dự án báo *"tầng 1 không mở được trang bị cũng như nhân vật"*. **Nguyên nhân**: `BuildM1Scene.cs` nối bốn nút bằng `btn.onClick.AddListener(...)`. Dòng đó chạy đúng trong phiên Editor dựng scene rồi **biến mất khi `SaveScene` ghi file** — `AddListener` tạo đăng ký LÚC CHẠY, Unity chỉ tuần tự hoá `m_PersistentCalls`. File `M1.unity` ghi đúng điều đó: cả **7 Button** đều `m_Calls: []`, trong khi `m_Interactable: 1` và `m_TargetGraphic` đủ cả — nên nút *trông* hoàn toàn bình thường. TRANG BỊ chết từ **M2**, NHÂN VẬT từ **M3**, và cả hai nút ĐÓNG cũng chết, tức mở được cũng không thoát ra được. **Không phải lỗi của tầng 1** — hai nút câm ở MỌI tầng, chủ dự án chỉ tình cờ thử ở tầng đầu. **Vì sao bốn lớp kiểm chứng đều mù:** `VerifyM1Scene` soi tham chiếu `[SerializeField]` — nút được nối đủ, không thiếu gì; test PlayMode gọi thẳng `UpgradeScreen.Toggle()` — đi vòng qua đúng cái hỏng; ảnh chụp hai màn đó cũng do chính test ấy dựng ra nên nhìn vẫn đẹp; và chơi thử thì tôi chưa từng bấm hai nút đó bằng một cú chạm mô phỏng. Cả bốn lớp tránh đúng một chỗ duy nhất bị hỏng. **Sửa**: nối onClick ở `Start()` lúc chạy — đúng khuôn QUÉT NHANH và TỰ ĐÁNH, hai nút chưa bao giờ hỏng vì chúng vẫn luôn nối kiểu đó. Đổi tên nút đóng của màn nhân vật thành `CloseChar` vì trùng tên `Close` đã làm chính bộ đo của lỗi này bấm nhầm nút. **Lớp kiểm mới `HudNutTests`**: mọi test ở đó **BẤM**, không gọi hàm — kèm một phép raycast qua `EventSystem` khẳng định không thứ gì nuốt cú chạm trước khi nó tới nút. Nhóm 21 agent điều tra song song độc lập ra cùng kết luận, và chính họ sửa lại cách tôi mô tả triệu chứng |
| 38 | **BA THANH VƠI ĐẦY CỦA GAME CHƯA BAO GIỜ VƠI — và mục kiểm đi xác nhận đúng cái tính chất gây ra lỗi** | Tin mục kiểm `HealthBar: Image.Type = Filled` *(loại: nó khẳng định đúng thứ làm hỏng)*; chỉ sửa vạch quét vì chủ dự án chỉ báo mỗi nút đó *(loại: cùng một dòng mã sai lặp ba lần, sửa một chỗ là để lại hai)* | Chủ dự án báo *"bấm QUÉT NHANH tôi không thấy có gì khác biệt"*. Điều tra ra hai nguyên nhân tách bạch, và nguyên nhân thứ hai lớn hơn nhiều: **`Image.Type.Filled` với sprite RỖNG không vơi.** `Image.OnPopulateMesh` mở đầu bằng `if (activeSprite == null) { base.OnPopulateMesh(toFill); return; }` — nhánh Filled không bao giờ chạy, nó vẽ nguyên khối chữ nhật và bỏ qua `fillAmount` hoàn toàn. Cả **ba** thanh của game dựng kiểu đó: đo bằng lưới thật ở `fillAmount = 0,25` ra **556/556 · 940/940 · 160/160 pixel**. Nghĩa là **thanh máu người chơi chưa bao giờ vơi kể từ M1**, **thanh máu boss kể từ M3** (thứ §5.1 đòi có vì "một trận 200 giây mà không có vạch tiến trình"), và **vạch quét chưa bao giờ chạy** — `HudUI.OnSweepProgress` gán `fillAmount` 60 lần/giây suốt 3 giây quét mà không một pixel nào đổi. **Mục kiểm cũ trong `VerifyM1Scene` khẳng định `img.type == Image.Type.Filled`** — tức nó cấp giấy thông hành cho đúng tính chất gây ra lỗi, và đã cấp suốt bốn mốc. Sửa: gán `WhitePixelSprite()` (đã có sẵn, đang dùng cho thanh máu quái) cho cả ba; mục kiểm giờ đòi **có sprite**; và test mới **hỏi LƯỚI** mà `Image` sinh ra thay vì hỏi thuộc tính — kèm một test đầu-cuối đánh mất 70% máu thật rồi đo. Nhóm 43 agent quét "giao diện nói sai về chính nó" tìm ra lỗi này; tôi đo lại bằng lưới trước khi tin. Ba lỗi nhỏ hơn cùng đợt: nút khoá bị Unity nuốt cú chạm nên **im lặng tuyệt đối** (giờ nói lý do bằng banner + tiếng `Cancel2.wav`); nhãn `"QUÉT NHANH / DỌN TẦNG 1"` đọc ra như MÔ TẢ VIỆC NÚT LÀM chứ không phải điều kiện (đổi sang khuôn đếm ngược `"CÒN 1 TẦNG"` như ba nút kia); và `"TRANG BỊ / TỚI HẠN"` hiện cả khi chỉ **thiếu Lõi** chứ chưa kịch bậc — bảo người chơi dừng lại đúng lúc hệ thống đang chờ họ đi gom Lõi |
| 39 | **Đo hình học ở sáu tỉ lệ màn hình — và bộ đo bắt được hai lỗi nặng hơn cái nó đi tìm** | Chỉ vá banner cho vừa 20:9 *(loại: cùng một dòng sai — bề ngang tính từ hằng số `UiRefW` — lặp ở BA chỗ)*; giữ `matchWidthOrHeight = 0,5` *(loại: game DỌC thì bề ngang là trục bị bó, lấy trung bình hai trục là tự bóp trục đang bó)* | Dựng `KiemTiLeManHinh` trong `VerifyM1Scene`: tính khung thiết kế thật theo đúng công thức `ScaleWithScreenSize`, dựng lại rect của 10 phần tử HUD bằng toán (batchmode không có Game view để đổi cỡ), rồi khẳng định không cặp nào chồng nhau và không gì tràn ra ngoài — ở **sáu** tỉ lệ từ 16:9 tới 21:9 và một máy tính bảng 4:3. **Lần chạy đầu hỏng cả sáu**, và thứ nặng nhất KHÔNG phải chuyện tỉ lệ: `BossBar đè CharButton (184×44)` **ngay ở 1080×1920, đúng khung thiết kế** — thanh máu boss rộng `UiRefW − Edge×2` = 952 nên đi từ x 64 tới 1016, mà cột nút bắt đầu ở 832; **184 đơn vị bên phải của nó chui xuống dưới nút, ở mọi độ phân giải, suốt từ M3**. Đúng con bọ đã sửa một lần cho banner sự kiện — chú thích ở đó còn nguyên — mà không ai áp ngược lại. Sửa: ba phần tử (thanh máu, thanh boss, banner) chuyển sang **trải ngang** tới đúng mép cột nút thay vì rộng cứng; `matchWidthOrHeight` **0,5 → 0** để bề ngang khung thiết kế luôn đúng 1080 ở mọi tỉ lệ. **Rồi ảnh chụp ở 20:9 lộ ra lỗi thứ hai, hoàn toàn khác loại:** camera trực giao khoá nửa chiều CAO nên màn hình càng cao thấy càng HẸP — `OrthoSize = 7,2` cho nửa bề ngang **4,05** ở 16:9 (vừa đủ cho quái ở bán kính 3,5 cộng nửa thân, dư 1,2%) nhưng chỉ **3,24** ở 20:9, tức **tâm hai con quái hai bên nằm ngoài màn hình**. Trên điện thoại thật hôm nay người chơi đánh nhau với thứ họ chỉ thấy một nửa. Thêm `CameraFit`: nới `orthographicSize` vừa đủ để bề ngang cần thiết luôn lọt, không bao giờ thu nhỏ hơn cỡ thiết kế; lề suy từ chính `enemy.spawnRadius` + khoá mới `camera.marginX` chứ không viết cứng; sàn nới 14 → 24 đơn vị vì ở 21:9 camera nhìn thấy 18,7 đơn vị theo chiều dọc |

---

## 7. Câu hỏi còn treo

- Tên game.
- Bộ asset pack cụ thể (quyết định này khóa luôn phong cách hình ảnh — chọn sớm).
- Boss có cơ chế riêng hay chỉ là quái nhiều máu?
  *(M3 cài theo hướng "nhiều máu + tầm đánh xa hơn 1,4 lần". Đo được 75–193 giây mỗi con,
  nằm trong dải §5.1 đã chốt. Từ chương 3 mới thêm cơ chế — để bản chơi được ra sớm.)*
- ~~Hệ số máu 10 con boss~~ — **đã chốt**, xem §5.10.
- ~~Bộ asset pack cụ thể~~ — **đã chốt**: Ninja Adventure, CC0. Xem §3 giả định 8.
- ~~Nhạc và âm thanh lấy từ đâu~~ — **đã chốt**: cùng bộ asset, cũng CC0.
- ~~Icon cho ba ô Giáp / Găng / Nhẫn~~ — **đã có sẵn**, tôi tìm nhầm chỗ: chúng nằm ở
  `Ui/Skill Icon/` chứ không phải `Items/`. Xem §5 của `GIAO-DIEN.md`.
- ~~Tách giao diện khỏi quy tắc "world"~~ — **đã làm**, 359 file giữ nguyên tông ấm.
- ~~⚠️ **NHÂN VẬT KHÔNG CÒN LAM — tầng đọc thứ ba đang hỏng**~~ — **ĐÃ SỬA**, xem quyết định
  #35. Nguyên nhân hoá ra không phải "chưa tô" mà là quy tắc **đã có và tự tắt trong im
  lặng**: `rule_for()` của `tools/doi-bang-mau.py` chỉ tô lam cho `Actor/CharacterAnimated`,
  còn M3 đổi nguồn nhân vật sang `Actor/Character` (thư mục duy nhất có đủ 5 người, xem
  `BuildM1Scene.cs:65`) và không ai sửa dòng đó. Từ đó cả 5 nhân vật rơi vào nhánh `world`,
  tức **bị xám hoá cùng với chính cái sàn họ đang đứng**. Đo bằng ΔE trên sprite thật:
  chương 1 cho **12,6 và 70% pixel thân chìm dưới ΔE 25**. Sau khi sửa: **49,0 và 0% chìm**.
  Đợt đo đó bắt thêm một con nữa — xem mục dưới.
- ~~⚠️ **QUÁI `Spirit` CHÌM VÀO SÀN CHƯƠNG 5**~~ — **ĐÃ SỬA** cùng đợt màu lam, và nó chỉ lộ
  ra vì bộ kiểm màu mới soi cả năm loại quái chứ không chỉ con đầu. `Spirit` vẽ gần như toàn
  pixel TRẮNG; trần sáng 0,86 của `threat_tint` biến trắng thành (240,209,197) — kem nhạt —
  trong khi sàn "Sương Lệch" là (221,213,222). Đo được **ΔE 14,1 với 51% pixel thân chìm**,
  còn bốn con kia đạt 46–66 và 0%. Đó là **20 tầng CUỐI**, và chính §5.1 chương 5 tự viết:
  *"ở 20 tầng khó nhất, quái đỏ và nhân vật lam là hai thứ duy nhất còn màu thật."*
  Hạ trần xuống **0,66** ("quái không bao giờ được nhạt") đưa Spirit lên **41,5 / 0% chìm**,
  và bốn con kia **xê dịch không quá 0,2** vì vốn gần như không có pixel nào vượt trần đó.
  Mười boss đều đã đo lại: 100% sắc đỏ, ΔE 42–82, chìm 0–14%.
- ~~⚠️ **GIAO DIỆN MỚI CHỈ ĐÚNG Ở 16:9**~~ — **ĐÃ SỬA**, xem quyết định #39. Và bộ đo dựng ra
  để chứng minh nó lại bắt được hai thứ nặng hơn: thanh máu boss chui dưới nút NHÂN VẬT ở
  **mọi** độ phân giải kể cả 16:9, và **đấu trường không lọt khung** ở 20:9 — quái bị cắt
  mất một phần thân.
- **Nút HUD bị tấm phủ chặn khi bảng đang mở.** Sau quyết định #36 thì cụm HUD *thông tin*
  (tầng · máu · Mảnh · Lõi) nổi lên trên tấm phủ, nhưng cột nút bên phải (TRANG BỊ · NHÂN VẬT ·
  QUÉT NHANH · TỰ ĐÁNH) thì không — nên đang mở bảng TRANG BỊ mà bấm lại chính nút đó sẽ
  không đóng được, phải bấm ĐÓNG. Cho cả cột nổi lên thì đóng/mở tiện hơn nhưng mở ra nguy cơ
  chạm nhầm QUÉT NHANH hoặc TỰ ĐÁNH khi ngón tay đang ở vùng bảng. Chưa chốt.
- ⚠️ **Font HUD — `NormalFont.ttf` KHÔNG DÙNG ĐƯỢC, đo ra rồi.** Mục này treo từ M2 với giả
  định "bộ asset có sẵn font, chưa nối thôi". Đếm bảng mã: `NormalFont.ttf` có **147 glyph**
  và **thiếu 23 ký tự tiếng Việt** — `ă Đ đ ũ ơ Ư ư ả ấ ầ ẫ Ậ ắ ế ệ Ỉ Ị ồ Ộ ộ ờ Ợ Ự`. Toàn bộ
  giao diện game viết bằng tiếng Việt, nên nối nó vào là *ĐỈNH THÁP* thành *?INH TH?P*.
  Không phải "chưa làm" mà là **làm không được**. Ba đường còn lại: (a) giữ LiberationSans —
  đọc tốt, nhưng nó là font mặc định của TextMeshPro nên người xem portfolio đọc ra ngay là
  "chưa ai chọn font"; (b) thêm một font OFL có dải Việt đầy đủ (*Be Vietnam Pro* thiết kế
  riêng cho tiếng Việt) — đây là **thêm phụ thuộc mới vào repo**, nên để chủ dự án quyết;
  (c) tự vẽ bổ sung 23 glyph vào `NormalFont.ttf` — đắt và phải giữ giấy phép CC0 sạch.
  **Chưa chọn.**
- ~~**Hoạt ảnh chết cho quái và boss**~~ — **ĐÃ LÀM** (M5), đúng theo kế hoạch ghi sẵn ở đây:
  sáu khung `FX/Smoke/Smoke` bung ra 1,45 lần rồi mờ ở nửa sau, cỡ khói theo boss hay quái
  thường (boss to gấp 2 lần nên khói bằng nhau là cái chết của boss trông NHỎ HƠN của lâu la).
  Pool riêng, truy cập tĩnh cùng khuôn `SfxPlayer` — xem `PuffFxSpawner`.
- ⚠️ **HÚT MÁU CHƯA CÓ TRONG MÔ HÌNH — phải chốt trước mốc M4.** §5.5 ghi Giáp cho "máu +
  hút máu" nhưng bảng tính chỉ mô hình hoá máu. Đo ngược tại tầng 100: người chơi nhận
  **72,8 sát thương/giây**, nên chỉ cần hút máu **5,85%** là build sát thương cao nhất hồi
  nhanh hơn mất — **bất tử, biên vô cực**. 5,85% là con số hoàn toàn tầm thường trong ARPG.
  Chốt hút máu mà không đưa vào bảng tính thì toàn bộ dải biên 1,50–5,15 và bảng hệ số boss
  §5.10 trở thành vô nghĩa. Ngưỡng bất tử theo build: sát thương cao nhất 5,85% · dàn đều
  10,3% · thủ dày 19,4%.
- ⚠️ **CHẾT ĐANG CÓ LỢI — `ResetHealth()` là nguồn hồi máu DUY NHẤT trong game.** Bảng bấm
  giờ tự động (quyết định #32) đo được HP tụt đều 100 → 9 % suốt tầng 1–9 rồi nhảy lên 89 %;
  cú nhảy đó là một lần chết ở tầng 9. Không có hồi máu giữa các tầng, không có vật phẩm hồi
  máu, nên cách duy nhất để vào tầng boss với máu đầy là **cố tình chết trước đó**. Chỉ tiêu 6
  ("HP ≥ 50 % khi vào tầng 10") đạt được 89 % CHÍNH VÌ có cú chết đó — không có nó thì gần 0.
  Bảng tính dùng **máu ĐẦY** ở `'Kiểm chứng build'!P` nên hồi đầy trước boss là đúng mô hình.
  Chưa sửa vì nó chồng lên ba lớp buff sinh tồn vừa thêm (van #4, `dpsFloor1` 1,5, đóng băng
  hồi chiêu) — phải nhân ra một lần rồi mới quyết.
- ~~⚠️ **HAI TÍNH NĂNG ĐANG MỞ THÌ CÂM**~~ — **ĐÃ SỬA** (gói giao tiếp). Nút TRANG BỊ đếm
  *"CÒN 180"* → *"NÂNG ĐƯỢC"* → *"ĐỘT PHÁ ĐƯỢC"*; nút NHÂN VẬT đếm *"CÒN 10 TẦNG"*; thẻ
  khoá ghi *"Tầng 20 · còn 10 tầng"*; màn nhân vật hiện số thật (*"đòn 14,1 · máu 110"*) và
  nói thẳng rằng đổi nhân vật KHÔNG làm mạnh hơn. Xoá `PlayerAppearance` (mã chết). Nội dung cũ: Nhóm điều
  tra chỉ ra một bất đối xứng không ai để ý: QUÉT NHANH và TỰ ĐÁNH (đang khoá) được chăm
  từng li — đổi nền, đổi màu chữ, đếm ngược *"CÒN 20 TẦNG"*. Còn TRANG BỊ và NHÂN VẬT
  (đang MỞ từ giây 0) thì `HudUI` không giữ nổi một tham chiếu, và `BuildM1Scene.cs:345`
  vứt luôn tham chiếu nhãn nút NHÂN VẬT (`(Button charBtn, TMP_Text _)`) nên nhãn đó **về
  mặt kỹ thuật không đổi được lúc chạy**. Hệ quả: bấm NHÂN VẬT ở giây 0 ra **5 thẻ chết
  trên 5**, kéo dài 8 phút, và thẻ khoá ghi *"Hạ boss 2 để mở"* mà không nói boss 2 là
  tầng 20 — đúng câu chủ dự án hỏi, màn hình từ chối trả lời. Chưa sửa (chủ dự án chọn chỉ
  vá bốn lỗi trước); gói giao tiếp còn nguyên trong `docs/KE-HOACH-NHIP-DO.md`.
- **Nhẫn là ô đổ rác** (§5.5) — sửa hay chấp nhận? Cân lại bằng số thì phá §5.11; thêm cơ chế
  thì phá §5.1. Chưa có lời giải rẻ. **Cập nhật:** đo lại cho thấy nó là bẫy **ngay từ tầng 1**,
  không phải chỉ ở tầng 100 như mục này từng ghi — cùng 300 Mảnh, Vũ khí cho **+4,40%** DPS còn
  Nhẫn **+1,15%** (hệ số chí mạng chỉ vào công thức qua `1 + (cm−1)/meterSize`, tức bị chia cho 5),
  đắt gấp **3,8 lần** trên mỗi phần trăm sức mạnh. Chú thích `Equipment.cs:17` ghi *"không có ô nào
  tốt hơn ô nào"* — **sai đo được**, và chính câu đó là lý do suốt từ M2 không ai đi kiểm lại ô này.
  Chưa cân bằng lại (đụng `can-bang.xlsx`), nhưng giao diện giờ **nói thẳng**: Nhẫn hiện
  *"7 đòn · chưa trần nào hạ được — cần đột phá"* trong khi Vũ khí hiện *"còn 4 cấp nữa xuống 6"*.
- ⚠️ **QUÉT NHANH LÀ BẮT BUỘC ĐỂ CHƠI HẾT GAME, không phải tiện ích.** Mô phỏng leo hết
  100 tầng: với ngân sách 2× (đúng ô `B32`) thì **cả 10 boss đạt ≥ 1,50**; nếu KHÔNG quét
  (chỉ leo) thì **kẹt ở boss 2, 3, 4** — biên 0,99 / 0,91 / 0,92, tức thời gian sống ngắn
  hơn thời gian giết. Một người chơi không bao giờ bấm nút QUÉT NHANH sẽ **không thể** qua
  tầng 20, và game chưa nói điều đó với họ ở bất cứ đâu.
- ~~⚠️ **QUÉT NHANH ĐANG VƯỢT TRẦN CỦA BẢNG TÍNH**~~ — **ĐÃ SỬA** (Việc 2). Trần ngân sách
  `sweep.totalMult = 2` lấy thẳng từ ô `B32`: quét kiếm thêm được tối đa `(2−1) × Mảnh-đã-leo`,
  hết thì nút báo *HẾT NGÂN SÁCH*, leo thêm thì trần tự nới. Save lên v3 để lưu hai nguồn Mảnh
  riêng, có di trú dựng lại `shardsClimbed` cho bản cũ. Ba test khoá: `Quet_nhanh_dung_lai_khi_
  het_ngan_sach`, `Tong_Manh_khong_bao_gio_vuot_he_so_cay_lai`. Nội dung cũ: Quyết định #27 viết rằng
  `can-bang.xlsx` "chưa bao giờ tính farm". **Câu đó sai**, và tôi đã đọc lại bảng để xác nhận:
  ô `'Thông số'!B32 = 2` nhãn *"Hệ số cày lại (quét nhanh)"*, chú thích *"1.0 = chỉ clear mỗi
  tầng một lần"*, và `'Đường cong tầng'!E = D × B32`. Bảng tính CÓ mô hình farm, ở đúng **2×**
  Mảnh leo — và cột cấp trang bị kỳ vọng cũng suy từ ngân sách đó. Mã đang giao **vô hạn**:
  133 Mảnh/giây khi quét so với 9,6 khi đánh tay (**13,9 lần**), max cả bốn ô lên cấp 10 trong
  **2,6 phút** giữ nút ở tầng 1. Hồi 3 giây không chặn được gì. Cách sửa đã rõ: trần ngân sách
  2× `ShardReward` mỗi tầng.
- ⚠️ **NÉ ĐÒN: đã sửa một nửa, NỬA CÒN LẠI LÀ LỖI CỦA BẢNG TÍNH.** Việc 2 đã đảo thứ tự trong
  `Enemy.Update()` nên hồi chiêu quái đóng băng khi người chơi ngoài tầm. Đo bằng mô phỏng vòng
  chiến đấu thật (dt 10ms) tại boss 1:

  | Lối chơi | Biên TRƯỚC | Biên SAU |
  |---|---|---|
  | đứng lì | 0,75 | 0,75 |
  | đứng 1s lùi 1s | **0,60** | **0,75** |
  | đứng 1s lùi 2s | **0,60** | **0,75** |

  Tức là trước đây di chuyển bị phạt HAI lần (mất DPS mà vẫn ăn đủ đòn); giờ né là trung tính.
  **Nhưng biên vẫn bất biến 0,75, không đạt 1,50.** Và nó bất biến vì một lý do cấu trúc: §5.3
  khoá *sát thương gây ra* và *sát thương nhận vào* vào **cùng một đại lượng** — thời gian đứng
  yên trong tầm. Gọi f là tỉ lệ đó thì giết mất `Q/(N·f)` còn chết mất `P/(R·f)`, f triệt tiêu.
  Không lối chơi nào đổi được biên.

  **Chỗ sai nằm ở bảng tính, không ở mã.** `'Kiểm chứng build'!T5 = P5/(R5 × B9)` dùng B9 = 0,5
  cùng lúc với `S5 = Q5/N5` dùng DPS ĐẦY ĐỦ — tức giả định *vừa né nửa đòn vừa đánh full*. Đo
  hình học thì B9 = 0,5 **đúng cho tầng thường**: 6 quái trải trên vòng bán kính 3,5 mà tầm quái
  2,8, nên đứng đánh một con thì chỉ **1/6** con với tới được — ăn 17% tổng dps của tầng, còn
  rộng tay hơn 0,5. Nhưng **tầng boss chỉ có 1 con**, đứng đánh nó là ăn 100%. Bảng đã đem một
  hằng số của ĐÁM ĐÔNG áp vào bối cảnh ĐƠN MỤC TIÊU.

  **CẬP NHẬT — nhóm phản biện đã bác bỏ hai lần cách tôi giải thích nguyên nhân, và tìm ra
  một lỗi thứ ba.** Con số 0,75 thì ba lần dựng lại độc lập đều xác nhận. Nhưng:

  1. *"Không lối chơi nào đổi được / f triệt tiêu"* — **SAI**. `AutoAttack.cs` trừ hồi chiêu
     chỉ với guard `IsMoving`, **không có guard tầm** — đứng yên NGOÀI tầm boss vẫn nạp đòn
     miễn phí. Hai đồng hồ tách rời, f không triệt tiêu. Đánh-rồi-chạy có lãi khi
     `2×(tầm địch − tầm mình)/tốc độ < nhịp đánh`. Ở quái thường: 0,356s < 0,816s → **có lãi**.
     Ở boss: 0,853s > 0,816s → **lỗ**, hụt đúng 4,5%. Thứ phá bất đẳng thức đó là
     `boss.attackRangeMult = 1,4` — **một khoá tôi tự thêm ở M3, không có trong bảng tính**.
  2. *"B9 là hằng số đám đông bị áp nhầm vào đơn mục tiêu"* — **SAI về sự kiện**. B9 xuất hiện
     đúng **66 ô, cả 66 đều là ô boss**. Không một ô tầng thường nào dùng nó. B9 sinh ra đã là
     hằng số né **riêng cho boss**.
  3. **LỖI THỨ BA, đã sửa:** `PlayerHealth` cache `_maxHp` và chỉ gọi `Rescale()` lúc Start /
     mua Giáp / tẩy điểm / đổi nhân vật — **không bao giờ khi lên tầng**. Ai leo một mạch tới
     tầng 10 mà không mở màn nâng cấp thì đánh boss bằng máu tầng 1, mất trọn **1,486 lần**.
     Van an toàn #4 của §5.8 đã im lặng không chạy suốt từ M2. Test khoá:
     `Mau_nen_PHAI_tang_theo_tang_da_qua`.

  **Biên thật, ở đúng cấp trang bị bảng kỳ vọng:**

  | Boss | Tầng | Trước khi sửa | Sau khi sửa van #4 | Nếu gộp thêm B9 vào CSV |
  |---|---|---|---|---|
  | 1 | 10 | 0,51 | 0,75 | **1,50** |
  | 2 | 20 | 0,33 | 0,75 | **1,50** |
  | 3 | 30 | 0,21 | 0,75 | **1,50** |
  | 4 | 40 | 0,14 | 0,77 | **1,54** |

  Hai lỗi cộng lại cho đúng 1,50 ở cả bốn boss — đường cong máu boss được giải ngược với giả
  định CẢ HAI đều chạy. **Còn một quyết định chưa chốt:** `enemy.dpsFloor1` nên là 3 (số thô,
  ô B7) hay 1,5 (đã gộp B9, đúng thứ công thức biên dùng). Đây là lựa chọn thiết kế, không
  phải lỗi — xem ba hướng dưới: (a) nhận B9 chỉ dành
  cho tầng thường, tính lại cột boss với B9 = 1,0 rồi hạ hệ số máu boss cho khớp; (b) cho quái
  có nhịp vung tay, rời tầm trong lúc vung thì đòn trượt — làm B9 đạt được thật, nhưng kế hoạch
  đã loại vì coi là "hoàn tiền chứ không phải né"; (c) tăng máu nền hoặc hạ máu boss 1.

  Nội dung chẩn đoán gốc:
  Ô `'Thông số'!B9 = 0,5` nhãn *"Tỉ lệ sát thương thực nhận — nhờ luật di chuyển ở mục 5.3"*,
  và dòng 7 ghi 3 dps là *"nếu người chơi đứng yên hoàn toàn"*. Nhưng `Enemy.Update()` trừ hồi
  chiêu **bất kể người chơi ở đâu**, và ra ngoài tầm thì `return` mà không reset — hồi chiêu âm
  sẵn, bước vào là ăn đòn ngay. Đo với ngân sách của bảng (cấp 7 cả bốn ô): né 50% cho biên
  **1,50** (đúng bằng ngưỡng `B40`), mã hôm nay cho **0,75**. Đây chính là lỗi đã sửa cho NGƯỜI
  CHƠI ở M1 mà quên áp ngược cho QUÁI — `AutoAttack.cs` đặt guard trước khi trừ, `Enemy.cs` thì
  không. Sửa là đảo thứ tự hai khối.
- **Đánh lâu hơn khi lên tầng, không phải ngắn đi.** Máu quái +5,18%/tầng còn Vũ khí +4,4%/cấp:
  số đòn giết một con đi từ 7 (tầng 1) lên 11 (tầng 20) — **chậm đi 57%**. Đã thử cả ngân sách
  2× lẫn trần 60, vẫn còn tụt 29%. Biên an toàn vẫn đạt nên **không phải lỗi**, nhưng nó đảo
  ngược đúng lời hứa "mạnh đều đều" ở §1. Bảng tính ghi `hpGrowth` 5,18% là *"giá trị LỚN NHẤT
  giữ được biên ≥ 1,60"*, tức **hạ xuống là an toàn** — nhưng `shard.growth` 9% đang bám theo
  nó nên phải chỉnh cùng lúc. Chưa chốt.
- **Tháp mới có 40/100 tầng và 4/10 boss.** Đúng phạm vi M3 (§8 giao "hệ thống đủ, thiếu nội
  dung"), nhưng nghĩa là sáu cổng đột phá cuối và sáu nhân vật cuối **chưa từng chạy thật**.
  Hệ số máu boss 5–10 ở §5.10 vẫn chỉ là số trên giấy.

---

## 8. Lộ trình đề xuất

Ràng buộc lớn nhất của dự án này **không phải kỹ thuật mà là bỏ dở**.
Lộ trình dưới đây thiết kế để luôn có thứ chạy được trên máy.

| Mốc | Nội dung | Tiêu chí hoàn thành |
|---|---|---|
| ~~**M1 — Vòng lặp sống**~~ ✅ | 1 tầng, 1 loại quái, di chuyển + tự đánh, thanh chí mạng | Đánh được, thấy vui hoặc không vui. **Nếu không vui, dừng lại và sửa ở đây** |
| ~~**M2 — Tiến trình**~~ | 20 tầng, rơi Mảnh, nâng cấp 4 ô trang bị, save/load | ✅ **XONG** — 17/17 test PlayMode |
| ~~**M3 — Xương sống**~~ | Boss, Lõi, đột phá, tẩy điểm, quét nhanh, auto-battle, **mở khoá và đổi nhân vật (§5.5b)** | ✅ **XONG** — 30/30 test PlayMode, 37 mục kiểm scene. Tháp 40 tầng, 4 boss, 12 Lõi, 5 nhân vật |
| ~~**M4 — Nội dung**~~ | 100 tầng, 5 chương, 10 boss | ✅ **XONG** — 66/66 test, 53 mục kiểm. Mô phỏng leo hết: cả 10 boss đạt biên ≥ 1,50 |
| ~~**M5 — Bóng bẩy**~~ | Hiệu ứng, rung màn hình, âm thanh, chuyển cảnh, màn hình chúc mừng | ✅ **XONG** — **96/96 test, 94 mục kiểm** (số hiện tại, sau các đợt vá #36-#39). **Tháp 100 tầng lần đầu có KẾT THÚC** |

**Cảnh báo (giữ lại làm ghi chép):** M5 là phần dev hay bỏ qua nhất và cũng là phần
người xem portfolio đánh giá đầu tiên. Đừng cắt nó để thêm tầng.

**Cả năm mốc đã xong.** Việc còn lại không nằm ở §8 nữa mà ở §7: hút máu chưa vào mô
hình, Nhẫn là ô đổ rác, quét nhanh bắt buộc mà game không nói, font HUD. Và một thứ
không lớp máy nào thay được — **chưa ai chơi hết 100 tầng**.

---

## 9. Ghi chú kỹ thuật — Unity 6

Các quyết định dưới đây chốt trước khi mở editor, để không phải làm lại giữa chừng.

### 9.1 Thiết lập dự án

| Hạng mục | Chọn | Lý do |
|---|---|---|
| Phiên bản | **Unity 6 LTS** | Bản ổn định dài hạn; tránh bản beta cho dự án làm dần |
| Render pipeline | **URP, 2D Renderer** | Cho phép dùng đèn 2D và shader — vũ khí chính của mốc M5 |
| Input | **Input System (mới)** | Có sẵn `On-Screen Stick` cho cần gạt ảo; không dùng `Input` legacy |
| UI | **uGUI (Canvas)** | Ổn định, tài liệu nhiều, hợp game UI. UI Toolkit để dành cho công cụ editor |
| Scripting backend | **IL2CPP** | Bắt buộc cho iOS; bật Managed Stripping mức Medium để giảm dung lượng |
| Hướng màn hình | Khóa **Portrait** | Theo giả định mục 3 |

### 9.2 Dữ liệu cân bằng để ngoài code

Yêu cầu ở mục 4 được hiện thực bằng hai lớp:

- **ScriptableObject** cho cấu trúc: `EquipmentDef`, `EnemyDef`, `FloorDef`, `BossDef`.
  Sửa được ngay trong Inspector, không cần biên dịch lại.
- **CSV/JSON trong `StreamingAssets`** cho ba con số của mục 5.7 và bảng chi phí nâng cấp.
  Sửa được **không cần mở Unity** — quan trọng khi bạn ngồi cân bằng trên bảng tính.

> **Quy tắc cứng:** không một con số cân bằng nào được viết thẳng trong file `.cs`.
> Nếu thấy mình gõ `damage * 1.12f` thì đó là lỗi.

### 9.3 Những chỗ dễ vấp trên mobile

| Vấn đề | Cách xử lý |
|---|---|
| Sinh/hủy quái liên tục gây giật do GC | `UnityEngine.Pool.ObjectPool<T>` (có sẵn từ Unity 2021+) cho quái, đạn, **và số sát thương bay lên** |
| Số sát thương dùng `Text` gây tốn draw call | Dùng **TextMeshPro** + pooling; gom chung một canvas riêng |
| Canvas UI vẽ lại toàn bộ khi một phần tử đổi | Tách canvas: canvas tĩnh và canvas động **để riêng** |
| Build iOS chậm làm nản vòng lặp | Phát triển và cân bằng trên **Android + Unity Remote**, chỉ build iOS ở các mốc |

### 9.4 Save — rủi ro tin cậy duy nhất

Theo mục 4, hỏng save là rủi ro thật duy nhất của dự án. Cách làm:

1. Serialize bằng `JsonUtility` (đủ dùng, không cần thư viện ngoài).
2. Ghi vào `Application.persistentDataPath`.
3. **Ghi nguyên tử:** ghi ra `save.tmp` → `File.Replace(tmp, save, save.bak)`.
   Cách này để lại một bản backup miễn phí và không bao giờ có trạng thái ghi dở.
4. Lưu khi: qua tầng, nâng cấp, và `OnApplicationPause(true)` — **bắt buộc có cái cuối**,
   vì iOS giết app trong nền mà không báo.

### 9.5 Thư viện ngoài

Giữ tối thiểu. Chỉ một thứ đáng thêm:

- **DOTween (bản Free)** — tween cho juice ở mốc M5. Tự viết tween sẽ tốn hàng chục giờ
  để có chất lượng tệ hơn.

Không dùng Addressables (thừa cho dung lượng dự án này), không dùng ECS/DOTS
(vài chục quái trên màn hình không cần đến nó).

### 9.6 Mốc M1 trong Unity trông như thế nào

Để không bị tê liệt lúc bắt đầu, M1 chỉ gồm đúng sáu thứ:

1. Một scene, một sprite nhân vật, cần gạt ảo di chuyển được.
2. Quái đứng yên, có máu.
3. Luật cốt lõi: **đứng yên thì tự đánh kẻ gần nhất, di chuyển thì không**.
4. Thanh chí mạng nạp theo mỗi đòn; đầy thì đòn kế tiếp nhân hệ số.
5. Số sát thương bay lên + màn hình rung nhẹ khi chí mạng.
6. Quái chết thì biến mất.

Không UI nâng cấp, không tầng, không save. Mục tiêu duy nhất là trả lời:
**vòng lặp đứng-yên-để-đánh có vui không?** Nếu không vui, mọi thứ phía sau đều vô nghĩa.
