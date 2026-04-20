# 🚀 Deployment Guide - Stormchaser Loop

## Quick Deploy to itch.io (15 minutes)

---

## Step 1: Build Production Version

Run these commands in PowerShell:

```powershell
# Build the game
npm run build

# Test the build locally first
npm run preview
```

This creates a `dist/` folder with optimized files.

**Open:** http://localhost:4173 and test the preview build!

---

## Step 2: Prepare ZIP File

### **IMPORTANT:** Zip the CONTENTS of dist/, not the folder itself!

**Windows PowerShell:**
```powershell
# Navigate to dist folder
cd dist

# Create ZIP of contents
Compress-Archive -Path * -DestinationPath ../stormchaser-loop-v0.1.0.zip -Force

# Go back to project root
cd ..
```

**Or manually:**
1. Open `dist/` folder
2. Select ALL files inside (index.html, assets/, etc.)
3. Right-click → Send to → Compressed (zipped) folder
4. Name it: `stormchaser-loop-v0.1.0.zip`

**File should be ~350-500KB**

---

## Step 3: Upload to itch.io

### **A. Create Account**
1. Go to https://itch.io
2. Sign up / Log in
3. Go to Dashboard

### **B. Create New Project**
1. Click "Create new project"
2. Fill in details:

**Title:** `Stormchaser Loop`

**Project URL:** `your-username.itch.io/stormchaser-loop`

**Short description:**
```
Chase tornados and take perfect photos in this 90-second skill-based simulator!
```

**Classification:** Game

**Kind of project:** HTML

### **C. Upload Files**
1. **Uploads** section
2. Click "Upload files"
3. Select `stormchaser-loop-v0.1.0.zip`
4. Check "This file will be played in the browser"

### **D. Embed Settings**
**Viewport dimensions:**
- Width: `800`
- Height: `450`

**Embed options:**
- ✓ "Automatically start on page load" 
- ✓ "Fullscreen button"
- ✓ "Mobile friendly"

**Frame options:**
- Select: "Click to launch in fullscreen"

### **E. Details**

**Cover image:** (Take screenshot of title screen)

**Screenshots:** (Take 4-5 in-game screenshots)

**Tags:**
```
tornado, photography, driving, action, short-session, roguelike, skill-based, weather
```

**Description:**
```markdown
# Stormchaser Loop

Chase tornados and capture the perfect shot!

## 🌪️ About
You're a storm chaser with one goal: photograph tornados and survive. Each 90-second session is a race against time and nature.

## 🎮 How to Play
- **WASD / Arrows** - Drive your chase vehicle
- **SPACE** - Hold to aim camera (2 seconds = perfect shot!)
- **X** - Dismiss weather alerts
- **ESC** - Pause

## 🎯 Features
- Realistic vehicle physics (90 MPH max)
- Dynamic tornados (EF0-EF5, 15-70 MPH)
- Photo quality system (Poor → Perfect)
- Procedural terrain with collision
- Pixelated ground-level tornado photos
- TV-style weather alerts
- Skill-based gameplay

## 💡 Tips
- Highways are faster than off-road!
- Aim for 2 seconds for perfect photos
- Don't get too close to the tornado's danger zone!
- Collect fuel pickups to extend your time

## ⚠️ Known Issues (Beta v0.1.0)
- Scene transitions may occasionally freeze (press ENTER to restart)
- Performance varies on different computers
- Best played in Chrome/Edge browser
- Leaderboard is local only (online coming soon!)

## 🐛 Troubleshooting
- **Black screen?** Press ENTER to restart
- **Debug info?** Press ` (backtick key)
- **Slow performance?** Close other browser tabs, reduce zoom to 100%

## 🎵 Credits
© 2025 Ghostweave Games
Built with Phaser 3, TypeScript, Vite

Version 0.1.0 - Beta
Feedback welcome!
```

### **F. Access & Visibility**

**Release status:**
- For beta testing: "Restricted" (then share link with testers)
- For public: "Public" 

**Pricing:** Free (or set a price)

**Community:**
- ✓ Enable comments
- ✓ Enable ratings

### **G. Save & Publish**

1. Click "Save & view page" (bottom of page)
2. Test the game on itch.io!
3. Share the link!

---

## Alternative Deployments

### **Option 2: Netlify (Free)**

**Faster than itch.io:**

```powershell
# Install Netlify CLI
npm install -g netlify-cli

