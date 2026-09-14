# -*- coding: utf-8 -*-
"""Đổi bảng màu cho pixel art bằng gradient map theo độ sáng, giữ nguyên alpha."""
import colorsys

def hexc(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i+2], 16) for i in (0, 2, 4))

def lum(r, g, b):
    return (0.299*r + 0.587*g + 0.114*b) / 255.0

def ramp_at(stops, t):
    """stops = [(vị trí 0..1, '#rrggbb')], nội suy tuyến tính"""
    t = max(0.0, min(1.0, t))
    for i in range(len(stops) - 1):
        p0, c0 = stops[i]; p1, c1 = stops[i+1]
        if p0 <= t <= p1:
            f = 0.0 if p1 == p0 else (t - p0) / (p1 - p0)
            a, b_ = hexc(c0), hexc(c1)
            return tuple(int(round(a[k] + (b_[k] - a[k]) * f)) for k in range(3))
    return hexc(stops[-1][1])

def blend(c1, c2, w):
    return tuple(int(round(c1[k]*(1-w) + c2[k]*w)) for k in range(3))

PALETTES = {
    "thap-dem": {
        "ten": "Tháp Đêm",
        "mota": "Lạnh, tối, ngột ngạt. Tháp là nơi nguy hiểm.",
        "stops": [(0.00,"#0b0d1a"),(0.22,"#1d2742"),(0.45,"#3d5480"),
                  (0.68,"#7794bd"),(0.86,"#b9cde6"),(1.00,"#f2f7ff")],
        "giu_hue": 0.28,   # pha lại bao nhiêu màu gốc
        "sat_boost": 1.0,
    },
    "huyet-nguyet": {
        "ten": "Huyết Nguyệt",
        "mota": "Trăng máu — tím thẫm ngả đỏ. Ấm nhưng độc, không hiền như bảng gốc.",
        "stops": [(0.00,"#0d0611"),(0.22,"#2e0f2b"),(0.46,"#6b1838"),
                  (0.68,"#b32f3f"),(0.86,"#e0705f"),(1.00,"#ffd9c2")],
        "giu_hue": 0.10,
        "sat_boost": 1.15,
    },
    "muc-son": {
        "ten": "Mực & Son",
        "mota": "Tranh thuỷ mặc: thế giới gần như đơn sắc, chỉ ĐỎ được giữ màu. "
                "Mắt tự đi tới thứ nguy hiểm — hợp màn hình dọc nhỏ.",
        "stops": [(0.00,"#101216"),(0.25,"#2b3038"),(0.50,"#5c646f"),
                  (0.74,"#9ba4ae"),(0.90,"#d6dbe0"),(1.00,"#f6f4ef")],
        "giu_hue": 0.06,
        "sat_boost": 1.0,
    },
}

def map_color(rgb, cfg):
    r, g, b = rgb
    t = lum(r, g, b)
    base = ramp_at(cfg["stops"], t)

    if cfg.get("giu_do"):
        h, l, s = colorsys.rgb_to_hls(r/255, g/255, b/255)
        deg = h * 360
        la_do = (deg < 22 or deg > 338) and s > 0.35
        if la_do:
            s2 = min(1.0, s * 1.25)
            l2 = max(0.0, min(1.0, l * 0.95))
            rr, gg, bb = colorsys.hls_to_rgb(h, l2, s2)
            return (int(rr*255), int(gg*255), int(bb*255))

    out = blend(base, (r, g, b), cfg["giu_hue"])

    if cfg.get("sat_boost", 1.0) != 1.0:
        h, l, s = colorsys.rgb_to_hls(*[v/255 for v in out])
        s = min(1.0, s * cfg["sat_boost"])
        rr, gg, bb = colorsys.hls_to_rgb(h, l, s)
        out = (int(rr*255), int(gg*255), int(bb*255))

    return out

def build_lut(colors, cfg):
    return {c: map_color(c, cfg) for c in colors}


# ─────────────────────────────────────────────────────────────────────────────
# CÁCH DÙNG — đổi màu hàng loạt cả bộ asset, ghi ra thư mục mới
#
#   python3 tools/doi-bang-mau.py muc-son \
#       art-source/NinjaAdventure \
#       unity/Assets/Art/NinjaAdventure-MucSon
#
# Nhân vật người chơi dùng quy tắc RIÊNG (hero_accent) để không lẫn vào thế giới xám.
# ─────────────────────────────────────────────────────────────────────────────

