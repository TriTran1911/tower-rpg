# -*- coding: utf-8 -*-
"""Mockup 2: màn hình nâng cấp trang bị (M2/M3)."""
import sys
sys.path.insert(0, "/private/tmp/claude-501/-Users-trantri-development/c86edaff-d480-45c5-8203-f3c12cd5d4a9/scratchpad/ui")
from kit import *
from PIL import Image, ImageDraw

im = Image.new("RGBA", (W, H), (34, 30, 26, 255))
d = ImageDraw.Draw(im)

# ── ĐẦU MÀN: hai tài nguyên, phân cấp rõ ──────────────────────────────────────
im.alpha_composite(panel(W-2*EDGE, 190), (EDGE, EDGE))
text(d, (EDGE+40, EDGE+34), "TRANG BỊ", 40, GOLD)

# Mảnh — vô hạn, trình bày bình thường
text(d, (EDGE+40, EDGE+98), "Mảnh", 24, (196,180,156))
text(d, (EDGE+40, EDGE+126), "12.480", 32, PAPER)

# Lõi — HỮU HẠN, đây là trái tim thiết kế nên phải nổi nhất
lx = W - EDGE - 420
d.rectangle([lx-20, EDGE+30, W-EDGE-28, EDGE+158], fill=(72,48,22,255), outline=GOLD, width=3)
text(d, (lx, EDGE+46), "LÕI CÒN LẠI", 24, GOLD)
text(d, (lx, EDGE+80), "18", 56, PAPER)
text(d, (lx+96, EDGE+108), "/ 30 cả game", 24, (200,176,132))

SLOTS = [
    ("Vũ khí", "Skill Icon/Spell/Cut.png",              "Sát thương",    38, 40, 3, 0.62),
    ("Giáp",   "Skill Icon/Items & Weapon/Armor.png",   "Máu + hút máu", 36, 40, 3, 0.30),
    ("Găng",   "Skill Icon/Job & Action/Punch.png",     "Tốc độ đánh",   22, 30, 2, 0.85),
    ("Nhẫn",   "Skill Icon/Items & Weapon/Ring.png",    "Hệ số chí mạng", 9, 10, 0, 0.45),
]
GATE_COST = [1, 2, 3, 4, 5]

y = EDGE + 190 + UNIT
CARD_H = 300
for name, ipath, stat, lvl, cap, gates, prog in SLOTS:
    im.alpha_composite(panel(W-2*EDGE, CARD_H, "nine_path_panel_2.png"), (EDGE, y))
    cx0 = EDGE + 36

    # ô icon
    cell = panel(160, 160, "inventory_cell.png", 4)
    im.alpha_composite(cell, (cx0, y+36))
    ic = icon(ipath, 112)
    im.alpha_composite(ic, (cx0+(160-ic.width)//2, y+36+(160-ic.height)//2))

    tx = cx0 + 160 + UNIT
    text(d, (tx, y+34), name, 40, PAPER)
    text(d, (tx, y+82), stat, 24, (196,180,156))

    # cấp hiện tại / trần
    text(d, (tx, y+124), f"Cấp {lvl}", 34, GOLD)
    text(d, (tx+150, y+132), f"trần {cap}", 24, (170,152,128))

    # thanh tiến tới cấp kế
    bw = W - tx - EDGE - 260
    by = y + 172
    d.rectangle([tx, by, tx+bw, by+30], fill=(52,38,24,255))
    d.rectangle([tx, by, tx+int(bw*prog), by+30], fill=JADE if lvl < cap else (110,96,80))
    text(d, (tx, by+42), f"{int(prog*100)}% tới cấp {lvl+1}" if lvl < cap
                          else "CHẠM TRẦN — cần đột phá", 24,
         (196,180,156) if lvl < cap else BLOOD)

    # năm cổng đột phá — khan hiếm phải NHÌN THẤY được, đặt TRÊN nút
    gw, gh, ggap = 30, 30, 10
    gtotal = 5*gw + 4*ggap
    gx = W - EDGE - 36 - gtotal
    text(d, (gx, y+30), "ĐỘT PHÁ", 20, (196,180,156))
    for g in range(5):
        x0 = gx + g*(gw+ggap)
        col = GOLD if g < gates else (66,50,36)
        d.rectangle([x0, y+58, x0+gw, y+58+gh], fill=col, outline=(26,20,16), width=2)
        if g >= gates:
            text(d, (x0+gw//2, y+62), str(GATE_COST[g]), 19, (176,158,132), anchor="mt", shadow=False)

    # nút gỗ tự vẽ — panel 16x8 không đủ ruột để kéo 9-patch
    btn_w, btn_h = gtotal, TOUCH
    bxp, byp = gx, y+CARD_H-btn_h-30
    can = lvl < cap
    face   = (192,124,56) if can else (78,64,50)
    shade  = (128,76,30)  if can else (58,46,36)
    d.rectangle([bxp, byp, bxp+btn_w, byp+btn_h], fill=shade)
    d.rectangle([bxp, byp, bxp+btn_w, byp+btn_h-10], fill=face)
    d.rectangle([bxp, byp, bxp+btn_w, byp+btn_h], outline=(26,20,16), width=4)
    text(d, (bxp+btn_w//2, byp+44), "NÂNG" if can else "ĐỘT PHÁ", 30,
         (250,242,226) if can else (156,138,114), anchor="mm")
    text(d, (bxp+btn_w//2, byp+92), "820 Mảnh" if can else f"{GATE_COST[gates]} Lõi", 24,
         (252,236,200) if can else GOLD, anchor="mm")

    y += CARD_H + UNIT

# ── nhắc nhở đánh đổi, đáy màn ────────────────────────────────────────────────
text(d, (W//2, H-EDGE-46), "Mỗi Lõi tiêu đi là một cánh cửa đóng lại", 26,
     (186,168,142), anchor="mm")

im.resize((W//2, H//2), Image.LANCZOS).save(
    "/private/tmp/claude-501/-Users-trantri-development/c86edaff-d480-45c5-8203-f3c12cd5d4a9/scratchpad/ui/mock-upgrade.png")
print("  mock-upgrade.png xong")
