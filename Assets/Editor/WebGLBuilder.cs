using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Batchmode WebGL build entry point.
/// Invoked as: Unity -batchmode -quit -nographics -projectPath <proj>
///             -buildTarget WebGL -executeMethod WebGLBuilder.Build
///             -buildOutput <dir> [-webglCompression Disabled|Gzip|Brotli]
///             [-webglFallback true|false] [-packedAssetsCsv <file>]
///
/// -webglCompression / -webglFallback are TEMPORARY, per-build overrides: the previous
/// PlayerSettings values are restored before the process exits, so ProjectSettings.asset
/// keeps whatever the project committed. Use them for local test builds; never edit
/// ProjectSettings by hand to get an uncompressed build.
///
/// For reference when reading a ProjectSettings.asset diff, webGLCompressionFormat
/// serialises as 0=Brotli, 1=Gzip, 2=Disabled. Prefer the log lines below over
/// interpreting the raw number.
/// </summary>
public static class WebGLBuilder
{
    public static void Build()
    {
        // EditorApplication.Exit terminates the process without unwinding the stack, so it
        // must never be called from inside RunBuild - a finally block there would be
        // skipped and the temporary compression override would leak into ProjectSettings.
        int exitCode;
        try
        {
            exitCode = RunBuild();
        }
        catch (Exception e)
        {
            Console.WriteLine("[WebGLBuilder] FATAL unhandled exception: " + e);
            exitCode = Fail("Unhandled exception in build script.");
        }

        Console.Out.Flush();
        EditorApplication.Exit(exitCode);
    }

    private static int RunBuild()
    {
        string outputPath = GetArg("-buildOutput");
        if (string.IsNullOrEmpty(outputPath))
        {
            return Fail("-buildOutput not supplied.");
        }

        // Enabled scenes only, preserving EditorBuildSettings order.
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        Console.WriteLine("[WebGLBuilder] ---- build configuration ----");
        Console.WriteLine("[WebGLBuilder] output      : " + outputPath);
        Console.WriteLine("[WebGLBuilder] scene count : " + scenes.Length);
        for (int i = 0; i < scenes.Length; i++)
        {
            Console.WriteLine(string.Format("[WebGLBuilder]   [{0}] {1}", i, scenes[i]));
        }

        if (scenes.Length == 0)
        {
            return Fail("No enabled scenes in EditorBuildSettings - refusing to build an empty player.");
        }

        // Verify every scene file actually exists before handing off to BuildPipeline,
        // which otherwise reports this as an opaque failure.
        var missing = scenes.Where(p => !File.Exists(p)).ToList();
        if (missing.Count > 0)
        {
            foreach (string m in missing)
            {
                Console.WriteLine("[WebGLBuilder] MISSING SCENE FILE: " + m);
            }
            return Fail("One or more enabled scenes do not exist on disk.");
        }

        // Record the project's own settings BEFORE any override, so the build log is
        // an honest record of what the project ships with.
        var webglTarget = NamedBuildTarget.WebGL;
        Console.WriteLine("[WebGLBuilder] ---- project settings as committed ----");
        Console.WriteLine("[WebGLBuilder] compressionFormat      : "
                          + PlayerSettings.WebGL.compressionFormat);
        Console.WriteLine("[WebGLBuilder] decompressionFallback  : "
                          + PlayerSettings.WebGL.decompressionFallback);
        Console.WriteLine("[WebGLBuilder] stripEngineCode        : "
                          + PlayerSettings.stripEngineCode);
        Console.WriteLine("[WebGLBuilder] managedStrippingLevel  : "
                          + PlayerSettings.GetManagedStrippingLevel(webglTarget));

        // Compression is the ONLY override, and only so the output can be served by a
        // plain static file server (python -m http.server) which cannot emit
        // Content-Encoding: br/gzip. Nothing else is pre-tuned.
        //
        // Assigning PlayerSettings.WebGL.* rewrites ProjectSettings.asset on disk, so an
        // override applied for one throwaway build used to silently become the project's
        // committed default. The override is therefore scoped: original values are captured
        // here and restored in the finally below, whatever the build does.
        bool overridden = !string.IsNullOrEmpty(GetArg("-webglCompression"))
                          || !string.IsNullOrEmpty(GetArg("-webglFallback"));
        WebGLCompressionFormat originalCompression = PlayerSettings.WebGL.compressionFormat;
        bool originalFallback = PlayerSettings.WebGL.decompressionFallback;

        try
        {
            ApplyCompressionOverrides();
            return BuildAndReport(outputPath, scenes);
        }
        finally
        {
            if (overridden)
            {
                PlayerSettings.WebGL.compressionFormat = originalCompression;
                PlayerSettings.WebGL.decompressionFallback = originalFallback;
                // The assignments above only mark the settings object dirty; save explicitly
                // so the restored values, not the override, are what lands on disk.
                AssetDatabase.SaveAssets();
                Console.WriteLine("[WebGLBuilder] ---- compression override reverted ----");
                Console.WriteLine("[WebGLBuilder] compressionFormat      -> "
                                  + PlayerSettings.WebGL.compressionFormat);
                Console.WriteLine("[WebGLBuilder] decompressionFallback  -> "
                                  + PlayerSettings.WebGL.decompressionFallback);
            }
        }
    }

