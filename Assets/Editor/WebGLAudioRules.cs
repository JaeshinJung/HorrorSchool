using System;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Reports and applies AudioImporter settings from a rules CSV, so every
/// per-clip decision is recorded in one auditable input file. Settings are
/// written to defaultSampleSettings because Unity 2022.3 exposes no "WebGL"
/// audio platform override; see the note in RunApply.
///
/// CSV columns (header required):
///   assetPath,loadType,compressionFormat,quality,sampleRateSetting,sampleRateOverride,forceToMono
///     loadType          : DecompressOnLoad | CompressedInMemory | Streaming
///     compressionFormat : PCM | Vorbis | ADPCM | AAC
///     quality           : 0..100 (percent)
///     sampleRateSetting : PreserveSampleRate | OptimizeSampleRate | OverrideSampleRate
///
///   Unity -batchmode -quit -nographics -projectPath <proj> -buildTarget WebGL
///         -executeMethod WebGLAudioRules.Report
///         -executeMethod WebGLAudioRules.Apply -audioRules <csv>
/// </summary>
public static class WebGLAudioRules
{
    // quality is a float that round-trips through the .meta YAML, so it is
    // compared with a tolerance. The CSV expresses quality in whole percent,
    // so the smallest difference that can ever be intended is 0.01; 1e-4 is
    // 100x below that and still ~1000x above float32 noise at this magnitude,
    // which makes a stuck default of 1.00 against a wanted 0.70 unmissable.
    private const float QualityEpsilon = 1e-4f;

    public static void Report()
    {
        try
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) { continue; }

                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                var def = importer.defaultSampleSettings;
                var wgl = importer.ContainsSampleSettingsOverride("WebGL")
                    ? importer.GetOverrideSampleSettings("WebGL")
                    : def;

                Console.WriteLine(string.Format(
                    "[AudioReport] {0} | len={1:F2}s ch={2} hz={3} | forceMono={4} "
                    + "| default: load={5} fmt={6} q={7:F2} srSet={8} srOvr={9} "
                    + "| WebGL(override={10}): load={11} fmt={12} q={13:F2} srSet={14} srOvr={15}",
                    path,
                    clip != null ? clip.length : 0f,
                    clip != null ? clip.channels : 0,
                    clip != null ? clip.frequency : 0,
                    importer.forceToMono,
                    def.loadType, def.compressionFormat, def.quality,
                    def.sampleRateSetting, def.sampleRateOverride,
                    importer.ContainsSampleSettingsOverride("WebGL"),
                    wgl.loadType, wgl.compressionFormat, wgl.quality,
                    wgl.sampleRateSetting, wgl.sampleRateOverride));
            }
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Console.WriteLine("[AudioReport] FATAL: " + e);
            EditorApplication.Exit(1);
        }
    }

    public static void Apply()
    {
        try
        {
            RunApply();
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Console.WriteLine("[AudioRules] FATAL: " + e);
            EditorApplication.Exit(1);
        }
    }

    private static void RunApply()
    {
        string rulesPath = GetArg("-audioRules");
        if (string.IsNullOrEmpty(rulesPath))
        {
            throw new Exception("-audioRules is required.");
        }

        string[] lines = File.ReadAllLines(rulesPath);
        int applied = 0, missing = 0;

        for (int i = 1; i < lines.Length; i++) // skip header
        {
            string line = lines[i].Trim();
            if (line.Length == 0) { continue; }

            string[] f = line.Split(',');
            if (f.Length < 7)
            {
                throw new Exception("Malformed rule on line " + (i + 1) + ": " + line);
            }

            string path = f[0];
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                Console.WriteLine("[AudioRules] NOT AN AUDIOCLIP / MISSING: " + path);
                missing++;
                continue;
            }

            var s = importer.defaultSampleSettings;
            s.loadType = (AudioClipLoadType)Enum.Parse(typeof(AudioClipLoadType), f[1], true);
            s.compressionFormat = (AudioCompressionFormat)Enum.Parse(
                typeof(AudioCompressionFormat), f[2], true);
            s.quality = float.Parse(f[3], CultureInfo.InvariantCulture) / 100f;
            s.sampleRateSetting = (AudioSampleRateSetting)Enum.Parse(
                typeof(AudioSampleRateSetting), f[4], true);
            s.sampleRateOverride = uint.Parse(f[5], CultureInfo.InvariantCulture);
            s.conversionMode = 0;

            // Unity 2022.3 AudioImporter has no "WebGL" platform override:
            // SetOverrideSampleSettings("WebGL", ...) returns false and writes
            // nothing, while Standalone/iPhone/Android/console names are accepted.
            // A WebGL build therefore imports from defaultSampleSettings, so that
            // is what this tool writes.
            importer.defaultSampleSettings = s;
            importer.forceToMono = f[6].Trim() == "1";
            importer.SaveAndReimport();

            // Re-read from disk: a silent no-op here would otherwise be reported
            // below as a successful apply.
            var written = (AssetImporter.GetAtPath(path) as AudioImporter).defaultSampleSettings;
            if (written.loadType != s.loadType
                || written.compressionFormat != s.compressionFormat
                || Mathf.Abs(written.quality - s.quality) > QualityEpsilon)
            {
                throw new Exception(string.Format(
                    "Import settings did not persist for {0}: wanted load={1} fmt={2} q={3:F2}, "
                    + "got load={4} fmt={5} q={6:F2}",
                    path, s.loadType, s.compressionFormat, s.quality,
                    written.loadType, written.compressionFormat, written.quality));
            }

            Console.WriteLine(string.Format(
                "[AudioRules] {0} -> load={1} fmt={2} q={3:F2} sr={4}/{5} mono={6}",
                path, s.loadType, s.compressionFormat, s.quality,
                s.sampleRateSetting, s.sampleRateOverride, importer.forceToMono));
            applied++;
        }

        AssetDatabase.Refresh();
        Console.WriteLine(string.Format("[AudioRules] applied={0} missing={1} from {2}",
            applied, missing, rulesPath));
    }

    private static string GetArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }
        return null;
    }
}
