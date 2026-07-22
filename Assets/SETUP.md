# Top-down 2D setup guide

Unity 6000.5.4f1 · 2D URP · **Input System Package (New)**

> Because this project uses the new Input System, `Input.GetAxis("Horizontal")`
> will throw an error. `PlayerMovement.cs` uses `Keyboard.current` instead.

---

## 1. Import your images correctly

Drop your PNGs into `Assets/Sprites/`. Select them, then in the **Inspector**:

| Setting | Value |
|---|---|
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | Single (or Multiple if it's a spritesheet) |
| Pixels Per Unit | **16** or **32** — must match your tile's pixel size |
| Filter Mode | **Point (no filter)** ← critical for pixel art, else it looks blurry |
| Compression | None |

Click **Apply**. If a tile image is 16×16 px, set Pixels Per Unit to 16 so one
tile is exactly 1 world unit. Everything lines up on the grid after that.

---

## 2. Make the Player

Movement is **grid-based**: the player fills exactly one cell and slides
cell-to-cell, up/down/left/right only. It moves the Transform directly, so it
needs **no Rigidbody2D and no Collider** — walls are detected by looking ahead
at the destination cell.

1. Do **step 4 first** so a Grid exists in the scene.
2. `GameObject > 2D Object > Sprites > Square` (or drag your player PNG in).
3. Rename it **Player**.
4. Add Component → **Player Movement**. Settings:
   - **Grid** — leave empty (it finds the Grid automatically) or drag your Grid in
   - **Time Per Cell** — `0.15` is snappy, `0.3` is slow and deliberate
   - **Hold To Repeat** — on = holds to keep walking, off = one cell per tap
   - **Obstacle Layers** — set this in step 5
5. Set **Sprite Renderer → Order in Layer** to `10` so it draws above tiles.

### Making the player exactly 1 cell big
If your player sprite is the same pixel size as your tiles and uses the same
**Pixels Per Unit**, it's already 1 cell — leave Scale at 1. Otherwise set the
sprite's Pixels Per Unit to its own pixel width (a 24×24 sprite → 24) and it
becomes exactly 1 unit. Only scale the Transform as a last resort.

## 3. Camera follow

1. Select **Main Camera**, Add Component → **Camera Follow**.
2. Drag **Player** from the Hierarchy into the **Target** slot.
3. Camera's **Size** (it's Orthographic) controls zoom — try `6`.

Press **Play**. WASD should move you.

---

## 4. Painting tiles (the easy placement part)

### Create the tilemaps
`GameObject > 2D Object > Tilemap > Rectangular`. This creates a **Grid** with a
**Tilemap** child.

Right-click the **Grid** → `2D Object > Tilemap > Rectangular` again to add a
second tilemap. Rename them and set the **Tilemap Renderer → Order in Layer**:

| Tilemap | Order in Layer | Use for |
|---|---|---|
| `Background` | `0` | grass, floor, water |
| `Ground` | `5` | paths, rugs, decoration |
| `Walls` | `20` | things you collide with |

Lower Order in Layer = drawn behind.

### Open the Tile Palette
`Window > 2D > Tile Palette`. Dock it somewhere visible.

1. Click **Create New Palette** → name it `MainPalette` → save into `Assets/Tiles/Palettes/`.
2. **Drag your tile PNGs from the Project window straight onto the palette grid.**
   Unity auto-generates Tile assets — save them into `Assets/Tiles/`.
3. In the Tile Palette window there's an **Active Tilemap** dropdown at the top.
   Pick which layer you're painting onto (`Background`, `Walls`, …).
4. Pick a tool and paint in the Scene view:

| Key | Tool |
|---|---|
| `B` | Brush — paint one tile |
| `U` | Box fill — drag a rectangle |
| `G` | Picker — eyedropper a tile already in the scene |
| `D` | Eraser |
| `Shift` + click | Erase with the brush |

To add more tiles later, just drag more PNGs onto the palette.

---

## 5. Making walls solid

The player checks the cell it's about to step into, so walls just need a
collider on a layer the script watches.

1. Top-right of the Inspector: **Layer ▾ → Add Layer…**, type `Obstacle` into
   an empty slot.
2. Select the **Walls** tilemap → set its **Layer** to `Obstacle`.
3. Walls tilemap → Add Component → **Tilemap Collider 2D**. That's it — no
   Composite Collider and no Rigidbody needed.
4. Select **GameManager** → on Turn Manager set **Obstacle Layers** to
   `Obstacle`. Walls are configured in this one place; the Player and
   projectiles both read it from here.

Now painting a tile on the Walls tilemap makes that cell impassable, and
erasing it opens the cell back up. Anything else you want solid (a chest, an
NPC) just needs the `Obstacle` layer and any Collider 2D.

