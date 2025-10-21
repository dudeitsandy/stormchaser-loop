# 🌪️ Stormchaser Loop

A 2D tornado chasing game built with Phaser 3, TypeScript, and Vite. Drive your storm chasing vehicle through procedurally generated towns, capture photos of tornadoes, and survive the storm!

## 🎮 Game Features

### Core Gameplay
- **Realistic Car Physics:** Acceleration, braking, terrain-based speed modifiers
- **Tornado Chasing:** Dynamic tornado with realistic movement and wind effects
- **Photo System:** Aim and capture tornado photos for points and leaderboard ranking
- **Weather Alerts:** TV-style breaking news system with ticker updates
- **Strategic Positioning:** Large open areas for tactical tornado chasing

### Procedural Town Generation
- **Connected Road Networks:** Highways, secondary roads, and local streets
- **Proper Road Orientation:** Horizontal/vertical roads with correct dividing lines
- **Realistic Zoning:** Residential, commercial, industrial, and rural districts
- **Building Variety:** Houses, barns, silos, offices, gas stations with 3D depth
- **Strategic Open Spaces:** Large fields for tornado positioning

### Visual & Audio
- **8-bit Art Style:** Pixel-perfect graphics with depth and shadows
- **Sound Effects:** ZzFX procedural audio for immersive experience
- **Weather Effects:** Dynamic clouds, wind, and atmospheric effects
- **Minimap:** Real-time world overview
- **UI/HUD:** Health, speed, score, and warning displays

## 🛠️ Technical Stack

- **Engine:** Phaser 3
- **Language:** TypeScript
- **Build Tool:** Vite
- **Physics:** Arcade Physics System
- **Audio:** ZzFX (procedural sound generation)
- **Terrain:** Simplex Noise for procedural generation

## 🚀 Getting Started

### Prerequisites
- Node.js 16+ and npm
- Modern web browser

### Installation
```bash
# Clone the repository
git clone <your-github-repo-url>
cd stormchaser

# Install dependencies
npm install

# Start development server
npm run dev

# Build for production
npm run build
```

### Development Commands
```bash
npm run dev      # Start development server (http://localhost:5173)
npm run build    # Build for production
npm run preview  # Preview production build
```

## 🎯 Controls

- **WASD** or **Arrow Keys:** Drive vehicle
- **SPACE:** Aim camera (hold and release to take photo)
- **X:** Dismiss weather alerts
- **F5:** Emergency restart (if game freezes)

## 🏗️ Project Structure

```
src/
├── scenes/           # Game scenes (Boot, Title, Game, Results, GameOver)
├── entities/         # Game entities (Player, Tornado)
├── systems/          # Game systems (Camera, Terrain, Sound, etc.)
├── ui/              # User interface components
├── types.ts         # TypeScript type definitions
└── main.ts          # Game entry point

public/
├── remote-config.json  # Game configuration
└── index.html         # HTML template
```

## 🎮 Game Systems

### Terrain Generation
- **L-System Road Networks:** Organic road layouts with proper intersections
- **District-Based Zoning:** Realistic town planning principles
- **Building Placement:** Proximity-based building distribution
- **Strategic Open Areas:** Large fields for tornado chasing gameplay

### Camera System
- **Cone-Based Aiming:** Realistic camera field of view
- **Distance-Based Scoring:** Closer shots get better ratings
- **Quality System:** Poor, Decent, Good, Excellent, Perfect ratings
- **Photo Gallery:** Best 3 photos displayed on results screen

### Physics System
- **Realistic Vehicle Physics:** Acceleration curves, terrain modifiers
- **Wind Effects:** Tornado pulls vehicle toward it
- **Collision Damage:** Speed-based damage system
- **Terrain Interaction:** Different surfaces affect speed and handling

## 🚀 Deployment

### Itch.io
1. Run `npm run build`
2. Copy `remote-config.json` to `dist/` folder
3. Zip contents of `dist/` folder
4. Upload to itch.io as HTML5 game

### Build Output
- **Size:** ~1.5MB (uncompressed)
- **Dependencies:** Self-contained, no external CDN
- **Compatibility:** All modern browsers

## 🔮 Future Development

This 2D version serves as a proof-of-concept for a potential 3D version using **PlayCanvas**. The core game systems and mechanics can be adapted to 3D:

### Planned 3D Features
- **3D Tornado Models:** Realistic tornado meshes and animations
- **3D Vehicle:** Detailed car model with physics
- **Volumetric Clouds:** Dynamic weather systems
- **Enhanced Graphics:** Modern 3D rendering pipeline
- **VR Support:** Immersive storm chasing experience

### Migration Strategy
- **Core Systems:** Game logic and scoring systems remain the same
- **Asset Pipeline:** Convert 2D sprites to 3D models
- **Physics:** Upgrade from 2D Arcade to 3D physics engine
- **Rendering:** Replace Phaser with PlayCanvas rendering

## 📊 Performance

- **Target FPS:** 60 FPS on modern hardware
- **Memory Usage:** <100MB typical
- **Load Time:** <3 seconds on fast connections
- **Mobile Ready:** Responsive design for various screen sizes

## 🐛 Known Issues & Fixes

### Recently Fixed
- ✅ Black screen after collisions
- ✅ Shift+Arrow key conflicts  
- ✅ Camera scoring always "poor"
- ✅ Ticker text overlap
- ✅ Missing buildings in town
- ✅ Road orientation issues

### Performance Optimizations
- ✅ Reduced terrain complexity for better FPS
- ✅ Optimized rendering pipeline
- ✅ Memory leak fixes in HUD system
- ✅ Efficient collision detection

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **Phaser 3** for the excellent 2D game framework
- **ZzFX** for procedural sound generation
- **Simplex Noise** for terrain generation algorithms
- **Vite** for the fast development experience

---

**Ready to chase some tornadoes?** 🌪️📸

*Built with ❤️ by Ghostweave Games*