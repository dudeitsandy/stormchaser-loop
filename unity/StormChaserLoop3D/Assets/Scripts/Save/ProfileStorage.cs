using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>Where the stem's files live. Injected so tests never touch disk.</summary>
public interface IProfileStorage
{
    /// <summary>Reads a file's text; false when it doesn't exist or can't be read.</summary>
    bool TryRead(string name, out string text);
    /// <summary>Writes a file safely (tmp + rename); false on any failure.</summary>
    bool TryWrite(string name, string text);
}

/// <summary>
/// Files under <see cref="Application.persistentDataPath"/>, written tmp + rename (the stem's "simple safe write").
/// On WebGL that path is IndexedDB-backed and a write is lost on reload unless it is flushed, so every write is followed
/// by an explicit FS sync (ProfileSync.jslib). Verified 2026-10-04: with the sync a launch counter survived three
/// reloads; without it the next reload read the previous value.
/// </summary>
public sealed class FileProfileStorage : IProfileStorage
{
    private readonly string _dir;

    // The editor (and its test runs) shares persistentDataPath with the Windows build, so it saves in a subfolder
    // and never touches a player's real profile.
    public FileProfileStorage(string directory = null) =>
        _dir = directory ?? (Application.isEditor ? Path.Combine(Application.persistentDataPath, "editor") : Application.persistentDataPath);

    public bool TryRead(string name, out string text)
    {
        text = null;
        try
        {
            string path = Path.Combine(_dir, name);
            string tmp = path + ".tmp";
            if (File.Exists(path)) text = File.ReadAllText(path);
            else if (File.Exists(tmp)) text = File.ReadAllText(tmp); // died between write and rename
            return text != null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Profile] read {name} failed: {e.Message}");
            return false;
        }
    }

    public bool TryWrite(string name, string text)
    {
        try
        {
            Directory.CreateDirectory(_dir);
            string path = Path.Combine(_dir, name);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, text);
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
            SyncWebGLFileSystem();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Profile] write {name} failed: {e.Message}");
            return false;
        }
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void DoomsdaySyncFs();
    private static void SyncWebGLFileSystem() => DoomsdaySyncFs();
#else
    private static void SyncWebGLFileSystem() { }
#endif

}

/// <summary>In-memory storage: tests, and the session-only fallback if browser persistence fails.</summary>
public sealed class MemoryProfileStorage : IProfileStorage
{
    public readonly Dictionary<string, string> Files = new Dictionary<string, string>();
    /// <summary>When true every write fails (tests of the write-failure path).</summary>
    public bool FailWrites;
    public int Writes { get; private set; }

    public bool TryRead(string name, out string text) => Files.TryGetValue(name, out text);

    public bool TryWrite(string name, string text)
    {
        if (FailWrites) return false;
        Files[name] = text;
        Writes++;
        return true;
    }
}
