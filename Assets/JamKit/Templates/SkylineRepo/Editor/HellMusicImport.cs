using UnityEditor;
using UnityEngine;

namespace JamKit.SkylineRepo.EditorTools
{
    /// <summary>The music stems load Compressed In Memory (not Streaming), preloaded: the in-run set starts 19 stems on one
    /// dspTime, which streaming can't do reliably (docs/HELL-MUSIC-DIRECTOR.md).</summary>
    public sealed class HellMusicImport : AssetPostprocessor
    {
        const string Folder = "/Resources/SkylineAudio/hell-music/";

        void OnPreprocessAudio()
        {
            if (!assetPath.Replace('\\', '/').Contains(Folder)) return;
            var importer = (AudioImporter)assetImporter;
            var s = importer.defaultSampleSettings;
            s.loadType = AudioClipLoadType.CompressedInMemory;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.preloadAudioData = true;
            importer.defaultSampleSettings = s;
            importer.loadInBackground = false;
        }
    }
}
