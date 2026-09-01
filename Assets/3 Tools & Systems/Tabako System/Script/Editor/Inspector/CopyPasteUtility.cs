/*
 * MIT License
 *
 * Copyright (c) 2025 Satania
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf.runtime;
using net.satania_shopping.tabakosystem.ndmf;
using net.satania_shopping.tabakosystem.runtime;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.Constraint.Components;

namespace net.satania_shopping.tabakosystem.editor
{
    public class CopyPasteUtility
    {
        internal enum ConstraintSourcePosition
        {
            Right,
            Left,
            Mouth
        }

        internal class CopyPasteSetting : ScriptableSingleton<CopyPasteSetting>
        {
            internal class ContactShape
            {
                public ContactBase.ShapeType shapeType;
                public float radius;
                public float height;
                public Vector3 position;
                public Quaternion rotation;
            }

            internal class Placement
            {
                public Vector3 position;
                public Quaternion rotation;
            }

            internal class TabakoPrefabTransform
            {
                public enum Hand
                {
                    Right,
                    Left,
                }

                //Placementはボーンからの相対位置を保存

                public Placement Case_Right;
                public Placement Case_Left;
                public Vector3 Case_Scale;

                public Placement Tabako_Right;
                public Placement Tabako_Left;
                public Placement Tabako_Mouth;
                public Vector3 Tabako_Scale; //タバコスケールはケースと同じであるべき

                public Placement Lighter_Right;
                public Placement Lighter_Left;
                public Vector3 Lighter_Scale;

                public Hand hand = Hand.Right;
            }

            internal Vector3 localPosition;
            internal Quaternion localRotation;

            internal ContactShape contactShape = null;
            internal TabakoPrefabTransform tabakoPrefabTransform = null;
        }

        private static CopyPasteSetting setting => CopyPasteSetting.instance;

        #region Transform
        [MenuItem("CONTEXT/Transform/Satania Tabako System/Copy Position AND Rotation", priority = 50, secondaryPriority = 1)]
        static void CopyTransform(MenuCommand menuCommand)
        {
            Transform c = menuCommand.context as Transform;

            setting.localPosition = c.localPosition;
            setting.localRotation = c.localRotation;
        }

        [MenuItem("CONTEXT/Transform/Satania Tabako System/Paste Position AND Rotation", priority = 50, secondaryPriority = 2)]
        static void PasteTransform(MenuCommand menuCommand)
        {
            Transform t = menuCommand.context as Transform;
            ModularAvatarBoneProxy maBoneProxy = t.GetComponent<ModularAvatarBoneProxy>();

            Transform bone = maBoneProxy.target;
            if (bone != null)
            {
                Vector3 worldPosition = bone.TransformPoint(setting.localPosition);
                Quaternion worldRotation = bone.rotation * setting.localRotation;

                t.position = worldPosition;
                t.rotation = worldRotation;

                EditorUtility.SetDirty(t);
            }
        }

        [MenuItem("CONTEXT/Transform/Satania Tabako System/Paste Position AND Rotation", priority = 50, secondaryPriority = 2, validate = true)]
        static bool PasteTransformValidate(MenuCommand menuCommand)
        {
            Transform t = menuCommand.context as Transform;
            return t.GetComponent<ModularAvatarBoneProxy>() != null;
        }

        [MenuItem("CONTEXT/Transform/Satania Tabako System/Paste Position AND Rotation (Mirror X)", priority = 50, secondaryPriority = 3)]
        static void PasteTransformFlipX(MenuCommand menuCommand)
        {
            Transform t = menuCommand.context as Transform;
            ModularAvatarBoneProxy maBoneProxy = t.GetComponent<ModularAvatarBoneProxy>();

            Transform bone = maBoneProxy.target;
            if (bone != null)
            {
                Vector3 worldPosition = bone.TransformPoint(setting.localPosition);
                Quaternion worldRotation = bone.rotation * setting.localRotation;

                t.position = worldPosition;
                t.rotation = worldRotation;

                Transform root = FindAvatarDescriptorInParent(t);

                if (root != null)
                    TransformUtility.MirrorX(t, root);

                EditorUtility.SetDirty(t);
            }
        }

        [MenuItem("CONTEXT/Transform/Satania Tabako System/Paste Position AND Rotation (Mirror X)", priority = 50, secondaryPriority = 2, validate = true)]
        static bool PasteTransformFlipXValidate(MenuCommand menuCommand)
        {
            Transform t = menuCommand.context as Transform;
            return t.GetComponent<ModularAvatarBoneProxy>() != null;
        }

        [MenuItem("CONTEXT/Transform/Satania Tabako System/Avatar-based mirroring (X)", priority = 65, secondaryPriority = 15)]
        static void FlipX(MenuCommand menuCommand)
        {
            Transform t = menuCommand.context as Transform;
            Transform root = FindAvatarDescriptorInParent(t);

            if (root != null)
                TransformUtility.MirrorX(t, root);
        }

        [MenuItem("CONTEXT/Transform/Satania Tabako System/Avatar-based mirroring (X)", priority = 50, secondaryPriority = 15, validate = true)]
        static bool FlipX_Validate(MenuCommand menuCommand)
        {
            Transform t = menuCommand.context as Transform;
            return FindAvatarDescriptorInParent(t) != null;
        }
        #endregion

        #region VRC ContactBase
        [MenuItem("CONTEXT/ContactBase/Satania Tabako System/Copy Contact Shapes", priority = 50, secondaryPriority = 1)]
        private static void CopyContactBaseShape(MenuCommand menuCommand)
        {
            ContactBase contactBase = menuCommand.context as ContactBase;

            if (contactBase != null)
            {
                setting.contactShape = new CopyPasteSetting.ContactShape()
                {
                    shapeType = contactBase.shapeType,
                    radius = contactBase.radius,
                    height = contactBase.height,
                    position = contactBase.position,
                    rotation = contactBase.rotation
                };
            }
        }

        [MenuItem("CONTEXT/ContactBase/Satania Tabako System/Paste Contact Shapes", priority = 50, secondaryPriority = 2)]
        private static void PasteContactBaseShape(MenuCommand menuCommand)
        {
            ContactBase contactBase = menuCommand.context as ContactBase;

            if (contactBase != null && setting.contactShape != null)
            {
                Undo.RecordObject(contactBase, "Paste ContactBase shape");
                contactBase.shapeType = setting.contactShape.shapeType;
                contactBase.radius = setting.contactShape.radius;
                contactBase.height = setting.contactShape.height;
                contactBase.position = setting.contactShape.position;
                contactBase.rotation = setting.contactShape.rotation;

                EditorUtility.SetDirty(contactBase);
            }
        }
        #endregion

        #region Tabako Prefab Transform
        [MenuItem(MenuItemDictionary.p_CopyTransforms, priority = 100, secondaryPriority = 6)]
        private static void CopyPrefabTransforms()
        {
            GameObject go = Selection.activeGameObject;
            if (go == null)
                return;

            Transform avatarRoot = RuntimeUtil.FindAvatarInParents(go.transform);
            if (!SatabakoDatabase.s.constraintDatabases.ContainsKey(avatarRoot))
                throw new System.Exception("Keyが存在しません。");

            var constraintDatabase = SatabakoDatabase.s.constraintDatabases[avatarRoot];
            if (constraintDatabase == null)
                throw new System.Exception("ConstraintDatabaseが存在しません。");

            try
            {
                CopyPasteSetting.TabakoPrefabTransform tpt = new CopyPasteSetting.TabakoPrefabTransform();

                //---------------------------------------------------
                //スケール取得用
                Transform ct_case = constraintDatabase.CT_Case; //ケース追従先
                Transform ct_tabacco = constraintDatabase.CT_Tabako; //タバコ追従先
                Transform ct_lighter = constraintDatabase.CT_Lighter; //ライター追従先

                tpt.Case_Scale = ct_case.localScale;
                tpt.Tabako_Scale = ct_tabacco.localScale;
                tpt.Lighter_Scale = ct_lighter.localScale;

                //---------------------------------------------------
                //位置・回転取得用
                VRCParentConstraint pc_case = constraintDatabase.PC_Case;
                VRCParentConstraint pc_tabacco = constraintDatabase.PC_Tabako;
                VRCParentConstraint pc_lighter = constraintDatabase.PC_Lighter;

                Transform[] caseTransforms = GetTransforms(pc_case);
                Transform[] tabaccoTransforms = GetTransforms(pc_tabacco);
                Transform[] lighterTransforms = GetTransforms(pc_lighter);

                tpt.Case_Right = GetPlacement(caseTransforms[(int)ConstraintSourcePosition.Right]);
                tpt.Case_Left = GetPlacement(caseTransforms[(int)ConstraintSourcePosition.Left]);

                tpt.Tabako_Right = GetPlacement(tabaccoTransforms[(int)ConstraintSourcePosition.Right]);
                tpt.Tabako_Left = GetPlacement(tabaccoTransforms[(int)ConstraintSourcePosition.Left]);
                tpt.Tabako_Mouth = GetPlacement(tabaccoTransforms[(int)ConstraintSourcePosition.Mouth]);

                tpt.Lighter_Right = GetPlacement(lighterTransforms[(int)ConstraintSourcePosition.Right]);
                tpt.Lighter_Left = GetPlacement(lighterTransforms[(int)ConstraintSourcePosition.Left]);

                setting.tabakoPrefabTransform = tpt;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.ToString());
            }
        }

        [MenuItem(MenuItemDictionary.p_CopyTransforms,
            priority = 100,
            secondaryPriority = 6,
            validate = true)]
        private static bool CopyPrefabTransformsValidate()
        {
            GameObject go = Selection.activeGameObject;
            if (go == null)
                return false;

            Transform avatarRoot = RuntimeUtil.FindAvatarInParents(go.transform);
            if (avatarRoot == null)
                return false;

            return SatabakoDatabase.s.constraintDatabases.ContainsKey(avatarRoot);
        }

        [MenuItem(MenuItemDictionary.p_PasteTransforms,
            priority = 100 + 12,
            secondaryPriority = 9 + 20)]
        private static void PastePrefabTransformsNonMirror()
        {
            PastePrefabTransforms(false);
        }


        [MenuItem(MenuItemDictionary.p_PasteTransformsMirror, priority = 100, secondaryPriority = 7)]
        private static void PastePrefabMirrorTransforms()
        {
            PastePrefabTransforms(true);
        }


        [MenuItem(MenuItemDictionary.p_PasteTransforms,
            priority = 100,
            secondaryPriority = 9,
            validate = true)]
        private static bool PastePrefabTransformsValidate()
        {
            GameObject go = Selection.activeGameObject;
            if (go == null)
                return false;

            return go.transform.GetComponentInChildren<TabakoScriptBehaviour>(true) != null;
        }

        [MenuItem(MenuItemDictionary.p_PasteTransformsMirror,
            priority = 100,
            secondaryPriority = 7,
            validate = true)]
        private static bool PastePrefabMirrorTransformsValidate()
        {
            GameObject go = Selection.activeGameObject;
            if (go == null)
                return false;

            return go.transform.GetComponentInChildren<TabakoScriptBehaviour>(true) != null;
        }

        private static void PastePrefabTransforms(bool mirror)
        {
            GameObject go = Selection.activeGameObject;
            if (go == null)
                return;

            TabakoScriptBehaviour tabakoScript = go.GetComponentInChildren<TabakoScriptBehaviour>(true);
            CopyPasteSetting.TabakoPrefabTransform tpt = setting.tabakoPrefabTransform;

            //Transform avatarRoot = RuntimeUtil.FindAvatarInParents(go.transform);
            Transform t = tabakoScript.transform;

            if (tpt == null || t == null)
                return;

            //---------------------------------------------------
            //スケール取得用
            Transform ct_case = tabakoScript.CT_Case; //ケース追従先
            Transform ct_tabacco = tabakoScript.CT_Tabako; //タバコ追従先
            Transform ct_lighter = tabakoScript.CT_Lighter; //ライター追従先

            //---------------------------------------------------
            //位置・回転取得用
            VRCParentConstraint pc_case = tabakoScript.PC_Case;
            VRCParentConstraint pc_tabacco = tabakoScript.PC_Tabako;
            VRCParentConstraint pc_lighter = tabakoScript.PC_Lighter;

            Transform[] caseTransforms = GetTransforms(pc_case);
            Transform[] tabaccoTransforms = GetTransforms(pc_tabacco);
            Transform[] lighterTransforms = GetTransforms(pc_lighter);

            Transform case_R = caseTransforms[(int)ConstraintSourcePosition.Right];
            Transform case_L = caseTransforms[(int)ConstraintSourcePosition.Left];

            Transform tabacco_R = tabaccoTransforms[(int)ConstraintSourcePosition.Right];
            Transform tabacco_L = tabaccoTransforms[(int)ConstraintSourcePosition.Left];
            Transform tabacco_M = tabaccoTransforms[(int)ConstraintSourcePosition.Mouth];

            Transform lighter_R = lighterTransforms[(int)ConstraintSourcePosition.Right];
            Transform lighter_L = lighterTransforms[(int)ConstraintSourcePosition.Left];

            Undo.RecordObjects(new Object[]
            {
                ct_case,
                ct_tabacco,
                ct_lighter,

                case_R,
                case_L,

                tabacco_R,
                tabacco_L,
                tabacco_M,

                lighter_R,
                lighter_L
            }, "Paste Prefab Transforms");

            //---------------------------------------------------
            //スケール適用
            ct_case.localScale = tpt.Case_Scale;
            ct_tabacco.localScale = tpt.Tabako_Scale;
            ct_lighter.localScale = tpt.Lighter_Scale;

            //---------------------------------------------------
            //位置・回転適用
            if (mirror)
            {
                PastePlacementAndMirrorByBoneProxy(tpt.Case_Right, case_R, case_L, t);
                PastePlacementAndMirrorByBoneProxy(tpt.Tabako_Right, tabacco_R, tabacco_L, t);
                PastePlacementByBoneBroxy(tpt.Tabako_Mouth, tabacco_M);
                PastePlacementAndMirrorByBoneProxy(tpt.Lighter_Right, lighter_R, lighter_L, t);
            }
            else
            {
                PastePlacementByBoneBroxy(tpt.Case_Right, case_R);
                PastePlacementByBoneBroxy(tpt.Case_Left, case_L);

                PastePlacementByBoneBroxy(tpt.Tabako_Right, tabacco_R);
                PastePlacementByBoneBroxy(tpt.Tabako_Left, tabacco_L);
                PastePlacementByBoneBroxy(tpt.Tabako_Mouth, tabacco_M);

                PastePlacementByBoneBroxy(tpt.Lighter_Right, lighter_R);
                PastePlacementByBoneBroxy(tpt.Lighter_Left, lighter_L);
            }

            EditorUtility.SetDirty(ct_case);
            EditorUtility.SetDirty(ct_tabacco);
            EditorUtility.SetDirty(ct_lighter);

            EditorUtility.SetDirty(caseTransforms[0]);
            EditorUtility.SetDirty(caseTransforms[1]);

            EditorUtility.SetDirty(tabaccoTransforms[0]);
            EditorUtility.SetDirty(tabaccoTransforms[1]);
            EditorUtility.SetDirty(tabaccoTransforms[2]);

            EditorUtility.SetDirty(lighterTransforms[0]);
            EditorUtility.SetDirty(lighterTransforms[1]);
        }

        private static bool IsTabakoScriptObject()
        {
            GameObject go = Selection.activeGameObject;
            if (go == null)
                return false;

            TabakoScriptBehaviour tabakoScript = go.GetComponent<TabakoScriptBehaviour>();
            return tabakoScript != null;
        }

        private static Transform[] GetTransforms(VRCParentConstraint constraint)
        {
            //constraint.Sources.Select(x => x.SourceTransform).ToArray();
            Transform[] output = new Transform[constraint.Sources.Count];
            for (int i = 0; i < output.Length; i++)
            {
                output[i] = constraint.Sources[i].SourceTransform;
            }

            return output;
        }

        private static CopyPasteSetting.Placement GetPlacement(Transform transform)
        {
            return new CopyPasteSetting.Placement() { position = transform.localPosition, rotation = transform.localRotation };
        }

        private static void PastePlacementByBoneBroxy(CopyPasteSetting.Placement placement, Transform transform)
        {
            ModularAvatarBoneProxy maBoneProxy = transform.GetComponent<ModularAvatarBoneProxy>();

            if (maBoneProxy == null) return;
            Transform bone = maBoneProxy.target;
            if (bone == null) return;

            Vector3 globalPosition = bone.TransformPoint(placement.position);
            Quaternion globalRotation = bone.rotation * placement.rotation;

            transform.position = globalPosition;
            transform.rotation = globalRotation;
        }

        private static void PastePlacementAndMirrorByBoneProxy(CopyPasteSetting.Placement placement, Transform Right, Transform Left, Transform root)
        {
            ModularAvatarBoneProxy maBoneProxy = Right.GetComponent<ModularAvatarBoneProxy>();

            if (maBoneProxy == null) return;
            Transform bone = maBoneProxy.target;
            if (bone == null) return;

            Vector3 globalPosition = bone.TransformPoint(placement.position);
            Quaternion globalRotation = bone.rotation * placement.rotation;

            Right.position = globalPosition;
            Right.rotation = globalRotation;

            Left.position = globalPosition;
            Left.rotation = globalRotation;
            TransformUtility.MirrorX(Left, root);
        }
        #endregion

        public static Transform FindAvatarDescriptorInParent(Transform t)
        {
            while (t != null)
            {
                if (IsAvatarDescriptor(t))
                    return t;

                t = t.parent;
            }

            return null;
        }

        public static bool IsAvatarDescriptor(Transform target)
        {
            return target.TryGetComponent<VRCAvatarDescriptor>(out _);
        }
    }
}