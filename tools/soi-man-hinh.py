# -*- coding: utf-8 -*-
"""Soi ảnh chụp game, báo lỗi hiển thị ĐO ĐƯỢC — thay cho việc ngồi chơi để tìm.

    python3 tools/soi-man-hinh.py [thư-mục]      (mặc định: soi-man-hinh/)

Đọc cặp file mà `SoiManHinh.cs` xuất ra — một PNG và một JSON kê mọi phần tử giao
diện đang hiện — rồi kiểm bốn thứ:

  1. CHỮ TRÀN KHUNG (ngang) hoặc BỊ CẮT (dọc). Nhãn cho xuống dòng không tràn
     ngang bao giờ — nó xuống thêm dòng rồi bị cắt cụt. Đã cắn ba lần: banner sự kiện,
     nhãn Mảnh trong bảng trang bị, nhãn nút TRANG BỊ.
  2. TƯƠNG PHẢN. Lấy màu chữ và màu nền THẬT trong vùng đó trên ảnh, tính theo WCAG.
     Ngưỡng 4,5:1 chữ thường, 3:1 chữ lớn (>= 24 đơn vị THIẾT KẾ, không phải px ảnh).
     Bỏ qua chữ nằm sau tấm phủ: màu danh nghĩa của nó không còn là màu vẽ ra.
  3. LỌT RA NGOÀI MÀN HÌNH.
  4. HAI VÙNG CHẠM CHỒNG NHAU — cú chạm sẽ rơi vào cái nằm trên, không phải cái người
     chơi nhắm.

KHÔNG kiểm được: câu chữ có dễ hiểu không, game có vui không. Hai thứ đó cần người.
"""
import json, os, sys, glob
from collections import Counter

try:
    from PIL import Image
except ImportError:
    sys.exit("cần Pillow:  pip install pillow")


def sang(c):
    def f(v):
        v /= 255.0
        return v / 12.92 if v <= 0.03928 else ((v + 0.055) / 1.055) ** 2.4
    return 0.2126 * f(c[0]) + 0.7152 * f(c[1]) + 0.0722 * f(c[2])


def tuong_phan(a, b):
    la, lb = sang(a), sang(b)
    hi, lo = max(la, lb), min(la, lb)
    return (hi + 0.05) / (lo + 0.05)


