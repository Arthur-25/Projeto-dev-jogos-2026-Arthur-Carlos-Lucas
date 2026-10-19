# Ghosts - Pixel Art Pack

Complete ghost animation pack created in pure pixel art by **Alenia Studios**.  
Includes 42 color variants, each with a full movement spritesheet and 3 independent attack projectiles — all 4-directional.

---

## Folder Structure

```
Ghosts-pixelart/
  spritesheets/
    <variant>/
      ghost_<variant>.png            — Base ghost: idle + walk animation, 4 directions
      orb_projectile_<variant>.png   — Attack 1: Spectral Orb projectile, 4 directions
      claw_projectile_<variant>.png  — Attack 2: Ghost Claw slashes, 4 directions
      pillar_projectile_<variant>.png — Attack 3: Demonic Pillar (area), 4 directions
      preview/
        ghost_<variant>.gif
        orb_projectile_<variant>.gif
        claw_projectile_<variant>.gif
        pillar_projectile_<variant>.gif
  catalogo_ghosts.png                — Full variant catalog at a glance
  LICENSE
  README.md
```

All spritesheets are **64x64px per frame**, laid out as a grid:
- **Rows** → direction (Down, Left, Right, Up)
- **Columns** → animation frames (8 frames per direction)

Final spritesheet dimensions: **512 x 256 px**

---

## Attacks

| File prefix | Attack | Type | Description |
|---|---|---|---|
| `orb_projectile_` | Spectral Orb | Projectile | A glowing ectoplasm orb that flies forward, dripping slime |
| `claw_projectile_` | Ghost Claw | Melee burst | Three crescent-shaped energy slashes that arc outward |
| `pillar_projectile_` | Demonic Pillar | Area / AoE | A pentagram summons a towering ectoplasm column that leaves a puddle |

> All attacks are **fully separated from the ghost body** so developers can place and animate them independently in any engine.

---

## Variants (42 total)

| Key | Description |
|---|---|
| `void_base` | Blank white base (recolorable) |
| `pure_fire` | Flame orange tones |
| `celestial` | Soft sky blue |
| `shadow` | Dark, near-black |
| `metallic_gold` | Gold metallic sheen |
| `chrome` | Chrome / silver metallic |
| `holo_iridescent` | Iridescent holographic |
| `variant_1–35` | 35 hue variants (normal, dark, pastel) across the full color wheel |

---

## Suggested Uses

- **Platformer / Action:** Patrol enemies, wall-phasing spirits, dungeon ghosts
- **RPG / Metroidvania:** Graveyard specters, haunted mansion bosses, NPC souls
- **Bullet Hell / Survivor:** Horde enemies, projectile-spamming casters

---

## Contact & More Resources

- **Catalog (itch.io):** [https://alenia-studios.itch.io](https://alenia-studios.itch.io)
- **Contact:** contact.aleniastudios@gmail.com