> Leave Obstacle Layers empty and the player walks through everything — handy
> while you're just testing movement.

---

## 6. Things that commonly go wrong

- **Player sits between tiles / off-centre** → its sprite pivot isn't Center.
  Select the sprite, set **Pivot = Center**, Apply.
- **Player is bigger/smaller than one tile** → its Pixels Per Unit doesn't
  match its own pixel size. See "Making the player exactly 1 cell big".
- **Player walks through walls** → Obstacle Layers isn't set on the Turn
  Manager, or the Walls tilemap isn't on the `Obstacle` layer, or it has no
  Tilemap Collider 2D.
- **No red tiles / clock stuck** → no GameManager in the scene, or it's
  missing the Turn Manager component.
- **Red tiles hidden under the floor** → raise the Enemy's `Highlight Sorting
  Order` above your tilemaps' Order in Layer.
- **Player won't move at all** → every direction reads as blocked. Check the
  Background tilemap isn't also on the `Obstacle` layer.
- **Movement feels sluggish** → lower Time Per Cell.
- **Tiles look blurry** → Filter Mode isn't Point (no filter).
- **Gaps/lines between tiles** → all tiles must share the same Pixels Per Unit,
  and set `Project Settings > Quality > Anti Aliasing` to Disabled.
- **Player renders behind tiles** → raise its Sprite Renderer Order in Layer.
- **Tiles don't snap to grid** → Pixels Per Unit doesn't match the image size.

---

## 7. The game loop (clock, projectiles, enemies)

### Scene setup

1. `GameObject > Create Empty`, rename it **GameManager**.
2. Add Component → **Turn Manager**, then → **Game HUD**.
3. On Turn Manager set **Obstacle Layers** to `Obstacle` (the same layer as
   your Walls tilemap from step 5). This is now the *only* place walls are
   configured — the Player reads it from here.
4. Select **Player** → Add Component → **Health** (Max Health `5`).
   Player Movement already has the projectile settings on it.

That's the whole game running. Press Play.

### Making an enemy

1. `GameObject > 2D Object > Sprites > Square`, rename it **Enemy**, tint its
   Sprite Renderer red, set **Order in Layer** to `10`.
2. Add Component → **Enemy**. It auto-adds **Health** — set Max Health to `2`.
3. Drag it onto whatever tile you want. It snaps to the grid on Play.
4. To place more, drag it into `Assets/` to make a prefab, then drag copies
   into the scene.

Enemies need no collider — everything talks in grid cells, not physics.

### How a turn plays out

| | |
|---|---|
| **Your turn** | 10s counting down in real time. Move freely with WASD. |
| **Mouse** | Aims. The direction snaps to whichever of the 4 cardinals points closest to the cursor. A white square shows the tile you're aiming at. |
| **Z** | Places a projectile on your tile, flying the way you're aiming. Costs **2 seconds**. |
| **Clock hits 0** | You freeze. Projectiles fly first, then enemies attack. |
| **Then** | Enemies roll a new direction, clock resets to 10s. |

Projectiles resolving **before** enemy attacks is deliberate: killing an enemy
during resolution cancels the attack it had telegraphed. That's the main way
skill shows up in the loop.

Red tiles show exactly which 3 cells each enemy will hit — the tile ahead of it
plus the two diagonals. It picks a random one of the 4 directions each turn, so
read the red squares before the clock runs out.

### Knobs worth turning

On **Turn Manager**:
- `Turn Seconds` — length of a turn (10)
- `Projectile Time Cost` — cost of pressing Z (2)
- `Projectile Step Delay` — how fast projectiles visibly travel during
  resolution. Set to `0` for instant.

On **Player Movement**:
- `Projectile Range` — max tiles travelled (5)
- `Projectile Scatter` — random spread inside the tile so stacked projectiles
  don't overlap. `0` = dead centre
- `Show Aim Preview` — the white square marking the tile you're aiming at
- `Fallback Direction` — only used if there's no mouse/camera
- `Projectile Prefab` — leave empty for a generated yellow square, or drop in
  your own sprite prefab later

On **Enemy**: `Damage`, and Health's `Max Health`.

### Notes on the prototype bits

- The **HUD is drawn with OnGUI** so it needs no Canvas, no fonts, nothing.
  It's ugly on purpose. Replace it with a real UI canvas once the mechanics
  feel good.
- **Red warning tiles and default projectiles generate their own sprite at
  runtime**, so there's no art to make. Assign a Projectile Prefab when you
  have real art.
- There's no game-over yet — at 0 HP the HUD says DEAD and the player keeps
  playing.
