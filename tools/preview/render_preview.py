"""Render a layout preview of a real Kombi Rush frame.

This is NOT a screenshot of the running game (the editor is unlicensed, so nothing has been
rendered by Unity yet). Entity positions, HUD numbers and camera framing come from an actual
RoadSim frame dumped to JSON; the shapes approximate what SpriteFactory draws in-game. It exists
to check framing and readability on a portrait phone before the first build.
"""
import json
import sys
from PIL import Image, ImageDraw, ImageFont

W, H = 1080, 1920
SS = 2  # supersample factor
FONT_BOLD = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
FONT_REG = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"

P = {
    "tarmac": (58, 58, 62),
    "lane": (226, 226, 214),
    "kerb": (196, 190, 176),
    "kerb_red": (214, 58, 44),
    "dust": (158, 126, 82),
    "dust_dark": (132, 103, 64),
    "grass": (94, 118, 62),
    "body": (244, 244, 238),
    "green": (0, 138, 61),
    "gold": (252, 209, 22),
    "red": (206, 17, 38),
    "glass": (96, 152, 176),
    "tyre": (28, 28, 30),
    "traffic": [(198, 66, 52), (64, 96, 168), (212, 168, 60)],
    "pothole": (24, 22, 24),
    "pothole_rim": (84, 78, 72),
    "orange": (232, 118, 32),
    "white": (240, 238, 232),
    "skin": (232, 196, 148),
    "shirt": (72, 132, 196),
    "coin": (250, 206, 62),
    "coin_edge": (196, 148, 24),
    "fuel": (228, 74, 62),
    "ink": (24, 24, 28),
    "paper": (248, 246, 240),
    "good": (0, 168, 74),
    "bad": (214, 58, 44),
}


