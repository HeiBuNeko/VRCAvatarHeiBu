using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;

namespace net.satania_shopping.tabakosystem.editor
{
    internal static class AnimatorLayerMatcher
    {
        public static int FindLayerIndex(AnimatorController controller, string layerName)
        {
            if (controller == null || string.IsNullOrEmpty(layerName))
                return -1;

            var layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] != null && layers[i].name == layerName)
                    return i;
            }
            return -1;
        }

        public static List<AnimatorState> CollectStates(AnimatorStateMachine stateMachine)
        {
            var states = new List<AnimatorState>();
            Collect(stateMachine, states, new HashSet<AnimatorStateMachine>());
            return states;
        }

        private static void Collect(AnimatorStateMachine stateMachine, List<AnimatorState> states, HashSet<AnimatorStateMachine> visited)
        {
            if (stateMachine == null || !visited.Add(stateMachine))
                return;

            foreach (var child in stateMachine.states)
            {
                if (child.state != null)
                    states.Add(child.state);
            }

            foreach (var child in stateMachine.stateMachines)
                Collect(child.stateMachine, states, visited);
        }

        public static string[] StateNames(AnimatorControllerLayer layer)
        {
            if (layer == null || layer.stateMachine == null)
                return new string[0];

            return CollectStates(layer.stateMachine).Select(s => s.name).ToArray();
        }

        public static string Check(AnimatorControllerLayer layer, string[] expectedStateNames, bool strictStateCount, out int stateCount)
        {
            string[] names = StateNames(layer);
            stateCount = names.Length;

            var expected = (expectedStateNames ?? new string[0]).Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()).ToArray();
            if (expected.Length == 0)
                return null;

            var missing = expected.Where(e => !names.Contains(e)).ToArray();
            if (missing.Length > 0)
                return "無いステート: " + string.Join(", ", missing);

            if (strictStateCount && names.Length != expected.Length)
                return $"ステート数が違う ({names.Length} / 期待 {expected.Length})";

            return null;
        }
    }
}
