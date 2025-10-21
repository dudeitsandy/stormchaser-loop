# 🗺️ Terrain Generation Guide

## What Was Implemented

The game now features **procedurally generated terrain** using Simplex noise with the following features:

### Terrain Types:
- 🟩 **Grass** - Base terrain (light/medium green)
- 🌾 **Fields** - Yellow-green agricultural areas
- 🌲 **Forests** - Dark green areas with scattered trees
- 🛣️ **Roads** - Gray paths connecting areas
- 🛤️ **Highways** - Darker, wider main roads
- 🏢 **Buildings** - Brown structures in urban zones
- 🌳 **Trees** - Individual trees in fields/forests

## How It Works

### Noise Layers:
1. **Biome Layer** - Determines regions (urban, forest, field)
2. **Road Layer** - Creates road networks (horizontal & vertical)
3. **Feature Layer** - Places trees and buildings

### Generation Parameters:
- **World Size**: 1600x900 pixels
- **Tile Size**: 50x50 pixels
- **Scale**: 0.08 (controls "zoom" level)

## Customization Options

### 1. Change Terrain Density
Edit `src/systems/TerrainGenerator.ts`:

```typescript
// Line 48 - More/fewer roads
const isRoadH = Math.abs(roadNoiseX) < 0.15  // Increase for more roads
const isRoadV = Math.abs(roadNoiseY) < 0.15

// Line 57 - More/fewer urban areas
if (biomeNoise > 0.3) {  // Lower number = more urban
  // Buildings
  if (featureNoise > 0.5) {  // Lower number = more buildings
```

### 2. Change Colors
Edit the `color` values in `getTileAt()`:

```typescript
return { x: 0, y: 0, type: 'field', color: 0x9acd32 }  // Change hex color
```

**Suggested Color Palettes:**

**Realistic:**
- Grass: `0x7cb342`
- Fields: `0xcddc39`
- Forest: `0x33691e`
- Roads: `0x757575`
- Buildings: `0x8d6e63`

**Desert Theme:**
- Grass: `0xd4a574`
- Fields: `0xe6c88d`
- Forest: `0xa37c4f`
- Roads: `0x9e9e9e`

**Winter Theme:**
- Grass: `0xe0f2f1`
- Fields: `0xffffff`
- Forest: `0x1b5e20`
- Roads: `0x546e7a`

### 3. Increase World Size
Edit `src/scenes/Game.ts`:

```typescript
// Line 33-34
this.physics.world.setBounds(0,0,3200,1800)  // Larger world
this.cameras.main.setBounds(0,0,3200,1800)

// Line 38
const tiles = this.terrain.generateTerrain(3200, 1800)
```

### 4. Change Tile Size
Edit `src/scenes/Game.ts`:

```typescript
// Line 37 - Smaller tiles = more detail
this.terrain = new TerrainGenerator(Math.random(), 25)  // Try 25, 30, or 40
```

### 5. Use Fixed Seed (Same terrain every time)
Edit `src/scenes/Game.ts`:

```typescript
// Line 37 - Use fixed seed instead of Math.random()
this.terrain = new TerrainGenerator(12345, 50)  // Same seed = same terrain
```

### 6. Add More Biome Types
Edit `src/systems/TerrainGenerator.ts` - Add to the `TerrainTile` interface:

```typescript
export interface TerrainTile {
  x: number
  y: number
  type: 'grass' | 'field' | 'forest' | 'road' | 'highway' | 'building' | 'tree' | 'water' | 'desert'  // Add types
  color: number
}
```

Then add new conditions in `getTileAt()`.

## Advanced Options

### Option A: Add Water Bodies
```typescript
// In getTileAt() method, add before roads:
const waterNoise = this.noise3(tx * scale * 0.5, ty * scale * 0.5)
if (waterNoise < -0.6) {
  return { x: 0, y: 0, type: 'water', color: 0x2196f3 }
}
```

### Option B: City Centers
```typescript
// Calculate distance from center for density
const centerX = tilesX / 2
const centerY = tilesY / 2
const distFromCenter = Math.sqrt(Math.pow(tx - centerX, 2) + Math.pow(ty - centerY, 2))
const normalizedDist = distFromCenter / (tilesX / 2)

// More buildings near center
if (normalizedDist < 0.3 && featureNoise > 0.3) {
  return { x: 0, y: 0, type: 'building', color: 0x8b7355 }
}
```

### Option C: Animate Terrain (Wind in grass)
Add to `src/scenes/Game.ts` update loop:

```typescript
// Make grass tiles wave slightly
this.tweens.add({
  targets: grassTiles,
  alpha: 0.9,
  duration: 2000,
  yoyo: true,
  repeat: -1
})
```

## Performance Tips

1. **Optimize for large worlds**: Generate terrain in chunks, only render visible tiles
2. **Use tilemap**: Convert to Phaser Tilemap for better performance
3. **Bake terrain**: Generate once and save as JSON for reuse
4. **Reduce tile count**: Use larger tiles (100x100) for huge worlds

## Next Steps

- ✅ Working WASD controls
- ✅ Procedural terrain generation
- ⏳ Add collision with buildings
- ⏳ Make tornado prefer roads/open areas
- ⏳ Add weather effects (rain, fog)
- ⏳ Add dynamic shadows
- ⏳ Add minimap