    /// <summary>
    /// Applies -webglCompression / -webglFallback to PlayerSettings for this build only.
    /// The caller is responsible for restoring the previous values.
    /// </summary>
    private static void ApplyCompressionOverrides()
    {
        string compressionArg = GetArg("-webglCompression");
        if (!string.IsNullOrEmpty(compressionArg))
        {
            try
            {
                var compression = (WebGLCompressionFormat)Enum.Parse(
                    typeof(WebGLCompressionFormat), compressionArg, true);
                PlayerSettings.WebGL.compressionFormat = compression;
                Console.WriteLine("[WebGLBuilder] compressionFormat      -> " + compression
                                  + " (temporary override for this build only)");
            }
            catch (Exception)
            {
                Console.WriteLine("[WebGLBuilder] Unrecognised -webglCompression '"
                                  + compressionArg + "', leaving project setting alone.");
            }
        }

        // Decompression fallback is what lets a static host with no Content-Encoding
        // headers (itch.io, python -m http.server) serve a compressed build.
        string fallbackArg = GetArg("-webglFallback");
        if (!string.IsNullOrEmpty(fallbackArg))
        {
            bool fallback;
            if (bool.TryParse(fallbackArg, out fallback))
            {
                PlayerSettings.WebGL.decompressionFallback = fallback;
                Console.WriteLine("[WebGLBuilder] decompressionFallback  -> " + fallback
                                  + " (temporary override for this build only)");
            }
            else
            {
                Console.WriteLine("[WebGLBuilder] Unrecognised -webglFallback '"
                                  + fallbackArg + "', leaving project setting alone.");
            }
        }
    }

    private static int BuildAndReport(string outputPath, string[] scenes)
    {
        EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.WebGL;
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;

        Directory.CreateDirectory(outputPath);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            // NOTE: BuildOptions.DetailedBuildReport crashes 2022.3.5f1 (segfault in
            // BuildReporting::ScenesUsingAssets::RegisterScenesUsingAssets). packedAssets
            // is populated anyway as long as player data is actually rebuilt, which is
            // why Library/PlayerDataCache is cleared before a measurement build.
            options = BuildOptions.None,
        };

