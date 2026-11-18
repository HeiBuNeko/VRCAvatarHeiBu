using net.satania_shopping.tabakosystem.runtime;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace net.satania_shopping.tabakosystem.editor
{
    public class ForUnsupportedAvatarMenuItem
    {
        private const string k_layerName = "Hand Gesture";

        private static void PlayGesture(GestureKind kind)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("さたにあ式タバコ", "プレイモード中に使用してください！", "OK");
                return;
            }

            Animator animator = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<Animator>() : null;
            if (animator == null)
                return;

            var data = animator.GetComponent<SatabakoPlayModeData>();
            if (data == null)
            {
                EditorUtility.DisplayDialog("さたにあ式タバコ", "さたにあ式タバコが入った状態でプレイモードに入ったアバターを選んでください。", "OK");
                return;
            }

            Motion motion = kind == GestureKind.Case ? data.gestureCase : kind == GestureKind.Tabako ? data.gestureTabako : data.gestureLighter;
            if (motion == null)
            {
                Debug.LogWarning("[SataniaTabako] ハンドジェスチャーのアニメーションが見つかりません。");
                return;
            }

            var controller = new AnimatorController();
            var layer = new AnimatorControllerLayer
            {
                name = k_layerName,
                defaultWeight = 1f,
                stateMachine = new AnimatorStateMachine()
            };
            AnimatorState state = layer.stateMachine.AddState(kind.ToString(), new Vector3(0, 200, 0));
            state.motion = motion;
            controller.AddLayer(layer);

            animator.runtimeAnimatorController = controller;
            animator.Play(state.name);
        }

        [MenuItem(MenuItemDictionary.p_UnsupportedCase, false, 22)]
        private static void SetCase() => PlayGesture(GestureKind.Case);

        [MenuItem(MenuItemDictionary.p_UnsupportedCase, true, 22)]
        private static bool ValidateCase() => IsHumanoid(Selection.activeGameObject);

        [MenuItem(MenuItemDictionary.p_UnsupportedLighter, false, 22)]
        private static void SetLighter() => PlayGesture(GestureKind.Lighter);

        [MenuItem(MenuItemDictionary.p_UnsupportedLighter, true, 22)]
        private static bool ValidateLighter() => IsHumanoid(Selection.activeGameObject);

        [MenuItem(MenuItemDictionary.p_UnsupportedTabako, false, 22)]
        private static void SetTabako() => PlayGesture(GestureKind.Tabako);

        [MenuItem(MenuItemDictionary.p_UnsupportedTabako, true, 22)]
        private static bool ValidateTabako() => IsHumanoid(Selection.activeGameObject);

        private static bool IsHumanoid(GameObject go)
        {
            Animator animator = go != null ? go.GetComponent<Animator>() : null;
            return animator != null && animator.isHuman;
        }
    }
}
