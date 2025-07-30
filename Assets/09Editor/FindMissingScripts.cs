using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FindMissingScripts : EditorWindow
{
    [MenuItem("Tools/Find Missing Scripts")]
    public static void ShowWindow()
    {
        GetWindow(typeof(FindMissingScripts));
    }

    public void OnGUI()
    {
        if (GUILayout.Button("Find Missing Scripts in Scene"))
        {
            FindInScene();
        }
    }

    private static void FindInScene()
    {
        GameObject[] go = FindObjectsOfType<GameObject>();
        int goCount = 0;
        int componentsCount = 0;
        int missingCount = 0;

        foreach (GameObject g in go)
        {
            goCount++;
            Component[] components = g.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                componentsCount++;
                if (components[i] == null)
                {
                    missingCount++;
                    string s = g.name;
                    Transform t = g.transform;
                    while (t.parent != null)
                    {
                        s = t.parent.name + "/" + s;
                        t = t.parent;
                    }
                    Debug.Log(s + " has an empty script attached in position: " + i, g);
                }
            }
        }

        Debug.Log(string.Format("Searched {0} GameObjects, {1} components, found {2} missing", goCount, componentsCount, missingCount));
    }
}