def cents(value):
    return "$%d.%02d" % (value // 100, value % 100)


class Cam:
    def __init__(self, frame):
        lanes, lane_w = frame["laneCount"], frame["laneWidth"]
        aspect = W / H
        half_wanted = lanes * lane_w * 0.5 + 3.0
        self.size = min(max(half_wanted / max(0.35, aspect), 9.0), 22.0)
        self.scale = (H * SS * 0.5) / self.size
        self.y = frame["playerY"] + self.size * (1 - 2 * 0.20)
        self.lanes, self.lane_w = lanes, lane_w

    def lane_x(self, lane):
        return (lane - (self.lanes - 1) * 0.5) * self.lane_w

    def px(self, x, y):
        return (W * SS * 0.5 + x * self.scale, H * SS * 0.5 - (y - self.y) * self.scale)

    def m(self, metres):
        return metres * self.scale


def rrect(d, cx, cy, hw, hh, r, fill, outline=None, ow=0):
    box = [cx - hw, cy - hh, cx + hw, cy + hh]
    d.rounded_rectangle(box, radius=r, fill=fill, outline=outline, width=ow)


def draw_kombi(d, cam, x, y):
    w, l = cam.m(1.9), cam.m(4.75)
    cx, cy = cam.px(x, y)
    # shadow
    d.ellipse([cx - w * 0.62, cy - l * 0.5 + 8, cx + w * 0.62, cy + l * 0.55], fill=(0, 0, 0, 60))
    for sx in (-1, 1):
        for sy in (-0.32, 0.32):
            rrect(d, cx + sx * w * 0.54, cy + sy * l, cam.m(0.09), cam.m(0.22), 6, P["tyre"])
    rrect(d, cx, cy, w * 0.5, l * 0.5, 22, P["body"], P["ink"], 5)
    rrect(d, cx, cy - l * 0.36, w * 0.36, l * 0.09, 10, P["glass"])          # windscreen (up = forward)
    rrect(d, cx, cy + l * 0.40, w * 0.32, l * 0.06, 8, (120, 170, 190))      # rear window
    for i in range(3):
        yy = cy - l * 0.1 + i * l * 0.15
        rrect(d, cx - w * 0.40, yy, cam.m(0.10), cam.m(0.13), 5, P["glass"])
        rrect(d, cx + w * 0.40, yy, cam.m(0.10), cam.m(0.13), 5, P["glass"])
    band = cy + l * 0.16
    for i, colour in enumerate((P["green"], P["gold"], P["red"])):
        d.rectangle([cx - w * 0.5, band + i * 6, cx + w * 0.5, band + i * 6 + 6], fill=colour)
    rrect(d, cx, cy - l * 0.47, w * 0.26, 8, 3, P["gold"])
    for sx in (-1, 1):
        rrect(d, cx + sx * w * 0.36, cy - l * 0.47, 9, 6, 3, P["gold"])


def draw_entity(d, cam, kind, lane, span, y, index):
    cx, cy = cam.px(cam.lane_x(lane + (span - 1) * 0.5), y)
    if kind == "Pothole":
        d.ellipse([cx - cam.m(1.0), cy - cam.m(0.68), cx + cam.m(1.0), cy + cam.m(0.68)], fill=P["pothole_rim"])
        d.ellipse([cx - cam.m(0.86), cy - cam.m(0.56), cx + cam.m(0.86), cy + cam.m(0.56)], fill=P["pothole"])
        d.ellipse([cx - cam.m(0.4), cy - cam.m(0.1), cx + cam.m(0.05), cy + cam.m(0.14)], fill=(107, 133, 143))
    elif kind == "Traffic":
        w, l = cam.m(1.78), cam.m(4.35)
        rrect(d, cx, cy, w * 0.5, l * 0.5, 20, P["traffic"][index % 3], P["ink"], 4)
        rrect(d, cx, cy - l * 0.12, w * 0.38, l * 0.16, 12, P["glass"])
        for sx in (-1, 1):
            rrect(d, cx + sx * w * 0.3, cy - l * 0.42, 10, 6, 3, P["bad"])
    elif kind == "Roadblock":
        w = cam.m(span * cam.lane_w - 0.25)
        rrect(d, cx, cy, w * 0.5, cam.m(0.2), 8, P["white"], P["ink"], 4)
        step = cam.m(0.34)
        n = int(w / step)
        for i in range(-n // 2, n // 2 + 1):
            x0 = cx + i * step
            d.line([x0 - cam.m(0.16), cy + cam.m(0.19), x0 + cam.m(0.16), cy - cam.m(0.19)],
                   fill=P["orange"], width=int(cam.m(0.16)))
        rrect(d, cx, cy, w * 0.5, cam.m(0.2), 8, None, P["ink"], 4)
        for sx in (-1, 1):
            rrect(d, cx + sx * (w * 0.5 - cam.m(0.2)), cy + cam.m(0.34), 6, cam.m(0.18), 3, P["ink"])
    elif kind == "Passenger":
        r = cam.m(0.2)
        d.line([cx + cam.m(0.16), cy, cx + cam.m(0.3), cy - cam.m(0.42)], fill=P["skin"], width=int(cam.m(0.1)))
        rrect(d, cx, cy + cam.m(0.05), cam.m(0.22), cam.m(0.32), 10, P["shirt"])
        d.ellipse([cx - r, cy - cam.m(0.5) - r, cx + r, cy - cam.m(0.5) + r], fill=P["skin"])
        d.ellipse([cx - r, cy - cam.m(0.62) - r * 0.7, cx + r, cy - cam.m(0.5)], fill=(41, 33, 31))
    elif kind == "Coin":
        r = cam.m(0.36)
        d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=P["coin_edge"])
        d.ellipse([cx - r * 0.82, cy - r * 0.82, cx + r * 0.82, cy + r * 0.82], fill=P["coin"])
        d.line([cx, cy - r * 0.5, cx, cy + r * 0.5], fill=P["coin_edge"], width=6)
    elif kind == "FuelCan":
        rrect(d, cx, cy, cam.m(0.26), cam.m(0.34), 8, P["fuel"], P["ink"], 4)
        rrect(d, cx - cam.m(0.05), cy - cam.m(0.42), cam.m(0.13), cam.m(0.07), 4, P["ink"])
        d.rectangle([cx - cam.m(0.19), cy - 5, cx + cam.m(0.19), cy + 5], fill=P["white"])
    elif kind == "Stop":
        w = cam.m(span * cam.lane_w - 0.4)
        d.rectangle([cx - w * 0.5, cy - cam.m(0.55), cx + w * 0.5, cy + cam.m(0.55)], fill=P["gold"])
        rrect(d, cx + w * 0.5 + cam.m(1.4), cy, cam.m(0.7), cam.m(0.5), 10, P["green"], P["ink"], 4)


def render(frame_path, out_path):
    frame = json.load(open(frame_path))
    cam = Cam(frame)
    img = Image.new("RGB", (W * SS, H * SS), P["dust_dark"])
    d = ImageDraw.Draw(img, "RGBA")

    road_half = cam.lanes * cam.lane_w * 0.5
    left, _ = cam.px(-road_half, 0)
    right, _ = cam.px(road_half, 0)

    d.rectangle([0, 0, left, H * SS], fill=P["dust"])
    d.rectangle([right, 0, W * SS, H * SS], fill=P["dust"])
    d.rectangle([left, 0, right, H * SS], fill=P["tarmac"])

    # verge scenery
    for i in range(-2, 40):
        y = (int((cam.y - 12) / 9.0)) * 9.0 + i * 9.0
        side = -1 if int(round(y / 9.0)) % 2 == 0 else 1
        bx, by = cam.px(side * (road_half + 1.6), y)
        for dx, dy, rr, col in ((-14, -10, 44, P["grass"]), (18, 6, 34, (108, 132, 70)), (0, 20, 28, (126, 148, 84))):
            d.ellipse([bx + dx - rr, by + dy - rr * 0.8, bx + dx + rr, by + dy + rr * 0.8], fill=col)

    # kerbs
    for i in range(-2, 44):
        y = (int((cam.y - 12) / 3.0)) * 3.0 + i * 3.0
        colour = P["kerb"] if int(round(y / 3.0)) % 2 == 0 else P["kerb_red"]
        for sign in (-1, 1):
            kx, ky = cam.px(sign * (road_half + 0.28), y)
            hw, hh = cam.m(0.17), cam.m(0.75)
            d.rectangle([kx - hw, ky - hh, kx + hw, ky + hh], fill=colour)

    # edge lines and lane dashes
    for sign in (-1, 1):
        ex, _ = cam.px(sign * (road_half - 0.18), 0)
        d.rectangle([ex - cam.m(0.07), 0, ex + cam.m(0.07), H * SS], fill=P["lane"])
    for lane in range(1, cam.lanes):
        x = (lane - cam.lanes * 0.5) * cam.lane_w
        for i in range(-2, 40):
            y = (int((cam.y - 12) / 4.2)) * 4.2 + i * 4.2
            dx, dy = cam.px(x, y)
            rrect(d, dx, dy, cam.m(0.08), cam.m(0.4), 6, P["lane"])

    entities = sorted(frame["entities"], key=lambda e: e["y"], reverse=True)
    for i, e in enumerate(entities):
        draw_entity(d, cam, e["kind"], e["lane"], e["span"], e["y"], i)

    draw_kombi(d, cam, cam.lane_x(frame["laneF"]), frame["playerY"])

    img = img.resize((W, H), Image.LANCZOS)
    d = ImageDraw.Draw(img, "RGBA")
    hud(d, frame)
    img.save(out_path)
    print("wrote", out_path)


def hud(d, frame):
    big = ImageFont.truetype(FONT_BOLD, 52)
    mid = ImageFont.truetype(FONT_BOLD, 44)
    small = ImageFont.truetype(FONT_BOLD, 34)
    tiny = ImageFont.truetype(FONT_REG, 30)

    def chip(box, radius=26, fill=(18, 20, 24, 220)):
        d.rounded_rectangle(box, radius=radius, fill=fill)

    chip([26, 26, 356, 122])
    d.text((52, 60), cents(frame["cash"] + frame["banked"]), font=big, fill=P["gold"])
    chip([724, 26, 1054, 122])
    dist = frame["playerY"]
    text = "%d m" % int(dist) if dist < 1000 else "%.2f km" % (dist / 1000)
    d.text((1030, 62), text, font=mid, fill=P["paper"], anchor="ra")

    chip([26, 136, 356, 176], radius=18, fill=(0, 0, 0, 140))
    frac = max(0.0, min(1.0, frame["fuel"] / frame["fuelCap"]))
    colour = P["bad"] if frac < 0.2 else (P["gold"] if frac < 0.45 else P["good"])
    d.rounded_rectangle([32, 142, 32 + int(318 * frac), 170], radius=14, fill=colour)
    d.text((44, 148), "FUEL", font=tiny, fill=(255, 255, 255, 220))

    for i in range(frame["hullMax"]):
        x = 30 + i * 44
        fill = P["bad"] if i < frame["hull"] else (255, 255, 255, 46)
        d.rounded_rectangle([x, 190, x + 34, 224], radius=10, fill=fill)

    chip([814, 136, 1054, 200], radius=20)
    d.text((934, 168), "%d/%d" % (frame["riders"], frame["seats"]), font=mid, fill=P["paper"], anchor="mm")

    if frame["combo"] > 1.01:
        d.text((540, 190), "x%.2f" % frame["combo"], font=big, fill=P["gold"], anchor="mm")

    d.text((540, 1730), "Tap a side or slide to steer", font=tiny, fill=(255, 255, 255, 210), anchor="mm")


if __name__ == "__main__":
    render(sys.argv[1], sys.argv[2])
