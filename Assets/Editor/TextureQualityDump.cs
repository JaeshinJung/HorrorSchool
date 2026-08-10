using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Decodes imported textures to PNG so the same texture can be compared before and
/// after an importer change (PSNR/MAE per file). The in-game screenshots are a dark
/// corridor and never show the character textures, so this is the only way to put a
/// number on what a maxTextureSize / compression change actually cost those assets.
///
/// Must be run WITH graphics (Blit needs a device):
///   Unity -batchmode -quit -projectPath <proj>
///         -executeMethod TextureQualityDump.Dump
///         -texDumpList <file of asset paths> -texDumpOut <dir> [-texDumpSize 1024]
/// </summary>
public static class TextureQualityDump
{
    public static void Dump()
    {
        try
        {
            RunDump();
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Console.WriteLine("[TexDump] FATAL: " + e);
            EditorApplication.Exit(1);
        }
    }

    private static void RunDump()
    {
        string listPath = GetArg("-texDumpList");
        string outDir = GetArg("-texDumpOut");
        string sizeArg = GetArg("-texDumpSize");
        int size = 1024;
        if (!string.IsNullOrEmpty(sizeArg)) { int.TryParse(sizeArg, out size); }

        if (string.IsNullOrEmpty(listPath) || string.IsNullOrEmpty(outDir))
        {
            throw new Exception("-texDumpList and -texDumpOut are required.");
        }

        Directory.CreateDirectory(outDir);
        string[] paths = File.ReadAllLines(listPath);

        int done = 0;
        foreach (string raw in paths)
        {
            string path = raw.Trim();
            if (path.Length == 0) { continue; }

            var tex = AssetDatabase.LoadAssetAtPath<Texture>(path);
            if (tex == null)
            {
                Console.WriteLine("[TexDump] MISSING: " + path);
                continue;
            }

            var rt = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32,
                                                RenderTextureReadWrite.Linear);
            var prev = RenderTexture.active;
            Graphics.Blit(tex, rt);
            RenderTexture.active = rt;
            var readback = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            readback.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            readback.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            string safe = path.Replace('/', '_').Replace('\\', '_');
            File.WriteAllBytes(Path.Combine(outDir, safe + ".png"), readback.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(readback);

            Console.WriteLine(string.Format("[TexDump] {0} -> {1}x{2} src {3}x{4}",
                path, size, size, tex.width, tex.height));
            done++;
        }

        Console.WriteLine("[TexDump] dumped " + done + " textures to " + outDir);
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