# ── Năm bảng nền cho năm chương (§5.1) ────────────────────────────────────────
# Chỉ đổi tầng THẾ GIỚI. Quái đỏ và nhân vật lam ngọc GIỮ NGUYÊN suốt 100 tầng —
# người chơi học thứ bậc đọc một lần, dùng mãi.
# Mốc dải: (0.00, 0.25, 0.50, 0.74, 0.90, 1.00) — dùng sai quy ước là sai toàn bộ số kiểm chứng.
CHUONG = {
    "1-nen-da": {
        "ten": "Nền Đá", "tang": "1-20",
        "mota": "Xám lạnh gần trung tính. Chân tháp, phần do con người xây.",
        "stops": [(0.00,"#0c0b0f"),(0.25,"#1f1e24"),(0.50,"#484650"),
                  (0.74,"#63616c"),(0.90,"#78767f"),(1.00,"#8f8d96")],
        "giu_hue": 0.06, "sat_boost": 1.0,
    },
    "2-chieu-giay": {
        "ten": "Chiếu & Giấy Dầu", "tang": "21-40",
        "mota": "Rơm, chiếu tatami, giấy dầu ngả vàng. Cùng độ sáng chương 1 nên "
                "bước chuyển là ẤM LÊN chứ không sáng lên — tầng 20 vừa mở auto-battle (§5.2), "
                "thế giới không nên đổi giọng đột ngột ngay lúc đó.",
        "stops": [(0.00,"#100f08"),(0.25,"#262616"),(0.50,"#4c4c2f"),
                  (0.74,"#6d6d4c"),(0.90,"#84845f"),(1.00,"#9c9b78")],
        "giu_hue": 0.06, "sat_boost": 1.0,
    },
    "3-nang-dong": {
        "ten": "Nắng Đồng", "tang": "41-60",
        "mota": "Mái đã mất, nắng gắt. Bảng SÁNG NHẤT và ỒN NHẤT — chói ngay sau hai chương "
                "trầm là chủ ý. Sắc độ 46 là ép buộc toán học, không phải thẩm mỹ: ở L* 67 "
                "trong dải ấm, mọi màu nhạt hơn đều lọt vào vùng ΔE 45 của nhân vật lam ngọc.",
        "stops": [(0.00,"#1d1f0d"),(0.25,"#4b4e20"),(0.50,"#a3a952"),
                  (0.74,"#c6ca9c"),(0.90,"#cacd9b"),(1.00,"#dee0c1")],
        "giu_hue": 0.05, "sat_boost": 1.0,
    },
    "4-cham-dem": {
        "ten": "Chàm Đêm", "tang": "61-80",
        "mota": "Chàm thẫm — sắc Đông Á rõ nhất của dãy, nhưng đã lạnh và đã trên tầng mây. "
                "Tụt 20 điểm L* so với chương 3: cú sập lớn nhất hành trình.",
        "stops": [(0.00,"#05060f"),(0.25,"#12162f"),(0.50,"#343c6b"),
                  (0.74,"#535a91"),(0.90,"#6f76ab"),(1.00,"#8c92c0")],
        "giu_hue": 0.04, "sat_boost": 1.0,
    },
    "5-suong-lech": {
        "ten": "Sương Lệch", "tang": "81-100",
        "mota": "Tím nhạt, loãng, không thuộc về thế giới này nữa. Nền càng nhạt thì toàn bộ "
                "biên tương phản còn lại càng dồn cho hai tầng đọc cố định — ở 20 tầng khó nhất, "
                "quái đỏ và nhân vật lam là hai thứ duy nhất còn màu thật.",
        "stops": [(0.00,"#2a2133"),(0.25,"#6b5c7a"),(0.50,"#a99bb5"),
                  (0.74,"#cfc6d6"),(0.90,"#e6e0ea"),(1.00,"#f6f3f8")],
        "giu_hue": 0.03, "sat_boost": 1.0,
    },
}
PALETTES.update(CHUONG)

