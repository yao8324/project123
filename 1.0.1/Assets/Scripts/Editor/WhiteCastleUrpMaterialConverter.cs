using UnityEditor;
using UnityEngine;

/// <summary>
/// Converts the legacy Amplify/Built-In materials shipped with _WhiteCastle to URP/Lit.
/// The source package has no URP variant, so its custom Surface Shaders cannot be
/// converted by Unity's Render Pipeline Converter.
/// </summary>
public static class WhiteCastleUrpMaterialConverter
{
    private const string MaterialFolder = "Assets/_WhiteCastle/Materials";

    [MenuItem("Tools/Virtual Environment/Convert WhiteCastle Materials to URP")]
    private static void ConvertMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            EditorUtility.DisplayDialog(
                "URP Lit shader unavailable",
                "Universal Render Pipeline/Lit was not found. Install and enable URP before converting.",
                "OK");
            return;
        }

        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { MaterialFolder });
        int converted = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || material.shader == urpLit)
                {
                    continue;
                }

                Texture albedo = GetTexture(material, "_Albedo");
                Texture normal = GetTexture(material, "_NormalMap");
                Texture metallic = GetTexture(material, "_MetallicMap");
                Texture emission = GetTexture(material, "_EmissionMap");
                Color baseColor = GetColor(material, "_AlbedoColor", Color.white);
                Color emissionColor = GetColor(material, "_EmissionColor", Color.black);
                float metallicValue = GetFloat(material, "_Metallic", 0f);
                float smoothness = GetFloat(material, "_Snoothness", 0.5f);
                Vector2 tiling = GetScale(material, "_Albedo");
                Vector2 offset = GetOffset(material, "_Albedo");

                Undo.RecordObject(material, "Convert WhiteCastle Material to URP");
                material.shader = urpLit;
                material.SetColor("_BaseColor", baseColor);
                material.SetFloat("_Metallic", metallicValue);
                material.SetFloat("_Smoothness", smoothness);
                material.SetTexture("_BaseMap", albedo);
                material.SetTextureScale("_BaseMap", tiling);
                material.SetTextureOffset("_BaseMap", offset);
                material.SetTexture("_BumpMap", normal);
                material.SetTexture("_MetallicGlossMap", metallic);
                material.SetTexture("_EmissionMap", emission);
                material.SetColor("_EmissionColor", emissionColor);

                SetKeyword(material, "_NORMALMAP", normal != null);
                SetKeyword(material, "_METALLICSPECGLOSSMAP", metallic != null);
                SetKeyword(material, "_EMISSION", emission != null && emissionColor.maxColorComponent > 0f);
                EditorUtility.SetDirty(material);
                converted++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("WhiteCastle conversion complete", $"Converted {converted} materials to URP/Lit.", "OK");
    }

    private static Texture GetTexture(Material material, string property) =>
        material.HasProperty(property) ? material.GetTexture(property) : null;

    private static Color GetColor(Material material, string property, Color fallback) =>
        material.HasProperty(property) ? material.GetColor(property) : fallback;

    private static float GetFloat(Material material, string property, float fallback) =>
        material.HasProperty(property) ? material.GetFloat(property) : fallback;

    private static Vector2 GetScale(Material material, string property) =>
        material.HasProperty(property) ? material.GetTextureScale(property) : Vector2.one;

    private static Vector2 GetOffset(Material material, string property) =>
        material.HasProperty(property) ? material.GetTextureOffset(property) : Vector2.zero;

    private static void SetKeyword(Material material, string keyword, bool enabled)
    {
        if (enabled)
        {
            material.EnableKeyword(keyword);
        }
        else
        {
            material.DisableKeyword(keyword);
        }
    }
}
