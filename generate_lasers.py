import os
import numpy as np
from PIL import Image, ImageDraw

# Créer les répertoires laser
laser_dir = '/workspace/Prog/dotnet/Programs/MonoGames/Shooter/Content/pics/lasers'
os.makedirs(laser_dir, exist_ok=True)

# --- 1. player_laser ---
img = Image.new('RGBA', (8, 32), (0, 0, 0, 0))
draw = ImageDraw.Draw(img)
# Barre lumineuse bleu-cyan
for y in range(32):
    alpha = int(255 * (1 - y/32))
    draw.rectangle([3, y, 4, y], fill=(100, 220, 255, alpha))
# Point lumineux au sommet
draw.rectangle([2, 0, 5, 6], fill=(180, 240, 255, 255))
img.save(os.path.join(laser_dir, 'player_laser.png'), 'PNG')

# --- 2. enemy_laser ---
img = Image.new('RGBA', (12, 20), (0, 0, 0, 0))
draw = ImageDraw.Draw(img)
# Laser rouge orangé
for y in range(20):
    r = int(200 + 55 * (y/20))
    g = int(60 + 20 * (y/20))
    b = int(40 + 20 * (y/20))
    alpha = int(200 * (1 - y/20))
    draw.rectangle([4, y, 7, y], fill=(r, g, b, alpha))
draw.rectangle([3, 0, 8, 8], fill=(255, 120, 60, 255))
img.save(os.path.join(laser_dir, 'enemy_laser.png'), 'PNG')

# --- 3. turret_laser ---
img = Image.new('RGBA', (16, 28), (0, 0, 0, 0))
draw = ImageDraw.Draw(img)
# Laser vert
for y in range(28):
    g = int(200 + 55 * (y/28))
    r = int(30 + 20 * (y/28))
    alpha = int(220 * (1 - y/28))
    draw.rectangle([5, y, 10, y], fill=(r, g, r, alpha))
draw.rectangle([4, 0, 11, 8], fill=(80, 255, 80, 255))
img.save(os.path.join(laser_dir, 'turret_laser.png'), 'PNG')

print("Laser assets générés:")
for f in os.listdir(laser_dir):
    size = os.path.getsize(os.path.join(laser_dir, f))
    print(f"  {f}: {size} bytes")
