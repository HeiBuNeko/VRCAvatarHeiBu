using UnityEditor;
using UnityEngine;
using net.satania_shopping.tabakosystem.runtime;
using net.satania_shopping.tabakosystem.editor;
using System.Linq;

namespace net.satania_shopping.tabakosystem
{
    [CustomEditor(typeof(IControlledByAnimation))]
    public class IControlledByAnimationEditor : Editor
    {
        public string GetText(string id)
        {
            return LanguageManager.GetText(id);
        }

        public GUIContent GetGUIContent(string id)
        {
            return new GUIContent(GetText(id));
        }

        private IControlledByAnimation script => target as IControlledByAnimation;

        private SerializedProperty automaticMotion;
        private SerializedProperty additiveMotion;

        private SerializedProperty tabakoMesh;
        private SerializedProperty shortShapekeyName;
        private SerializedProperty shortShapekeyMaxValue;

        private SerializedProperty short2ShapekeyName;
        private SerializedProperty short2ShapekeyMaxValue;

        private SerializedProperty firedAnim_SmokeStart;
        private SerializedProperty firedAnim_SmokeEnd;
        private SerializedProperty tabakoTipPosition;
        private SerializedProperty tabakoSmokeParticle;
        private SerializedProperty fireSenderGO;
        private SerializedProperty fireReceiverGO;
        private SerializedProperty fireSenderVRC_GO;
        private SerializedProperty fireReceiverVRC_GO;

        private SerializedProperty offEmissiveColor;
        private SerializedProperty onEmissiveColor;
        private SerializedProperty m_time_EmissiveON;

        private SerializedProperty lighterMeshRenderer;
        private SerializedProperty lighterONShapeName;
        private SerializedProperty lighterONShapekeyMaxValue;

        private SerializedProperty fireParticlePosition;

        private SerializedProperty fireParticlePositionPairs;
        private SerializedProperty lightIntensityPairs;
        private SerializedProperty lightRangePairs;
        private SerializedProperty fireLight;
        private SerializedProperty fireParticle;
        private SerializedProperty VRCLighter;
        private SerializedProperty VRC_Fire;
        private SerializedProperty lightSound;
        private SerializedProperty lighterSpark;

        private SkinnedMeshRenderer TabakoMesh
        {
            get
            {
                SkinnedMeshRenderer tabakoMeshRenderer = tabakoMesh != null && tabakoMesh.objectReferenceValue != null ? tabakoMesh.objectReferenceValue as SkinnedMeshRenderer : null;

                return tabakoMeshRenderer;
            }
        }

        private void OnEnable()
        {
            automaticMotion = serializedObject.FindProperty("automaticMotion");
            additiveMotion = serializedObject.FindProperty("additiveMotion");

            tabakoMesh = serializedObject.FindProperty("tabakoMesh");
            shortShapekeyName = serializedObject.FindProperty("shortShapekeyName");
            shortShapekeyMaxValue = serializedObject.FindProperty("shortShapekeyMaxValue");

            short2ShapekeyName = serializedObject.FindProperty("short2ShapekeyName");
            short2ShapekeyMaxValue = serializedObject.FindProperty("short2ShapekeyMaxValue");

            firedAnim_SmokeStart = serializedObject.FindProperty("firedAnim_SmokeStart");
            firedAnim_SmokeEnd = serializedObject.FindProperty("firedAnim_SmokeEnd");
            tabakoTipPosition = serializedObject.FindProperty("tabakoTipPosition");
            tabakoSmokeParticle = serializedObject.FindProperty("tabakoSmokeParticle");
            fireSenderGO = serializedObject.FindProperty("fireSenderGO");
            fireReceiverGO = serializedObject.FindProperty("fireReceiverGO");
            fireSenderVRC_GO = serializedObject.FindProperty("fireSenderVRC_GO");
            fireReceiverVRC_GO = serializedObject.FindProperty("fireReceiverVRC_GO");

            offEmissiveColor = serializedObject.FindProperty("offEmissiveColor");
            onEmissiveColor = serializedObject.FindProperty("onEmissiveColor");
            m_time_EmissiveON = serializedObject.FindProperty("m_time_EmissiveON");

            lighterMeshRenderer = serializedObject.FindProperty("lighterMeshRenderer");
            lighterONShapeName = serializedObject.FindProperty("lighterONShapeName");
            lighterONShapekeyMaxValue = serializedObject.FindProperty("lighterONShapekeyMaxValue");

            fireParticlePosition = serializedObject.FindProperty("fireParticlePosition");

            fireParticlePositionPairs = serializedObject.FindProperty("fireParticlePositionPairs");
            lightIntensityPairs = serializedObject.FindProperty("lightIntensityPairs");
            lightRangePairs = serializedObject.FindProperty("lightRangePairs");
            fireLight = serializedObject.FindProperty("fireLight");
            fireParticle = serializedObject.FindProperty("fireParticle");
            VRCLighter = serializedObject.FindProperty("VRCLighter");
            VRC_Fire = serializedObject.FindProperty("VRC_Fire");

            lightSound = serializedObject.FindProperty("lightSound");
            lighterSpark = serializedObject.FindProperty("lighterSpark");
        }

