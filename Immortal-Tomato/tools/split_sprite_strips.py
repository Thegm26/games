#!/usr/bin/env python3
"""Mechanically split the supplied eight-frame strips into normalized canvases.

Each source cell is alpha-cropped, then placed at the bottom centre of a common
canvas.  No pixels are redrawn, recoloured, or scaled.
"""
from pathlib import Path
from PIL import Image
from collections import deque

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets" / "Art"
DESTINATION = ROOT / "Assets" / "Resources" / "Frames"
STRIPS = {
    "Idle": "Idle.png", "Run": "Running.png", "Jump": "Jump.png",
    "Shoot": "Shooting.png", "Hit": "HitNsplat.png", "Regen": "Regeneration.png",
}
FRAME_COUNT = 8
CANVAS = (420, 724)
ALPHA_THRESHOLD = 128
SEAM_SEARCH_RADIUS = 100
MIN_COMPONENT_PIXELS = 180

def dynamic_seams(image):
    """Find the quietest vertical alpha valleys near the eight frame divisions.

    AI sprite strips often vary their cell widths.  A conventional 1/8 cut can
    slice a muzzle or boot in half, whereas a local transparent/low-content
    valley keeps every pose intact.  We retain the original pixels after the
    boundary is selected; thresholding is only used to locate the seam.
    """
    alpha = image.getchannel("A")
    counts = []
    for x in range(image.width):
        column = alpha.crop((x, 0, x + 1, image.height))
        counts.append(sum(1 for value in column.getdata() if value >= ALPHA_THRESHOLD))
    seams = [0]
    for frame in range(1, FRAME_COUNT):
        target = round(frame * image.width / FRAME_COUNT)
        left = max(seams[-1] + 1, target - SEAM_SEARCH_RADIUS)
        right = min(image.width - 1, target + SEAM_SEARCH_RADIUS)
        # Tie-break toward the intended regular position, avoiding arbitrary
        # gaps far from the frame's visual centre.
        seam = min(range(left, right + 1), key=lambda x: (counts[x], abs(x - target)))
        seams.append(seam + 1)
    seams.append(image.width)
    return seams

def discard_tiny_detached_fragments(image, keep_only_main=False, discard_edge_debris=False):
    """Drop cut-off debris left by a neighbouring pose at a dynamic seam.

    Complete characters remain large connected alpha groups. Tiny detached
    barrel/boot slivers are artefacts caused by source frames overlapping their
    neighbours, rather than actual animation elements.
    """
    alpha = image.getchannel("A")
    width, height = image.size
    pixels = alpha.load()
    seen = bytearray(width * height)
    components = []
    for y in range(height):
        for x in range(width):
            start = y * width + x
            if seen[start] or pixels[x, y] < ALPHA_THRESHOLD:
                continue
            seen[start] = 1
            queue = deque([(x, y)])
            component = []
            while queue:
                cx, cy = queue.popleft()
                component.append((cx, cy))
                for ny in range(max(0, cy - 1), min(height, cy + 2)):
                    for nx in range(max(0, cx - 1), min(width, cx + 2)):
                        key = ny * width + nx
                        if not seen[key] and pixels[nx, ny] >= ALPHA_THRESHOLD:
                            seen[key] = 1
                            queue.append((nx, ny))
            components.append(component)
    largest = max((len(component) for component in components), default=0)
    discarded = []
    for component in components:
        touches_seam = any(x == 0 or x == width - 1 for x, _ in component)
        if (len(component) < MIN_COMPONENT_PIXELS or
            (keep_only_main and len(component) != largest) or
            (discard_edge_debris and len(component) != largest and touches_seam)):
            discarded.extend(component)
    if discarded:
        cleaned = image.copy()
        cleaned_alpha = cleaned.getchannel("A")
        for x, y in discarded:
            cleaned_alpha.putpixel((x, y), 0)
        cleaned.putalpha(cleaned_alpha)
        return cleaned
    return image

def split(name, filename):
    image = Image.open(SOURCE / filename).convert("RGBA")
    out = DESTINATION / name
    out.mkdir(parents=True, exist_ok=True)
    seams = dynamic_seams(image)
    for index in range(FRAME_COUNT):
        left, right = seams[index], seams[index + 1]
        # The running strip has overlapping neighbouring poses; retain only its
        # primary connected character group to prevent a previous frame's gun
        # barrel or boot entering the next frame. Other strips deliberately use
        # detached pieces during splat/regeneration and keep those intact.
        cell = discard_tiny_detached_fragments(
            image.crop((left, 0, right, image.height)),
            keep_only_main=name == "Run",
            discard_edge_debris=name in {"Idle", "Jump", "Shoot"},
        )
        bounds = cell.getchannel("A").getbbox()
        if bounds is None:
            raise RuntimeError(f"{filename} frame {index + 1} has no visible pixels")
        cropped = cell.crop(bounds)
        canvas = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
        x = (CANVAS[0] - cropped.width) // 2
        y = CANVAS[1] - cropped.height
        canvas.alpha_composite(cropped, (x, y))
        canvas.save(out / f"{name}_{index + 1:02}.png")

if __name__ == "__main__":
    for animation, source in STRIPS.items():
        split(animation, source)
    print(f"Wrote normalized frames to {DESTINATION}")
