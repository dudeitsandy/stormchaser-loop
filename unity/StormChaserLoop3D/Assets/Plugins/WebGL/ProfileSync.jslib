// Save & Profile M1 stem: flush Application.persistentDataPath (IDBFS) to IndexedDB after each profile write.
// Verified 2026-10-04: without this, a write did not survive a page reload; with it, it did.
mergeInto(LibraryManager.library, {
  DoomsdaySyncFs: function () {
    if (typeof FS === 'undefined' || !FS.syncfs) return;
    FS.syncfs(false, function (err) {
      if (err) console.warn('[Profile] IndexedDB sync: ' + (err.message || err.code || err.errno || JSON.stringify(err)));
    });
  }
});
