using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Renders prefabs to PNG from a fixed, bounds-derived camera so the SAME asset can be
/// compared before and after an importer change at real render size (960x600).
///
/// The shipped game is a near-black corridor; the deterministic in-browser states never
/// put the heavy character textures on screen, so a screenshot diff of the player cannot
/// certify them. This renders them directly, lit, filling the frame - a strictly harsher
/// test than how they ever appear in game.
///
///   Unity -batchmode -quit -projectPath <proj> -buildTarget WebGL
///         -executeMethod PrefabRenderDump.Dump
///         -prefabList <file of prefab paths> -prefabOut <dir>
/// </summary>
public static class PrefabRenderDump
{
    private const int Width = 960;
    private const int Height = 600;

    public static void Dump()
    {
        try
        {
            RunDump();
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Console.WriteLine("[PrefabDump] FATAL: " + e);
            EditorApplication.Exit(1);
        }
    }

    private static void RunDump()
    {
        string listPath = GetArg("-prefabList");
        string outDir = GetArg("-prefabOut");
        if (string.IsNullOrEmpty(listPath) || string.IsNullOrEmpty(outDir))
        {
            throw new Exception("-prefabList and -prefabOut are required.");
        }
        Directory.CreateDirectory(outDir);

        foreach (string raw in File.ReadAllLines(listPath))
        {
            string path = raw.Trim();
            if (path.Length == 0) { continue; }
            RenderOne(path, outDir);
        }
    }

    private static void RenderOne(string prefabPath, string outDir)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Console.WriteLine("[PrefabDump] MISSING: " + prefabPath);
            return;
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.identity;
        go.SetActive(true);

        // Fixed key light so shading is identical across runs.
        var lightGo = new GameObject("Key");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 2.6f;
        light.color = Color.white;
        lightGo.transform.rotation = Quaternion.Euler(35f, 200f, 0f);

        // Flat ambient so the subject is well exposed. A dark render would inflate PSNR
        // by filling the frame with black pixels that cannot differ.
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.58f, 1f);
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.fog = false;

        // Frame the renderer bounds. Deterministic: derived only from the asset itself.
        var renderers = go.GetComponentsInChildren<Renderer>(true)
                          .Where(r => r.enabled || true)
                          .ToArray();
        if (renderers.Length == 0)
        {
            Console.WriteLine("[PrefabDump] NO RENDERERS: " + prefabPath);
            return;
        }
        // SkinnedMeshRenderer bounds are authored bounds; good enough and stable.
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) { b.Encapsulate(renderers[i].bounds); }
        if (b.extents == Vector3.zero) { b.extents = Vector3.one * 0.5f; }

        var camGo = new GameObject("Cam");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.15f, 0.15f, 0.18f, 1f);
        cam.fieldOfView = 45f;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 1000f;
        cam.allowHDR = false;
        cam.allowMSAA = false;

        float radius = b.extents.magnitude;
        float dist = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.85f;
        var dir = new Vector3(0.35f, 0.18f, -1f).normalized;
        camGo.transform.position = b.center + dir * dist;
        camGo.transform.LookAt(b.center);
        cam.nearClipPlane = Mathf.Max(0.01f, dist - radius * 2f);
        cam.farClipPlane = dist + radius * 4f;

        var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32,
                                   RenderTextureReadWrite.sRGB);
        rt.antiAliasing = 1;
        cam.targetTexture = rt;
        cam.Render();

        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var shot = new Texture2D(Width, Height, TextureFormat.RGBA32, false, false);
        shot.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        shot.Apply();
        RenderTexture.active = prev;

        string safe = prefabPath.Replace('/', '_').Replace('\\', '_');
        File.WriteAllBytes(Path.Combine(outDir, safe + ".png"), shot.EncodeToPNG());

        cam.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(shot);
        rt.Release();
        UnityEngine.Object.DestroyImmediate(rt);

        Console.WriteLine(string.Format("[PrefabDump] {0} bounds={1} dist={2:F3}",
            prefabPath, b.size, dist));
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
