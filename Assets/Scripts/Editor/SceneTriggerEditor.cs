using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(SceneTrigger))]
public class SceneTriggerEditor : Editor
{
    private SerializedProperty requireTagProperty;
    private SerializedProperty requiredTagProperty;
    private SerializedProperty sceneNameProperty;

    private void OnEnable()
    {
        requireTagProperty = serializedObject.FindProperty("requireTag");
        requiredTagProperty = serializedObject.FindProperty("requiredTag");
        sceneNameProperty = serializedObject.FindProperty("sceneName");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(requireTagProperty);
        if (requireTagProperty.boolValue)
        {
            EditorGUILayout.PropertyField(requiredTagProperty);
        }

        DrawScenePopup();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScenePopup()
    {
        List<string> sceneNames = new List<string>();
        List<string> scenePaths = new List<string>();

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled)
            {
                continue;
            }

            string scenePath = scene.path;
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            sceneNames.Add(sceneName);
            scenePaths.Add(scenePath);
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);

        if (sceneNames.Count == 0)
        {
            EditorGUILayout.HelpBox("No hay escenas habilitadas en Build Settings.", MessageType.Warning);
            sceneNameProperty.stringValue = string.Empty;
            return;
        }

        int currentIndex = sceneNames.IndexOf(sceneNameProperty.stringValue);
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        int selectedIndex = EditorGUILayout.Popup("Scene", currentIndex, sceneNames.ToArray());
        if (selectedIndex >= 0 && selectedIndex < sceneNames.Count)
        {
            sceneNameProperty.stringValue = sceneNames[selectedIndex];
            EditorGUILayout.LabelField("Build Path", scenePaths[selectedIndex]);
        }
    }
}