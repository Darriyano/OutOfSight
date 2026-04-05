using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

static class ImportedModelMaterialSync
{
    const string TargetShaderName = "Universal Render Pipeline/Unlit";
    const string OutputRoot = "Assets/Graphics/Materials/ImportedModels";

    static readonly string[] TargetFolders =
    {
        "Assets/Cutscenes",
        "Assets/Graphics/Models"
    };

    static ImportedModelMaterialSync()
    {
        EditorApplication.delayCall += EnsureSynchronizedOnLoad;
    }

    [MenuItem("Tools/OutOfSight/Refresh Imported Model Materials")]
    static void RefreshImportedModelMaterialsMenu()
    {
        RefreshImportedModelMaterials(true);
    }

    static void EnsureSynchronizedOnLoad()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
        {
            return;
        }

        RefreshImportedModelMaterials(false);
    }

    static void RefreshImportedModelMaterials(bool verbose)
    {
        Shader targetShader = Shader.Find(TargetShaderName);
        if (targetShader == null)
        {
            Debug.LogWarning("ImportedModelMaterialSync could not find URP Unlit shader.");
            return;
        }

        bool changedAnything = false;
        string[] modelGuids = AssetDatabase.FindAssets("t:Model", TargetFolders);

        foreach (string modelGuid in modelGuids)
        {
            string modelPath = AssetDatabase.GUIDToAssetPath(modelGuid);
            if (!modelPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            changedAnything |= SyncModelMaterials(modelPath, targetShader, verbose);
        }

        if (!changedAnything)
        {
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static bool SyncModelMaterials(string modelPath, Shader targetShader, bool verbose)
    {
        ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (importer == null)
        {
            return false;
        }

        Material[] embeddedMaterials = AssetDatabase.LoadAllAssetRepresentationsAtPath(modelPath)
            .OfType<Material>()
            .Where(material => material != null)
            .ToArray();

        if (embeddedMaterials.Length == 0)
        {
            return false;
        }

        string modelFolder = CombineAssetPath(OutputRoot, GetModelFolderName(modelPath));
        EnsureFolder(modelFolder);

        var generatedMaterials = new Dictionary<string, Material>(StringComparer.Ordinal);
        bool changedExternalMaterials = false;

        foreach (Material embeddedMaterial in embeddedMaterials)
        {
            string materialPath = CombineAssetPath(modelFolder, SanitizeName(embeddedMaterial.name) + ".mat");
            Material externalMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (externalMaterial == null)
            {
                externalMaterial = new Material(embeddedMaterial);
                ApplyUnlitPresentation(externalMaterial, embeddedMaterial, targetShader);
                AssetDatabase.CreateAsset(externalMaterial, materialPath);
                externalMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                changedExternalMaterials = true;
            }
            else if (ApplyUnlitPresentation(externalMaterial, embeddedMaterial, targetShader))
            {
                EditorUtility.SetDirty(externalMaterial);
                changedExternalMaterials = true;
            }

            if (externalMaterial != null)
            {
                generatedMaterials[embeddedMaterial.name] = externalMaterial;
            }
        }

        bool changedRemaps = false;
        var externalMap = importer.GetExternalObjectMap();

        foreach (Material embeddedMaterial in embeddedMaterials)
        {
            if (embeddedMaterial == null)
            {
                continue;
            }

            AssetImporter.SourceAssetIdentifier sourceMaterial =
                new AssetImporter.SourceAssetIdentifier(embeddedMaterial);

            if (!generatedMaterials.TryGetValue(embeddedMaterial.name, out Material externalMaterial) || externalMaterial == null)
            {
                continue;
            }

            Material currentMappedMaterial = null;
            foreach (KeyValuePair<AssetImporter.SourceAssetIdentifier, UnityEngine.Object> entry in externalMap)
            {
                if (entry.Key.type == typeof(Material) && entry.Key.name == sourceMaterial.name)
                {
                    currentMappedMaterial = entry.Value as Material;
                    break;
                }
            }

            if (currentMappedMaterial == externalMaterial)
            {
                continue;
            }

            importer.AddRemap(sourceMaterial, externalMaterial);
            changedRemaps = true;
        }

        if (changedRemaps)
        {
            if (verbose)
            {
                Debug.Log($"ImportedModelMaterialSync remapped materials for {modelPath}.");
            }

            importer.SaveAndReimport();
            return true;
        }

        if (changedExternalMaterials && verbose)
        {
            Debug.Log($"ImportedModelMaterialSync updated extracted materials for {modelPath}.");
        }

        return changedExternalMaterials;
    }

    static bool ApplyUnlitPresentation(Material targetMaterial, Material sourceMaterial, Shader targetShader)
    {
        bool changed = false;

        Color baseColor = sourceMaterial.HasProperty("_BaseColor")
            ? sourceMaterial.GetColor("_BaseColor")
            : sourceMaterial.HasProperty("_Color")
                ? sourceMaterial.GetColor("_Color")
                : Color.white;

        Texture baseMap = sourceMaterial.HasProperty("_BaseMap")
            ? sourceMaterial.GetTexture("_BaseMap")
            : sourceMaterial.HasProperty("_MainTex")
                ? sourceMaterial.GetTexture("_MainTex")
                : null;

        Vector2 scale = sourceMaterial.HasProperty("_BaseMap")
            ? sourceMaterial.GetTextureScale("_BaseMap")
            : sourceMaterial.HasProperty("_MainTex")
                ? sourceMaterial.GetTextureScale("_MainTex")
                : Vector2.one;

        Vector2 offset = sourceMaterial.HasProperty("_BaseMap")
            ? sourceMaterial.GetTextureOffset("_BaseMap")
            : sourceMaterial.HasProperty("_MainTex")
                ? sourceMaterial.GetTextureOffset("_MainTex")
                : Vector2.zero;

        bool transparent = baseColor.a < 0.999f || sourceMaterial.renderQueue >= 3000;

        if (targetMaterial.shader != targetShader)
        {
            targetMaterial.shader = targetShader;
            changed = true;
        }

        if (targetMaterial.HasProperty("_BaseColor") && targetMaterial.GetColor("_BaseColor") != baseColor)
        {
            targetMaterial.SetColor("_BaseColor", baseColor);
            changed = true;
        }

        if (targetMaterial.HasProperty("_Color") && targetMaterial.GetColor("_Color") != baseColor)
        {
            targetMaterial.SetColor("_Color", baseColor);
            changed = true;
        }

        if (targetMaterial.HasProperty("_BaseMap"))
        {
            if (targetMaterial.GetTexture("_BaseMap") != baseMap)
            {
                targetMaterial.SetTexture("_BaseMap", baseMap);
                changed = true;
            }

            if (targetMaterial.GetTextureScale("_BaseMap") != scale)
            {
                targetMaterial.SetTextureScale("_BaseMap", scale);
                changed = true;
            }

            if (targetMaterial.GetTextureOffset("_BaseMap") != offset)
            {
                targetMaterial.SetTextureOffset("_BaseMap", offset);
                changed = true;
            }
        }

        if (targetMaterial.HasProperty("_Surface"))
        {
            float targetSurface = transparent ? 1f : 0f;
            if (!Mathf.Approximately(targetMaterial.GetFloat("_Surface"), targetSurface))
            {
                targetMaterial.SetFloat("_Surface", targetSurface);
                changed = true;
            }
        }

        if (targetMaterial.HasProperty("_Blend"))
        {
            float targetBlend = 0f;
            if (!Mathf.Approximately(targetMaterial.GetFloat("_Blend"), targetBlend))
            {
                targetMaterial.SetFloat("_Blend", targetBlend);
                changed = true;
            }
        }

        if (targetMaterial.renderQueue != (transparent ? 3000 : -1))
        {
            targetMaterial.renderQueue = transparent ? 3000 : -1;
            changed = true;
        }

        string renderType = transparent ? "Transparent" : "Opaque";
        if (targetMaterial.GetTag("RenderType", false, string.Empty) != renderType)
        {
            targetMaterial.SetOverrideTag("RenderType", renderType);
            changed = true;
        }

        return changed;
    }

    static void EnsureFolder(string assetPath)
    {
        string[] parts = assetPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    static string CombineAssetPath(string left, string right)
    {
        return left.TrimEnd('/') + "/" + right.TrimStart('/');
    }

    static string SanitizeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unnamed";
        }

        char[] invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new char[value.Length];

        for (int i = 0; i < value.Length; i++)
        {
            sanitized[i] = invalidChars.Contains(value[i]) ? '_' : value[i];
        }

        return new string(sanitized).Replace('/', '_').Replace('\\', '_');
    }

    static string GetModelFolderName(string modelPath)
    {
        string relativePath = modelPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
            ? modelPath.Substring("Assets/".Length)
            : modelPath;

        string withoutExtension = Path.ChangeExtension(relativePath, null) ?? relativePath;
        return SanitizeName(withoutExtension.Replace('/', '_').Replace('\\', '_'));
    }
}