def nen_trong_vung(im, r, mau_chu):
    """Màu nền hay gặp nhất trong khung, bỏ qua những pixel gần màu chữ."""
    x, y, w, h = r
    W, H = im.size
    # JSON dùng gốc trái-DƯỚI (Unity); PIL dùng trái-TRÊN.
    # DẢI GIỮA 60%: khung nhãn phủ cả viền 9-patch tối của nút, mà viền không phải
    # cái nền chữ thật sự nằm trên. Lấy cả khung thì mode rơi vào viền và báo sai.
    mx, my = w * 0.20, h * 0.20
    x, y, w, h = x + mx, y + my, w - 2 * mx, h - 2 * my
    x0, x1 = max(0, int(x)), min(W, int(x + w))
    y0, y1 = max(0, int(H - y - h)), min(H, int(H - y))
    if x1 - x0 < 2 or y1 - y0 < 2:
        return None
    c = Counter()
    for px in range(x0, x1, max(1, (x1 - x0) // 40)):
        for py in range(y0, y1, max(1, (y1 - y0) // 20)):
            p = im.getpixel((px, py))
            if tuong_phan(p, mau_chu) < 1.6:      # gần như chính là chữ
                continue
            c[p] += 1
    return c.most_common(1)[0][0] if c else None


def chong(a, b):
    ax, ay, aw, ah = a
    bx, by, bw, bh = b
    ox = min(ax + aw, bx + bw) - max(ax, bx)
    oy = min(ay + ah, by + bh) - max(ay, by)
    return (ox, oy) if ox > 1 and oy > 1 else None


def soi(js_path):
    d = json.load(open(js_path, encoding="utf-8"))
    png = js_path[:-5] + ".png"
    if not os.path.exists(png):
        return [("thiếu ảnh", os.path.basename(png))]
    im = Image.open(png).convert("RGB")
    W, H = d["canvas"]
    loi = []

    for t in d["chu"]:
        x, y, w, h = t["rect"]
        chu = t["chu"][:46]

        # 1. tràn khung — so trong CÙNG hệ toạ độ canvas, không phải pixel ảnh.
        #    Chữ nhiều dòng thì bề rộng "cần" là của cả câu gộp lại, không so được.
        if not t["nhieuDong"] and t["canRong"] > t["khungRong"] + 1.5:
            loi.append(("chữ tràn khung",
                        f'"{chu}" cần {t["canRong"]:.0f}, khung {t["khungRong"]:.0f}'
                        f'{"" if t["tuCo"] else "  (chưa bật tự co)"}'))

        # 1b. bị cắt theo CHIỀU DỌC. Nhãn cho xuống dòng không tràn ngang — nó xuống
        #     thêm dòng rồi bị cắt cụt, và soi mỗi bề ngang thì không bao giờ thấy.
        #     Chỉ tính là lỗi khi chữ THẬT SỰ bị xén — có Mask ở trên hoặc chế độ tràn
        #     là Truncate/Ellipsis. TMP mặc định vẽ tràn ra ngoài khung và vẫn hiện đủ.
        if t.get("xenThat") and t.get("canCao", 0) > t.get("khungCao", 1e9) + 1.5:
            loi.append(("chữ bị cắt theo chiều dọc",
                        f'"{chu}" cần cao {t["canCao"]:.0f}, khung cao {t["khungCao"]:.0f}'
                        f'{"" if t["tuCo"] else "  (chưa bật tự co)"}'))

        # 3. lọt ra ngoài
        if x < -1 or y < -1 or x + w > W + 1 or y + h > H + 1:
            loi.append(("lọt ra ngoài màn hình",
                        f'"{chu}" ở [{x:.0f},{y:.0f},{w:.0f},{h:.0f}] / màn {W}x{H}'))
            continue

        # 2. tương phản — bỏ qua chữ nằm SAU một tấm phủ: màu danh nghĩa của nó không
        #    còn là màu vẽ ra, nên so với nền thật là so nhầm hai hệ.
        if t.get("biChe"):
            continue
        mau = tuple(round(v * 255) for v in t["mau"])
        nen = nen_trong_vung(im, t["rect"], mau)
        if nen is None:
            continue
        tp = tuong_phan(mau, nen)
        # Ngưỡng theo cỡ chữ NGƯỜI CHƠI NHÌN (đơn vị thiết kế = pixel thật trên máy
        # 1080 rộng), không theo cỡ trên ảnh chụp thu nhỏ.
        nguong = 3.0 if t.get("coChuThietKe", t["coChuAnh"]) >= 24 else 4.5
        if tp < nguong:
            loi.append(("tương phản thấp",
                        f'"{chu}" {tp:.2f}:1 (cần {nguong}:1) — chữ {mau} trên nền {nen}'))

    # 4. nút bấm được nhưng cú chạm KHÔNG tới nơi
    # Màn hình che toàn bộ ĐANG MỞ thì nó chặn nút nền là CÓ CHỦ Ý — không phải lỗi.
    dang_mo = d.get("manHinhMo", "")
    if not dang_mo:
        for n in d["nut"]:
            if not n.get("bamDuoc"):
                continue
            trung = n.get("chamTrung", "")
            if trung and trung != n["ten"]:
                loi.append(("cú chạm không tới nút",
                            f'{n["ten"]} bấm được nhưng chạm vào giữa nó lại trúng "{trung}"'))
    return loi


def main():
    thu_muc = sys.argv[1] if len(sys.argv) > 1 else "soi-man-hinh"
    files = sorted(glob.glob(os.path.join(thu_muc, "*.json")))
    if not files:
        sys.exit(f"không thấy bản kê nào trong {thu_muc}/ — chạy test SoiManHinh trước")

    tong = 0
    for f in files:
        ten = os.path.basename(f)[:-5]
        loi = soi(f)
        tong += len(loi)
        dau = "✗" if loi else "✓"
        print(f"{dau} {ten}" + (f"   ({len(loi)} lỗi)" if loi else ""))
        for kieu, chi_tiet in loi:
            print(f"      {kieu}: {chi_tiet}")

    print(f"\n{len(files)} cảnh · {tong} lỗi hiển thị đo được")
    print("Không kiểm được bằng máy: câu chữ có dễ hiểu không, game có vui không.")
    sys.exit(1 if tong else 0)


if __name__ == "__main__":
    main()