# ─────────────────────────────────────────────────────────────────────────────
# BA TẦNG ĐỌC (xem §5.3 và giả định §3 — màn hình dọc nhỏ)
#   thế giới  -> xám trung tính, không tranh sự chú ý (ĐỔI theo chương)
#   quái      -> ĐỎ, luôn nổi bật, kể cả con vốn màu xanh/lam
#   nhân vật  -> LAM NGỌC, màu duy nhất không ai khác có
#   giao diện -> GIỮ NGUYÊN tông ấm gốc, KHÔNG đổi theo chương (xem docs/GIAO-DIEN.md §1)
#                Người chơi học giao diện một lần rồi dùng suốt 100 tầng.
#
# Không dựa vào màu gốc nữa: quy tắc áp theo THƯ MỤC. Bản đầu chỉ "giữ màu đỏ"
# nên 41/66 con quái bị chìm vào nền xám — lỗi đã sửa ở đây.
# ─────────────────────────────────────────────────────────────────────────────
import zlib as _zlib

def _ramp_tint(rgb, hue, sat, lo=0.14, hi=0.86):
    """Giữ cấu trúc sáng-tối của sprite, ép toàn bộ về một sắc."""
    import colorsys
    r, g, b = rgb
    l = (0.299*r + 0.587*g + 0.114*b) / 255
    rr, gg, bb = colorsys.hls_to_rgb(hue, max(lo, min(hi, l)), sat)
    return (int(rr*255), int(gg*255), int(bb*255))

def hero_tint(rgb):
    return _ramp_tint(rgb, 0.47, 0.62)          # lam ngọc

def threat_tint(rgb, key=""):
    """Đỏ, nhưng lệch sắc nhẹ theo TÊN thư mục để các loài còn phân biệt được.
    Dùng crc32 chứ không dùng hash() — hash() của Python đổi theo mỗi lần chạy.

    TRẦN SÁNG 0,66 — KHÔNG PHẢI 0,86 như bản đầu. QUÁI KHÔNG BAO GIỜ ĐƯỢC NHẠT.
    Con Spirit của chương 5 vẽ gần như toàn pixel TRẮNG (255,255,255); ở trần 0,86
    trắng ra (240,209,197) — kem nhạt, và sàn chương 5 "Sương Lệch" là (221,213,222).
    Đo được ΔE 14,1 với 51% pixel thân CHÌM vào sàn, trong khi bốn con kia đạt
    46-66 và 0% chìm. Đó là 20 tầng CUỐI, và chính tài liệu chương 5 tự viết:
    "nền càng nhạt thì biên tương phản còn lại càng dồn cho hai tầng đọc cố định —
    ở 20 tầng khó nhất, quái đỏ và nhân vật lam là hai thứ duy nhất còn màu thật."
    Hạ trần xuống 0,66 đưa Spirit lên ΔE 41,5 / 0% chìm, và bốn con kia xê dịch
    KHÔNG QUÁ 0,2 — vì chúng vốn gần như không có pixel nào vượt trần đó."""
    off = (_zlib.crc32(key.encode()) % 100) / 100.0      # 0..1
    hue = (0.935 + off * 0.115) % 1.0                    # đỏ thẫm -> đỏ cam
    return _ramp_tint(rgb, hue, 0.60, hi=0.66)

def boss_tint(rgb, key=""):
    off = (_zlib.crc32(key.encode()) % 100) / 100.0
    hue = (0.925 + off * 0.085) % 1.0
    return _ramp_tint(rgb, hue, 0.72, lo=0.10, hi=0.82)  # đậm và gắt hơn quái thường

