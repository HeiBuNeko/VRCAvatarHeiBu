using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;

namespace net.satania_shopping.tabakosystem
{
    public static class AnimatorControllerUtils
    {
        private static IEnumerable<AnimatorStateMachine> GetAllStateMachineRecursive(AnimatorStateMachine stateMachine)
        {
            if (stateMachine == null) yield break;

            yield return stateMachine;

            foreach (var childSmEntry in stateMachine.stateMachines)
            {
                // 子のマシンを再帰的に取得
                foreach (var childSm in GetAllStateMachineRecursive(childSmEntry.stateMachine))
                {
                    yield return childSm;
                }
            }
        }

        public static (AnimatorState[], TBehaviour[]) FindStatesAndSingleBehaviourAsArrays<TBehaviour>(AnimatorController controller)
    where TBehaviour : StateMachineBehaviour
        {
            if (controller == null)
            {
                Debug.LogError("AnimatorController is null.");
                return (new AnimatorState[0], new TBehaviour[0]);
            }

            // 条件に合うステートとBehaviourのペアのリストを作成
            var stateAndBehaviourPairs = controller.layers
            .Where(layer => layer.stateMachine != null)
            .SelectMany(layer => GetAllStateMachineRecursive(layer.stateMachine))
            .SelectMany(sm => sm.states.Select(s => s.state))
            .Where(state => state.behaviours.Any(behaviour => behaviour is TBehaviour))
            .Select(state => new
            {
                StateInstance = state,
                BehaviourInstance = state.behaviours.OfType<TBehaviour>().First()
            })
            .ToList();

            AnimatorState[] statesArray = stateAndBehaviourPairs.Select(pair => pair.StateInstance).ToArray();
            TBehaviour[] behavioursArray = stateAndBehaviourPairs.Select(pair => pair.BehaviourInstance).ToArray();

            return (statesArray, behavioursArray);
        }

        public static (AnimatorState[], TBehaviour[][]) FindStatesAndBehavioursAsArrays<TBehaviour>(AnimatorController controller)
            where TBehaviour : StateMachineBehaviour
        {
            if (controller == null)
            {
                Debug.LogError("AnimatorController is null.");
                return (new AnimatorState[0], new TBehaviour[0][]);
            }

            // 条件に合うステートとBehaviourのペアのリストを作成
            var pairs = controller.layers.Where(l => l.stateMachine != null)
                .SelectMany(l => GetAllStateMachineRecursive(l.stateMachine))
                .SelectMany(sm => sm.states.Select(s => s.state))
                .Where(s => s.behaviours.Any(behaviour => behaviour is TBehaviour))
                .Select(s => new
                {
                    StateInstance = s,
                    Behaviours = s.behaviours.OfType<TBehaviour>()
                });

            AnimatorState[] statesArray = pairs.Select(pair => pair.StateInstance).ToArray();
            TBehaviour[][] behavioursArray = pairs.Select(pair => pair.Behaviours.ToArray()).ToArray();

            return (statesArray, behavioursArray);
        }
    }
}