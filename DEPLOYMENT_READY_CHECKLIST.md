# 🚀 Deployment Ready Checklist

## Stormchaser Loop - Beta Testing Preparation

---

## ✅ Pre-Deployment Testing

### **Test 1: Crash Death (5x)**
**Action:** Drive into buildings at full speed  
**Expected:** Immediate GameOver screen  
**Status:** [ ] PASS / [ ] FAIL  

### **Test 2: Tornado Death (3x)**
**Action:** Drive into tornado danger zone until health = 0  
**Expected:** Immediate GameOver screen  
**Status:** [ ] PASS / [ ] FAIL  

### **Test 3: Time Expiration (3x)**
**Action:** Play full 90-second session  
**Expected:** Results screen with photos  
**Status:** [ ] PASS / [ ] FAIL  

### **Test 4: Photography (10 photos)**
**Action:** Take 10 photos (vary quality)  
**Expected:** All photos show in results  
**Status:** [ ] PASS / [ ] FAIL  

### **Test 5: Sound Effects**
**Check:**  
- [ ] Camera snap works  
- [ ] Collision sounds work  
- [ ] Pickup sounds work  
- [ ] UI clicks work  
- [ ] Alert beep works  

### **Test 6: Emergency Features**
**Check:**  
- [ ] ` key shows debug panel  
- [ ] X key dismisses alerts  
- [ ] ENTER restarts if stuck  
- [ ] Console logging works (F12)  

### **Test 7: Performance**
**Action:** Play for 5 minutes straight  
**Expected:** Stable 50-60 FPS  
**Status:** [ ] PASS / [ ] FAIL  

### **Test 8: Multiple Sessions**
**Action:** Play 10 sessions in a row  
**Expected:** No slowdown, no memory leaks  
**Status:** [ ] PASS / [ ] FAIL  

---

## 🏗️ Build & Deploy

### **Step 1: Build Production Version**
```bash
npm run build
```
**Expected:** dist/ folder created  
**Status:** [ ] COMPLETE  

### **Step 2: Test Production Build**
```bash
npm run preview
```
**Opens:** http://localhost:4173  
**Test:** Play 3 full sessions  
**Status:** [ ] PASS / [ ] FAIL  

### **Step 3: Prepare for itch.io**
1. Navigate to `dist/` folder
2. Select ALL files inside dist/
3. Create ZIP archive: `stormchaser-loop-v0.1.0.zip`
4. **Do NOT include the dist folder itself, just the contents!**

**Status:** [ ] COMPLETE  

### **Step 4: Upload to itch.io**
1. Go to itch.io → Dashboard → Create New Project
2. **Title:** Stormchaser Loop
3. **Type:** HTML
4. **Upload:** ZIP file
5. **Settings:**
   - Viewport dimensions: 800 x 450
   - Embed options: "Embed in page"
   - Fullscreen button: ✓ Enabled
   - Mobile friendly: ✓ Yes (for future)
6. **Classification:** Game
7. **Genre:** Action, Simulation
8. **Tags:** tornado, photography, driving, roguelike, short-session
9. **Pricing:** Free (or set price)
10. **Visibility:** Draft → Public

**Status:** [ ] COMPLETE  

---

## 📝 Description for itch.io

```markdown
# Stormchaser Loop

A skill-based tornado photography simulator. Chase tornados, take perfect shots, survive!

## How to Play
- Drive your chase vehicle with WASD/Arrows
- Hold SPACE for 2 seconds to aim camera
- Release SPACE to snap the perfect photo
- Closer to strong tornados = higher scores!
- Survive 90 seconds and build your photo portfolio

## Features
- Realistic vehicle physics (90 MPH max)
- Dynamic tornados (EF0-EF5, 15-70 MPH)
- Procedural terrain with collision
- Photo quality system (Poor → Perfect)
- Pixelated ground-level tornado photos
- TV-style weather alerts
- Leaderboard competition

## Controls
- WASD / Arrow Keys: Drive
- SPACE: Aim camera (hold), snap photo (release)
- X: Dismiss weather alerts
- ESC: Pause
- ` (backtick): Debug panel

## Tips
- Highways are faster than off-road!
- Aim for 2 seconds for perfect photos
- Don't get too close to the tornado!
- Collect fuel pickups to extend time

## Credits
© 2025 Ghostweave Games
Built with Phaser 3, TypeScript, Vite

**Beta Version 0.1.0** - Your feedback appreciated!
Report bugs at: [your email/discord]
```

---

## 🎨 Screenshots Needed

Before publishing, capture:

1. **Title screen** - Shows pixel art
2. **Gameplay** - Chasing tornado with camera cone
3. **Weather alert** - TV-style warning visible
4. **Photo moment** - Taking a shot
5. **Results screen** - Photo gallery displayed
6. **GameOver screen** - Death screen

**Tools:** Windows Snipping Tool or Game Bar (Win+G)

---

## 📊 Known Issues (Include in Description)

