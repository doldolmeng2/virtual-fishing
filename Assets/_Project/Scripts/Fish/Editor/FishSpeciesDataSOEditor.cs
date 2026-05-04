#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VirtualFishing.Data;

namespace VirtualFishing.EditorTools
{
    [CustomEditor(typeof(FishSpeciesDataSO))]
    public class FishSpeciesDataSOEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Fish Asset Setup", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(Selection.activeGameObject == null))
            {
                if (GUILayout.Button("Assign Selected GameObject As Fish Prefab"))
                {
                    SerializedObject serializedObject = new(target);
                    SerializedProperty fishPrefabProperty = serializedObject.FindProperty("fishPrefab");
                    fishPrefabProperty.objectReferenceValue = Selection.activeGameObject;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                }
            }

            if (Selection.activeGameObject == null)
            {
                EditorGUILayout.HelpBox("Select a fish prefab in the Project window, then press the button to assign it to fishPrefab.", MessageType.Info);
            }

            FishSpeciesDataSO speciesData = (FishSpeciesDataSO)target;
            if (speciesData.FishPrefab != null && GUILayout.Button("Ping Current Fish Prefab"))
            {
                EditorGUIUtility.PingObject(speciesData.FishPrefab);
            }
        }
    }
}
#endif