# Build
npm run build

# Deploy
netlify deploy --dir=dist --prod
```

Follow prompts, get instant URL!

**Pros:** Very fast, custom domain support  
**Cons:** No game community features

---

### **Option 3: GitHub Pages (Free)**

```powershell
# Install gh-pages
npm install --save-dev gh-pages

# Add to package.json scripts:
"deploy": "vite build && gh-pages -d dist"

# Deploy
npm run deploy
```

**URL:** `https://your-username.github.io/stormchaser`

---

### **Option 4: Vercel (Free)**

```powershell
# Install Vercel CLI
npm install -g vercel

# Build
npm run build

# Deploy
vercel --prod
```

**Pros:** Very fast, automatic SSL  
**Cons:** Not game-focused platform

---

## 📋 Beta Testing Instructions

### **Share with Testers:**

```
🌪️ STORMCHASER LOOP - Beta v0.1.0

Play: [your itch.io link]

We need your help testing! Please report:
1. Any black screens (when? doing what?)
2. Performance issues (FPS drops?)
3. Gameplay feedback (too hard? too easy?)
4. Sound feedback (annoying? satisfying?)
5. Any bugs or crashes

How to help debug:
- Press F12 to open console
- Screenshot any errors (red text)
- Press ` (backtick) for debug panel
- If stuck: Press ENTER to restart

Expected session: 90 seconds
Goal: Take perfect tornado photos!

Feedback to: [your email/discord]

Thank you for testing! 🙏
```

---

## 🎯 Quick Deployment (Right Now!)

**Fastest path to get it online:**

```powershell
# 1. Build
npm run build

# 2. Test build
npm run preview
# Play one session to verify

# 3. Create ZIP
cd dist
Compress-Archive -Path * -DestinationPath ../stormchaser-loop.zip -Force
cd ..

# 4. Upload stormchaser-loop.zip to itch.io
```

**Time:** 10-15 minutes total

---

## 📊 What to Expect from Testers

### **Good Feedback:**
- "Black screen after crashing into X"
- "FPS drops to 20 when near tornado"
- "Camera mechanic is unclear"
- "Can't find the tornado"

### **Actionable:**
- Console error screenshots
- Steps to reproduce bugs
- Gameplay suggestions
- Feature requests

### **Collect:**
- How many sessions they played
- What their high score was
- Would they play again? (Y/N)
- What would make it better?

---

## 🔧 Post-Launch Quick Fixes

**If testers report issues:**

### **Black Screen Fix** (when we debug it):
```powershell
# Make fixes to src/scenes/Game.ts
# Test locally
npm run dev

# Rebuild
npm run build

# Re-upload ZIP to itch.io (replaces old version)
```

### **Performance Fix**:
```typescript
// In Game.ts, reduce world size
this.physics.world.setBounds(0,0,1600,900)
```

### **Sound Toggle**:
```typescript
// Add mute button
SoundService.setEnabled(false)
```

---

## 📁 Build Checklist

Before uploading:

- [ ] `npm run build` completes successfully
- [ ] `npm run preview` works locally
- [ ] Test one full session in preview
- [ ] ZIP created from dist/ **contents** (not dist folder itself)
- [ ] ZIP is 300-600KB (if larger, something's wrong)

---

## 🎮 Your Game is Ready!

**Current state:**
- ✅ Full game loop works
- ✅ Photography mechanic works
- ✅ Sound effects work
- ✅ Terrain works
- ⚠️ Scene transitions have occasional issues (we can fix based on tester feedback)

**Deploy as:**
- "Beta v0.1.0"
- "Early Access"
- "Work in Progress"

**With note:**
- "Scene transitions under development - press ENTER if stuck"
- "Best in Chrome/Edge"
- "Feedback appreciated!"

---

## 🚀 **Deploy Right Now**

**Run these commands:**

```powershell
npm run build
npm run preview
# Test it at http://localhost:4173
# If it works, create ZIP and upload!
```

**Then:** Go to itch.io and upload!

---

**Want me to help with anything specific about deployment?** 🚀

Or should I keep working on the black screen fix? Let me know! 🔧
