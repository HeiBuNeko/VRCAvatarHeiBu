using System;
using net.satania_shopping.tabakosystem.runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;

namespace net.satania_shopping.tabakosystem.editor
{
    internal sealed class WizardPreviewSession : IDisposable
    {
        public GameObject Source { get; }
        public bool SourceIsAsset { get; }
        public GameObject SourceTemplateRoot { get; private set; }

        public GameObject Clone { get; private set; }
        public TabakoScriptBehaviour Script { get; private set; }
        public TabakoTemplateRefs Refs { get; private set; }
        public int ScriptCount { get; private set; }
        public PrefabSaveMode SaveMode { get; private set; }
        public string SaveModeAssetPath { get; private set; }
        public string Error { get; private set; }

        public bool IsValid => Clone != null && Script != null && Refs != null;
        public Transform CloneRoot => Clone != null ? Clone.transform : null;

        private WizardPreviewSession(GameObject source, bool isAsset)
        {
            Source = source;
            SourceIsAsset = isAsset;
        }

        public static bool IsAcceptableTarget(GameObject target, out string reason)
        {
            reason = null;
            if (target == null)
            {
                reason = "";
                return false;
            }
            if (target.GetComponent<VRCAvatarDescriptor>() == null)
            {
                reason = "t_Wiz_MsgTargetNotAvatar";
                return false;
            }
            if (EditorUtility.IsPersistent(target) && !PrefabUtility.IsPartOfPrefabAsset(target))
            {
                reason = "t_Wiz_MsgTargetNotAvatar";
                return false;
            }
            return true;
        }

        public static WizardPreviewSession Create(GameObject source, Scene scene)
        {
            var session = new WizardPreviewSession(source, EditorUtility.IsPersistent(source));
            try
            {
                session.Build(scene);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                session.Error = ex.Message;
                session.Dispose();
            }
            return session;
        }

        private void Build(Scene scene)
        {
            if (SourceIsAsset)
            {
                var instance = PrefabUtility.InstantiatePrefab(Source, scene) as GameObject;
                if (instance == null)
                    throw new InvalidOperationException("Prefab を配置できませんでした");
                if (PrefabUtility.IsPartOfPrefabInstance(instance))
                    PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                Clone = instance;
            }
            else
            {
                Clone = (GameObject)Object.Instantiate(Source, scene);
            }

            Clone.name = Source.name + " (Preview)";
            Clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Clone.transform.localScale = Source.transform.localScale;
            Clone.SetActive(true);

            if (SourceIsAsset)
                SetupTemplateFromAsset();
            else
                SetupTemplateFromInstance();

            ResolveRefs();
        }

        private void SetupTemplateFromInstance()
        {
            foreach (var copied in UnsupportedAvatarOps.FindScripts(Clone.transform))
                Object.DestroyImmediate(copied.gameObject);

            var sourceScripts = UnsupportedAvatarOps.FindScripts(Source.transform);
            ScriptCount = sourceScripts.Length;
            if (ScriptCount == 0)
            {
                UnsupportedAvatarOps.AddTemplate(Clone.transform, false);
                return;
            }
            if (ScriptCount > 1)
                return;

            SourceTemplateRoot = sourceScripts[0].gameObject;
            var mode = UnsupportedAvatarOps.DetectSaveMode(SourceTemplateRoot, out string assetPath);
            GameObject asset = null;
            if (mode == PrefabSaveMode.NewVariant || mode == PrefabSaveMode.UpdateVendorVariant || mode == PrefabSaveMode.UpdateUserVariant)
                asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            Transform src = SourceTemplateRoot.transform;
            GameObject instance;
            if (asset != null)
            {
                instance = PrefabUtility.InstantiatePrefab(asset, Clone.transform) as GameObject;
                if (instance == null)
                    throw new InvalidOperationException("テンプレートを配置できませんでした");
                instance.name = SourceTemplateRoot.name;
                instance.transform.localPosition = src.localPosition;
                instance.transform.localRotation = src.localRotation;
                instance.transform.localScale = src.localScale;
                UnsupportedAvatarOps.TransferPrefabModifications(SourceTemplateRoot, instance, Source.transform, Clone.transform);
            }
            else
            {
                instance = Object.Instantiate(SourceTemplateRoot, Clone.transform);
                instance.name = SourceTemplateRoot.name;
                instance.transform.localPosition = src.localPosition;
                instance.transform.localRotation = src.localRotation;
                instance.transform.localScale = src.localScale;
            }
        }

        private void SetupTemplateFromAsset()
        {
            var scripts = UnsupportedAvatarOps.FindScripts(Clone.transform);
            ScriptCount = scripts.Length;
            if (ScriptCount == 0)
                UnsupportedAvatarOps.AddTemplate(Clone.transform, false);
        }

        public bool ReplaceTemplate(GameObject prefabAsset, out string error)
        {
            error = null;
            if (Clone == null)
            {
                error = "プレビューがありません";
                return false;
            }
            if (prefabAsset == null || !EditorUtility.IsPersistent(prefabAsset) || UnsupportedAvatarOps.FindScripts(prefabAsset.transform).Length != 1)
            {
                error = "TabakoScriptBehaviour を 1 つ持つ Prefab アセットではありません";
                return false;
            }

            Transform old = Script != null ? Script.transform : null;
            Vector3 localPosition = old != null ? old.localPosition : Vector3.zero;
            Quaternion localRotation = old != null ? old.localRotation : Quaternion.identity;
            Vector3 localScale = old != null ? old.localScale : Vector3.one;

            foreach (var copied in UnsupportedAvatarOps.FindScripts(Clone.transform))
                Object.DestroyImmediate(copied.gameObject);

            var instance = PrefabUtility.InstantiatePrefab(prefabAsset, Clone.transform) as GameObject;
            if (instance == null)
            {
                error = "プレハブを配置できませんでした";
                ResolveRefs();
                return false;
            }
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            instance.transform.localScale = localScale;

            ResolveRefs();
            if (!IsValid)
            {
                error = "テンプレートの参照を解決できませんでした";
                return false;
            }
            return true;
        }

        public void ResolveRefs()
        {
            Script = null;
            Refs = null;
            SaveMode = PrefabSaveMode.NotPrefabInstance;
            SaveModeAssetPath = null;
            if (Clone == null)
                return;

            var scripts = UnsupportedAvatarOps.FindScripts(Clone.transform);
            ScriptCount = scripts.Length;
            if (ScriptCount != 1)
                return;

            Script = scripts[0];
            Refs = TabakoTemplateRefs.Resolve(Script);
            SaveMode = UnsupportedAvatarOps.DetectSaveMode(Script.gameObject, out string path);
            SaveModeAssetPath = path;
        }

        public Bounds? CloneBounds() => PreviewViewport.RendererBounds(Clone);

        public void Dispose()
        {
            if (Clone != null)
            {
                Object.DestroyImmediate(Clone);
                Clone = null;
            }
            Script = null;
            Refs = null;
        }
    }
}
