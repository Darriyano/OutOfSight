using Game.Interaction;
using UnityEditor;
using UnityEngine;

namespace Game.Interaction.Editor
{
    public static class ElevatorPlaceholderPrefabGenerator
    {
        private const string RootFolder = "Assets/Prefabs";
        private const string ElevatorFolder = "Assets/Prefabs/Elevator";
        private const string ElevatorDoorsPath = ElevatorFolder + "/PF_ElevatorDoors.prefab";
        private const string ElevatorControlPanelPath = ElevatorFolder + "/PF_ElevatorControlPanel.prefab";
        private const string ElevatorButtonPath = ElevatorFolder + "/PF_ElevatorButton.prefab";
        private const string ElevatorFusePath = ElevatorFolder + "/PF_ElevatorFuse.prefab";
        private const string ElectricalTapePath = ElevatorFolder + "/PF_ElectricalTape.prefab";
        private const string FuseBoxPath = ElevatorFolder + "/PF_FuseBox.prefab";
        private const string FuseBoxCableRunPath = ElevatorFolder + "/PF_FuseBoxCableRun.prefab";

        [InitializeOnLoadMethod]
        private static void GenerateMissingPrefabsOnLoad()
        {
            EditorApplication.delayCall += EnsureMissingPrefabs;
        }

        [MenuItem("Tools/OutOfSight/Generate Elevator Placeholder Prefabs")]
        private static void ForceGeneratePrefabs()
        {
            GeneratePrefabs(forceRegenerate: true);
        }

        public static void GenerateElevatorPlaceholderPrefabsFromCommandLine()
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
            EnsureFolder(ElevatorFolder);

            SavePrefabIfNeeded(ElevatorDoorsPath, CreateElevatorDoorsPrefabRoot, forceRegenerate);
            SavePrefabIfNeeded(ElevatorControlPanelPath, CreateElevatorControlPanelPrefabRoot, forceRegenerate);
            SavePrefabIfNeeded(ElevatorButtonPath, CreateElevatorButtonPrefabRoot, forceRegenerate);
            SavePrefabIfNeeded(ElevatorFusePath, CreateElevatorFusePrefabRoot, forceRegenerate);
            SavePrefabIfNeeded(ElectricalTapePath, CreateElectricalTapePrefabRoot, forceRegenerate);
            SavePrefabIfNeeded(FuseBoxPath, CreateFuseBoxPrefabRoot, forceRegenerate);
            SavePrefabIfNeeded(FuseBoxCableRunPath, CreateFuseBoxCableRunPrefabRoot, forceRegenerate);

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

