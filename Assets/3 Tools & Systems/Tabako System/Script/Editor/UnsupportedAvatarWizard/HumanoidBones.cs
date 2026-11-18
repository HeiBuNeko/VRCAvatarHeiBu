using System.Collections.Generic;
using UnityEngine;

namespace net.satania_shopping.tabakosystem.editor
{
    internal static class HumanoidBones
    {
        public static Transform Get(Animator animator, HumanBodyBones bone)
        {
            if (animator == null || bone < 0 || bone >= HumanBodyBones.LastBone)
                return null;

            Transform t = null;
            if (animator.isHuman)
            {
                try
                {
                    t = animator.GetBoneTransform(bone);
                }
                catch (System.Exception)
                {
                    t = null;
                }
            }
            if (t != null)
                return t;

            Avatar avatar = animator.avatar;
            if (avatar == null || !avatar.isHuman)
                return null;

            string humanName = HumanTrait.BoneName[(int)bone];
            foreach (var humanBone in avatar.humanDescription.human)
            {
                if (humanBone.humanName == humanName)
                    return FindByName(animator.transform, humanBone.boneName);
            }

            return null;
        }

        public static bool IsHumanoid(Animator animator)
        {
            return animator != null && animator.avatar != null && animator.avatar.isValid && animator.avatar.isHuman;
        }

        private static Transform FindByName(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name))
                return null;

            var queue = new Queue<Transform>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var t = queue.Dequeue();
                if (t.name == name)
                    return t;
                for (int i = 0; i < t.childCount; i++)
                    queue.Enqueue(t.GetChild(i));
            }

            return null;
        }
    }
}
