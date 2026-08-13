using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// TEMPORARY verification tool (not part of the build pipeline). Instantiates
/// DGreenBoard/NGreenBoard, finds the named text child, and renders a tight
/// close-up so the m_text content can be visually confirmed after an edit.
///
///   Unity -batchmode -quit -projectPath <proj>
///         -executeMethod GreenBoardTextShot.Dump -shotOut <dir>
/// </summary>
public static class GreenBoardTextShot
{
    private const int Width = 1024;
    private const int Height = 512;

    public static void Dump()
    {
        try
        {
            string outDir = GetArg("-shotOut");
            if (string.IsNullOrEmpty(outDir))
            {
                throw new Exception("-shotOut is required.");
            }
            Directory.CreateDirectory(outDir);

            RenderOne("Assets/04Prefabs/01GreenBoard/DGreenBoard.prefab", "떠든사람 변경", outDir, "d-change");
            RenderOne("Assets/04Prefabs/01GreenBoard/DGreenBoard.prefab", "떠든 사람 목록", outDir, "d-list");
            RenderOne("Assets/04Prefabs/01GreenBoard/DGreenBoard.prefab", "주번 목록", outDir, "d-duty");
            RenderOne("Assets/04Prefabs/01GreenBoard/NGreenBoard.prefab", "떠든 사람 목록", outDir, "n-list");
            RenderOne("Assets/04Prefabs/01GreenBoard/NGreenBoard.prefab", "주번 목록", outDir, "n-duty");

            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Console.WriteLine("[GreenBoardTextShot] FATAL: " + e);
            EditorApplication.Exit(1);
        }
    }

    private static void RenderOne(string prefabPath, string childName, string outDir, string tag)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Console.WriteLine("[GreenBoardTextShot] MISSING PREFAB: " + prefabPath);
            return;
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.identity;
        go.SetActive(true);

        // TMP meshes are only (re)generated lazily; force it so Renderer.bounds and the
        // rendered geometry are both valid before we screenshot.
        foreach (var tmp in go.GetComponentsInChildren<TMP_Text>(true))
        {
            tmp.ForceMeshUpdate();
        }

        Transform target = go.GetComponentsInChildren<Transform>(true)
                              .FirstOrDefault(t => t.name == childName);
        if (target == null)
        {
            Console.WriteLine("[GreenBoardTextShot] CHILD NOT FOUND: " + childName + " in " + prefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return;
        }

        var renderer = target.GetComponent<Renderer>();
        if (renderer == null)
        {
            Console.WriteLine("[GreenBoardTextShot] NO RENDERER on: " + childName);
            UnityEngine.Object.DestroyImmediate(go);
            return;
        }

        // The board mesh and other siblings sit right behind/around the thin text quad and
        // fill the whole frame at close range, hiding the text entirely. Disable everything
        // except the target renderer so only the text mesh can appear in the shot.
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            r.enabled = (r == renderer);
        }

        Bounds b = renderer.bounds;
        if (b.extents == Vector3.zero) { b.extents = Vector3.one * 0.1f; }

        var lightGo = new GameObject("Key");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightGo.transform.rotation = Quaternion.Euler(50f, 200f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.63f, 1f);
        RenderSettings.fog = false;

        var camGo = new GameObject("Cam");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.35f, 0.1f, 1f);
        cam.fieldOfView = 40f;
        cam.allowHDR = false;
        cam.allowMSAA = false;

        // Text mesh renders toward its local forward, but this text's RectTransform has
        // localScale.x = -1 (mirrored to read correctly from the board's real in-game
        // viewing side); view from the opposite side to un-mirror it here.
        Vector3 normal = -target.forward;
        float radius = Mathf.Max(b.extents.x, b.extents.y) + 0.05f;
        float dist = radius / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.1f;

        camGo.transform.position = b.center - normal * dist;
        camGo.transform.LookAt(b.center, target.up);
        cam.nearClipPlane = Mathf.Max(0.01f, dist - radius * 4f);
        cam.farClipPlane = dist + radius * 4f;

        var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32,
                                   RenderTextureReadWrite.sRGB);
        cam.targetTexture = rt;
        cam.Render();

        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var shot = new Texture2D(Width, Height, TextureFormat.RGBA32, false, false);
        shot.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        shot.Apply();
        RenderTexture.active = prev;

        File.WriteAllBytes(Path.Combine(outDir, tag + ".png"), shot.EncodeToPNG());

        cam.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(shot);
        rt.Release();
        UnityEngine.Object.DestroyImmediate(rt);

        Console.WriteLine(string.Format("[GreenBoardTextShot] {0}/{1} bounds={2} dist={3:F3} normal={4}",
            prefabPath, childName, b.size, dist, normal));

        UnityEngine.Object.DestroyImmediate(go);
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
