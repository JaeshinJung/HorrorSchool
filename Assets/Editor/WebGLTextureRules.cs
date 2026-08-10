using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Applies WebGL texture importer platform overrides from a rules CSV, so every
/// per-file decision is recorded in one auditable input file instead of being
/// hand-edited into .meta files.
///
/// CSV columns (header required):
///   assetPath,maxTextureSize,compression,crunched,compressionQuality
///     compression : Uncompressed | Compressed | CompressedHQ | CompressedLQ
///     crunched    : 0 | 1
///
///   Unity -batchmode -quit -nographics -projectPath <proj> -buildTarget WebGL
///         -executeMethod WebGLTextureRules.Apply -texRules <csv>
/// </summary>
public static class WebGLTextureRules
{
    public static void Apply()
    {
        try
        {
            RunApply();
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Console.WriteLine("[TexRules] FATAL: " + e);
            EditorApplication.Exit(1);
        }
    }

    private static void RunApply()
    {
        string rulesPath = GetArg("-texRules");
        if (string.IsNullOrEmpty(rulesPath))
        {
            throw new Exception("-texRules is required.");
        }

        string[] lines = File.ReadAllLines(rulesPath);
        int applied = 0, unchanged = 0, missing = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 1; i < lines.Length; i++) // skip header
            {
                string line = lines[i].Trim();
                if (line.Length == 0) { continue; }

                string[] f = line.Split(',');
                if (f.Length < 5)
                {
                    throw new Exception("Malformed rule on line " + (i + 1) + ": " + line);
                }

                string path = f[0];
                int maxSize = int.Parse(f[1], CultureInfo.InvariantCulture);
                var compression = (TextureImporterCompression)Enum.Parse(
                    typeof(TextureImporterCompression), f[2], true);
                bool crunched = f[3].Trim() == "1";
                int quality = int.Parse(f[4], CultureInfo.InvariantCulture);

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    Console.WriteLine("[TexRules] NOT A TEXTURE / MISSING: " + path);
                    missing++;
                    continue;
                }

                var ps = importer.GetPlatformTextureSettings("WebGL");
                bool same = ps.overridden
                            && ps.maxTextureSize == maxSize
                            && ps.textureCompression == compression
                            && ps.crunchedCompression == crunched
                            && ps.compressionQuality == quality;
                if (same) { unchanged++; continue; }

                ps.name = "WebGL";
                ps.overridden = true;
                ps.maxTextureSize = maxSize;
                ps.format = TextureImporterFormat.Automatic;
                ps.textureCompression = compression;
                ps.crunchedCompression = crunched;
                ps.compressionQuality = quality;
                importer.SetPlatformTextureSettings(ps);
                importer.SaveAndReimport();

                Console.WriteLine(string.Format(
                    "[TexRules] {0} -> max={1} comp={2} crunch={3} q={4}",
                    path, maxSize, compression, crunched ? 1 : 0, quality));
                applied++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.Refresh();
        Console.WriteLine(string.Format(
            "[TexRules] applied={0} unchanged={1} missing={2} from {3}",
            applied, unchanged, missing, rulesPath));
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
