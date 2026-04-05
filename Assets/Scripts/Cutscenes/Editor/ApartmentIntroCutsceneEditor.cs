using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(ApartmentIntroCutscene))]
public class ApartmentIntroCutsceneEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Timeline Tools", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Rebuilds a Timeline asset from the current intro points, timings, sounds, and dialogue cues.", MessageType.Info);

        ApartmentIntroCutscene cutscene = (ApartmentIntroCutscene)target;

        if (GUILayout.Button("Rebuild Timeline From Current Setup"))
        {
            cutscene.RebuildTimelineFromCurrentSetup();
            Object timelineAsset = cutscene.GetTimelineAssetForEditor();
            if (timelineAsset != null)
                Selection.activeObject = timelineAsset;
        }

        if (GUILayout.Button("Select Timeline Asset"))
        {
            Object timelineAsset = cutscene.GetTimelineAssetForEditor();
            if (timelineAsset != null)
                Selection.activeObject = timelineAsset;
        }

        if (GUILayout.Button("Open Timeline Window"))
        {
            EditorApplication.ExecuteMenuItem("Window/Sequencing/Timeline");
        }
    }
}

public static class ApartmentIntroCutsceneBatchTools
{
    public static void RebuildLocationIntroTimeline()
    {
        const string scenePath = "Assets/Scenes/Location.unity";

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        ApartmentIntroCutscene cutscene = Object.FindFirstObjectByType<ApartmentIntroCutscene>(FindObjectsInactive.Include);

        if (cutscene == null)
            throw new System.InvalidOperationException($"ApartmentIntroCutscene was not found in scene '{scenePath}'.");

        cutscene.RebuildTimelineFromCurrentSetup();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