        Console.WriteLine("[WebGLBuilder] starting BuildPipeline.BuildPlayer at " + DateTime.Now);
        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report == null)
        {
            return Fail("BuildPipeline.BuildPlayer returned a null BuildReport.");
        }

        BuildSummary summary = report.summary;

        // Ground truth for what is actually inside the .data file: every packed asset
        // with its post-import, in-player byte size. Written before any early return on
        // error so a partially bad build is still auditable.
        DumpPackedAssets(report, GetArg("-packedAssetsCsv"));

        // Count real errors from the build report.
        int errorCount = 0;
        int warningCount = 0;
        foreach (BuildStep step in report.steps)
        {
            foreach (BuildStepMessage msg in step.messages)
            {
                if (msg.type == LogType.Error || msg.type == LogType.Exception
                    || msg.type == LogType.Assert)
                {
                    errorCount++;
                    Console.WriteLine(string.Format("[WebGLBuilder] BUILD ERROR ({0}) [{1}]: {2}",
                        msg.type, step.name, msg.content));
                }
                else if (msg.type == LogType.Warning)
                {
                    warningCount++;
                }
            }
        }

        Console.WriteLine("[WebGLBuilder] ---- BuildReport summary ----");
        Console.WriteLine("[WebGLBuilder] result        : " + summary.result);
        Console.WriteLine("[WebGLBuilder] output path   : " + summary.outputPath);
        Console.WriteLine("[WebGLBuilder] total size    : " + summary.totalSize + " bytes ("
                          + (summary.totalSize / (1024f * 1024f)).ToString("F2") + " MiB)");
        Console.WriteLine("[WebGLBuilder] total time    : " + summary.totalTime);
        Console.WriteLine("[WebGLBuilder] error count   : " + errorCount
                          + " (summary.totalErrors=" + summary.totalErrors + ")");
        Console.WriteLine("[WebGLBuilder] warning count : " + warningCount
                          + " (summary.totalWarnings=" + summary.totalWarnings + ")");

        if (summary.result != BuildResult.Succeeded)
        {
            return Fail("Build result was " + summary.result + " with " + errorCount + " error(s).");
        }

        if (errorCount > 0 || summary.totalErrors > 0)
        {
            return Fail("Build reported Succeeded but logged " + errorCount + " error message(s).");
        }

        Console.WriteLine("[WebGLBuilder] BUILD SUCCEEDED -> " + outputPath);
        return 0;
    }

    /// <summary>
    /// Writes every entry of report.packedAssets as CSV, largest first.
    /// Columns: sourceAssetPath,packedSize,type,file
    /// </summary>
    private static void DumpPackedAssets(BuildReport report, string csvPath)
    {
        if (string.IsNullOrEmpty(csvPath))
        {
            Console.WriteLine("[WebGLBuilder] -packedAssetsCsv not supplied, skipping asset dump.");
            return;
        }

        try
        {
            var rows = report.packedAssets
                .SelectMany(pa => pa.contents.Select(c => new
                {
                    Path = c.sourceAssetPath,
                    Size = c.packedSize,
                    Type = c.type != null ? c.type.Name : "(null)",
                    File = pa.shortPath,
                }))
                .OrderByDescending(r => r.Size)
                .ToList();

            string dir = Path.GetDirectoryName(csvPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using (var w = new StreamWriter(csvPath, false))
            {
                w.WriteLine("sourceAssetPath,packedSize,type,file");
                foreach (var r in rows)
                {
                    w.WriteLine(string.Format("{0},{1},{2},{3}",
                        Csv(r.Path), r.Size, Csv(r.Type), Csv(r.File)));
                }
            }

            ulong total = 0;
            foreach (var r in rows) { total += r.Size; }
            Console.WriteLine("[WebGLBuilder] packed assets dumped : " + rows.Count
                              + " rows, " + total + " bytes -> " + csvPath);
        }
        catch (Exception e)
        {
            Console.WriteLine("[WebGLBuilder] packed asset dump FAILED: " + e);
        }
    }

    private static string Csv(string value)
    {
        if (string.IsNullOrEmpty(value)) { return "\"\""; }
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static int Fail(string reason)
    {
        Console.WriteLine("[WebGLBuilder] BUILD FAILED: " + reason);
        Console.Out.Flush();
        return 1;
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
