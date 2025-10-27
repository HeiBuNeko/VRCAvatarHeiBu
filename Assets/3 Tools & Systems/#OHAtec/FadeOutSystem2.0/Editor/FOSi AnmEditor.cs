using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class FOSiAnmEditor : EditorWindow
{
    // 语言支持
    private enum Language { English, Japanese, Chinese }
    private Language currentLanguage = Language.English;
    
    // 本地化字典
    private Dictionary<string, string[]> localizedStrings = new Dictionary<string, string[]>()
    {
        {"WindowTitle", new string[]{"FOSi Animation Editor", "FOSi Animation Editor", "FOSi Animation Editor"}},
        {"ScanButton", new string[]{"Scan Skinned Mesh Renderer", "Skinned Mesh Rendererを取得", "扫描Skinned Mesh Renderer物体"}},
        {"IncludeInactive", new string[]{"Include Inactive", "非アクティブオブジェクトを含む", "包含非激活对象"}},
        {"DragArea", new string[]{"Drag GameObjects Here", "ゲームオブジェクトをここにドラッグ", "拖拽游戏对象到这里"}},
        {"AddSlotButton", new string[]{"Add Slot", "スロット追加", "添加槽位"}},
        {"StatusText", new string[]{"Objects: {0}", "オブジェクト: {0}", "对象: {0}"}},
        {"LanguageLabel", new string[]{"Language", "言語", "语言"}},
        {"SelectAll", new string[]{"Select All", "すべて選択", "全选"}},
        {"DeselectAll", new string[]{"Deselect All", "選択解除", "取消全选"}},
        {"ClearAll", new string[]{"Clear All Objects", "すべてクリア", "清空所有物体"}},
        {"IdleSection", new string[]{"Idle Animation", "Idle アニメーション", "Idle 动画"}},
        {"SolidSection", new string[]{"Solid Animation", "Solid アニメーション", "Solid 动画"}},
        {"FadeoutSection", new string[]{"Fadeout Animation", "Fadeout アニメーション", "Fadeout 动画"}},
        {"IdleValue", new string[]{"Idle Value", "Idle 値", "Idle 值"}},
        {"IdleTime", new string[]{"Idle Time", "Idle時間", "Idle 时间"}},
        {"SolidStartValue", new string[]{"Border Start Value", "範囲開始値", "边界开始值"}},
        {"SolidStartTime", new string[]{"Start Time", "開始時間", "开始时间"}},
        {"SolidEndValue", new string[]{"Border End Value", "範囲終了値", "边界结束值"}},
        {"SolidEndTime", new string[]{"End Time", "終了時間", "结束时间"}},
        {"FadeoutStartValue", new string[]{"Border Start Value", "範囲開始値", "边界开始值"}},
        {"FadeoutStartTime", new string[]{"Start Time", "開始時間", "开始时间"}},
        {"FadeoutEndValue", new string[]{"Border End Value", "範囲終了値", "边界结束值"}},
        {"FadeoutEndTime", new string[]{"End Time", "終了時間", "结束时间"}},
        {"AddKeyframes", new string[]{"Add Keyframes", "キーフレーム追加", "添加关键帧"}},
        {"IdleAnimationFile", new string[]{"Idle Animation File", "Idleアニメーション", "Idle 动画"}},
        {"SolidAnimationFile", new string[]{"Solid Animation File", "Solidアニメーション", "Solid 动画"}},
        {"FadeoutAnimationFile", new string[]{"Fadeout Animation File", "Fadeoutアニメーション", "Fadeout 动画"}},
        {"Browse", new string[]{"Browse", "参照", "浏览"}},
        {"ObjectPath", new string[]{"Object Path", "オブジェクトパス", "对象路径"}},
        {"Selected", new string[]{"Selected", "選択済み", "已选择"}},
        {"DissolveXValue", new string[]{"Dissolve Type", "Dissolve Type", "Dissolve Type"}}
    };
    
    // 对象数据
    private List<GameObject> skinnedObjects = new List<GameObject>();
    private List<bool> objectSelection = new List<bool>();
    private List<string> objectPaths = new List<string>();
    private List<string> objectShortPaths = new List<string>();
    private List<Material> objectMaterials = new List<Material>();
    
    // UI状态
    private Vector2 scrollPosition;
    private bool includeInactive = true;
    
    // 动画参数
    private AnimationClip idleAnimationClip;
    private float idleValue = 0f;
    private float idleTime = 0f;
    private float idleDissolveX = 3f;
    
    private AnimationClip solidAnimationClip;
    private float solidStartValue = 0f;
    private float solidStartTime = 0f;
    private float solidEndValue = 1f;
    private float solidEndTime = 1f;
    private float solidDissolveX = 3f;
    
    private AnimationClip fadeoutAnimationClip;
    private float fadeoutStartValue = 0f;
    private float fadeoutStartTime = 0f;
    private float fadeoutEndValue = 1f;
    private float fadeoutEndTime = 1f;
    private float fadeoutDissolveX = 3f;
    
    [MenuItem("Tools/FOSi Animation Editor")]
    public static void ShowWindow()
    {
        GetWindow<FOSiAnmEditor>("FOSi Animation Editor");
    }
    
    private void OnEnable()
    {
        SetSystemLanguage();
        minSize = new Vector2(850, 600);
    }
    
    private void OnGUI()
    {
        Undo.RecordObject(this, "FOSi Animation Editor Change");
        
        EditorGUILayout.BeginVertical();
        
        DrawLanguageSelector();
        DrawHeader();
        
        EditorGUILayout.BeginHorizontal();
        DrawLeftPanel();
        DrawRightPanel();
        EditorGUILayout.EndHorizontal();
        
        DrawStatusFooter();
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.MinWidth(320), GUILayout.MaxWidth(480));
        
        DrawScanControls();
        DrawSelectionControls();
        DrawDragAndDropArea();
        DrawObjectList();
        DrawAddButton();
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawRightPanel()
    {
        float rightPanelWidth = this.position.width * 0.45f;
        rightPanelWidth = Mathf.Clamp(rightPanelWidth, 300, 500);
        
        EditorGUILayout.BeginVertical(GUILayout.Width(rightPanelWidth));
        
        DrawAnimationSections();
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawLanguageSelector()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GetLocalizedString("LanguageLabel"), GUILayout.Width(60));
        Language newLanguage = (Language)EditorGUILayout.EnumPopup(currentLanguage, GUILayout.Width(100));
        if (newLanguage != currentLanguage)
        {
            currentLanguage = newLanguage;
            this.titleContent = new GUIContent(GetLocalizedString("WindowTitle"));
        }
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawHeader()
    {
        GUILayout.Label(GetLocalizedString("WindowTitle"), EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Edit animation keyframes for skinned mesh objects.", MessageType.Info);
    }
    
    private void DrawScanControls()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(GetLocalizedString("ScanButton"), GUILayout.Height(30)))
        {
            ScanSkinnedObjects();
        }
        includeInactive = EditorGUILayout.ToggleLeft(GetLocalizedString("IncludeInactive"), includeInactive, GUILayout.Width(140));
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawSelectionControls()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(GetLocalizedString("SelectAll"), GUILayout.Height(20)))
        {
            SelectAllObjects();
        }
        if (GUILayout.Button(GetLocalizedString("DeselectAll"), GUILayout.Height(20)))
        {
            DeselectAllObjects();
        }
        if (GUILayout.Button(GetLocalizedString("ClearAll"), GUILayout.Height(20)))
        {
            ClearAllObjects();
        }
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawDragAndDropArea()
    {
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, GetLocalizedString("DragArea"), EditorStyles.helpBox);
        
        Event evt = Event.current;
        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
        {
            if (dropArea.Contains(evt.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    
                    foreach (Object draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is GameObject gameObject)
                        {
                            AddValidObject(gameObject);
                        }
                    }
                }
            }
        }
    }
    
    private void DrawObjectList()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
        
        for (int i = 0; i < skinnedObjects.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            
            if (i < objectSelection.Count)
            {
                objectSelection[i] = EditorGUILayout.Toggle(objectSelection[i], GUILayout.Width(20));
            }
            
            EditorGUI.BeginChangeCheck();
            GameObject newObject = (GameObject)EditorGUILayout.ObjectField(
                skinnedObjects[i], typeof(GameObject), false, GUILayout.Width(140));
                
            if (EditorGUI.EndChangeCheck() && newObject != null)
            {
                AddValidObject(newObject);
                if (i < skinnedObjects.Count)
                {
                    skinnedObjects[i] = newObject;
                }
            }
            
            if (i < objectShortPaths.Count)
            {
                EditorGUILayout.LabelField(objectShortPaths[i], EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
            }
            
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                RemoveObjectAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndScrollView();
                return;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawAddButton()
    {
        if (GUILayout.Button(GetLocalizedString("AddSlotButton")))
        {
            AddObjectSlot();
        }
    }
    
    private void DrawAnimationSections()
    {
        float labelWidth = 110f;
        float buttonWidth = 60f;
        float fieldWidth = 60f;
        
        // Idle动画部分
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(GetLocalizedString("IdleSection"), EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GetLocalizedString("IdleAnimationFile"), GUILayout.Width(labelWidth));
        idleAnimationClip = (AnimationClip)EditorGUILayout.ObjectField(idleAnimationClip, typeof(AnimationClip), false);
        if (GUILayout.Button(GetLocalizedString("Browse"), GUILayout.Width(buttonWidth)))
        {
            string path = EditorUtility.OpenFilePanel("Select Idle Animation File", "Assets", "anm");
            if (!string.IsNullOrEmpty(path))
            {
                string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                idleAnimationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(relativePath);
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GetLocalizedString("DissolveXValue"), GUILayout.Width(labelWidth));
        idleDissolveX = EditorGUILayout.FloatField(idleDissolveX, GUILayout.Width(fieldWidth));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GetLocalizedString("IdleValue"), GUILayout.Width(labelWidth));
        idleValue = EditorGUILayout.FloatField(idleValue, GUILayout.Width(fieldWidth));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GetLocalizedString("IdleTime"), GUILayout.Width(labelWidth));
        idleTime = EditorGUILayout.FloatField(idleTime, GUILayout.Width(fieldWidth));
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button(GetLocalizedString("AddKeyframes") + " (Idle)", GUILayout.Height(25)))
        {
            AddIdleKeyframes();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.Separator();
        
        // Solid动画部分
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(GetLocalizedString("SolidSection"), EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GetLocalizedString("SolidAnimationFile"), GUILayout.Width(labelWidth));
        solidAnimationClip = (AnimationClip)EditorGUILayout.ObjectField(solidAnimationClip, typeof(AnimationClip), false);
        if (GUILayout.Button(GetLocalizedString("Browse"), GUILayout.Width(buttonWidth)))
        {
            string path = EditorUtility.OpenFilePanel("Select Solid Animation File", "Assets", "anm");
            if (!string.IsNullOrEmpty(path))
            {
                string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                solidAnimationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(relativePath);
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GetLocalizedString("DissolveXValue"), GUILayout.Width(labelWidth));
        solidDissolveX = EditorGUILayout.FloatField(solidDissolveX, GUILayout.Width(fieldWidth));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GetLocalizedString("SolidStartValue"), GUILayout.Width(labelWidth));
        solidStartValue = EditorGUILayout.FloatField(solidStartValue, GUILayout.Width(fieldWidth));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GetLocalizedString("SolidStartTime"), GUILayout.Width(labelWidth));
        solidStartTime = EditorGUILayout.FloatField(solidStartTime, GUILayout.Width(fieldWidth));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GetLocalizedString("SolidEndValue"), GUILayout.Width(labelWidth));
        solidEndValue = EditorGUILayout.FloatField(solidEndValue, GUILayout.Width(fieldWidth));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GetLocalizedString("SolidEndTime"), GUILayout.Width(labelWidth));
        solidEndTime = EditorGUILayout.FloatField(solidEndTime, GUILayout.Width(fieldWidth));
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button(GetLocalizedString("AddKeyframes") + " (Solid)", GUILayout.Height(25)))
        {
            AddSolidKeyframes();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.Separator();
        
        // Fadeout动画部分
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(GetLocalizedString("FadeoutSection"), EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GetLocalizedString("FadeoutAnimationFile"), GUILayout.Width(labelWidth));
        fadeoutAnimationClip = (AnimationClip)EditorGUILayout.ObjectField(fadeoutAnimationClip, typeof(AnimationClip), false);
        if (GUILayout.Button(GetLocalizedString("Browse"), GUILayout.Width(buttonWidth)))
        {
            string path = EditorUtility.OpenFilePanel("Select Fadeout Animation File", "Assets", "anm");
            if (!string.IsNullOrEmpty(path))
            {
                string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                fadeoutAnimationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(relativePath);
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GetLocalizedString("DissolveXValue"), GUILayout.Width(labelWidth));
        fadeoutDissolveX = EditorGUILayout.FloatField(fadeoutDissolveX, GUILayout.Width(fieldWidth));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GetLocalizedString("FadeoutStartValue"), GUILayout.Width(labelWidth));
        fadeoutStartValue = EditorGUILayout.FloatField(fadeoutStartValue, GUILayout.Width(fieldWidth));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GetLocalizedString("FadeoutStartTime"), GUILayout.Width(labelWidth));
        fadeoutStartTime = EditorGUILayout.FloatField(fadeoutStartTime, GUILayout.Width(fieldWidth));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GetLocalizedString("FadeoutEndValue"), GUILayout.Width(labelWidth));
        fadeoutEndValue = EditorGUILayout.FloatField(fadeoutEndValue, GUILayout.Width(fieldWidth));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GetLocalizedString("FadeoutEndTime"), GUILayout.Width(labelWidth));
        fadeoutEndTime = EditorGUILayout.FloatField(fadeoutEndTime, GUILayout.Width(fieldWidth));
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button(GetLocalizedString("AddKeyframes") + " (Fadeout)", GUILayout.Height(25)))
        {
            AddFadeoutKeyframes();
        }
    }
    
    private void DrawStatusFooter()
    {
        int selectedCount = objectSelection.Count(s => s);
        EditorGUILayout.LabelField(
            $"{string.Format(GetLocalizedString("StatusText"), skinnedObjects.Count)} ({selectedCount} {GetLocalizedString("Selected")})", 
            EditorStyles.centeredGreyMiniLabel);
    }
    
    // ============ 新增的缺失方法 ============
    
    private void ScanSkinnedObjects()
    {
        skinnedObjects.Clear();
        objectSelection.Clear();
        objectPaths.Clear();
        objectShortPaths.Clear();
        objectMaterials.Clear();
        
        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("No GameObject selected!");
            return;
        }
        
        SkinnedMeshRenderer[] skinnedRenderers = Selection.activeGameObject.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive);
        foreach (SkinnedMeshRenderer renderer in skinnedRenderers)
        {
            if (renderer == null) continue;
            AddValidObject(renderer.gameObject);
        }
        
        Debug.Log($"Found {skinnedObjects.Count} skinned mesh objects");
    }
    
    private void AddValidObject(GameObject gameObject)
    {
        if (gameObject != null && gameObject.GetComponent<SkinnedMeshRenderer>() != null)
        {
            if (!skinnedObjects.Contains(gameObject))
            {
                skinnedObjects.Add(gameObject);
                objectSelection.Add(true);
                objectPaths.Add(GetGameObjectPath(gameObject));
                objectShortPaths.Add(GetShortGameObjectPath(gameObject));
                
                SkinnedMeshRenderer renderer = gameObject.GetComponent<SkinnedMeshRenderer>();
                objectMaterials.Add(renderer.sharedMaterial);
            }
        }
    }
    
    private void AddObjectSlot()
    {
        skinnedObjects.Add(null);
        objectSelection.Add(false);
        objectPaths.Add("");
        objectShortPaths.Add("");
        objectMaterials.Add(null);
    }
    
    private void RemoveObjectAtIndex(int index)
    {
        if (index >= 0 && index < skinnedObjects.Count)
        {
            skinnedObjects.RemoveAt(index);
            if (index < objectSelection.Count) objectSelection.RemoveAt(index);
            if (index < objectPaths.Count) objectPaths.RemoveAt(index);
            if (index < objectShortPaths.Count) objectShortPaths.RemoveAt(index);
            if (index < objectMaterials.Count) objectMaterials.RemoveAt(index);
        }
    }
    
    private void SelectAllObjects()
    {
        for (int i = 0; i < objectSelection.Count; i++)
        {
            objectSelection[i] = true;
        }
    }
    
    private void DeselectAllObjects()
    {
        for (int i = 0; i < objectSelection.Count; i++)
        {
            objectSelection[i] = false;
        }
    }
    
    private void ClearAllObjects()
    {
        skinnedObjects.Clear();
        objectSelection.Clear();
        objectPaths.Clear();
        objectShortPaths.Clear();
        objectMaterials.Clear();
    }
    
    private void AddIdleKeyframes()
    {
        if (idleAnimationClip == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select an Idle animation file first.", "OK");
            return;
        }
        
        Undo.RegisterCompleteObjectUndo(idleAnimationClip, "Add Idle Keyframes");
        
        int addedKeyframes = 0;
        for (int i = 0; i < skinnedObjects.Count; i++)
        {
            if (i < objectSelection.Count && objectSelection[i] && skinnedObjects[i] != null)
            {
                AddSingleKeyframe(idleAnimationClip, objectShortPaths[i], objectMaterials[i], idleValue, idleTime, idleDissolveX);
                addedKeyframes++;
            }
        }
        
        Debug.Log($"Added {addedKeyframes} Idle keyframes to {idleAnimationClip.name}");
        EditorUtility.SetDirty(idleAnimationClip);
        AssetDatabase.SaveAssets();
    }
    
    private void AddSolidKeyframes()
    {
        if (solidAnimationClip == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a Solid animation file first.", "OK");
            return;
        }
        
        Undo.RegisterCompleteObjectUndo(solidAnimationClip, "Add Solid Keyframes");
        
        int addedKeyframes = 0;
        for (int i = 0; i < skinnedObjects.Count; i++)
        {
            if (i < objectSelection.Count && objectSelection[i] && skinnedObjects[i] != null)
            {
                AddDoubleKeyframes(solidAnimationClip, objectShortPaths[i], objectMaterials[i], 
                                  solidStartValue, solidStartTime, solidEndValue, solidEndTime, solidDissolveX);
                addedKeyframes++;
            }
        }
        
        Debug.Log($"Added {addedKeyframes} Solid keyframes to {solidAnimationClip.name}");
        EditorUtility.SetDirty(solidAnimationClip);
        AssetDatabase.SaveAssets();
    }
    
    private void AddFadeoutKeyframes()
    {
        if (fadeoutAnimationClip == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a Fadeout animation file first.", "OK");
            return;
        }
        
        Undo.RegisterCompleteObjectUndo(fadeoutAnimationClip, "Add Fadeout Keyframes");
        
        int addedKeyframes = 0;
        for (int i = 0; i < skinnedObjects.Count; i++)
        {
            if (i < objectSelection.Count && objectSelection[i] && skinnedObjects[i] != null)
            {
                AddDoubleKeyframes(fadeoutAnimationClip, objectShortPaths[i], objectMaterials[i], 
                                  fadeoutStartValue, fadeoutStartTime, fadeoutEndValue, fadeoutEndTime, fadeoutDissolveX);
                addedKeyframes++;
            }
        }
        
        Debug.Log($"Added {addedKeyframes} Fadeout keyframes to {fadeoutAnimationClip.name}");
        EditorUtility.SetDirty(fadeoutAnimationClip);
        AssetDatabase.SaveAssets();
    }
    
    private void AddSingleKeyframe(AnimationClip clip, string objectPath, Material material, float zValue, float time, float xValue)
    {
        if (clip == null || string.IsNullOrEmpty(objectPath) || material == null) return;
        
        Vector4 dissolveParams = material.GetVector("_DissolveParams");
        
        AddFloatKeyframe(clip, objectPath, "material._DissolveParams.x", xValue, time);
        AddFloatKeyframe(clip, objectPath, "material._DissolveParams.y", dissolveParams.y, time);
        AddFloatKeyframe(clip, objectPath, "material._DissolveParams.z", zValue, time);
        AddFloatKeyframe(clip, objectPath, "material._DissolveParams.w", dissolveParams.w, time);
        
        Debug.Log($"Added keyframe: path='{objectPath}', property='material._DissolveParams', " +
                 $"values=({xValue}, {dissolveParams.y}, {zValue}, {dissolveParams.w}), time={time}");
    }
    
    private void AddDoubleKeyframes(AnimationClip clip, string objectPath, Material material, 
                                  float startZValue, float startTime, float endZValue, float endTime, float xValue)
    {
        if (clip == null || string.IsNullOrEmpty(objectPath) || material == null) return;
        
        Vector4 dissolveParams = material.GetVector("_DissolveParams");
        
        AddFloatKeyframes(clip, objectPath, "material._DissolveParams.x", xValue, xValue, startTime, endTime);
        AddFloatKeyframes(clip, objectPath, "material._DissolveParams.y", dissolveParams.y, dissolveParams.y, startTime, endTime);
        AddFloatKeyframes(clip, objectPath, "material._DissolveParams.z", startZValue, endZValue, startTime, endTime);
        AddFloatKeyframes(clip, objectPath, "material._DissolveParams.w", dissolveParams.w, dissolveParams.w, startTime, endTime);
        
        Debug.Log($"Added keyframes: path='{objectPath}', property='material._DissolveParams', " +
                 $"values=({xValue}->{xValue}, {dissolveParams.y}->{dissolveParams.y}, " +
                 $"{startZValue}->{endZValue}, {dissolveParams.w}->{dissolveParams.w}), " +
                 $"times={startTime}->{endTime}");
    }
    
    private void AddFloatKeyframe(AnimationClip clip, string objectPath, string propertyName, float value, float time)
    {
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(time, value);
        
        EditorCurveBinding binding = EditorCurveBinding.FloatCurve(
            objectPath,
            typeof(SkinnedMeshRenderer),
            propertyName
        );
        
        AnimationUtility.SetEditorCurve(clip, binding, curve);
    }
    
    private void AddFloatKeyframes(AnimationClip clip, string objectPath, string propertyName, 
                                 float startValue, float endValue, float startTime, float endTime)
    {
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(startTime, startValue);
        curve.AddKey(endTime, endValue);
        
        EditorCurveBinding binding = EditorCurveBinding.FloatCurve(
            objectPath,
            typeof(SkinnedMeshRenderer),
            propertyName
        );
        
        AnimationUtility.SetEditorCurve(clip, binding, curve);
    }
    
    private string GetGameObjectPath(GameObject gameObject)
    {
        if (gameObject == null) return "";
        
        string path = gameObject.name;
        Transform parent = gameObject.transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
    
    private string GetShortGameObjectPath(GameObject gameObject)
    {
        if (gameObject == null) return "";
        
        string path = gameObject.name;
        Transform parent = gameObject.transform.parent;
        
        bool skipFirst = true;
        while (parent != null)
        {
            if (parent.parent == null && skipFirst)
            {
                skipFirst = false;
                parent = parent.parent;
                continue;
            }
            
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
    
    private void SetSystemLanguage()
    {
        SystemLanguage systemLang = Application.systemLanguage;
        
        switch (systemLang)
        {
            case SystemLanguage.Japanese:
                currentLanguage = Language.Japanese;
                break;
            case SystemLanguage.Chinese:
            case SystemLanguage.ChineseSimplified:
            case SystemLanguage.ChineseTraditional:
                currentLanguage = Language.Chinese;
                break;
            default:
                currentLanguage = Language.English;
                break;
        }
        
        this.titleContent = new GUIContent(GetLocalizedString("WindowTitle"));
    }
    
    private string GetLocalizedString(string key)
    {
        if (localizedStrings.ContainsKey(key))
        {
            int langIndex = (int)currentLanguage;
            if (langIndex < localizedStrings[key].Length)
            {
                return localizedStrings[key][langIndex];
            }
        }
        return key;
    }
}