        public override void OnInspectorGUI()
        {
            SatabakoEditorUtils.DrawHeader();
            GUILayout.Space(10);

            serializedObject.UpdateIfRequiredOrScript();
            EditorGUILayout.PropertyField(automaticMotion, GetGUIContent("t_Target_Motion"));

            if (script.Motion == SataniaTabakoRequireMotion.AutomaticMotion.TabakoFiredAnimation)
            {
                TabakoFireAnimGUI();
            }
            else if (script.Motion == SataniaTabakoRequireMotion.AutomaticMotion.ExhaleEmission)
            {
                ExhaleEmissionGUI();
            }
            else if (script.Motion == SataniaTabakoRequireMotion.AutomaticMotion.Additive)
            {
                AdditiveGUI();
            }

            serializedObject.ApplyModifiedProperties();

            GUILayout.Space(10);

            LanguageManager.DrawLanguagePopup();
        }

        private void ExhaleEmissionGUI()
        {
            GUILayout.Space(5);

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label("(lilToon 1st Emission Only)", EditorStyles.boldLabel);
                GUILayout.Space(5);

                GUILayout.Label(GetGUIContent("t_Cigarette_Mesh"), EditorStyles.boldLabel);

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(tabakoMesh, GetGUIContent("t_Cigarette_Mesh"));

                GUILayout.Space(5);

                EditorGUILayout.PropertyField(offEmissiveColor, GetGUIContent("OFF EmissiveColor"));
                EditorGUILayout.PropertyField(onEmissiveColor, GetGUIContent("ON EmissiveColor"));
                EditorGUILayout.PropertyField(m_time_EmissiveON, GetGUIContent("EmissiveON AnimationClip Length"));
                EditorGUI.indentLevel--;
            }
        }

        private void TabakoFireAnimGUI()
        {
            GUILayout.Space(5);

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label(GetGUIContent("t_Cigarette_Mesh"), EditorStyles.boldLabel);

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(tabakoMesh, GetGUIContent("t_Cigarette_Mesh"));

                EditorGUI.indentLevel--;
            }

