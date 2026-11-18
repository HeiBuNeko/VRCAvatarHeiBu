using net.satania_shopping.tabakosystem.runtime;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace net.satania_shopping.tabakosystem.editor
{
    [CustomEditor(typeof(IModifyAnimatorLayer))]
    public class IModifyAnimatorLayerEditor : Editor
    {
        private SerializedProperty animLayerType;
        private SerializedProperty layerName;
        private SerializedProperty action;
        private SerializedProperty expectedStateNames;
        private SerializedProperty strictStateCount;

        private IModifyAnimatorLayer script => target as IModifyAnimatorLayer;

        private static string L(string id) => LanguageManager.GetText(id);

        private void OnEnable()
        {
            animLayerType = serializedObject.FindProperty("animLayerType");
            layerName = serializedObject.FindProperty("layerName");
            action = serializedObject.FindProperty("action");
            expectedStateNames = serializedObject.FindProperty("expectedStateNames");
            strictStateCount = serializedObject.FindProperty("strictStateCount");
        }

        public override void OnInspectorGUI()
        {
            SatabakoEditorUtils.DrawHeader(100);
            serializedObject.UpdateIfRequiredOrScript();

            AnimatorController controller = FindController(out bool inAvatar);

            EditorGUILayout.PropertyField(animLayerType, new GUIContent(L("t_MAL_LayerType")));

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(layerName, new GUIContent(L("t_MAL_LayerName")));
                using (new EditorGUI.DisabledScope(controller == null))
                {
                    if (GUILayout.Button(L("t_MAL_PickLayer"), GUILayout.Width(90)))
                        ShowLayerMenu(controller);
                }
            }

            int actionIndex = action.enumValueIndex;
            int newActionIndex = EditorGUILayout.Popup(new GUIContent(L("t_MAL_Action")), actionIndex,
                new[] { new GUIContent(L("t_MAL_ActionRemove")), new GUIContent(L("t_MAL_ActionWeightZero")) });
            if (newActionIndex != actionIndex)
                action.enumValueIndex = newActionIndex;
            if ((IModifyAnimatorLayer.eAction)action.enumValueIndex == IModifyAnimatorLayer.eAction.Remove)
                EditorGUILayout.HelpBox(L("t_MAL_MsgRemoveNote"), MessageType.None);

            GUILayout.Space(5);
            EditorGUILayout.PropertyField(expectedStateNames, new GUIContent(L("t_MAL_ExpectedStates")), true);
            EditorGUILayout.PropertyField(strictStateCount, new GUIContent(L("t_MAL_StrictCount")));

            int layerIndex = AnimatorLayerMatcher.FindLayerIndex(controller, layerName.stringValue.Trim());
            using (new EditorGUI.DisabledScope(layerIndex < 0))
            {
                if (GUILayout.Button(L("t_MAL_Capture")))
                {
                    string[] names = AnimatorLayerMatcher.StateNames(controller.layers[layerIndex]);
                    expectedStateNames.arraySize = names.Length;
                    for (int i = 0; i < names.Length; i++)
                        expectedStateNames.GetArrayElementAtIndex(i).stringValue = names[i];
                }
            }

            serializedObject.ApplyModifiedProperties();

            GUILayout.Space(5);
            DrawStatus(controller, inAvatar, layerIndex);

            GUILayout.Space(10);
            LanguageManager.DrawLanguagePopup();
        }

        private void DrawStatus(AnimatorController controller, bool inAvatar, int layerIndex)
        {
            if (!inAvatar)
            {
                EditorGUILayout.HelpBox(L("t_MAL_MsgNoAvatar"), MessageType.Info);
                return;
            }

            if (controller == null)
            {
                EditorGUILayout.HelpBox(L("t_MAL_MsgNoController"), MessageType.Warning);
                return;
            }

            if (layerIndex < 0)
            {
                EditorGUILayout.HelpBox(L("t_MAL_MsgLayerNotFound"), MessageType.Warning);
                return;
            }

            var layer = controller.layers[layerIndex];
            string mismatch = AnimatorLayerMatcher.Check(layer, script.expectedStateNames, script.strictStateCount, out int stateCount);
            if (layerIndex == 0 && script.action == IModifyAnimatorLayer.eAction.SetWeightZero)
                EditorGUILayout.HelpBox(L("t_MAL_MsgFirstLayerWeight"), MessageType.Warning);
            else if (mismatch != null)
                EditorGUILayout.HelpBox(string.Format(L("t_MAL_MsgMismatch"), mismatch), MessageType.Warning);
            else if (script.expectedStateNames == null || script.expectedStateNames.All(string.IsNullOrWhiteSpace))
                EditorGUILayout.HelpBox(L("t_MAL_MsgNoCheck"), MessageType.Info);
            else
                EditorGUILayout.HelpBox(string.Format(L("t_MAL_MsgMatch"), stateCount), MessageType.Info);
        }

        private AnimatorController FindController(out bool inAvatar)
        {
            inAvatar = false;
            if (script == null)
                return null;

            var descriptor = script.GetComponentInParent<VRCAvatarDescriptor>(true);
            if (descriptor == null)
                return null;

            inAvatar = true;
            return AvatarUtils.GetAvatarBaseLayer(descriptor, script.animLayerType);
        }

        private void ShowLayerMenu(AnimatorController controller)
        {
            var menu = new GenericMenu();
            foreach (var layer in controller.layers)
            {
                string name = layer.name;
                menu.AddItem(new GUIContent(name.Replace("/", "∕")), name == layerName.stringValue, () =>
                {
                    serializedObject.Update();
                    layerName.stringValue = name;
                    serializedObject.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }
    }
}
