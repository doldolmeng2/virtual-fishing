#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VirtualFishing.Core.Fish;

namespace VirtualFishing.EditorTools
{
    [CustomEditor(typeof(FishSpawner))]
    public class FishSpawnerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            FishSpawner fishSpawner = (FishSpawner)target;

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Inspector Test", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Start Bite Timer는 문서 흐름대로 대기 후 bite를 발생시키고, Force Bite Immediately는 선택 어종 또는 가중치 어종으로 즉시 bite를 발생시킵니다.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Start Bite Timer"))
                {
                    fishSpawner.StartBiteTimer();
                }

                if (GUILayout.Button("Force Bite Immediately"))
                {
                    fishSpawner.DebugForceBiteImmediately();
                }

                if (GUILayout.Button("Cancel Bite"))
                {
                    fishSpawner.CancelBite();
                }
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Play 모드에서만 bite 테스트 버튼이 동작합니다.", MessageType.None);
            }
        }
    }
}
#endif
