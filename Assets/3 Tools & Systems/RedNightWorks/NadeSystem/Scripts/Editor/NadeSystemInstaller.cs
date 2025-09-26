using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using System.IO;
using VRC.SDK3.Dynamics.Contact.Components;
using System;
using nadena.dev.ndmf;
using nadena.dev.modular_avatar.core;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Security;
using VRC.SDK3.Dynamics.Constraint.Components;

namespace RedNightWorks.NadeSystem
{
    public class NadeSystemInstaller : EditorWindow
    {
        // GUIDs for prefabs
        private const string NadeSystemGUID = "491c3f399da5d064d9966982ddf0d191";
        private const string NadeShadowGUID = "fd1d0e8cc6fc6f646ad9f24b156a31ac";
        private const string DummyLightGUID = "c46c6e537bbb1a140957ad83f15c5afb";
        private const string NadeShadowMenuGUID = "7f6a3a1aa3e98df4e88571c89d365603";
        private const string NadeShpereGUID = "e6971546677df8d449b746136433e2cc";
        private const string NadeShpereMenuGUID = "4912f408e5d621d43a4d48dd368e3c3a";


        // Default values and constants
        private const float DefaultContactRadius = 0.14f;
        private const float MinContactRadius = 0.01f;
        private const float MaxContactRadius = 1.0f;
        private const float DefaultHeadOffsetY = 0.035f;
        private const float DeactivationDelay = 0.5f;

        // UI state
        private VRCAvatarDescriptor _avatar;
        private float _contactRadius = DefaultContactRadius;
        private float _headOffsetY = DefaultHeadOffsetY;
        private bool _installNadeShaderForHands = true;
        private bool _installNadeShaderForHead = false;
        private bool _installNadeSphere = false;

        // Localization
        private static readonly string[] LangOptions = { "English", "Japanese", "Korean", "Chinese (Simplified)", "Chinese (Traditional)" };
        private static int _langIndex = 0;

        // For delayed action
        private float _delayStart;

        [MenuItem("RedNightWorks/NadeSystemInstaller")]
        public static void ShowWindow()
        {
            InitializeLanguage();
            var window = GetWindow<NadeSystemInstaller>(true, "Nade System Installer");
            window.minSize = new Vector2(350, 300);
        }