            GUILayout.Space(5);
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label(GetGUIContent("t_Burning_Tip_ShapeKey"), EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(shortShapekeyName, GetGUIContent("t_ShapeKey_Name"));

                EditorGUILayout.PropertyField(shortShapekeyMaxValue, GetGUIContent("t_Maximum_Value"));
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(5);
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label(GetGUIContent("t_Shorten_Cigarette_ShapeKey"), EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(short2ShapekeyName, GetGUIContent("t_ShapeKey_Name"));

                EditorGUILayout.PropertyField(short2ShapekeyMaxValue, GetGUIContent("t_Maximum_Value"));
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(5);
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label(GetText("t_For_Cigarette_Tip"), EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(firedAnim_SmokeStart, GetGUIContent("t_Cigarette_Tip_Start_Position"));
                EditorGUILayout.PropertyField(firedAnim_SmokeEnd, GetGUIContent("t_Cigarette_Tip_Final_Position"));
                EditorGUILayout.PropertyField(tabakoTipPosition, GetGUIContent("t_Cigarette_Tip_Object"));
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(5);
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label(GetText("t_Cigarette_Smoke"), EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(tabakoSmokeParticle, GetGUIContent("t_Smoke_from_the_cigarette"));
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(5);
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label(GetText("t_Contact"), EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(fireSenderGO, GetGUIContent("t_Fire_Sender"));
                EditorGUILayout.PropertyField(fireSenderVRC_GO, GetGUIContent("t_Fire_Sender_VRC"));
                EditorGUILayout.PropertyField(fireReceiverGO, GetGUIContent("t_Fire_Receiver"));
                EditorGUILayout.PropertyField(fireReceiverVRC_GO, GetGUIContent("t_Fire_Receiver_VRC"));
                EditorGUI.indentLevel--;
            }
        }

        private void OnFiredAnimSceneGUI()
        {
            if (script.Motion == SataniaTabakoRequireMotion.AutomaticMotion.TabakoFiredAnimation)
            {
                Transform firedAnimSmokeStart = script.FiredAnim_SmokeStart?.transform;
                Transform firedAnimSmokeEnd = script.FiredAnim_SmokeEnd?.transform;
                if (firedAnimSmokeStart != null)
                {
                    Handles.color = Color.red;
                    Handles.DrawWireCube(firedAnimSmokeStart.position, new Vector3(0.0012f, 0.0012f, 0.0012f));
                }

                if (firedAnimSmokeEnd != null)
                {
                    Handles.color = Color.blue;
                    Handles.DrawWireCube(firedAnimSmokeEnd.position, new Vector3(0.0012f, 0.0012f, 0.0012f));
                }
            }
        }

        private void AdditiveGUI()
        {
            GUILayout.Space(5);

            EditorGUILayout.PropertyField(additiveMotion, GetGUIContent("Additive Motion"));
            if (script.AdditiveMotion == SataniaTabakoRequireMotion.AdditiveMotionTarget.Lighter)
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label(GetGUIContent("Lighter ON Shapekey"), EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(lighterMeshRenderer, GetGUIContent("Lighter Mesh Renderer"));
                    EditorGUILayout.PropertyField(lighterONShapeName, GetGUIContent("t_ShapeKey_Name"));

                    EditorGUILayout.PropertyField(lighterONShapekeyMaxValue, GetGUIContent("t_Maximum_Value"));
                    EditorGUI.indentLevel--;
                }
            }
            else if (script.AdditiveMotion == SataniaTabakoRequireMotion.AdditiveMotionTarget.FireParticle)
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label(GetGUIContent("ライターの火"), EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(fireParticlePosition, GetGUIContent("Fire Particle Position Transform"));
                    EditorGUILayout.PropertyField(fireParticlePositionPairs, GetGUIContent("火のポジション (加算)"));
                    EditorGUI.indentLevel--;
                }

                GUILayout.Space(10);

                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label(GetGUIContent("ライト"), EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(fireLight);
                    EditorGUILayout.PropertyField(lightIntensityPairs, GetGUIContent("ライト Intensity (加算)"));
                    EditorGUILayout.PropertyField(lightRangePairs, GetGUIContent("ライト Range (加算)"));
                    EditorGUI.indentLevel--;
                }

                GUILayout.Space(10);

                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label(GetGUIContent("オブジェクト"), EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(VRCLighter, GetGUIContent("火のコンタクト判定"));
                    EditorGUILayout.PropertyField(VRC_Fire, GetGUIContent("火のコンタクト判定 (Fire)"));
                    EditorGUILayout.PropertyField(fireParticle, GetGUIContent("ライターの炎"));
                    EditorGUILayout.PropertyField(lightSound, GetGUIContent("ライターの音"));
                    EditorGUILayout.PropertyField(lighterSpark, GetGUIContent("ライターの火花"));

                    EditorGUI.indentLevel--;
                }
            }
        }

        private void OnSceneGUI()
        {
            OnFiredAnimSceneGUI();
        }
    }
}