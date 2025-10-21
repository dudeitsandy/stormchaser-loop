# 📝 Stormchaser Loop - Development Changelog

## Version 0.1.0 - Beta Release (October 2025)

### Project Setup
- ✅ Created Phaser 3 + TypeScript + Vite project structure
- ✅ Configured build system and dev server
- ✅ Set up file organization (entities, systems, ui, scenes)

---

## Core Features Implemented

### Vehicle Physics System
- ✅ Realistic acceleration-based driving (0→90 MPH in 8-10 seconds)
- ✅ Exponential acceleration curve (harder to accelerate near max speed)
- ✅ Speed-dependent turning (wide turns at high speed, tight at low speed)
- ✅ Smooth continuous rotation (360° rotation, not 8-directional)
- ✅ Drift compensation for keyboard control (85% factor for tight control)
- ✅ Terrain-based speed modifiers (highways 1.5×, mud 0.4×)
- ✅ Terrain-based acceleration penalties (off-road = slow to accelerate)
- ✅ Collision damage system (scales with impact speed 20-90 MPH)
- ✅ MPH-based speed display (real-world units)
- ✅ Vehicle sprite with smooth rotation

### Tornado System
- ✅ Animated spinning vortex with particles
- ✅ Dynamic strength system (EF0-EF5 scale)
- ✅ Cloud formations and debris particles
- ✅ Realistic movement speeds (15-70 MPH based on EF rating)
- ✅ Danger zones (damage when too close)
- ✅ Visual size scaling with strength
- ✅ Strength changes dynamically during session
- ✅ Intelligent movement (prefers roads, avoids water)

### Photography Mechanic
- ✅ Camera aiming system with 45° cone visualization
- ✅ Hold-to-aim mechanic (2 seconds = perfect quality)
- ✅ Color-coded crosshair (red → orange → yellow → green)
- ✅ Photo quality ratings (Poor, Decent, Good, Excellent, Perfect)
- ✅ Photo scoring based on aim duration + distance + tornado strength
- ✅ Pixelated ground-level photo generation
- ✅ Photo gallery display on results screen (top 3 photos)
- ✅ Different sounds for each quality level

### Procedural Terrain Generation
- ✅ Simplex noise-based generation with 4 layers
- ✅ Multiple biomes (highways, roads, fields, forests, water, mud, buildings, trees)
- ✅ Realistic terrain textures (road lines, crop rows, building windows)
- ✅ Collision system (buildings and trees block movement)
- ✅ Speed modifiers per terrain type
- ✅ Water bodies with animated waves
- ✅ 2400×1350 world (optimized from 3200×1800 for performance)
- ✅ 1,024 tiles with 75px size (optimized from 2,304 tiles)

