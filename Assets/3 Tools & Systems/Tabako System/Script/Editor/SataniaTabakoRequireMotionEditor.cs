using UnityEditor;

namespace net.satania_shopping.tabakosystem.editor
{
    [CustomEditor(typeof(SataniaTabakoRequireMotion))]
    public class SataniaTabakoRequireMotionEditor : Editor
    {
        SataniaTabakoRequireMotion script => target as SataniaTabakoRequireMotion;

        private SerializedProperty motionID;
        private SerializedProperty firedAnimationType;
        private SerializedProperty overrideMotion;
        private SerializedProperty exhaleAnimationType;
        private SerializedProperty additiveMotionTarget;
        private SerializedProperty lighterMotionType;

        private void OnEnable()
        {
            motionID = serializedObject.FindProperty("motionID");
            firedAnimationType = serializedObject.FindProperty("firedAnimationType");
            overrideMotion = serializedObject.FindProperty("overrideMotion");
            exhaleAnimationType = serializedObject.FindProperty("exhaleAnimationType");
            additiveMotionTarget = serializedObject.FindProperty("additiveMotionTarget");
            lighterMotionType = serializedObject.FindProperty("lighterMotionType");
        }

        public override void OnInspectorGUI()
        {
            SatabakoEditorUtils.DrawHeader(100);

            serializedObject.UpdateIfRequiredOrScript();
            EditorGUILayout.PropertyField(motionID);
            if (script.MotionID == SataniaTabakoRequireMotion.AutomaticMotion.Override)
            {
                EditorGUILayout.PropertyField(overrideMotion);
            }
            else if (script.MotionID == SataniaTabakoRequireMotion.AutomaticMotion.TabakoFiredAnimation)
            {
                EditorGUILayout.PropertyField(firedAnimationType);
            }
            else if (script.MotionID == SataniaTabakoRequireMotion.AutomaticMotion.ExhaleEmission)
            {
                EditorGUILayout.PropertyField(exhaleAnimationType);
            }
            else if (script.MotionID == SataniaTabakoRequireMotion.AutomaticMotion.Additive)
            {
                EditorGUILayout.PropertyField(additiveMotionTarget);
                if (script.AdditiveMotion == SataniaTabakoRequireMotion.AdditiveMotionTarget.Lighter ||
                    script.AdditiveMotion == SataniaTabakoRequireMotion.AdditiveMotionTarget.FireParticle)
                {
                    EditorGUILayout.PropertyField(lighterMotionType);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}