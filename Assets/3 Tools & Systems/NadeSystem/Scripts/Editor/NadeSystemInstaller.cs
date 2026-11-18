using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using System.IO;
using VRC.SDK3.Dynamics.Contact.Components;
using System;
using nadena.dev.modular_avatar.core;
using System.Reflection;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace RedNightWorks.NadeSystem
{
    public class NadeSystemInstaller : EditorWindow
    {
        // GUIDs for prefabs
        private const string NadeSystemGUID = "491c3f399da5d064d9966982ddf0d191";
        private const string FootSystemGUID = "ca3cfa8587af6cc4f8f7205a0c16e108";
        private const string FootSystemMenuGUID = "f7ce8e50badf67b418b0c9d5b7e73442";
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
        private const float DeactivationDelay = 1f;
        private const int SoundSlotCount = 16;

        // UI state
        [SerializeField] private VRCAvatarDescriptor _avatar;
        [SerializeField] private float _contactRadius = DefaultContactRadius;
        [SerializeField] private float _headOffsetY = DefaultHeadOffsetY;
        [SerializeField] private bool _installNadeShaderForHands = true;
        [SerializeField] private bool _installNadeShaderForFeet = true;
        [SerializeField] private bool _installNadeShaderForHead = false;
        [SerializeField] private bool _installNadeSphere = false;
        [SerializeField] private bool _installFootSystem = false;
        [SerializeField] private WriteDefaultsMode _writeDefaultsMode = WriteDefaultsMode.UsePrefabSettings;
        [SerializeField] private MenuLanguage _menuLanguage = MenuLanguage.Automatic;
        [SerializeField] private AudioClip[] _audioClips;
        [SerializeField] private VRCAvatarDescriptor _audioClipsAvatar;
        [SerializeField] private bool _audioClipsLoadedForAvatar;
        [SerializeField] private bool _loadAvatarAudioClips = true;
        [SerializeField] private bool _soundSettingsFoldout;
        [SerializeField] private bool _additionalFeaturesFoldout;
        [SerializeField] private bool _compatibilityFoldout;
        private Vector2 _scrollPosition;
        private string _budgetKey;
        private int? _estimatedParameterBits;
        private bool _budgetUnavailable;

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
            window.minSize = new Vector2(420, 300);
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

        private void OnEnable()
        {
            InitializeLanguage();
            EnsureAudioClips();
            TrySelectAvatar();
            LoadAudioClipsForSelectedAvatar();
            EditorApplication.projectChanged += InvalidateParameterBudget;
            Undo.undoRedoPerformed += InvalidateParameterBudget;
        }

        private void OnDisable()
        {
            _budgetKey = null;
            EditorApplication.projectChanged -= InvalidateParameterBudget;
            Undo.undoRedoPerformed -= InvalidateParameterBudget;
        }

        private void OnFocus() => InvalidateParameterBudget();

        private void InvalidateParameterBudget()
        {
            _budgetKey = null;
            Repaint();
        }

        private void OnSelectionChange()
        {
            if (_avatar == null) TrySelectAvatar();
            Repaint();
        }

        private void TrySelectAvatar()
        {
            if (_avatar != null || Selection.activeGameObject == null) return;
            var selected = Selection.activeGameObject;
            if (EditorUtility.IsPersistent(selected) || !selected.scene.IsValid()) return;
            _avatar = selected.GetComponentInParent<VRCAvatarDescriptor>();
        }

        private bool EnsureAudioClips()
        {
            // A serialized array preserves edits (including empty slots) across script reloads.
            if (_audioClips != null && _audioClips.Length == SoundSlotCount) return true;
            if (_audioClips != null && _audioClips.Length > 0)
            {
                Array.Resize(ref _audioClips, SoundSlotCount);
                return true;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(NadeSystemGUID));
            var defaults = prefab != null ? prefab.GetComponent<NadeSystemSettings>() : null;
            if (defaults == null) return false;

            _audioClips = new AudioClip[SoundSlotCount];
            if (defaults.audioClips != null)
                Array.Copy(defaults.audioClips, _audioClips, Math.Min(defaults.audioClips.Length, SoundSlotCount));
            return true;
        }

        private void LoadAudioClipsForSelectedAvatar()
        {
            if (!_loadAvatarAudioClips)
            {
                EnsureAudioClips();
                return;
            }
            // Load once per selection. After that, the Installer list is the source of truth.
            // Serialize the selection marker so script reloads do not discard pending edits.
            if (_audioClipsLoadedForAvatar && _audioClipsAvatar == _avatar) return;

            _audioClipsAvatar = _avatar;
            _audioClips = null;
            _audioClipsLoadedForAvatar = false;
            var existingRoot = _avatar != null ? _avatar.transform.Find("NadeSystem") : null;
            var existing = existingRoot != null ? existingRoot.GetComponent<NadeSystemSettings>() : null;
            if (existing != null)
            {
                _audioClips = new AudioClip[SoundSlotCount];
                if (existing.audioClips != null)
                    Array.Copy(existing.audioClips, _audioClips,
                        Math.Min(existing.audioClips.Length, SoundSlotCount));
                _audioClipsLoadedForAvatar = true;
            }
            else
            {
                _audioClipsLoadedForAvatar = EnsureAudioClips();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(12, 12, 10, 10) });
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Nade System {NadeSystemVersion.DisplayText}", EditorStyles.boldLabel);
            _langIndex = EditorGUILayout.Popup(_langIndex, LangOptions, GUILayout.Width(145));
            var resetIcon = EditorGUIUtility.IconContent("Refresh");
            var resetContent = new GUIContent(resetIcon.image, Localize.resetDefaultsTooltip[_langIndex]);
            if (GUILayout.Button(resetContent, GUILayout.Width(24), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                ResetInstallerDefaults();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);

            _avatar = EditorGUILayout.ObjectField(Localize.avatar[_langIndex], _avatar,
                typeof(VRCAvatarDescriptor), true) as VRCAvatarDescriptor;
            LoadAudioClipsForSelectedAvatar();
            if (_avatar == null)
                EditorGUILayout.LabelField(Localize.selectAvatarHint[_langIndex], EditorStyles.wordWrappedLabel);
            else if (EditorUtility.IsPersistent(_avatar))
                EditorGUILayout.HelpBox(Localize.sceneAvatarHint[_langIndex], MessageType.Warning);
            EditorGUILayout.Space(8);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            _additionalFeaturesFoldout = EditorGUILayout.Foldout(_additionalFeaturesFoldout,
                Localize.additionalFeatures[_langIndex], true, EditorStyles.foldoutHeader);
            if (_additionalFeaturesFoldout)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(16);
                EditorGUILayout.BeginVertical();
                DrawFootSystemOption();
                DrawNadeSphereOption();
                DrawShaderInstallOptions();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space(4);

            DrawSoundSettings();
            EditorGUILayout.Space(4);

            _compatibilityFoldout = EditorGUILayout.Foldout(_compatibilityFoldout,
                Localize.compatibilitySettings[_langIndex], true, EditorStyles.foldoutHeader);
            if (_compatibilityFoldout)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(16);
                EditorGUILayout.BeginVertical();
                _menuLanguage = (MenuLanguage)EditorGUILayout.Popup(Localize.menuLanguageLabel[_langIndex],
                    (int)_menuLanguage, Localize.MenuLanguageOptions(_langIndex));
                EditorGUILayout.Space(4);
                DrawContactParameters();
                EditorGUILayout.Space(4);
                _writeDefaultsMode = (WriteDefaultsMode)EditorGUILayout.Popup(
                    "Write Defaults", (int)_writeDefaultsMode, Localize.writeDefaultsOptions[_langIndex]);
                if (_writeDefaultsMode != WriteDefaultsMode.UsePrefabSettings)
                    EditorGUILayout.HelpBox(Localize.writeDefaultsHelp[_langIndex], MessageType.Info);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(8);
            DrawParameterBudgetWarning();
            DrawInstallButton();
            EditorGUILayout.EndVertical();
        }

        private void DrawSoundSettings()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            _soundSettingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(
                _soundSettingsFoldout, Localize.soundSettings[_langIndex]);
            if (_soundSettingsFoldout)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(16);
                EditorGUILayout.BeginVertical();
                EditorGUI.BeginChangeCheck();
                _loadAvatarAudioClips = EditorGUILayout.ToggleLeft(
                    Localize.loadAvatarAudioClips[_langIndex], _loadAvatarAudioClips);
                if (EditorGUI.EndChangeCheck())
                {
                    _audioClipsLoadedForAvatar = false;
                    LoadAudioClipsForSelectedAvatar();
                }
                if (EnsureAudioClips())
                {
                    EditorGUILayout.HelpBox(Localize.soundSettingsHelp[_langIndex], MessageType.Info);
                    for (int i = 0; i < SoundSlotCount; i++)
                    {
                        _audioClips[i] = (AudioClip)EditorGUILayout.ObjectField(
                            $"{Localize.soundSlot[_langIndex]} {i + 1}", _audioClips[i], typeof(AudioClip), false);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(Localize.soundSettingsUnavailable[_langIndex], MessageType.Error);
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.EndVertical();
        }

        private void DrawFootSystemOption()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            var defaultLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 250;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Localize.installFoot[_langIndex], EditorStyles.wordWrappedLabel);
            EditorGUIUtility.labelWidth = defaultLabelWidth;
            _installFootSystem = EditorGUILayout.Toggle(_installFootSystem, GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawContactParameters()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField(Localize.contactParameters[_langIndex], EditorStyles.boldLabel);
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

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Localize.installHands[_langIndex], EditorStyles.wordWrappedLabel);
            _installNadeShaderForHands = EditorGUILayout.Toggle(_installNadeShaderForHands, GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();

            if (_installFootSystem)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Localize.installFeet[_langIndex], EditorStyles.wordWrappedLabel);
                _installNadeShaderForFeet = EditorGUILayout.Toggle(_installNadeShaderForFeet, GUILayout.Width(40));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Localize.installHead[_langIndex], EditorStyles.wordWrappedLabel);
            _installNadeShaderForHead = EditorGUILayout.Toggle(_installNadeShaderForHead, GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = defaultLabelWidth;
            EditorGUILayout.EndVertical();
        }

        private void DrawNadeSphereOption()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            var defaultLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 250;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Localize.installNadeSphere[_langIndex], EditorStyles.wordWrappedLabel);
            _installNadeSphere = EditorGUILayout.Toggle(_installNadeSphere, GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = defaultLabelWidth;
            EditorGUILayout.EndVertical();
        }
        
        private void DrawParameterBudgetWarning()
        {
            if (_avatar == null || EditorUtility.IsPersistent(_avatar)
                || EditorApplication.isPlayingOrWillChangePlaymode) return;

            string key = $"{_avatar.GetInstanceID()}:{_installFootSystem}:{_installNadeSphere}:"
                + $"{_installNadeShaderForHands}:{_installNadeShaderForFeet}:{_installNadeShaderForHead}";
            if (_budgetKey != key)
            {
                // Delay work until after this GUI event; never build an avatar during repaint.
                _budgetKey = key;
                _estimatedParameterBits = null;
                _budgetUnavailable = false;
                EditorApplication.delayCall += () =>
                {
                    if (this == null || _budgetKey != key || _avatar == null
                        || EditorApplication.isPlayingOrWillChangePlaymode) return;
                    try
                    {
                        _estimatedParameterBits = NadeParameterBudget.Estimate(_avatar.gameObject, parent =>
                        {
                            var prefab = GetPrefabByGuid(NadeSystemGUID, "NadeSystem");
                            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                            var objects = FindNadeSystemObjects(root.transform);
                            objects.FootSystem = InstallFootSystem(objects, root.transform);
                            InstallShaders(objects, root.transform);
                            InstallNadeSphere(objects, root.transform);
                        });
                    }
                    catch (Exception)
                    {
                        _budgetUnavailable = true;
                    }
                    Repaint();
                };
            }

            int limit = VRCExpressionParameters.MAX_PARAMETER_COST;
            if (_budgetUnavailable)
                EditorGUILayout.HelpBox(Localize.parameterBudgetUnavailable[_langIndex], MessageType.Warning);
            else if (_estimatedParameterBits > limit)
                DrawParameterBudgetExceeded(_estimatedParameterBits.Value, limit);
        }

        private void DrawParameterBudgetExceeded(int used, int limit)
        {
            var panelStyle = new GUIStyle { padding = new RectOffset(12, 12, 10, 10) };
            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, wordWrap = true };
            var usageStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, wordWrap = true };
            var bodyStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
            titleStyle.normal.textColor = Color.white;
            usageStyle.normal.textColor = Color.white;
            bodyStyle.normal.textColor = Color.white;

            var rect = EditorGUILayout.BeginVertical(panelStyle);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, new Color(0.45f, 0.08f, 0.10f));
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 4, rect.height), new Color(1f, 0.35f, 0.35f));
            }
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(EditorGUIUtility.IconContent("console.erroricon"), GUILayout.Width(24), GUILayout.Height(24));
            EditorGUILayout.LabelField(Localize.parameterBudgetTitle[_langIndex], titleStyle);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(string.Format(Localize.parameterBudgetUsage[_langIndex],
                used, limit, used - limit), usageStyle);
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(Localize.parameterBudgetExceeded[_langIndex], bodyStyle);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6);
        }

        private void DrawInstallButton()
        {
            using (new EditorGUI.DisabledScope(_avatar == null || EditorUtility.IsPersistent(_avatar)
                || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button(Localize.installButton[_langIndex], GUILayout.Height(36)))
                    InstallNadeSystem();
            }
        }

        private void ResetInstallerDefaults()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(NadeSystemGUID));
            var defaults = prefab != null ? prefab.GetComponent<NadeSystemSettings>() : null;
            if (defaults == null)
            {
                EditorUtility.DisplayDialog(Localize.resetDefaults[_langIndex],
                    Localize.soundSettingsUnavailable[_langIndex], "OK");
                return;
            }

            _contactRadius = DefaultContactRadius;
            _headOffsetY = DefaultHeadOffsetY;
            _installNadeShaderForHands = true;
            _installNadeShaderForFeet = true;
            _installNadeShaderForHead = false;
            _installNadeSphere = false;
            _installFootSystem = false;
            _writeDefaultsMode = WriteDefaultsMode.UsePrefabSettings;
            _menuLanguage = MenuLanguage.Automatic;
            _audioClips = new AudioClip[SoundSlotCount];
            if (defaults.audioClips != null)
                Array.Copy(defaults.audioClips, _audioClips, Math.Min(defaults.audioClips.Length, SoundSlotCount));
            _loadAvatarAudioClips = true;
            // Keep the explicit reset until the user selects another avatar or requests a reload.
            _audioClipsAvatar = _avatar;
            _audioClipsLoadedForAvatar = true;
            InvalidateParameterBudget();
        }

        private void InstallNadeSystem()
        {
            if (!IsAvatarSelected()) return;
            LoadAudioClipsForSelectedAvatar();
            if (!EnsureAudioClips())
            {
                EditorUtility.DisplayDialog(Localize.notSelectAvatarTitle[_langIndex],
                    Localize.soundSettingsUnavailable[_langIndex], "OK");
                return;
            }

            ValidateParameters();
            
            CleanupExistingSystem();

            var nadeSystemRoot = InstantiateNadeSystemPrefab();
            if (nadeSystemRoot == null) return;

            var settings = nadeSystemRoot.GetComponent<NadeSystemSettings>();
            settings.writeDefaultsMode = _writeDefaultsMode;
            settings.menuLanguage = _menuLanguage;
            settings.audioClips = (AudioClip[])_audioClips.Clone();
            PrefabUtility.RecordPrefabInstancePropertyModifications(settings);

            var nadeObjects = FindNadeSystemObjects(nadeSystemRoot.transform);

            ConfigureComponents(nadeObjects);
            nadeObjects.FootSystem = InstallFootSystem(nadeObjects, nadeSystemRoot.transform);
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
                SystemRoot = nadeSystemRoot,
                RxHeadMain = FindRequiredChild(nadeSystemRoot, "RxHeadMain"),
                HeadSystem = FindRequiredChild(nadeSystemRoot, "HeadSystem"),
                RightHandSystem = FindRequiredChild(nadeSystemRoot, "RightHandSystem"),
                LeftHandSystem = FindRequiredChild(nadeSystemRoot, "LeftHandSystem"),
                ExMenu = FindRequiredChild(nadeSystemRoot, "ExMenu"),
                NadeControlMenu = FindRequiredChild(nadeSystemRoot, "ExMenu/Nade Control"),
                FootSystem = null
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

        private Transform InstallFootSystem(NadeSystemObjects nadeObjects, Transform nadeSystemRoot)
        {
            if (!_installFootSystem) return null;

            var footSystemPrefab = GetPrefabByGuid(FootSystemGUID, "FootSystem");
            var footSystemMenuPrefab = GetPrefabByGuid(FootSystemMenuGUID, "FootSystemMenu");
            var footSystem = PrefabUtility.InstantiatePrefab(footSystemPrefab, nadeSystemRoot) as GameObject;
            PrefabUtility.InstantiatePrefab(footSystemMenuPrefab, nadeObjects.NadeControlMenu.transform);

            return footSystem.transform;
        }

        private void InstallShaders(NadeSystemObjects nadeObjects, Transform nadeSystemRoot)
        {
            bool installFootShadow = _installNadeShaderForFeet && _installFootSystem && nadeObjects.FootSystem != null;
            if (!_installNadeShaderForHands && !_installNadeShaderForHead && !installFootShadow) return;

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
            if (installFootShadow)
            {
                var rightFootSystem = nadeObjects.FootSystem.Find("RightFootSystem");
                if (rightFootSystem != null)
                {
                    var rightFoot = rightFootSystem.Find("RxRightFoot");
                    if (rightFoot != null)
                    {
                        PrefabUtility.InstantiatePrefab(nadeShadowPrefab, rightFoot);
                    }
                }
                var leftFootSystem = nadeObjects.FootSystem.Find("LeftFootSystem");
                if (leftFootSystem != null)
                {
                    var leftFoot = leftFootSystem.Find("RxLeftFoot");
                    if (leftFoot != null)
                    {
                        PrefabUtility.InstantiatePrefab(nadeShadowPrefab, leftFoot);
                    }
                }
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
            public Transform SystemRoot { get; set; }
            public Transform RxHeadMain { get; set; }
            public Transform HeadSystem { get; set; }
            public Transform RightHandSystem { get; set; }
            public Transform LeftHandSystem { get; set; }
            public Transform ExMenu { get; set; }
            public Transform NadeControlMenu { get; set; }
            public Transform FootSystem { get; set; }
        }
    }
}