```
Known Issues (v0.1.0):
- Performance varies on older computers (1024 tiles to render)
- Weather particles disabled for performance
- Leaderboard is local only (online leaderboard coming soon)
- Mobile touch controls not yet implemented
- Sound is procedural (ZzFX) - professional audio coming later

Workarounds:
- If black screen: Press ENTER to restart
- If slow: Use Chrome/Edge browser, close other tabs
- For debug info: Press ` (backtick key)
```

---

## 🎯 Beta Testing Goals

### **What to Ask Testers:**

**1. Technical:**
- Any black screens? (when? doing what?)
- Performance issues? (FPS, lag?)
- Any crashes?
- Console errors? (F12)

**2. Gameplay:**
- Is driving fun?
- Is photography mechanic clear?
- Is it too hard? Too easy?
- Did they want to replay?

**3. Polish:**
- Are sounds satisfying?
- Is UI clear?
- Did they understand goals?
- Any confusing moments?

**4. Retention:**
- How many sessions did they play?
- What was their high score?
- Would they recommend it?

---

## 🔧 Quick Fixes Based on Feedback

### **If "Too Slow Performance"**
**Quick fix:** Reduce world size in `Game.ts`:
```typescript
// Change from 2400×1350 to 1600×900
this.physics.world.setBounds(0,0,1600,900)
```

### **If "Can't Find Tornado"**
**Quick fix:** Add arrow pointing to tornado (future)

### **If "Too Hard"**
**Quick fix:** Increase session time in `remote-config.json`:
```json
{ "sessionSeconds": 120 }
```

### **If "Too Easy"**
**Quick fix:** Increase tornado danger radius

### **If "Black Screen Still Happens"**
**Quick fix:** Share console errors with me for immediate fix

---

## 🎮 **Current Status**

### **Completed Features:**
- ✅ Full game loop (90-second sessions)
- ✅ Vehicle physics (realistic driving)
- ✅ Tornado system (animated, dynamic)
- ✅ Photography mechanic (aim + timing)
- ✅ Photo generation (pixelated ground views)
- ✅ Procedural terrain (roads, buildings, water)
- ✅ Sound effects (ZzFX, 10+ sounds)
- ✅ UI systems (HUD, minimap, alerts)
- ✅ Scene flow (Title → Game → GameOver/Results)
- ✅ Debug tools (panel, logging)

### **Ready For:**
- ✅ Local testing
- ✅ Beta deployment
- ✅ Feedback gathering
- ⏳ Full launch (needs online leaderboard)

### **Not Yet Implemented:**
- ⏳ Online leaderboard (Supabase)
- ⏳ Achievements
- ⏳ Mobile touch controls
- ⏳ Professional audio (music)
- ⏳ Tutorial screen
- ⏳ Daily challenges

---

## 📈 Version Roadmap

### **v0.1.0 (Current Beta)**
- Core gameplay complete
- Local testing
- Gather feedback

### **v0.2.0 (Next)**
- Supabase leaderboard
- Achievement system
- Better sound/music
- Tutorial

### **v0.3.0 (Future)**
- Mobile controls
- Daily challenges
- Photo sharing
- Polish based on feedback

### **v1.0 (Full Launch)**
- All features polished
- Professional audio
- Marketing materials
- Press kit

---

## 🎯 Success Criteria for Beta

**Minimum viable metrics:**
- [ ] 80%+ testers complete first session
- [ ] 50%+ testers play 3+ sessions
- [ ] <10% report black screen issues
- [ ] Average 3.5/5 fun rating
- [ ] Clear improvement suggestions

**If you hit these:** Ready for wider release!

---

## 🚀 **Next Steps**

**1. Test locally** (30 minutes)
- Run through checklist above
- Verify all transitions work
- No black screens

**2. Build for production** (5 minutes)
```bash
npm run build
npm run preview
# Test the preview build
```

**3. Deploy to itch.io** (15 minutes)
- Create project page
- Upload ZIP
- Write description
- Take screenshots
- Set to Public or Restricted (for beta)

**4. Share with 5-10 testers** (1-2 days)
- Friends, Discord, Reddit gamedev
- Gather feedback
- Fix critical issues

**5. Iterate** (ongoing)
- Fix bugs reported
- Add requested features
- Improve based on feedback

---

## 📊 **File Sizes**

**Source:** ~30 files, ~3,000 lines of TypeScript  
**Built:** ~1.5MB (Phaser is large)  
**Compressed:** ~350KB (gzipped)  

**itch.io limits:** 1GB (you're well under!)

---

## ✅ **Pre-Flight Checklist**

Before deploying to testers:

- [ ] Black screen issue resolved
- [ ] All sounds working
- [ ] Photo gallery displays correctly
- [ ] Leaderboard shows scores (local)
- [ ] No console errors
- [ ] Production build works (`npm run build`)
- [ ] Preview build tested (`npm run preview`)
- [ ] Screenshots taken (6+ images)
- [ ] Description written
- [ ] Known issues documented
- [ ] Support contact provided

**When all checked:** DEPLOY! 🚀

---

## 🎯 **Final Test Right Now**

**Quick 3-minute test:**

1. Open: http://localhost:5173
2. Play 3 full sessions
3. Crash into things
4. Take photos
5. Let time expire

**If all 3 sessions work perfectly:** 

**🎉 YOU'RE READY TO DEPLOY! 🎉**

---

The `scene.stop()` fix should have solved the black screen issue completely!

**Test it now and let me know if you're ready to launch!** 🚀

