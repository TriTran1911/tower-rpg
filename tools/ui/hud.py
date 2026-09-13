# -*- coding: utf-8 -*-
"""Mockup 1: HUD chiến đấu."""
import sys, math, glob
sys.path.insert(0, "/private/tmp/claude-501/-Users-trantri-development/c86edaff-d480-45c5-8203-f3c12cd5d4a9/scratchpad/ui")
from kit import *
from PIL import Image, ImageDraw

GAME = "/Users/trantri/development/tower-rpg/unity/Assets/Art"

def scene():
    """Nền game thật: sàn chương 1 + quái đỏ + nhân vật lam."""
    im = Image.new("RGBA", (W, H))
    fl = Image.open(f"{GAME}/Chapters/1-nen-da/Tilesets/TilesetFloor.png").convert("RGBA").crop((16,16,32,32))
    t = fl.resize((16*SCALE, 16*SCALE), Image.NEAREST)
    for y in range(0, H, 16*SCALE):
        for x in range(0, W, 16*SCALE): im.paste(t, (x, y))

    def mon(n):
        fs = [f for f in glob.glob(f"{GAME}/NinjaAdventure-MucSon/Actor/Monster/{n}/*.png") if "Faceset" not in f]
        return Image.open(fs[0]).convert("RGBA").crop((0,0,16,16)) if fs else None

    cx, cy = W//2, int(H*0.46)
    for i, n in enumerate(["Skull","Bear","Flam","Mushroom","Slime","Snake"]):
        m = mon(n)
        if not m: continue
        a = i*2*math.pi/6 - math.pi/2
        b = m.resize((16*SCALE, 16*SCALE), Image.NEAREST)
        im.alpha_composite(b, (int(cx+math.cos(a)*300)-b.width//2, int(cy+math.sin(a)*300)-b.height//2))

    p = Image.open(f"{GAME}/NinjaAdventure-MucSon/Actor/CharacterAnimated/NinjaGreen/Separate/Idle.png") \
             .convert("RGBA").crop((0,0,32,32))
    p = p.crop(p.getbbox())
    pb = p.resize((p.width*SCALE, p.height*SCALE), Image.NEAREST)
    im.alpha_composite(pb, (cx-pb.width//2, cy-pb.height//2))
    return im, (cx, cy)

im, (cx, cy) = scene()
d = ImageDraw.Draw(im)

# ── HÀNG TRÊN: tầng · máu ─────────────────────────────────────────────────────
# khung tầng, góc trái
tp = panel(240, 128)
im.alpha_composite(tp, (EDGE, EDGE))
text(d, (EDGE+120, EDGE+34), "TẦNG", 26, GOLD, anchor="mt")
text(d, (EDGE+120, EDGE+66), "07", 52, PAPER, anchor="mt")

# thanh máu, chiếm phần còn lại
BX, BY = EDGE+240+UNIT, EDGE
BW, BH = W-BX-EDGE, 128
im.alpha_composite(panel(BW, BH), (BX, BY))
inner_x, inner_y = BX+28, BY+40
inner_w, inner_h = BW-56, 48
d.rectangle([inner_x, inner_y, inner_x+inner_w, inner_y+inner_h], fill=(58,40,26,255))
fill = int(inner_w*0.72)
d.rectangle([inner_x, inner_y, inner_x+fill, inner_y+inner_h], fill=BLOOD)
d.rectangle([inner_x, inner_y, inner_x+fill, inner_y+12], fill=(230,110,110,255))
text(d, (inner_x+inner_w//2, inner_y+inner_h//2), "86 / 120", 30, PAPER, anchor="mm")
hi = icon("Receptacle/IconHeart.png", 56)
im.alpha_composite(hi, (BX+20, BY+16))

# ── THANH CHÍ MẠNG dưới chân nhân vật ─────────────────────────────────────────
mw, mh = 200, 26
mx, my = cx-mw//2, cy+80
d.rectangle([mx-4, my-4, mx+mw+4, my+mh+4], fill=(20,16,12,230))
d.rectangle([mx, my, mx+mw, my+mh], fill=(44,38,32,255))
f2 = int(mw*0.8)
d.rectangle([mx, my, mx+f2, my+mh], fill=GOLD)
for i in range(1, 5):                      # 5 vạch = 5 đòn, đếm được bằng mắt
    x = mx + int(mw*i/5)
    d.line([x, my, x, my+mh], fill=(20,16,12,255), width=3)

# ── CẦN GẠT ĐỘNG, góc phải dưới (ngón cái tay phải) ───────────────────────────
# Vẽ trên lớp riêng rồi alpha_composite — ImageDraw GHI ĐÈ alpha chứ không hoà trộn.
JR, JX, JY = 150, W-EDGE-170, H-EDGE-320
lay = Image.new("RGBA", (W, H), (0,0,0,0))
ld = ImageDraw.Draw(lay)
ld.ellipse([JX-JR, JY-JR, JX+JR, JY+JR], fill=(236,226,206,26), outline=(236,226,206,70), width=5)
ld.ellipse([JX+46-70, JY-34-70, JX+46+70, JY-34+70], fill=(236,226,206,86), outline=(30,24,18,120), width=6)
im.alpha_composite(lay)
text(d, (JX, JY+JR+52), "cần gạt hiện ra nơi ngón chạm", 24, (236,226,206), anchor="mm")

# gợi ý vùng ngón cái với được — nửa dưới màn hình
hint = Image.new("RGBA", (W, H), (0,0,0,0))
hd = ImageDraw.Draw(hint)
hd.arc([W-EDGE-940, H-EDGE-940, W-EDGE+80, H-EDGE+80], start=180, end=300, fill=(236,226,206,40), width=4)
im.alpha_composite(hint)

im.resize((W//2, H//2), Image.LANCZOS).save("/private/tmp/claude-501/-Users-trantri-development/c86edaff-d480-45c5-8203-f3c12cd5d4a9/scratchpad/ui/mock-hud.png")
print("  mock-hud.png xong")