        private static void InitializeLanguage()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Japanese:
                    _langIndex = 1;
                    break;
                case SystemLanguage.Korean:
                    _langIndex = 2;
                    break;
                case SystemLanguage.ChineseSimplified:
                    _langIndex = 3;
                    break;
                case SystemLanguage.ChineseTraditional:
                    _langIndex = 4;
                    break;
                case SystemLanguage.Chinese:
                    _langIndex = 3;
                    break;
                default:
                    _langIndex = 0;
                    break;
            }
        }

        private void OnGUI()
        {
            DrawLanguageSelection();
            EditorGUILayout.Space();

            _avatar = EditorGUILayout.ObjectField(Localize.avatar[_langIndex], _avatar, typeof(VRCAvatarDescriptor), true) as VRCAvatarDescriptor;
            EditorGUILayout.Space();

            DrawContactParameters();
            EditorGUILayout.Space();

            DrawShaderInstallOptions();
            EditorGUILayout.Space();

            DrawNadeSphereOption();
            EditorGUILayout.Space();

            DrawInstallButton();
        }

        private void DrawLanguageSelection()
        {
            _langIndex = EditorGUILayout.Popup("Language", _langIndex, LangOptions);
        }

        private void DrawContactParameters()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField(Localize.contactParameters[_langIndex]);
            _contactRadius = EditorGUILayout.FloatField(Localize.contactRadius[_langIndex], _contactRadius);
            _headOffsetY = EditorGUILayout.FloatField(Localize.contactOffsetY[_langIndex], _headOffsetY);
            EditorGUILayout.EndVertical();
        }

        private void DrawShaderInstallOptions()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField(Localize.shadowShaderInstall[_langIndex]);
            var defaultLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 250;
            _installNadeShaderForHands = EditorGUILayout.Toggle(Localize.installHands[_langIndex], _installNadeShaderForHands);
            _installNadeShaderForHead = EditorGUILayout.Toggle(Localize.installHead[_langIndex], _installNadeShaderForHead);
            EditorGUIUtility.labelWidth = defaultLabelWidth;
            EditorGUILayout.EndVertical();
        }

        private void DrawNadeSphereOption()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            var defaultLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 250;
            _installNadeSphere = EditorGUILayout.Toggle(Localize.installNadeSphere[_langIndex], _installNadeSphere);
            EditorGUIUtility.labelWidth = defaultLabelWidth;
            EditorGUILayout.EndVertical();
        }
        
        private void DrawInstallButton()
        {
            if (GUILayout.Button("Setup!"))
            {
                InstallNadeSystem();
            }
        }

        private void InstallNadeSystem()
        {
            if (!IsAvatarSelected()) return;

            ValidateParameters();
            
            CleanupExistingSystem();

            var nadeSystemRoot = InstantiateNadeSystemPrefab();
            if (nadeSystemRoot == null) return;

            var nadeObjects = FindNadeSystemObjects(nadeSystemRoot.transform);

            ConfigureComponents(nadeObjects);
            InstallShaders(nadeObjects, nadeSystemRoot.transform);
            InstallNadeSphere(nadeObjects, nadeSystemRoot.transform);
            
            ScheduleDelayedDeactivation(nadeObjects.HeadSystem.gameObject);

            EditorUtility.DisplayDialog(Localize.installCompleteTitle[_langIndex], Localize.installCompleteMsg[_langIndex], "OK");
            Debug.Log($"{_avatar.gameObject.name}: NadeSystem install complete!");
        }

        private bool IsAvatarSelected()
        {
            if (_avatar != null) return true;
            
            EditorUtility.DisplayDialog(
                Localize.notSelectAvatarTitle[_langIndex], 
                Localize.notSelectAvatarMsg[_langIndex], 
                "OK");
            return false;
        }

        private void ValidateParameters()
        {
            if (_contactRadius < MinContactRadius || _contactRadius > MaxContactRadius)
            {
                _contactRadius = DefaultContactRadius;
            }
        }

        private void CleanupExistingSystem()
        {
            var existingSystem = _avatar.transform.Find("NadeSystem");
            if (existingSystem != null)
            {
                DestroyImmediate(existingSystem.gameObject);
            }
        }

        private GameObject InstantiateNadeSystemPrefab()
        {
            var nadeSystemPrefab = GetPrefabByGuid(NadeSystemGUID, "NadeSystem");
            return (GameObject)PrefabUtility.InstantiatePrefab(nadeSystemPrefab, _avatar.transform);
        }

        private NadeSystemObjects FindNadeSystemObjects(Transform nadeSystemRoot)
        {
            return new NadeSystemObjects
            {
                RxHeadMain = FindRequiredChild(nadeSystemRoot, "RxHeadMain"),
                HeadSystem = FindRequiredChild(nadeSystemRoot, "HeadSystem"),
                RightHandSystem = FindRequiredChild(nadeSystemRoot, "RightHandSystem"),
                LeftHandSystem = FindRequiredChild(nadeSystemRoot, "LeftHandSystem"),
                ExMenu = FindRequiredChild(nadeSystemRoot, "ExMenu"),
                NadeControlMenu = FindRequiredChild(nadeSystemRoot, "ExMenu/Nade Control")
            };
        }

        private void ConfigureComponents(NadeSystemObjects nadeObjects)
        {
            var headBone = _avatar.GetComponent<Animator>()?.GetBoneTransform(HumanBodyBones.Head);
            if (headBone == null)
            {
                Debug.LogError("Head bone not found on avatar.");
                return;
            }
            
            Vector3 avatarHeadCenter = _avatar.collider_head.position + headBone.position;

            var rxHeadMainContact = nadeObjects.RxHeadMain.GetComponent<VRCContactReceiver>();
            rxHeadMainContact.radius = _contactRadius;
            nadeObjects.RxHeadMain.transform.position = avatarHeadCenter + new Vector3(0f, _headOffsetY, 0f);
            nadeObjects.HeadSystem.transform.position = _avatar.ViewPosition + _avatar.transform.position;
        }

        private void InstallShaders(NadeSystemObjects nadeObjects, Transform nadeSystemRoot)
        {
            if (!_installNadeShaderForHands && !_installNadeShaderForHead) return;

            var nadeShadowPrefab = GetPrefabByGuid(NadeShadowGUID, "NadeShadow");
            if (_installNadeShaderForHands)
            {
                PrefabUtility.InstantiatePrefab(nadeShadowPrefab, nadeObjects.RightHandSystem.transform);
                PrefabUtility.InstantiatePrefab(nadeShadowPrefab, nadeObjects.LeftHandSystem.transform);
            }
            if (_installNadeShaderForHead)
            {
                PrefabUtility.InstantiatePrefab(nadeShadowPrefab, nadeObjects.HeadSystem.transform);
            }

            var dummyLightPrefab = GetPrefabByGuid(DummyLightGUID, "DummyLight");
            PrefabUtility.InstantiatePrefab(dummyLightPrefab, nadeSystemRoot);

            var nadeShadowMenuPrefab = GetPrefabByGuid(NadeShadowMenuGUID, "NadeShadowMenu");
            PrefabUtility.InstantiatePrefab(nadeShadowMenuPrefab, nadeObjects.ExMenu.transform);
        }

        private void InstallNadeSphere(NadeSystemObjects nadeObjects, Transform nadeSystemRoot)
        {
            if (!_installNadeSphere) return;

            var nadeSpherePrefab = GetPrefabByGuid(NadeShpereGUID, "NadeSphere");
            var nadeSphereMenuPrefab = GetPrefabByGuid(NadeShpereMenuGUID, "NadeSphereMenu");
            var nadeSphereInstance = PrefabUtility.InstantiatePrefab(nadeSpherePrefab, nadeSystemRoot) as GameObject;
            PrefabUtility.InstantiatePrefab(nadeSphereMenuPrefab, nadeObjects.NadeControlMenu.transform);
        }

        private void ScheduleDelayedDeactivation(GameObject headSystem)
        {
            headSystem.SetActive(true);
            _delayStart = (float)EditorApplication.timeSinceStartup;
            EditorApplication.update += DelayUpdate;
        }

        private void DelayUpdate()
        {
            if (EditorApplication.timeSinceStartup < _delayStart + DeactivationDelay) return;

            EditorApplication.update -= DelayUpdate;

            var nadeSystemObj = _avatar.transform.Find("NadeSystem");
            if (nadeSystemObj == null) return;
            
            var headSystemObj = nadeSystemObj.transform.Find("HeadSystem");
            if (headSystemObj != null)
            {
                headSystemObj.gameObject.SetActive(false);
            }
        }

        private static GameObject GetPrefabByGuid(string guid, string prefabName)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                throw new FileNotFoundException($"Prefab '{prefabName}' with GUID '{guid}' not found. NadeSystem might be broken or missing.");
            }
            
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new FileNotFoundException($"Failed to load prefab '{prefabName}' from path '{path}'.");
            }
            return prefab;
        }

        private static Transform FindRequiredChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                throw new InvalidOperationException($"Required child object '{name}' not found in '{parent.name}'. The NadeSystem prefab might be broken.");
            }
            return child;
        }

        // Helper class to pass around the main GameObjects
        private class NadeSystemObjects
        {
            public Transform RxHeadMain { get; set; }
            public Transform HeadSystem { get; set; }
            public Transform RightHandSystem { get; set; }
            public Transform LeftHandSystem { get; set; }
            public Transform ExMenu { get; set; }
            public Transform NadeControlMenu { get; set; }
        }
    }
}