if __name__ == "__main__":
    import sys, os, glob, shutil, colorsys
    from PIL import Image

    if len(sys.argv) < 4:
        print("dùng: doi-bang-mau.py <ten-bang-mau> <thu-muc-vao> <thu-muc-ra>")
        print("bảng màu có sẵn:", ", ".join(PALETTES))
        sys.exit(1)

    name, src, dst = sys.argv[1], sys.argv[2], sys.argv[3]
    only = None
    if "--only" in sys.argv:
        only = sys.argv[sys.argv.index("--only")+1]   # ví dụ: --only Backgrounds
    cfg = PALETTES[name]

    def rule_for(path):
        """-> (ten_quy_tac, khoa_lech_sac). Khoá là tên thư mục loài."""
        parts = path.replace("\\", "/").split("/")
        if "Ui" in parts: return "ui", ""          # giao diện: giữ nguyên, không đụng
        # CẢ HAI thư mục nhân vật, không chỉ CharacterAnimated.
        #
        # Đây là lỗi đã âm thầm tắt tầng đọc thứ ba suốt từ M3. Bản đầu chỉ liệt kê
        # CharacterAnimated vì lúc đó game lấy nhân vật từ đó. Tới M3 (§5.5b, 5 nhân vật)
        # BuildM1Scene.cs:65 đổi nguồn sang Actor/Character — thư mục DUY NHẤT có đủ
        # 5 người và cùng cỡ ô 16px — nhưng không ai sửa dòng này. Từ đó hero_tint chỉ
        # còn tô cho một thư mục game KHÔNG ĐỌC NỮA, và cả 5 nhân vật rơi vào nhánh
        # "world" ở cuối hàm, tức bị XÁM HOÁ CÙNG VỚI CÁI SÀN HỌ ĐANG ĐỨNG.
        #
        # Đo trên sprite thật ở chương 1 (tầng 1-20): ΔE giữa thân nhân vật và sàn là
        # 12,6 và 70% pixel thân nằm dưới ΔE 25 — tức phần lớn nhân vật CHÌM vào nền.
        # Sau khi sửa: ΔE 49,0 và 0% chìm.
        if "CharacterAnimated" in parts or "Character" in parts: return "hero", ""
        for anchor, rule in (("Monster", "threat"), ("Boss", "boss")):
            if anchor in parts:
                k = parts[parts.index(anchor)+1] if parts.index(anchor)+1 < len(parts) else ""
                return rule, k
        return "world", ""

    root = os.path.join(src, only) if only else src
    everything = [f for f in glob.glob(os.path.join(root, "**", "*"), recursive=True)
                  if os.path.isfile(f)]
    pngs  = [f for f in everything if f.lower().endswith(".png")]
    others = [f for f in everything if not f.lower().endswith(".png")]
    print(f"{len(pngs)} PNG cần đổi màu, {len(others)} file khác chép nguyên")

    # Bộ nhớ đệm TOÀN CỤC: cả bộ chỉ có vài chục màu nên tra cứu gần như miễn phí
    caches, stats = {}, {}
    changed = 0

    for i, f in enumerate(pngs, 1):
        im = Image.open(f).convert("RGBA")
        rule, tint_key = rule_for(f)
        cache = caches.setdefault((rule, tint_key), {})
        stats[rule] = stats.get(rule, 0) + 1

        px = []
        for r, g, b, a in im.getdata():
            if a == 0:
                px.append((0, 0, 0, 0)); continue
            key = (r, g, b)
            v = cache.get(key)
            if v is None:
                if   rule == "ui":     v = key            # nguyên trạng
                elif rule == "hero":   v = hero_tint(key)
                elif rule == "threat": v = threat_tint(key, tint_key)
                elif rule == "boss":   v = boss_tint(key, tint_key)
                else:                  v = map_color(key, cfg)
                cache[key] = v
            px.append((v[0], v[1], v[2], a))

        out = Image.new("RGBA", im.size); out.putdata(px)
        o = os.path.join(dst, os.path.relpath(f, root if only else src))
        os.makedirs(os.path.dirname(o), exist_ok=True)
        out.save(o)
        changed += 1
        if i % 300 == 0: print(f"  PNG {i}/{len(pngs)}")

    # CHÉP NGUYÊN mọi thứ không phải PNG — âm thanh, giấy phép, ghi chú, gif xem trước
    for f in others:
        o = os.path.join(dst, os.path.relpath(f, root if only else src))
        os.makedirs(os.path.dirname(o), exist_ok=True)
        shutil.copy2(f, o)

    print(f"xong: {changed} PNG đổi màu, {len(others)} file chép nguyên")
    for k in ("world", "ui", "threat", "boss", "hero"):
        if k in stats: print(f"  {k:<7}: {stats[k]:>5} file")
    print(f"  -> {dst}")
