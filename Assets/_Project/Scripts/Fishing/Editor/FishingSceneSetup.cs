using UnityEditor;
using UnityEngine;

namespace VirtualFishing.Fishing.Editor
{
    public static class FishingSceneSetup
    {
        [MenuItem("VirtualFishing/Setup Test Scene References")]
        public static void SetupReferences()
        {
            var gs = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/_Project/SO/Settings/GameSettings.asset");
            var pd = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/_Project/SO/Data/PlayerData.asset");
            var wl = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/_Project/SO/Events/OnWaterLanded.asset");
            var bo = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/_Project/SO/Events/OnBiteOccurred.asset");
            var rsc = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/_Project/SO/Events/OnRodStateChanged.asset");
            var rg = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/_Project/SO/Events/OnRodGrabbed.asset");
            var cs = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/_Project/SO/Events/OnCastStarted.asset");
            var hs = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/_Project/SO/Events/OnHookSuccess.asset");
            var hf = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/_Project/SO/Events/OnHookFailed.asset");

            var rodTip = GameObject.Find("RodTip").transform;

            var floatGO = GameObject.Find("Float");
            var fc = floatGO.GetComponent<FloatController>();
            var fSO = new SerializedObject(fc);
            fSO.FindProperty("gameSettings").objectReferenceValue = gs;
            fSO.FindProperty("onWaterLandedEvent").objectReferenceValue = wl;
            fSO.FindProperty("rodTip").objectReferenceValue = rodTip;
            var waterGO = GameObject.Find("Water");
            if (waterGO != null)
                fSO.FindProperty("waterSurface").objectReferenceValue = waterGO.transform;
            fSO.ApplyModifiedProperties();

            var rodGO = GameObject.Find("FishingRod");
            var rc = rodGO.GetComponent<FishingRodController>();
            var rSO = new SerializedObject(rc);
            rSO.FindProperty("gameSettings").objectReferenceValue = gs;
            rSO.FindProperty("playerData").objectReferenceValue = pd;
            rSO.FindProperty("floatController").objectReferenceValue = fc;
            rSO.FindProperty("rodTip").objectReferenceValue = rodTip;
            rSO.FindProperty("onRodGrabbed").objectReferenceValue = rg;
            rSO.FindProperty("onCastStarted").objectReferenceValue = cs;
            rSO.FindProperty("onHookSuccess").objectReferenceValue = hs;
            rSO.FindProperty("onHookFailed").objectReferenceValue = hf;
            rSO.FindProperty("onRodStateChanged").objectReferenceValue = rsc;
            rSO.FindProperty("onBiteOccurred").objectReferenceValue = bo;
            rSO.FindProperty("onWaterLanded").objectReferenceValue = wl;
            rSO.ApplyModifiedProperties();

            var lineGO = GameObject.Find("FishingLine");
            if (lineGO != null)
            {
                var lr = lineGO.GetComponent<FishingLineRenderer>();
                var lSO = new SerializedObject(lr);
                lSO.FindProperty("rodTip").objectReferenceValue = rodTip;
                lSO.FindProperty("floatController").objectReferenceValue = fc;
                lSO.ApplyModifiedProperties();
            }

            var testGO = GameObject.Find("TestInputManager");
            if (testGO != null)
            {
                var ti = testGO.GetComponent<FishingTestInput>();
                var tSO = new SerializedObject(ti);
                tSO.FindProperty("rodController").objectReferenceValue = rc;
                tSO.FindProperty("simulatedHand").objectReferenceValue = GameObject.Find("SimulatedHand").transform;
                tSO.FindProperty("playerData").objectReferenceValue = pd;
                tSO.FindProperty("gameSettings").objectReferenceValue = gs;
                tSO.ApplyModifiedProperties();
            }

            var debugGO = GameObject.Find("DebugUI");
            if (debugGO != null)
            {
                var dui = debugGO.GetComponent<FishingDebugUI>();
                var dSO = new SerializedObject(dui);
                dSO.FindProperty("rodController").objectReferenceValue = rc;
                dSO.FindProperty("floatController").objectReferenceValue = fc;
                dSO.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(floatGO);
            EditorUtility.SetDirty(rodGO);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[FishingSceneSetup] All references connected and scene marked dirty!");
        }
    }
}