        private static GameObject CreateElevatorDoorsPrefabRoot()
        {
            GameObject root = new GameObject("PF_ElevatorDoors");

            Material frameMaterial = LoadMaterial("Assets/Graphics/Materials/Gray.mat");
            Material panelMaterial = LoadMaterial("Assets/Graphics/Materials/White.mat");
            Material accentMaterial = LoadMaterial("Assets/Graphics/Materials/Blue.mat");

            Transform frameRoot = CreateEmptyChild("Frame", root.transform);
            CreateBoxChild("FrameLeft", frameRoot, new Vector3(-0.95f, 1.15f, 0f), new Vector3(0.18f, 2.3f, 0.22f), frameMaterial, false);
            CreateBoxChild("FrameRight", frameRoot, new Vector3(0.95f, 1.15f, 0f), new Vector3(0.18f, 2.3f, 0.22f), frameMaterial, false);
            CreateBoxChild("FrameTop", frameRoot, new Vector3(0f, 2.22f, 0f), new Vector3(2.08f, 0.18f, 0.22f), frameMaterial, false);
            CreateBoxChild("CallPanel", frameRoot, new Vector3(1.28f, 1.25f, 0.02f), new Vector3(0.22f, 0.55f, 0.08f), accentMaterial, false);

            GameObject leftPanel = CreateBoxChild("LeftPanel", root.transform, new Vector3(-0.43f, 1.05f, 0f), new Vector3(0.82f, 2.05f, 0.12f), panelMaterial, true);
            GameObject rightPanel = CreateBoxChild("RightPanel", root.transform, new Vector3(0.43f, 1.05f, 0f), new Vector3(0.82f, 2.05f, 0.12f), panelMaterial, true);

            CreateBoxChild("LeftStripe", leftPanel.transform, new Vector3(0f, 0.72f, 0.07f), new Vector3(0.64f, 0.08f, 0.01f), accentMaterial, false);
            CreateBoxChild("RightStripe", rightPanel.transform, new Vector3(0f, 0.72f, 0.07f), new Vector3(0.64f, 0.08f, 0.01f), accentMaterial, false);

            SlidingDoorInteractable interactable = root.AddComponent<SlidingDoorInteractable>();
            SerializedObject serializedObject = new SerializedObject(interactable);
            serializedObject.FindProperty("leftPanel").objectReferenceValue = leftPanel.transform;
            serializedObject.FindProperty("rightPanel").objectReferenceValue = rightPanel.transform;
            serializedObject.FindProperty("leftOpenOffset").vector3Value = new Vector3(-0.72f, 0f, 0f);
            serializedObject.FindProperty("rightOpenOffset").vector3Value = new Vector3(0.72f, 0f, 0f);
            serializedObject.FindProperty("speed").floatValue = 4.5f;
            serializedObject.FindProperty("startOpen").boolValue = false;
            serializedObject.FindProperty("openPrompt").stringValue = "Open elevator";
            serializedObject.FindProperty("closePrompt").stringValue = "Close elevator";
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static GameObject CreateElevatorControlPanelPrefabRoot()
        {
            GameObject root = new GameObject("PF_ElevatorControlPanel");

            Material bodyMaterial = LoadMaterial("Assets/Graphics/Materials/Gray.mat");
            Material accentMaterial = LoadMaterial("Assets/Graphics/Materials/Blue.mat");
            Material buttonMaterial = LoadMaterial("Assets/Graphics/Materials/Red.mat");
            Material indicatorMaterial = LoadMaterial("Assets/Graphics/Materials/White.mat");

            CreateBoxChild("PanelBody", root.transform, new Vector3(0f, 0.42f, 0f), new Vector3(0.55f, 0.84f, 0.12f), bodyMaterial, false);
            CreateBoxChild("PanelInset", root.transform, new Vector3(0f, 0.42f, 0.04f), new Vector3(0.34f, 0.52f, 0.03f), accentMaterial, false);

            GameObject indicatorOff = CreateCylinderChild("IndicatorOff", root.transform, new Vector3(0f, 0.74f, 0.07f), new Vector3(0.08f, 0.01f, 0.08f), bodyMaterial, false);
            GameObject indicatorOn = CreateCylinderChild("IndicatorOn", root.transform, new Vector3(0f, 0.74f, 0.071f), new Vector3(0.09f, 0.012f, 0.09f), indicatorMaterial, false);
            indicatorOn.SetActive(false);

            GameObject slotRoot = new GameObject("ButtonSlot");
            slotRoot.transform.SetParent(root.transform, false);
            slotRoot.transform.localPosition = new Vector3(0f, 0.32f, 0.06f);
            BoxCollider slotCollider = slotRoot.AddComponent<BoxCollider>();
            slotCollider.size = new Vector3(0.22f, 0.18f, 0.12f);

            GameObject emptyVisual = CreateBoxChild("EmptySocketVisual", slotRoot.transform, Vector3.zero, new Vector3(0.22f, 0.1f, 0.08f), bodyMaterial, false);
            GameObject installedButton = CreateBoxChild("InstalledButton", slotRoot.transform, new Vector3(0f, 0f, 0.015f), new Vector3(0.16f, 0.06f, 0.16f), buttonMaterial, false);
            installedButton.SetActive(false);

            ItemSocketGroup socketGroup = root.AddComponent<ItemSocketGroup>();
            InventoryItemSocketInteractable buttonSocket = ConfigureSocket(
                slotRoot,
                "ElevatorButton",
                "elevator button",
                1,
                "Insert {0}",
                "Button installed",
                installedButton,
                emptyVisual,
                socketGroup);

            ConfigureSocketGroup(socketGroup, new Object[] { buttonSocket }, new Object[] { indicatorOn }, new Object[] { indicatorOff });
            return root;
        }

        private static GameObject CreateElevatorButtonPrefabRoot()
        {
            GameObject root = CreatePickupRoot(
                "PF_ElevatorButton",
                PrimitiveType.Cube,
                new Vector3(0.2f, 0.08f, 0.2f),
                LoadMaterial("Assets/Graphics/Materials/Red.mat"));

            CreateBoxChild("Backplate", root.transform, new Vector3(0f, -0.05f, 0f), new Vector3(0.28f, 0.04f, 0.28f), LoadMaterial("Assets/Graphics/Materials/Gray.mat"), false);
            ConfigurePickup(root, "ElevatorButton", "Elevator button", "Pick up", false);
            return root;
        }

        private static GameObject CreateElevatorFusePrefabRoot()
        {
            GameObject root = CreatePickupRoot(
                "PF_ElevatorFuse",
                PrimitiveType.Cylinder,
                new Vector3(0.12f, 0.22f, 0.12f),
                LoadMaterial("Assets/Graphics/Materials/Blue.mat"));

            CreateCylinderChild("TopCap", root.transform, new Vector3(0f, 0.19f, 0f), new Vector3(0.09f, 0.03f, 0.09f), LoadMaterial("Assets/Graphics/Materials/Gray.mat"), false);
            CreateCylinderChild("BottomCap", root.transform, new Vector3(0f, -0.19f, 0f), new Vector3(0.09f, 0.03f, 0.09f), LoadMaterial("Assets/Graphics/Materials/Gray.mat"), false);
            ConfigurePickup(root, "ElevatorFuse", "Fuse", "Pick up", true);
            return root;
        }

        private static GameObject CreateElectricalTapePrefabRoot()
        {
            GameObject root = CreatePickupRoot(
                "PF_ElectricalTape",
                PrimitiveType.Cylinder,
                new Vector3(0.22f, 0.08f, 0.22f),
                LoadMaterial("Assets/Graphics/Materials/Black.mat"));

            CreateCylinderChild("InnerCore", root.transform, Vector3.zero, new Vector3(0.12f, 0.081f, 0.12f), LoadMaterial("Assets/Graphics/Materials/Brown.mat"), false);
            ConfigurePickup(root, "ElectricalTape", "Electrical tape", "Pick up", false);
            return root;
        }

        private static GameObject CreateFuseBoxPrefabRoot()
        {
            GameObject root = new GameObject("PF_FuseBox");

            Material boxMaterial = LoadMaterial("Assets/Graphics/Materials/Gray.mat");
            Material accentMaterial = LoadMaterial("Assets/Graphics/Materials/Blue.mat");
            Material fuseMaterial = LoadMaterial("Assets/Graphics/Materials/Blue.mat");
            Material tapeMaterial = LoadMaterial("Assets/Graphics/Materials/Black.mat");
            Material wireMaterial = LoadMaterial("Assets/Graphics/Materials/Brown.mat");
            Material readyMaterial = LoadMaterial("Assets/Graphics/Materials/White.mat");

            CreateBoxChild("FuseBoxBody", root.transform, new Vector3(0f, 0.85f, 0f), new Vector3(1.1f, 1.45f, 0.16f), boxMaterial, false);
            CreateBoxChild("FuseBoxInset", root.transform, new Vector3(0f, 0.85f, 0.05f), new Vector3(0.86f, 1.16f, 0.03f), accentMaterial, false);

            GameObject readyLightOff = CreateCylinderChild("ReadyLightOff", root.transform, new Vector3(0f, 1.47f, 0.07f), new Vector3(0.09f, 0.012f, 0.09f), boxMaterial, false);
            GameObject readyLightOn = CreateCylinderChild("ReadyLightOn", root.transform, new Vector3(0f, 1.47f, 0.071f), new Vector3(0.1f, 0.014f, 0.1f), readyMaterial, false);
            readyLightOn.SetActive(false);

            InventoryItemSocketInteractable[] fuseSockets = new InventoryItemSocketInteractable[4];
            Vector3[] fusePositions =
            {
                new Vector3(-0.24f, 1.1f, 0.06f),
                new Vector3(0.24f, 1.1f, 0.06f),
                new Vector3(-0.24f, 0.58f, 0.06f),
                new Vector3(0.24f, 0.58f, 0.06f)
            };

            for (int i = 0; i < fusePositions.Length; i++)
            {
                GameObject slotRoot = new GameObject($"FuseSlot_{i + 1}");
                slotRoot.transform.SetParent(root.transform, false);
                slotRoot.transform.localPosition = fusePositions[i];

                BoxCollider slotCollider = slotRoot.AddComponent<BoxCollider>();
                slotCollider.size = new Vector3(0.22f, 0.32f, 0.14f);

                GameObject emptySocket = CreateBoxChild("EmptySocketVisual", slotRoot.transform, Vector3.zero, new Vector3(0.18f, 0.28f, 0.08f), boxMaterial, false);
                GameObject installedFuse = CreateCylinderChild("InstalledFuse", slotRoot.transform, Vector3.zero, new Vector3(0.08f, 0.16f, 0.08f), fuseMaterial, false);
                installedFuse.SetActive(false);

                fuseSockets[i] = ConfigureSocket(
                    slotRoot,
                    "ElevatorFuse",
                    "fuse",
                    1,
                    "Insert {0}",
                    "Fuse installed",
                    installedFuse,
                    emptySocket,
                    null);
            }

            Transform cableAttachPoint = CreateEmptyChild("CableAttachPoint", root.transform);
            cableAttachPoint.localPosition = new Vector3(0.73f, 0.55f, 0f);

            GameObject wireLeft = CreateCylinderChild("BrokenWireLeft", root.transform, new Vector3(0.58f, 0.26f, 0.03f), new Vector3(0.02f, 0.14f, 0.02f), wireMaterial, false);
            wireLeft.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            GameObject wireRight = CreateCylinderChild("BrokenWireRight", root.transform, new Vector3(0.76f, 0.26f, 0.03f), new Vector3(0.02f, 0.1f, 0.02f), wireMaterial, false);
            wireRight.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            GameObject tapeSlotRoot = new GameObject("TapeSocket");
            tapeSlotRoot.transform.SetParent(root.transform, false);
            tapeSlotRoot.transform.localPosition = new Vector3(0.67f, 0.26f, 0.05f);
            BoxCollider tapeCollider = tapeSlotRoot.AddComponent<BoxCollider>();
            tapeCollider.size = new Vector3(0.28f, 0.18f, 0.14f);

            GameObject brokenGapVisual = CreateBoxChild("BrokenGapVisual", tapeSlotRoot.transform, Vector3.zero, new Vector3(0.18f, 0.08f, 0.08f), wireMaterial, false);
            GameObject wrappedTapeVisual = CreateCylinderChild("WrappedTapeVisual", tapeSlotRoot.transform, Vector3.zero, new Vector3(0.06f, 0.08f, 0.06f), tapeMaterial, false);
            wrappedTapeVisual.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            wrappedTapeVisual.SetActive(false);

            ItemSocketGroup socketGroup = root.AddComponent<ItemSocketGroup>();
            for (int i = 0; i < fuseSockets.Length; i++)
                AssignSocketGroup(fuseSockets[i], socketGroup);

            InventoryItemSocketInteractable tapeSocket = ConfigureSocket(
                tapeSlotRoot,
                "ElectricalTape",
                "electrical tape",
                1,
                "Wrap with {0}",
                "Wire wrapped",
                wrappedTapeVisual,
                brokenGapVisual,
                socketGroup);

            Object[] allSockets = new Object[fuseSockets.Length + 1];
            for (int i = 0; i < fuseSockets.Length; i++)
                allSockets[i] = fuseSockets[i];
            allSockets[allSockets.Length - 1] = tapeSocket;

            ConfigureSocketGroup(socketGroup, allSockets, new Object[] { readyLightOn }, new Object[] { readyLightOff });
            return root;
        }

        private static GameObject CreateFuseBoxCableRunPrefabRoot()
        {
            GameObject root = new GameObject("PF_FuseBoxCableRun");

            Material pipeMaterial = LoadMaterial("Assets/Graphics/Materials/Gray.mat");
            Material wireMaterial = LoadMaterial("Assets/Graphics/Materials/Brown.mat");

            Transform fuseBoxAttachPoint = CreateEmptyChild("FuseBoxAttachPoint", root.transform);
            fuseBoxAttachPoint.localPosition = new Vector3(-0.42f, 1.08f, 0f);

            GameObject conduitPipe = CreateCylinderChild("ConduitPipe", root.transform, new Vector3(0.42f, 1.6f, 0f), new Vector3(0.12f, 1.6f, 0.12f), pipeMaterial, false);
            GameObject horizontalWire = CreateCylinderChild("HorizontalWire", root.transform, new Vector3(0f, 1.08f, 0f), new Vector3(0.03f, 0.42f, 0.03f), wireMaterial, false);
            horizontalWire.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            GameObject verticalWire = CreateCylinderChild("VerticalWire", root.transform, new Vector3(0.42f, 1.6f, 0f), new Vector3(0.03f, 1.52f, 0.03f), wireMaterial, false);

            CreateBoxChild("PipeBaseClamp", root.transform, new Vector3(0.42f, 0.08f, 0f), new Vector3(0.24f, 0.08f, 0.24f), pipeMaterial, false);
            CreateBoxChild("PipeTopClamp", root.transform, new Vector3(0.42f, 3.12f, 0f), new Vector3(0.24f, 0.08f, 0.24f), pipeMaterial, false);

            return root;
        }

        private static GameObject CreatePickupRoot(string name, PrimitiveType primitiveType, Vector3 localScale, Material material)
        {
            GameObject root = GameObject.CreatePrimitive(primitiveType);
            root.name = name;
            root.transform.localScale = localScale;

            MeshRenderer renderer = root.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;

            return root;
        }

        private static void ConfigurePickup(GameObject root, string itemId, string displayName, string pickupPrompt, bool allowDuplicatePickup)
        {
            PickupItemInteractable pickup = root.AddComponent<PickupItemInteractable>();
            SerializedObject serializedObject = new SerializedObject(pickup);
            serializedObject.FindProperty("itemId").stringValue = itemId;
            serializedObject.FindProperty("itemDisplayName").stringValue = displayName;
            serializedObject.FindProperty("pickupPrompt").stringValue = pickupPrompt;
            serializedObject.FindProperty("allowDuplicatePickup").boolValue = allowDuplicatePickup;
            serializedObject.FindProperty("destroyAfterPickup").boolValue = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static InventoryItemSocketInteractable ConfigureSocket(
            GameObject slotRoot,
            string requiredItemId,
            string requiredDisplayName,
            int requiredItemCount,
            string insertPromptFormat,
            string filledPrompt,
            GameObject showWhenFilled,
            GameObject hideWhenFilled,
            ItemSocketGroup socketGroup)
        {
            InventoryItemSocketInteractable socket = slotRoot.AddComponent<InventoryItemSocketInteractable>();
            SerializedObject serializedObject = new SerializedObject(socket);

            serializedObject.FindProperty("requiredItemId").stringValue = requiredItemId;
            serializedObject.FindProperty("requiredDisplayName").stringValue = requiredDisplayName;
            serializedObject.FindProperty("requiredItemCount").intValue = requiredItemCount;
            serializedObject.FindProperty("consumeItemOnInsert").boolValue = true;
            serializedObject.FindProperty("socketGroup").objectReferenceValue = socketGroup;
            serializedObject.FindProperty("insertPromptFormat").stringValue = insertPromptFormat;
            serializedObject.FindProperty("filledPrompt").stringValue = filledPrompt;
            serializedObject.FindProperty("interactionCollider").objectReferenceValue = slotRoot.GetComponent<Collider>();

            SetObjectReferenceArray(serializedObject.FindProperty("showWhenFilled"), new Object[] { showWhenFilled });
            SetObjectReferenceArray(serializedObject.FindProperty("hideWhenFilled"), new Object[] { hideWhenFilled });
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return socket;
        }

        private static void AssignSocketGroup(InventoryItemSocketInteractable socket, ItemSocketGroup socketGroup)
        {
            if (socket == null || socketGroup == null)
                return;

            SerializedObject serializedObject = new SerializedObject(socket);
            serializedObject.FindProperty("socketGroup").objectReferenceValue = socketGroup;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSocketGroup(ItemSocketGroup group, Object[] sockets, Object[] showWhenCompleted, Object[] hideWhenCompleted)
        {
            SerializedObject serializedObject = new SerializedObject(group);
            SetObjectReferenceArray(serializedObject.FindProperty("sockets"), sockets);
            SetObjectReferenceArray(serializedObject.FindProperty("showWhenCompleted"), showWhenCompleted);
            SetObjectReferenceArray(serializedObject.FindProperty("hideWhenCompleted"), hideWhenCompleted);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectReferenceArray(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static Transform CreateEmptyChild(string name, Transform parent)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static GameObject CreateBoxChild(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, bool keepCollider)
        {
            GameObject child = GameObject.CreatePrimitive(PrimitiveType.Cube);
            child.name = name;
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;

            MeshRenderer renderer = child.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;

            if (!keepCollider)
                Object.DestroyImmediate(child.GetComponent<Collider>());

            return child;
        }

        private static GameObject CreateCylinderChild(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, bool keepCollider)
        {
            GameObject child = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            child.name = name;
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;

            MeshRenderer renderer = child.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;

            if (!keepCollider)
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
