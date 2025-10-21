# 🔧 itch.io Black Screen Fix

## Problem

Uploaded to itch.io but shows black screen.

## Solution

The issue was **absolute paths** in the build. I've fixed it!

---

## ✅ What I Fixed

**Updated `vite.config.ts`:**
```typescript
base: './',  // Use relative paths for itch.io
```

This makes all asset paths relative instead of absolute.

---

## 🚀 New Build Created

**File:** `stormchaser-loop-fixed.zip` (in your project root)

This ZIP has the correct configuration for itch.io!

---

## 📋 Upload Instructions

### 1. Go to Your itch.io Project

### 2. Delete Old Upload
- Go to "Edit game"
- Scroll to "Uploads"
- Delete the old stormchaser-loop.zip

### 3. Upload New File
- Click "Upload files"
- Select **stormchaser-loop-fixed.zip**
- Check "This file will be played in the browser"

### 4. Verify Settings
**Embed options:**
- Viewport: `800` × `450`
- ✓ "Automatically start on page load"
- ✓ "Fullscreen button"
- Frame: "Click to launch in fullscreen"

### 5. Save & Test
- Save changes
- Click "View page"
- Test the game on itch.io!

---

## 🔍 How to Test

**On itch.io page:**
1. Click "Run game"
2. Should see title screen (NOT black)
3. Play a session
4. Verify everything works

**If still black screen:**
- Right-click → Inspect (F12)
- Go to Console tab
- Screenshot any errors
- Share with me

---

## 🎯 What's Different

**Old ZIP (broken):**
- Paths like `/assets/index-abc123.js` (absolute)
- Works locally, breaks on itch.io

**New ZIP (fixed):**
- Paths like `./assets/index-abc123.js` (relative)
- Works everywhere!

---

## ✅ Checklist

- [ ] Delete old upload on itch.io
- [ ] Upload `stormchaser-loop-fixed.zip`
- [ ] Set viewport to 800×450
- [ ] Enable fullscreen button
- [ ] Save changes
- [ ] Test on itch.io page
- [ ] Verify game loads (NOT black screen!)

---

**The fix is ready - upload the new ZIP file!** 🚀

**File location:** `C:\Users\subfl\workspace\stormchaser\stormchaser-loop-fixed.zip`

