# -*- coding: utf-8 -*-
"""Bộ dựng mockup giao diện từ asset thật của Ninja Adventure."""
from PIL import Image, ImageDraw, ImageFont
import os

ART = "/Users/trantri/development/tower-rpg/art-source/NinjaAdventure"
UI  = f"{ART}/Ui"

# ── HỆ THIẾT KẾ ───────────────────────────────────────────────────────────────
W, H      = 1080, 1920     # độ phân giải thiết kế, màn hình dọc
SCALE     = 4              # pixel 16px -> 64px, bội số NGUYÊN bắt buộc
UNIT      = 32             # đơn vị khoảng cách; mọi lề/khe là bội của 32
EDGE      = 64             # lề an toàn từ mép màn hình (2 đơn vị)
TOUCH     = 144            # vùng chạm tối thiểu (Android 48dp ~132px, làm tròn lên)

# Tầng UI: ẤM, GIỮ NGUYÊN suốt 100 tầng — không theo bảng màu chương
INK       = (28, 24, 20)
PAPER     = (232, 224, 206)
WOOD      = (168, 112, 56)
GOLD      = (232, 178, 74)
JADE      = (72, 206, 178)   # trùng màu nhân vật
BLOOD     = (204, 62, 66)    # trùng màu quái

def load(p):
    f = os.path.join(UI, p)
    return Image.open(f).convert("RGBA") if os.path.exists(f) else None

def nine(img, w, h, b=5):
    """Kéo giãn 9-patch: giữ nguyên bốn góc, lặp cạnh và ruột."""
    iw, ih = img.size
    out = Image.new("RGBA", (w, h))
    parts = {
        "tl": img.crop((0,0,b,b)),            "tr": img.crop((iw-b,0,iw,b)),
        "bl": img.crop((0,ih-b,b,ih)),        "br": img.crop((iw-b,ih-b,iw,ih)),
        "t":  img.crop((b,0,iw-b,b)),         "b":  img.crop((b,ih-b,iw-b,ih)),
        "l":  img.crop((0,b,b,ih-b)),         "r":  img.crop((iw-b,b,iw,ih-b)),
        "c":  img.crop((b,b,iw-b,ih-b)),
    }
    out.paste(parts["c"].resize((max(1,w-2*b), max(1,h-2*b)), Image.NEAREST), (b,b))
    out.paste(parts["t"].resize((max(1,w-2*b), b), Image.NEAREST), (b,0))
    out.paste(parts["b"].resize((max(1,w-2*b), b), Image.NEAREST), (b,h-b))
    out.paste(parts["l"].resize((b, max(1,h-2*b)), Image.NEAREST), (0,b))
    out.paste(parts["r"].resize((b, max(1,h-2*b)), Image.NEAREST), (w-b,b))
    for k,(x,y) in (("tl",(0,0)),("tr",(w-b,0)),("bl",(0,h-b)),("br",(w-b,h-b))):
        out.paste(parts[k], (x,y))
    return out

def panel(w, h, variant="nine_path_panel.png", border=5):
    """Khung gỗ co giãn đúng tỉ lệ pixel: dựng ở kích thước gốc rồi phóng nguyên."""
    src = load(f"Theme/Theme Wood/{variant}")
    if src is None: 
        im = Image.new("RGBA",(w,h),(60,42,26,235)); return im
    sw, sh = max(16, w//SCALE), max(16, h//SCALE)
    return nine(src, sw, sh, border).resize((sw*SCALE, sh*SCALE), Image.NEAREST)

def icon(path, size):
    im = load(path)
    if im is None: return Image.new("RGBA",(size,size),(90,70,50,255))
    s = max(1, size // max(im.width, im.height))
    return im.resize((im.width*s, im.height*s), Image.NEAREST)

def font(sz, bold=True):
    p = "/System/Library/Fonts/Supplemental/Arial Bold.ttf" if bold else "/System/Library/Fonts/Supplemental/Arial.ttf"
    return ImageFont.truetype(p, sz)

def text(d, xy, s, sz=34, col=PAPER, bold=True, shadow=True, anchor=None):
    f = font(sz, bold)
    if shadow: d.text((xy[0]+3, xy[1]+3), s, font=f, fill=(0,0,0,190), anchor=anchor)
    d.text(xy, s, font=f, fill=col, anchor=anchor)
