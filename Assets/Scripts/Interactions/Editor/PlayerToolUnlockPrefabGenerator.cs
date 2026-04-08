using UnityEditor;
using UnityEngine;

namespace Game.Interaction.Editor
{
    public static class PlayerToolUnlockPrefabGenerator
    {
        private const string RootFolder = "Assets/Prefabs";
        private const string ToolsFolder = "Assets/Prefabs/PlayerTools";
        private const string GenericToolPickupPath = ToolsFolder + "/PF_PlayerToolUnlockPickup.prefab";

        [InitializeOnLoadMethod]
        private static void GenerateMissingPrefabsOnLoad()
        {
            EditorApplication.delayCall += EnsureMissingPrefabs;
        }

        [MenuItem("Tools/OutOfSight/Generate Player Tool Unlock Prefab")]
        private static void ForceGeneratePrefabs()
        {
            GeneratePrefabs(forceRegenerate: true);
        }

        private static void EnsureMissingPrefabs()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            GeneratePrefabs(forceRegenerate: false);
        }

        private static void GeneratePrefabs(bool forceRegenerate)
        {
            EnsureFolder(RootFolder);
            EnsureFolder(ToolsFolder);

            SavePrefabIfNeeded(GenericToolPickupPath, CreateGenericToolPickupPrefabRoot, forceRegenerate);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void SavePrefabIfNeeded(string prefabPath, System.Func<GameObject> factory, bool forceRegenerate)
        {
            if (!forceRegenerate && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                return;

            GameObject root = factory();
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
        }

        private static GameObject CreateGenericToolPickupPrefabRoot()
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = "PF_PlayerToolUnlockPickup";
            root.transform.localScale = new Vector3(0.32f, 0.14f, 0.32f);

            Material bodyMaterial = LoadMaterial("Assets/Graphics/Materials/Blue.mat");
            Material accentMaterial = LoadMaterial("Assets/Graphics/Materials/White.mat");

            MeshRenderer renderer = root.GetComponent<MeshRenderer>();
            if (renderer != null && bodyMaterial != null)
                renderer.sharedMaterial = bodyMaterial;

            CreateCylinderChild("TopRing", root.transform, new Vector3(0f, 0.11f, 0f), new Vector3(0.22f, 0.025f, 0.22f), accentMaterial);
            CreateCylinderChild("BottomRing", root.transform, new Vector3(0f, -0.11f, 0f), new Vector3(0.22f, 0.025f, 0.22f), accentMaterial);
            CreateBoxChild("Handle", root.transform, new Vector3(0f, 0.24f, 0f), new Vector3(0.08f, 0.08f, 0.08f), accentMaterial);

            PlayerToolUnlockInteractable pickup = root.AddComponent<PlayerToolUnlockInteractable>();
            SerializedObject serializedObject = new SerializedObject(pickup);
            serializedObject.FindProperty("pickupPrompt").stringValue = "Pick up";
            serializedObject.FindProperty("toolDisplayName").stringValue = "tool";
            serializedObject.FindProperty("queueDialogueIfBusy").boolValue = true;
            serializedObject.FindProperty("destroyAfterPickup").boolValue = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static GameObject CreateCylinderChild(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject child = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            child.name = name;
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;

            MeshRenderer renderer = child.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;

            Object.DestroyImmediate(child.GetComponent<Collider>());
            return child;
        }

        private static GameObject CreateBoxChild(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject child = GameObject.CreatePrimitive(PrimitiveType.Cube);
            child.name = name;
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;

            MeshRenderer renderer = child.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;

            Object.DestroyImmediate(child.GetComponent<Collider>());
            return child;
        }

        private static Material LoadMaterial(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return;

            string parentPath = System.IO.Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            string folderName = System.IO.Path.GetFileName(assetPath);

            if (!string.IsNullOrEmpty(parentPath) && !AssetDatabase.IsValidFolder(parentPath))
                EnsureFolder(parentPath);

            if (!string.IsNullOrEmpty(parentPath))
                AssetDatabase.CreateFolder(parentPath, folderName);
        }
    }
}
