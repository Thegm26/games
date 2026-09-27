#!/usr/bin/env python3
"""Fixed-canvas mechanical splitter for the ambient source strips.

Only proportional cropping occurs here: no alpha conversion, trim, recolour,
or pivot adjustment is performed. Chroma keying is entirely handled in Unity.
"""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets" / "Art" / "Ambient"
OUTPUT = SOURCE / "Frames"
NAMES = ("Flame", "Steam", "ColdMist", "Fan", "ExitLamp", "Sausage", "Pan")

for name in NAMES:
    image = Image.open(SOURCE / f"{name}.png")
    target = OUTPUT / name
    target.mkdir(parents=True, exist_ok=True)
    for index in range(8):
        left = index * image.width // 8
        right = (index + 1) * image.width // 8
        image.crop((left, 0, right, image.height)).save(target / f"{name}_{index + 1:02}.png")
print(f"Wrote fixed-origin frames to {OUTPUT}")
