using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Registers Shilla_Culture(B) SDF as a Preloaded Asset so the WebGL build's
/// data container actually ships it (see live-repro investigation: the font
/// was reachable via the static scene reference at build time but the
/// container was missing from the final .data.unityweb payload).
///
/// Invoked as: Unity -batchmode -quit -nographics -projectPath <proj>
///             -executeMethod PreloadedFontFix.Apply
/// </summary>
public static class PreloadedFontFix
{
    private const string FontPath = "Assets/05Fonts/Shilla_Culture(B) SDF.asset";

    public static void Apply()
    {
        var font = AssetDatabase.LoadAssetAtPath<Object>(FontPath);
        if (font == null)
        {
            Debug.LogError("[PreloadedFontFix] Could not load asset at " + FontPath);
            return;
        }

        var preloaded = PlayerSettings.GetPreloadedAssets().ToList();
        if (!preloaded.Contains(font))
        {
            preloaded.Add(font);
            PlayerSettings.SetPreloadedAssets(preloaded.ToArray());
            AssetDatabase.SaveAssets();
            Debug.Log("[PreloadedFontFix] Added " + FontPath + " to Preloaded Assets.");
        }
        else
        {
            Debug.Log("[PreloadedFontFix] " + FontPath + " already present in Preloaded Assets.");
        }
    }
}