### UI Systems
- ✅ HUD (Time, Score, Combo, Speed in MPH, Health bar)
- ✅ Minimap (150×150, real-time player/tornado tracking)
- ✅ TV-style weather alerts (picture-in-picture warnings)
- ✅ News ticker (scrolling bottom bar)
- ✅ Camera crosshair with progress indicator
- ✅ Danger zone warnings
- ✅ In-game debug panel (toggle with ` key)

### Sound System (ZzFX)
- ✅ Camera snap sound (crisp, satisfying shutter)
- ✅ Photo quality feedback sounds (different tones per quality)
- ✅ Collision sounds (scale with crash severity)
- ✅ Pickup collection sounds (fuel = low pitch, combo = high pitch)
- ✅ UI click sounds (button feedback)
- ✅ Weather alert beep (emergency tone)
- ✅ Victory fanfare (time expires)
- ✅ Game over tone (vehicle destroyed)
- ✅ File size: <1KB (procedurally generated)

### Game Flow
- ✅ Boot scene (procedural asset generation)
- ✅ Title scene (pixel art car chasing tornado)
- ✅ Game scene (90-second sessions)
- ✅ GameOver scene (death screen with options)
- ✅ Results scene (photo gallery + leaderboard)
- ✅ © Ghostweave Games copyright

---

## Performance Optimizations

### Terrain Rendering
- ✅ Reduced tile count from 2,304 to 1,024 (55% reduction)
- ✅ Increased tile size from 50px to 75px
- ✅ Simplified graphics per tile (85% less complex)
- ✅ Removed texture loops and heavy operations

### Memory Leaks Fixed
- ✅ Health bar text reuse (was creating 60 objects/second!)
- ✅ No object creation in update loops
- ✅ Proper cleanup on scene transitions

### World Size
- ✅ Optimized from 3200×1800 to 2400×1350 (better performance)
- ✅ Still large enough for exploration

### Particles
- ✅ Weather particles disabled for performance
- ✅ Reduced tornado debris particle count

---

## Bug Fixes & Improvements

### Scene Transition Issues
- ✅ Fixed missing `endRun()` method (was causing crashes)
- ✅ Added `scene.stop()` before `scene.start()`
- ✅ Player input disabled on game over (prevents SPACE sound)
- ✅ Player physics disabled before transitions
- ✅ Immediate visual overlays (red/green) to prevent black screens
- ✅ Console logging at every transition step
- ✅ Emergency restart failsafe (ENTER key)

### HUD & UI
- ✅ Fixed health bar overlapping text
- ✅ Proper spacing for all UI elements
- ✅ Text strokes for readability
- ✅ Health bar repositioned to line 135

### Weather Alerts
- ✅ Repositioned to left side (doesn't cover minimap)
- ✅ Dismissible with X key
- ✅ Clickable × button
- ✅ Auto-dismiss after 8 seconds
- ✅ Alert sound on appearance

### Controls
- ✅ Added WASD support (was arrow keys only)
- ✅ Improved responsiveness (40% faster turn speed)
- ✅ Better acceleration (33% more responsive)
- ✅ Stronger braking (33% more powerful)

---

## Known Issues (v0.1.0)

### Scene Transitions
- ⚠️ Occasional black screen on game over (working on fix)
- **Workaround:** Press ENTER to restart
- **Debug:** Press ` for debug panel, F12 for console

### Performance
- ⚠️ Performance varies on different hardware (1,024 tiles to render)
- **Best in:** Chrome/Edge browsers
- **Workaround:** Close other tabs, reduce browser zoom to 100%

### Gameplay
- ⚠️ Leaderboard is local only (online leaderboard planned)
- ⚠️ No tutorial (controls shown on title screen)
- ⚠️ No mobile touch controls yet

---

## Technical Specifications

### Physics & Speeds
- Vehicle max speed: 90 MPH
- Vehicle reverse: 25 MPH
- Tornado speeds: EF0 (15 MPH) → EF5 (70 MPH)
- Acceleration: Exponential curve
- Turn rate: 2.5-3.5 rad/s (speed-dependent)

### World
- Dimensions: 2400×1350 pixels
- Tiles: 1,024 (75×75 pixels each)
- Collision objects: ~200 (buildings/trees)

### Session
- Duration: 90 seconds
- Extendable with fuel pickups (+6 seconds each)

### Scoring
- Photo score: (Aim × 0.6 + Distance × 0.4) × Tornado Strength × 100
- Perfect photo of EF5: 400-500+ points
- Typical session score: 500-4,000 points
- Expert session score: 8,000-15,000+ points

---

## Development Timeline

### Initial Setup (Day 1)
- Project structure created
- Basic game loop implemented
- Simple vehicle movement
- Basic tornado sprite

### Core Mechanics (Day 1-2)
- Realistic vehicle physics
- Terrain generation system
- Collision detection
- Photography mechanic
- Photo quality system

### Polish & Features (Day 2-3)
- Sound effects (ZzFX)
- Weather alerts (TV-style)
- Minimap
- Enhanced terrain visuals
- Camera cone visualization

### Bug Fixes & Optimization (Day 3-4)
- Performance optimization (tile reduction)
- Memory leak fixes
- Scene transition debugging
- Control improvements
- UI layout fixes

---

## Credits

**Stormchaser Loop v0.1.0**  
© 2025 Ghostweave Games

**Technology:**
- Phaser 3.70.0 (game engine)
- TypeScript 5.5.4 (language)
- Vite 5.4.0 (build tool)
- simplex-noise (terrain generation)
- zzfx (procedural audio)

**Inspiration:**
- Hotline Miami (tight gameplay loop)
- Pokémon Snap (photography scoring)
- Reed Timmer & storm chasers
- GTA 1/2 (top-down driving)

**Total Development:** ~4 days intensive development

**Lines of Code:** ~3,000+ lines TypeScript

---

## Next Version Plans (v0.2.0)

### High Priority
- [ ] Fix scene transition black screen bug
- [ ] Supabase online leaderboard
- [ ] Achievement system
- [ ] Tutorial screen
- [ ] Professional music

### Medium Priority
- [ ] Mobile touch controls (virtual joystick)
- [ ] Save photo gallery permanently
- [ ] Session stats dashboard
- [ ] Daily challenges

### Low Priority
- [ ] Multiple tornados at once
- [ ] Weather variety (fog, clear, night)
- [ ] L-System city generation
- [ ] Photo sharing
- [ ] Vehicle upgrades

---

## File Structure

```
stormchaser/
├── package.json           # Dependencies
├── vite.config.ts        # Build config (fixed for itch.io)
├── tsconfig.json         # TypeScript config
├── index.html            # Entry point
├── public/
│   └── remote-config.json
└── src/
    ├── main.ts
    ├── types.ts
    ├── entities/
    │   ├── Player.ts      # Vehicle (220 lines)
    │   └── Tornado.ts     # Animated tornado (180 lines)
    ├── ui/
    │   ├── HUD.ts
    │   ├── Minimap.ts
    │   └── WeatherAlert.ts
    ├── systems/
    │   ├── TerrainGenerator.ts
    │   ├── TerrainRenderer.ts
    │   ├── SpeedConverter.ts
    │   ├── CameraSystem.ts
    │   ├── PhotoRenderer.ts
    │   ├── SoundService.ts
    │   ├── DebugPanel.ts
    │   ├── Config.ts
    │   ├── SaveService.ts
    │   ├── NetService.ts
    │   └── LeaderboardService.ts
    └── scenes/
        ├── Boot.ts
        ├── Title.ts
        ├── Game.ts        # Main gameplay (400+ lines)
        ├── GameOver.ts
        └── Results.ts
```

---

## Build Info

**Source Files:** ~30 TypeScript files  
**Total Code:** ~3,000 lines  
**Build Output:** ~1.5MB (Phaser is large)  
**Compressed:** ~350KB

---

*This changelog tracks all major features, fixes, and changes made during development.*

