using UnityEditor;
using UnityEngine;

/// <summary>
/// Radio lite (S8-09): every clip dropped into Resources/Music imports as compressed-in-memory Vorbis (q 0.7), loaded in
/// the background. Unity's default (decompress on load) would cost ≈ 30 MB of memory per 3-minute song, which WebGL
/// can't afford. Applies on first import only, so a hand-tuned clip keeps its settings.
/// </summary>
public sealed class MusicImportSettings : AssetPostprocessor
{
    private void OnPreprocessAudio()
    {
        if (!assetPath.Replace(System.IO.Path.DirectorySeparatorChar, '/').Contains("/Resources/Music/")) return;
        if (!assetImporter.importSettingsMissing) return;
        var importer = (AudioImporter)assetImporter;
        importer.loadInBackground = true;
        AudioImporterSampleSettings s = importer.defaultSampleSettings;
        s.loadType = AudioClipLoadType.CompressedInMemory;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        s.quality = 0.7f;
        importer.defaultSampleSettings = s;
    }
}
