using net.satania_shopping.tabakosystem.ndmf;
using net.satania_shopping.tabakosystem.runtime;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace net.satania_shopping.tabakosystem.editor
{
    public class ForUnsupportedAvatarMenuItem
    {
        private const string k_hands_case = "hands_case";
        private const string k_hands_tabacco = "hands_tabacco";
        private const string k_hands_lighter = "hands_lighter";

        private static void SetController(Animator animator)
        {
            GameObject avatarRoot = animator.gameObject;

            if (!SatabakoDatabase.s.handGestureDatabases.ContainsKey(avatarRoot.transform))
                throw new System.Exception("Databaseが存在しません。");

            SatabakoDatabase.HandGestureDatabase HandGestureDatabase = SatabakoDatabase.s.handGestureDatabases[avatarRoot.transform];

            //HandGestureDatabase handGestureDatabase = avatarRoot.GetComponentInChildren<HandGestureDatabase>(true);

            if (HandGestureDatabase == null)
                throw new System.Exception("handGestureDatabase is null");

            if (HandGestureDatabase.handGestureController == null)
            {
                AnimatorController newController = new AnimatorController();
                AnimatorControllerLayer newLayer = new AnimatorControllerLayer();
                newLayer.defaultWeight = 1;
                newLayer.stateMachine = new AnimatorStateMachine();
                newLayer.name = "Hand Gesture";

                //ケース
                AnimatorState caseState = new AnimatorState();
                caseState.motion = HandGestureDatabase.gestureCase;
                caseState.name = k_hands_case;
                newLayer.stateMachine.AddState(caseState, new Vector3(0, 200, 0));

                //タバコ
                AnimatorState tabaccoState = new AnimatorState();
                tabaccoState.motion = HandGestureDatabase.gestureTabacco;
                tabaccoState.name = k_hands_tabacco;
                newLayer.stateMachine.AddState(tabaccoState, new Vector3(0, 300, 0));

                //ライター
                AnimatorState lighterState = new AnimatorState();
                lighterState.motion = HandGestureDatabase.gestureLighter;
                lighterState.name = k_hands_lighter;
                newLayer.stateMachine.AddState(lighterState, new Vector3(0, 400, 0));

                newController.AddLayer(newLayer);

                HandGestureDatabase.handGestureController = newController;
            }

            RuntimeAnimatorController handGestureController = HandGestureDatabase.handGestureController;

            animator.runtimeAnimatorController = handGestureController;
        }

        [MenuItem(MenuItemDictionary.p_UnsupportedCase, false, 22)]
        private static void SetCase()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("さたにあ式タバコ", "プレイモード中に使用してください！", "OK");
                return;
            }

            Animator animator = Selection.activeGameObject.GetComponent<Animator>();
            SetController(animator);

            animator.Play(k_hands_case);
        }

        [MenuItem(MenuItemDictionary.p_UnsupportedCase, true, 22)]
        private static bool ValidateCase()
        {
            return IsHumanoid(Selection.activeGameObject);
        }

        [MenuItem(MenuItemDictionary.p_UnsupportedLighter, false, 22)]
        private static void SetLighter()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("さたにあ式タバコ", "プレイモード中に使用してください！", "OK");
                return;
            }

            Animator animator = Selection.activeGameObject.GetComponent<Animator>();
            SetController(animator);

            animator.Play(k_hands_lighter);
        }

        [MenuItem(MenuItemDictionary.p_UnsupportedLighter, true, 22)]
        private static bool ValidateLighter()
        {
            return IsHumanoid(Selection.activeGameObject);
        }

        [MenuItem(MenuItemDictionary.p_UnsupportedTabako, false, 22)]
        private static void SetTabako()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("さたにあ式タバコ", "プレイモード中に使用してください！", "OK");
                return;
            }

            Animator animator = Selection.activeGameObject.GetComponent<Animator>();
            SetController(animator);

            animator.Play(k_hands_tabacco);
        }

        [MenuItem(MenuItemDictionary.p_UnsupportedTabako, true, 22)]
        private static bool ValidateTabako()
        {
            return IsHumanoid(Selection.activeGameObject);
        }

        private static bool IsHumanoid(GameObject go)
        {
            Animator animator = go?.GetComponent<Animator>();
            return animator != null && animator.isHuman;
        }
    }
}