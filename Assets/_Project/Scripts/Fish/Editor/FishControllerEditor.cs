#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VirtualFishing.Core.Fish;

namespace VirtualFishing.EditorTools
{
    [CustomEditor(typeof(FishController))]
    public class FishControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            FishController fishController = (FishController)target;

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Inspector Test", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. FishSpawner에서 먼저 bite를 시작하거나 즉시 bite를 발생시킨 뒤, 2. 여기서 원하는 phase를 적용해 테스트하세요.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Apply Selected Phase"))
                {
                    fishController.ApplyInspectorDebugPhase();
                }

                if (GUILayout.Button("Trigger Random Move Mode"))
                {
                    fishController.TriggerRandomMoveMode();
                }

                if (GUILayout.Button("Preview Reeling Pull"))
                {
                    fishController.PreviewReelingPull();
                }

                if (GUILayout.Button("Preview Reeling Pull 100%"))
                {
                    fishController.PreviewReelingPull(100f);
                }

                if (GUILayout.Button("Preview Hook Success"))
                {
                    fishController.PreviewHookSuccess();
                }

                if (GUILayout.Button("Try Start MiniGame"))
                {
                    fishController.TryStartMiniGame();
                }

                if (GUILayout.Button("Simulate Line Break"))
                {
                    fishController.SimulateLineBreak();
                }

                if (GUILayout.Button("Simulate Fish Escape"))
                {
                    fishController.SimulateFishEscape();
                }

                if (GUILayout.Button("Reset Fish"))
                {
                    fishController.ResetFish();
                }
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Play 모드에서만 phase 적용 버튼이 동작합니다.", MessageType.None);
            }
        }
    }
}
#endif
