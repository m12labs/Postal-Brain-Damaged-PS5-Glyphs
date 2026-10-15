"""Draws the PS5 symbols stamped into the game's own button art (glyphs/<Name>.png, 70x70, black on transparent).

The plugin erases the Xbox label inside each button and centres one of these over where the label was,
so only these overlays are shipped, never the game's art. Run: python -I scripts/make_symbols.py
"""
import math, os, random
from PIL import Image, ImageDraw, ImageFont

SIZE, SS = 70, 8                      # output size, supersampling factor
OUT = os.path.join(os.path.dirname(__file__), '..', 'assets', 'glyphs')
FONT = r'C:\Windows\Fonts\courbd.ttf'  # Courier New Bold: closest stock face to the game's typewriter labels
INK = (0, 0, 0, 255)
rng = random.Random(1997)


def canvas():
    im = Image.new('RGBA', (SIZE * SS, SIZE * SS), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im)


def finish(im, name):
    im = im.resize((SIZE, SIZE), Image.LANCZOS)
    im.save(os.path.join(OUT, name + '.png'))


def wobble(points, amount=0.6):
    """Nudges vertices a little so strokes look hand-inked like the game's art."""
    return [(x + rng.uniform(-amount, amount) * SS, y + rng.uniform(-amount, amount) * SS) for x, y in points]


def stroke(d, pts, width, closed=False):
    pts = [(x * SS, y * SS) for x, y in pts]
    pts = wobble(pts)
    if closed: pts = pts + pts[:1]
    w = int(width * SS)
    d.line(pts, fill=INK, width=w, joint='curve')
    for x, y in pts:  # round caps and joins
        d.ellipse((x - w / 2, y - w / 2, x + w / 2, y + w / 2), fill=INK)


def cross():
    im, d = canvas(); r = 11
    stroke(d, [(35 - r, 35 - r), (35 + r, 35 + r)], 6)
    stroke(d, [(35 + r, 35 - r), (35 - r, 35 + r)], 6)
    finish(im, 'Cross')


def circle():
    im, d = canvas(); r = 12.5 * SS; w = 5.5 * SS; c = 35 * SS
    d.ellipse((c - r - w / 2, c - r - w / 2, c + r + w / 2, c + r + w / 2), fill=INK)
    d.ellipse((c - r + w / 2, c - r + w / 2, c + r - w / 2, c + r - w / 2), fill=(0, 0, 0, 0))
    finish(im, 'Circle')


def square():
    im, d = canvas(); r = 11
    stroke(d, [(35 - r, 35 - r), (35 + r, 35 - r), (35 + r, 35 + r), (35 - r, 35 + r)], 5.5, closed=True)
    finish(im, 'Square')


def triangle():
    im, d = canvas(); r = 16
    pts = [(35 + r * math.sin(a), 38 - r * math.cos(a)) for a in (0, 2 * math.pi / 3, 4 * math.pi / 3)]
    stroke(d, pts, 5.5, closed=True)
    finish(im, 'Triangle')


def label(text):
    """L1/R1/L2/R2 in a typewriter slab serif, sized like the game's LB/LT labels."""
    im, d = canvas()
    font = ImageFont.truetype(FONT, 30 * SS)
    box = d.textbbox((0, 0), text, font=font)
    w, h = box[2] - box[0], box[3] - box[1]
    d.text((35 * SS - w / 2 - box[0], 35 * SS - h / 2 - box[1]), text, font=font, fill=INK,
           stroke_width=int(0.6 * SS), stroke_fill=INK)
    finish(im, text)


def create():
    """PS5 Create: three separate dashes bursting upward (spaced apart so it never reads as an arrow)."""
    im, d = canvas()
    for ang in (-50, 0, 50):
        a = math.radians(ang)
        x0, y0 = 35 + 9 * math.sin(a), 46 - 9 * math.cos(a)
        x1, y1 = 35 + 20 * math.sin(a), 46 - 20 * math.cos(a)
        stroke(d, [(x0, y0), (x1, y1)], 5)
    finish(im, 'Create')


if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    cross(); circle(); square(); triangle(); create()
    for t in ('L1', 'R1', 'L2', 'R2'): label(t)
    print('symbols written to', os.path.abspath(OUT))
