# 🐛 itch.io Black Screen Troubleshooting

## Critical Diagnostic Steps

The itch.io black screen is different from local issues. We need to see what's failing!

---

## 🔍 Step 1: Check Console on itch.io

**This is the MOST IMPORTANT step:**

1. **Go to your itch.io game page**
2. **Click "Run game"**
3. **Right-click on the game window** → Inspect (or press F12)
4. **Go to Console tab**
5. **Look for RED error messages**

### What to Look For:

**Common itch.io errors:**

```
❌ Failed to load module
❌ CORS error
❌ 404 Not Found (asset path issue)
❌ Uncaught ReferenceError
❌ Failed to fetch
```

**Screenshot or copy/paste ANY red errors you see!**

---

## 🔍 Step 2: Check Network Tab

While on itch.io with game open:

1. **F12 → Network tab**
2. **Refresh the game**
3. **Look for red entries** (failed to load)
4. **Check if index.html loaded** (should be 200 OK)
5. **Check if JavaScript files loaded** (should be 200 OK)

**Screenshot the Network tab if anything is red!**

---

## 🔧 Common itch.io Issues & Fixes

### Issue 1: Viewport Settings Wrong

**Check your itch.io settings:**
- Viewport width: **800** (not 800px)
- Viewport height: **450** (not 450px)
- Should be numbers only!

### Issue 2: Wrong Embed Option

**Check:**
- ✓ "This file will be played in the browser" is checked
- ✓ "Embed in page" is selected (not "Fullscreen")

### Issue 3: Index File Not Detected

**itch.io needs to see index.html at root of ZIP**

Verify ZIP structure:
```
stormchaser-loop-fixed.zip
├── index.html          ← Must be at root!
├── assets/
│   └── index-xxx.js
└── remote-config.json
```

**NOT like this:**
```
stormchaser-loop-fixed.zip
└── dist/               ← WRONG!
    └── index.html
```

---

## 🛠️ Quick Fix: Verify ZIP Contents

**On Windows PowerShell:**

```powershell
# Check what's in the ZIP
Expand-Archive -Path stormchaser-loop-fixed.zip -DestinationPath temp-check -Force
ls temp-check
# Should show: index.html, assets/, etc at TOP LEVEL

# Clean up
rm -r temp-check
```

---

## 🔍 Most Likely Causes

### Cause A: Console Errors
**Symptom:** JavaScript fails to load or run  
**Check:** Console tab for red errors  
**Share:** Screenshot of console

### Cause B: Asset Path Issues
**Symptom:** Files return 404  
**Check:** Network tab for failed requests  
**Fix:** Might need to adjust paths further

### Cause C: itch.io Settings Wrong
**Symptom:** Game doesn't embed correctly  
**Check:** Viewport settings, embed options  
**Fix:** Update settings, re-save

---

## 📋 What to Send Me

**To help debug, I need:**

1. **Console screenshot** from itch.io game page (F12 → Console)
2. **Network tab screenshot** (F12 → Network, refresh, show red items)
3. **Your itch.io embed settings:**
   - Viewport dimensions?
   - Embed option selected?
   - "This file will be played in browser" checked?

**With these, I can identify and fix the exact issue!**

---

## 🚑 Emergency Fix: Test Build Locally First

Before uploading to itch.io, let's verify the build works:

```powershell
npm run preview
```

**Open:** http://localhost:4173

**Does the preview work?**
- **YES** → Build is fine, itch.io settings issue
- **NO** → Build is broken, need to fix locally first

---

## 🔧 Alternative: Try Different Build

If preview works but itch.io doesn't, try:

```powershell
# Clear dist folder
rm -r dist/*

# Rebuild with different config
npm run build

# Check dist/index.html - should have relative paths
cat dist/index.html
# Look for: src="./assets/..." (good)
# NOT: src="/assets/..." (bad for itch.io)
```

---

## 📊 Debugging Checklist

On itch.io game page:

- [ ] F12 → Console → Any RED errors? (screenshot!)
- [ ] F12 → Network → Any 404/failed requests? (screenshot!)
- [ ] Game settings → Viewport: 800 × 450 (exact numbers)
- [ ] Game settings → "Played in browser" checked
- [ ] ZIP contains index.html at root level
- [ ] Preview build works locally (http://localhost:4173)

---

## 🎯 What I Need to Fix It

**Send me:**
1. Console screenshot from itch.io
2. OR description of any error messages you see

**I'll provide:**
1. Exact fix for the issue
2. Updated build command
3. Working solution

---

**Most important: Check the Console (F12) on itch.io and tell me what errors you see!** 🔍

That will tell us exactly what's wrong! 🎮

