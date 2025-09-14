using UnityEditor;
using UnityEngine;

public class ReplaceAllMaterialsEditor : EditorWindow
{
    private Material newMaterial;

    [MenuItem("Tools/Replace All Materials In Scene")]
    public static void ShowWindow()
    {
        GetWindow<ReplaceAllMaterialsEditor>("Replace Materials");
    }

    private void OnGUI()
    {
        GUILayout.Label("Material Replacer", EditorStyles.boldLabel);
        newMaterial = (Material)EditorGUILayout.ObjectField("Target Material", newMaterial, typeof(Material), false);

        if (GUILayout.Button("Replace All Materials in Scene"))
        {
            ReplaceMaterials();
        }
    }

    private void ReplaceMaterials()
    {
        if (newMaterial == null)
        {
            Debug.LogWarning("No material selected.");
            return;
        }

        Renderer[] renderers = FindObjectsOfType<Renderer>(true); // true: include inactive objects
        int count = 0;

        foreach (Renderer rend in renderers)
        {
            Undo.RecordObject(rend, "Replace Material");
            rend.sharedMaterial = newMaterial;
            count++;
        }

        Debug.Log($"Replaced material in {count} renderer(s).");
    }
}
