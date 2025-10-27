using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class FOSiMaterialTuner : EditorWindow
{
    #region 语言和UI配置
    private enum Language { English, Japanese, Chinese }
    private Language currentLanguage = Language.English;
    
    private readonly Dictionary<string, string[]> localizedStrings = new Dictionary<string, string[]>
    {
        {"WindowTitle", new[]{"FOSi Material Tuner 1.1", "FOSi Material Tuner 1.1", "FOSi Material Tuner 1.1"}},
        {"ScanButton", new[]{"Scan Selected Objects", "オブジェクトからマテリアル取得", "检测选中对象材质"}},
        {"IncludeInactive", new[]{"Include Inactive", "非アクティブを含む", "包含非激活对象"}},
        {"DragArea", new[]{"Drag Materials Here", "マテリアルをドラッグドロップ", "拖拽材质到此"}},
        {"AddSlotButton", new[]{"+ Add Slot", "+ スロット追加", "+ 添加槽位"}},
        {"StatusText", new[]{"Materials: {0} | Selected: {1} | Modified: {2}", "マテリアル: {0} | 選択: {1} | 変更: {2}", "材质: {0} | 选中: {1} | 已修改: {2}"}},
        {"ProcessButton", new[]{"Process Materials", "マテリアル処理", "处理材质"}},
        {"ProcessSuccess", new[]{"Processed {0} materials", "{0}個のマテリアルを処理", "处理{0}个材质"}},
        {"NoMaterials", new[]{"No materials selected", "マテリアルが選択されていません", "未选中材质"}},
        {"LanguageLabel", new[]{"Language", "言語", "语言"}},
        {"SelectAll", new[]{"Select All", "すべて選択", "全选"}},
        {"DeselectAll", new[]{"Deselect All", "選択解除", "取消全选"}},
        {"ClearAll", new[]{"Clear All", "すべてクリア", "清空所有"}},
        {"SearchPlaceholder", new[]{"Search...", "検索...", "搜索..."}},
        {"Modified", new[]{"Modified", "変更済み", "已修改"}},
        {"DissolveMode", new[]{"Dissolve Mode", "Dissolveモード", "消散模式"}},
        {"AlphaMode", new[]{"Alpha Mode", "透明度モード", "Alpha模式"}},
        {"PositionMode", new[]{"Position Mode", "座標モード", "位置模式"}},
        {"Border", new[]{"Border", "範囲", "边界"}},
        {"Blur", new[]{"Blur", "ぼかし", "模糊"}},
        {"Mask", new[]{"Mask", "マスク", "遮罩"}},
        {"Noise", new[]{"Noise", "ノイズ", "噪波"}},
        {"NoiseStrength", new[]{"Noise Strength", "ノイズ強度", "噪波强度"}},
        {"Tiling", new[]{"Tiling", "Tiling", "平铺"}},
        {"Offset", new[]{"Offset", "Offset", "偏移"}},
        {"ScrollRotate", new[]{"Scroll", "スクロール", "滚动"}},
        {"Color", new[]{"HDR Color", "HDRカラー", "HDR颜色"}},
        {"Shape", new[]{"Shape", "形状", "形状"}},
        {"ShapeLine", new[]{"Line", "線", "线条"}},
        {"ShapePoint", new[]{"Point", "点", "点"}},
        {"Position", new[]{"Position", "向き", "位置"}},
        {"ApplyButton", new[]{"Apply Changes", "変更を適用", "应用更改"}},
        {"ModeNone", new[]{"None", "なし", "无"}},
        {"ModeAlpha", new[]{"Alpha", "透明度", "Alpha"}},
        {"ModePosition", new[]{"Position", "座標", "位置"}},
        {"UndoGroup", new[]{"Dissolve Parameters", "Dissolveパラメータ", "消散参数"}},
        {"MaterialTools", new[]{"Material Tools", "Material Tools", "Material Tools"}},
        {"DissolveTools", new[]{"Dissolve Tools", "Dissolve Tools", "Dissolve Tools"}},
        {"BasicSettings", new[]{"Basic Settings", "基本設定", "基本设置"}},
        {"MaskSettings", new[]{"Mask Settings", "マスク設定", "遮罩设置"}},
        {"NoiseSettings", new[]{"Noise Settings", "ノイズ設定", "噪波设置"}},
        {"ColorSettings", new[]{"Color Settings", "カラー設定", "颜色设置"}}
    };
    #endregion

    #region 数据存储 (共享)
    private readonly List<Material> materials = new List<Material>();
    private readonly List<bool> materialSelection = new List<bool>();
    private readonly Dictionary<Material, int> originalValues = new Dictionary<Material, int>();
    private readonly HashSet<Material> modifiedMaterials = new HashSet<Material>();
    #endregion

    #region Dissolve 参数
    private enum DissolveMode { None = 0, Alpha = 1, UV = 2, Position = 3 }
    private DissolveMode currentDissolveMode = DissolveMode.Alpha;
    
    private enum ShapeType { Point = 0, Line = 1 }
    private ShapeType currentShapeType = ShapeType.Line;
    
    private float alphaBorder = 0.1f, alphaBlur = 0.1f, alphaNoiseStrength = 0.1f;
    private Texture2D alphaMask, alphaNoiseMask;
    private Vector2 alphaNoiseTiling = Vector2.one, alphaNoiseOffset, alphaNoiseScrollRotate;
    private Color alphaColor = Color.white;
    
    private float positionBorder = 0.1f, positionBlur = 0.1f, positionNoiseStrength = 0.1f;
    private Vector3 positionValue;
    private Texture2D positionNoiseMask;
    private Vector2 positionNoiseTiling = Vector2.one, positionNoiseOffset, positionNoiseScrollRotate;
    private Color positionColor = Color.white;
    #endregion

    #region UI状态
    private Vector2 materialScrollPosition;
    private bool includeInactive = true;
    private string searchText = "";
    private bool showBasicSettings = true;
    private bool showMaskSettings = true;
    private bool showNoiseSettings = true;
    private bool showColorSettings = true;
    #endregion

    [MenuItem("Tools/FOSi Material Tuner")]
    public static void ShowWindow()
    {
        var window = GetWindow<FOSiMaterialTuner>("FOSi Material Tuner 1.1");
        window.minSize = new Vector2(800, 600);
    }

    private void OnEnable()
    {
        SetSystemLanguage();
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;

    private void OnUndoRedo()
    {
        Repaint();
        UpdateModifiedState();
    }

    private void OnGUI()
    {
        try
        {
            EditorGUILayout.BeginHorizontal();
            
            // 左侧：材质列表区域
            DrawMaterialListPanel();
            
            // 右侧：工具区域
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            DrawLanguageSelector();
            DrawMaterialTools();
            DrawDissolveTools();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            DrawStatusBar();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GUI Error: {e.Message}");
        }
    }

    #region UI绘制方法
    private void DrawMaterialListPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(450));
        
        // 扫描按钮
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(GetLocalizedString("ScanButton"), GUILayout.Height(30))) ScanMaterials();
        includeInactive = EditorGUILayout.ToggleLeft(GetLocalizedString("IncludeInactive"), includeInactive, GUILayout.Width(120));
        EditorGUILayout.EndHorizontal();
        
        // 搜索框
        EditorGUILayout.BeginHorizontal(GUILayout.Height(25));
        GUILayout.Label(EditorGUIUtility.IconContent("Search Icon"), GUILayout.Width(20), GUILayout.Height(20));
        searchText = EditorGUILayout.TextField(searchText, GUILayout.Height(20));
        if (GUILayout.Button("×", GUILayout.Width(24), GUILayout.Height(20)))
        {
            searchText = "";
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();
        
        // 选择控制按钮
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(GetLocalizedString("SelectAll"), GUILayout.Height(20))) SelectAll();
        if (GUILayout.Button(GetLocalizedString("DeselectAll"), GUILayout.Height(20))) DeselectAll();
        if (GUILayout.Button(GetLocalizedString("ClearAll"), GUILayout.Height(20))) ClearAll();
        EditorGUILayout.EndHorizontal();
        
        // 材质列表
        materialScrollPosition = EditorGUILayout.BeginScrollView(materialScrollPosition, GUILayout.Height(350));
        
        for (int i = 0; i < materials.Count; i++)
        {
            if (ShouldSkipMaterial(i)) continue;
            
            EditorGUILayout.BeginHorizontal();
            DrawMaterialRow(i);
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
        
        // 添加槽位按钮
        if (GUILayout.Button(GetLocalizedString("AddSlotButton"), GUILayout.Height(25))) 
        {
            AddMaterialSlot();
        }
        
        // 拖拽区域
        var dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, GetLocalizedString("DragArea"), EditorStyles.helpBox);
        HandleDragAndDrop(dropArea);
        
        EditorGUILayout.EndVertical();
    }

    private void DrawMaterialTools()
    {
        GUILayout.Label(GetLocalizedString("MaterialTools"), EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("LilToon material management tool ", MessageType.Info);
        
        // 添加说明文
        EditorGUILayout.HelpBox("Opaque→Transparent\nctrl Z to Undo", MessageType.Info);
        
        // 处理材质按钮
        if (GUILayout.Button(GetLocalizedString("ProcessButton"), GUILayout.Height(30))) 
        {
            ProcessMaterials();
        }
        
        EditorGUILayout.Space(10);
    }

    private void DrawDissolveTools()
    {
        GUILayout.Label(GetLocalizedString("DissolveTools"), EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Batch edit lilToon dissolve parameters for selected materials.", MessageType.Info);
        
        // Dissolve模式选择
        EditorGUILayout.LabelField(GetLocalizedString("DissolveMode"), EditorStyles.boldLabel);
        var oldMode = currentDissolveMode;
        currentDissolveMode = (DissolveMode)EditorGUILayout.EnumPopup(currentDissolveMode);
        if (currentDissolveMode != oldMode)
        {
            Undo.RecordObject(this, "Change Dissolve Mode");
        }
        
        // 根据模式显示参数
        switch (currentDissolveMode)
        {
            case DissolveMode.Alpha: DrawAlphaModeParameters(); break;
            case DissolveMode.Position: DrawPositionModeParameters(); break;
            case DissolveMode.UV: DrawUVModeParameters(); break;
            case DissolveMode.None: 
                EditorGUILayout.HelpBox("Dissolve mode is set to None. No additional parameters available.", MessageType.Info); 
                break;
        }
        
        EditorGUILayout.Space(10);
        if (GUILayout.Button(GetLocalizedString("ApplyButton"), GUILayout.Height(30))) 
        {
            Undo.SetCurrentGroupName("Apply Dissolve Parameters");
            int undoGroup = Undo.GetCurrentGroup();
            
            ApplyDissolveParameters();
            
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    private void DrawLanguageSelector()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GetLocalizedString("LanguageLabel"), GUILayout.Width(60));
        var newLang = (Language)EditorGUILayout.EnumPopup(currentLanguage, GUILayout.Width(100));
        if (newLang != currentLanguage)
        {
            currentLanguage = newLang;
            titleContent = new GUIContent(GetLocalizedString("WindowTitle"));
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawMaterialRow(int index)
    {
        materialSelection[index] = EditorGUILayout.Toggle(materialSelection[index], GUILayout.Width(20));
        
        EditorGUI.BeginChangeCheck();
        materials[index] = (Material)EditorGUILayout.ObjectField(materials[index], typeof(Material), false, GUILayout.Height(20));
        if (EditorGUI.EndChangeCheck() && materials[index] != null) AddValidMaterial(materials[index]);
        
        if (materials[index] != null)
        {
            GUILayout.Label(materials[index].shader.name, GUILayout.Width(200));
            if (modifiedMaterials.Contains(materials[index]))
            {
                GUILayout.Label(GetLocalizedString("Modified"), EditorStyles.miniLabel, GUILayout.Width(60));
                GUILayout.Label(EditorGUIUtility.IconContent("FilterSelectedOnly"), GUILayout.Width(20));
            }
        }
        
        if (GUILayout.Button("×", GUILayout.Width(24), GUILayout.Height(20)))
        {
            RemoveMaterial(index);
        }
    }

    private void DrawStatusBar()
    {
        EditorGUILayout.LabelField(
            string.Format(GetLocalizedString("StatusText"), materials.Count, GetSelectedCount(), modifiedMaterials.Count), 
            EditorStyles.centeredGreyMiniLabel);
    }
    #endregion

    #region Dissolve UI 组件
    private void DrawAlphaModeParameters()
    {
        // 基本设置
        showBasicSettings = EditorGUILayout.Foldout(showBasicSettings, GetLocalizedString("BasicSettings"), true);
        if (showBasicSettings)
        {
            EditorGUI.indentLevel++;
            alphaBorder = DrawUndoableFloatField("Border", alphaBorder, "Change Alpha Border");
            alphaBlur = DrawUndoableFloatField("Blur", alphaBlur, "Change Alpha Blur");
            EditorGUI.indentLevel--;
        }
        
        // 遮罩设置
        showMaskSettings = EditorGUILayout.Foldout(showMaskSettings, GetLocalizedString("MaskSettings"), true);
        if (showMaskSettings)
        {
            EditorGUI.indentLevel++;
            alphaMask = DrawUndoableObjectField("Mask", alphaMask, typeof(Texture2D), "Change Alpha Mask");
            alphaNoiseMask = DrawUndoableObjectField("Noise", alphaNoiseMask, typeof(Texture2D), "Change Alpha Noise Mask");
            EditorGUI.indentLevel--;
        }
        
        // 噪波设置
        showNoiseSettings = EditorGUILayout.Foldout(showNoiseSettings, GetLocalizedString("NoiseSettings"), true);
        if (showNoiseSettings)
        {
            EditorGUI.indentLevel++;
            alphaNoiseStrength = DrawUndoableFloatField("NoiseStrength", alphaNoiseStrength, "Change Alpha Noise Strength");
            alphaNoiseTiling = DrawUndoableVector2Field("Tiling", alphaNoiseTiling, "Change Alpha Noise Tiling");
            alphaNoiseOffset = DrawUndoableVector2Field("Offset", alphaNoiseOffset, "Change Alpha Noise Offset");
            alphaNoiseScrollRotate = DrawUndoableVector2Field("ScrollRotate", alphaNoiseScrollRotate, "Change Alpha Scroll/Rotate");
            EditorGUI.indentLevel--;
        }
        
        // 颜色设置
        showColorSettings = EditorGUILayout.Foldout(showColorSettings, GetLocalizedString("ColorSettings"), true);
        if (showColorSettings)
        {
            EditorGUI.indentLevel++;
            alphaColor = DrawUndoableHDRColorField("Color", alphaColor, "Change Alpha Color");
            EditorGUI.indentLevel--;
        }
    }

    private void DrawPositionModeParameters()
    {
        // 基本设置
        showBasicSettings = EditorGUILayout.Foldout(showBasicSettings, GetLocalizedString("BasicSettings"), true);
        if (showBasicSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField(GetLocalizedString("Shape"), EditorStyles.boldLabel);
            var oldShape = currentShapeType;
            currentShapeType = (ShapeType)EditorGUILayout.EnumPopup(currentShapeType);
            if (currentShapeType != oldShape)
            {
                Undo.RecordObject(this, "Change Shape Type");
            }
            
            positionBorder = DrawUndoableFloatField("Border", positionBorder, "Change Position Border");
            positionBlur = DrawUndoableFloatField("Blur", positionBlur, "Change Position Blur");
            
            EditorGUILayout.LabelField(GetLocalizedString("Position"), EditorStyles.boldLabel);
            positionValue = DrawUndoableVector3Field("", positionValue, "Change Position Value");
            EditorGUI.indentLevel--;
        }
        
        // 噪波设置
        showNoiseSettings = EditorGUILayout.Foldout(showNoiseSettings, GetLocalizedString("NoiseSettings"), true);
        if (showNoiseSettings)
        {
            EditorGUI.indentLevel++;
            positionNoiseMask = DrawUndoableObjectField("Noise", positionNoiseMask, typeof(Texture2D), "Change Position Noise Mask");
            positionNoiseStrength = DrawUndoableFloatField("NoiseStrength", positionNoiseStrength, "Change Position Noise Strength");
            positionNoiseTiling = DrawUndoableVector2Field("Tiling", positionNoiseTiling, "Change Position Noise Tiling");
            positionNoiseOffset = DrawUndoableVector2Field("Offset", positionNoiseOffset, "Change Position Noise Offset");
            positionNoiseScrollRotate = DrawUndoableVector2Field("ScrollRotate", positionNoiseScrollRotate, "Change Position Scroll/Rotate");
            EditorGUI.indentLevel--;
        }
        
        // 颜色设置
        showColorSettings = EditorGUILayout.Foldout(showColorSettings, GetLocalizedString("ColorSettings"), true);
        if (showColorSettings)
        {
            EditorGUI.indentLevel++;
            positionColor = DrawUndoableHDRColorField("Color", positionColor, "Change Position Color");
            EditorGUI.indentLevel--;
        }
    }

    private void DrawUVModeParameters()
    {
        EditorGUILayout.LabelField("UV Mode", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("UV mode parameters are not implemented in this version.", MessageType.Info);
    }

    // 支持撤销的UI控件方法
    private float DrawUndoableFloatField(string label, float value, string undoMessage)
    {
        EditorGUI.BeginChangeCheck();
        var newValue = EditorGUILayout.FloatField(GetLocalizedString(label), value);
        if (EditorGUI.EndChangeCheck() && newValue != value)
        {
            Undo.RecordObject(this, undoMessage);
            return newValue;
        }
        return value;
    }

    private Vector2 DrawUndoableVector2Field(string label, Vector2 value, string undoMessage)
    {
        EditorGUI.BeginChangeCheck();
        var newValue = EditorGUILayout.Vector2Field(GetLocalizedString(label), value);
        if (EditorGUI.EndChangeCheck() && newValue != value)
        {
            Undo.RecordObject(this, undoMessage);
            return newValue;
        }
        return value;
    }

    private Vector3 DrawUndoableVector3Field(string label, Vector3 value, string undoMessage)
    {
        EditorGUI.BeginChangeCheck();
        var newValue = EditorGUILayout.Vector3Field(label, value);
        if (EditorGUI.EndChangeCheck() && newValue != value)
        {
            Undo.RecordObject(this, undoMessage);
            return newValue;
        }
        return value;
    }

    private Color DrawUndoableHDRColorField(string label, Color value, string undoMessage)
    {
        EditorGUI.BeginChangeCheck();
        var newValue = EditorGUILayout.ColorField(new GUIContent(GetLocalizedString(label)), value, true, true, true);
        if (EditorGUI.EndChangeCheck() && newValue != value)
        {
            Undo.RecordObject(this, undoMessage);
            return newValue;
        }
        return value;
    }

    private T DrawUndoableObjectField<T>(string label, T value, System.Type type, string undoMessage) where T : Object
    {
        EditorGUI.BeginChangeCheck();
        var newValue = (T)EditorGUILayout.ObjectField(GetLocalizedString(label), value, type, false);
        if (EditorGUI.EndChangeCheck() && newValue != value)
        {
            Undo.RecordObject(this, undoMessage);
            return newValue;
        }
        return value;
    }
    #endregion

    #region 核心功能
    private void ScanMaterials()
    {
        ClearAll();
        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("No object selected!");
            return;
        }

        foreach (var renderer in Selection.activeGameObject.GetComponentsInChildren<Renderer>(includeInactive))
        {
            if (renderer == null) continue;
            foreach (var material in renderer.sharedMaterials) AddValidMaterial(material);
        }

        Debug.Log($"Found {materials.Count} materials");
    }

    private void ProcessMaterials()
    {
        var selected = GetSelectedMaterials();
        if (selected.Count == 0)
        {
            EditorUtility.DisplayDialog(GetLocalizedString("WindowTitle"), GetLocalizedString("NoMaterials"), "OK");
            return;
        }

        Undo.SetCurrentGroupName("Process Materials");
        int processed = 0;

        foreach (var mat in selected)
        {
            if (mat == null) continue;
            SaveOriginalValues(mat);
            
            if (TryReplaceShader(mat))
            {
                SetCutoffValue(mat);
                processed++;
                modifiedMaterials.Add(mat);
            }
            
            RestoreOriginalValues(mat);
        }

        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        if (processed > 0) ShowSuccessMessage(processed);
    }

    private void ApplyDissolveParameters()
    {
        int appliedCount = 0;
        for (int i = 0; i < materials.Count; i++)
        {
            if (materialSelection[i] && materials[i] != null)
            {
                Undo.RecordObject(materials[i], "Apply Dissolve Parameters");
                if (ApplyToMaterial(materials[i]))
                {
                    appliedCount++;
                }
            }
        }
        
        if (appliedCount > 0) 
        {
            Debug.Log($"Applied dissolve parameters to {appliedCount} materials");
            foreach (var material in materials)
            {
                if (material != null)
                {
                    EditorUtility.SetDirty(material);
                }
            }
        }
        else 
        {
            Debug.LogWarning(GetLocalizedString("NoMaterials"));
        }
    }

    private void HandleDragAndDrop(Rect dropArea)
    {
        var evt = Event.current;
        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
        {
            if (!dropArea.Contains(evt.mousePosition)) return;
            
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (var obj in DragAndDrop.objectReferences)
                    if (obj is Material mat) AddValidMaterial(mat);
            }
        }
    }
    #endregion

    #region 工具方法
    private bool ShouldSkipMaterial(int index)
{
    var mat = materials[index];
    
    // 空槽位不应该被跳过
    if (mat == null)
        return false;
    
    return (!string.IsNullOrEmpty(searchText) && 
           !mat.name.ToLower().Contains(searchText.ToLower()) && 
           !mat.shader.name.ToLower().Contains(searchText.ToLower()));
    }

    private void AddValidMaterial(Material material)
    {
        if (material == null || material.shader == null || 
            !(material.shader.name.Contains("lilToon") || material.shader.name.Contains("lts_"))) return;
        
        if (materials.Contains(material)) return;
        
        materials.Add(material);
        materialSelection.Add(true);
        originalValues[material] = material.renderQueue;
    }

    private void AddMaterialSlot()
    {
        materials.Add(null);
        materialSelection.Add(true); // 修复：新添加的槽位默认选中
        Repaint();
    }

    private void RemoveMaterial(int index)
    {
        if (index < 0 || index >= materials.Count) return;
        
        var mat = materials[index];
        if (mat != null)
        {
            originalValues.Remove(mat);
            modifiedMaterials.Remove(mat);
        }
        
        materials.RemoveAt(index);
        materialSelection.RemoveAt(index);
        Repaint();
    }

    private bool TryReplaceShader(Material mat)
    {
        var shaderName = mat.shader.name;
        Shader newShader = null;

        if (shaderName.EndsWith("lts_o.shader") || shaderName == "Hidden/lilToonOutline")
            newShader = Shader.Find("Hidden/lilToonTransparentOutline");
        else if (shaderName.EndsWith("lts.shader") || shaderName == "lilToon")
            newShader = Shader.Find("Hidden/lilToonTransparent");

        if (newShader != null && mat.shader != newShader)
        {
            Undo.RecordObject(mat, "Change Shader");
            mat.shader = newShader;
            return true;
        }
        return false;
    }

    private void SetCutoffValue(Material mat)
    {
        if (mat.HasProperty("_Cutoff"))
        {
            Undo.RecordObject(mat, "Set Cutoff");
            mat.SetFloat("_Cutoff", 0.001f);
        }
    }

    private void SaveOriginalValues(Material mat)
    {
        if (!originalValues.ContainsKey(mat)) originalValues[mat] = mat.renderQueue;
    }

    private void RestoreOriginalValues(Material mat)
    {
        if (originalValues.ContainsKey(mat) && mat.renderQueue != originalValues[mat])
        {
            Undo.RecordObject(mat, "Restore Values");
            mat.renderQueue = originalValues[mat];
        }
    }

    private List<Material> GetSelectedMaterials()
    {
        var selected = new List<Material>();
        for (int i = 0; i < materialSelection.Count; i++)
            if (materialSelection[i] && i < materials.Count && materials[i] != null)
                selected.Add(materials[i]);
        return selected;
    }

    private int GetSelectedCount()
    {
        int count = 0;
        for (int i = 0; i < materialSelection.Count; i++)
            if (materialSelection[i] && i < materials.Count && materials[i] != null)
                count++;
        return count;
    }

    private void SelectAll()
    {
        for (int i = 0; i < materialSelection.Count; i++) materialSelection[i] = true;
    }

    private void DeselectAll()
    {
        for (int i = 0; i < materialSelection.Count; i++) materialSelection[i] = false;
    }

    private void ClearAll()
    {
        materials.Clear();
        materialSelection.Clear();
        originalValues.Clear();
        modifiedMaterials.Clear();
    }

    private void UpdateModifiedState()
    {
        modifiedMaterials.Clear();
        foreach (var mat in materials)
            if (mat != null && originalValues.ContainsKey(mat) && mat.renderQueue != originalValues[mat])
                modifiedMaterials.Add(mat);
    }

    private void ShowSuccessMessage(int count)
    {
        EditorUtility.DisplayDialog(GetLocalizedString("WindowTitle"), 
            string.Format(GetLocalizedString("ProcessSuccess"), count), "OK");
    }

    private bool ApplyToMaterial(Material material)
    {
        if (!material.HasProperty("_DissolveParams")) return false;
        
        bool changed = false;
        
        var currentParams = material.GetVector("_DissolveParams");
        var newParams = currentParams;
        newParams.x = (float)currentDissolveMode;
        
        switch (currentDissolveMode)
        {
            case DissolveMode.Alpha:
                newParams.z = alphaBorder;
                newParams.w = alphaBlur;
                changed |= ApplyAlphaModeParameters(material);
                break;
            case DissolveMode.Position:
                newParams.y = (float)currentShapeType;
                newParams.z = positionBorder;
                newParams.w = positionBlur;
                changed |= ApplyPositionModeParameters(material);
                break;
        }
        
        if (currentParams != newParams)
        {
            material.SetVector("_DissolveParams", newParams);
            changed = true;
        }
        
        return changed;
    }

    private bool ApplyAlphaModeParameters(Material material)
    {
        bool changed = false;
        if (material.HasProperty("_DissolveMask")) { material.SetTexture("_DissolveMask", alphaMask); changed = true; }
        if (material.HasProperty("_DissolveNoiseMask")) { material.SetTexture("_DissolveNoiseMask", alphaNoiseMask); changed = true; }
        if (material.HasProperty("_DissolveNoiseMask_ST")) { material.SetVector("_DissolveNoiseMask_ST", new Vector4(alphaNoiseTiling.x, alphaNoiseTiling.y, alphaNoiseOffset.x, alphaNoiseOffset.y)); changed = true; }
        if (material.HasProperty("_DissolveNoiseStrength")) { material.SetFloat("_DissolveNoiseStrength", alphaNoiseStrength); changed = true; }
        if (material.HasProperty("_DissolveNoiseMask_ScrollRotate")) { material.SetVector("_DissolveNoiseMask_ScrollRotate", new Vector4(alphaNoiseScrollRotate.x, alphaNoiseScrollRotate.y, 0, 0)); changed = true; }
        if (material.HasProperty("_DissolveColor")) 
        { 
            material.SetColor("_DissolveColor", alphaColor); 
            changed = true; 
        }
        return changed;
    }

    private bool ApplyPositionModeParameters(Material material)
    {
        bool changed = false;
        if (material.HasProperty("_DissolvePos"))
        {
            var currentPos = material.GetVector("_DissolvePos");
            var newPos = new Vector4(positionValue.x, positionValue.y, positionValue.z, currentPos.w);
            if (currentPos != newPos) { material.SetVector("_DissolvePos", newPos); changed = true; }
        }
        if (material.HasProperty("_DissolveNoiseMask")) { material.SetTexture("_DissolveNoiseMask", positionNoiseMask); changed = true; }
        if (material.HasProperty("_DissolveNoiseMask_ST")) { material.SetVector("_DissolveNoiseMask_ST", new Vector4(positionNoiseTiling.x, positionNoiseTiling.y, positionNoiseOffset.x, positionNoiseOffset.y)); changed = true; }
        if (material.HasProperty("_DissolveNoiseStrength")) { material.SetFloat("_DissolveNoiseStrength", positionNoiseStrength); changed = true; }
        if (material.HasProperty("_DissolveNoiseMask_ScrollRotate")) { material.SetVector("_DissolveNoiseMask_ScrollRotate", new Vector4(positionNoiseScrollRotate.x, positionNoiseScrollRotate.y, 0, 0)); changed = true; }
        if (material.HasProperty("_DissolveColor")) 
        { 
            material.SetColor("_DissolveColor", positionColor); 
            changed = true; 
        }
        return changed;
    }

    private void SetSystemLanguage()
    {
        var sysLang = Application.systemLanguage;
        currentLanguage = sysLang == SystemLanguage.Japanese ? Language.Japanese :
                         sysLang == SystemLanguage.Chinese || 
                         sysLang == SystemLanguage.ChineseSimplified || 
                         sysLang == SystemLanguage.ChineseTraditional ? Language.Chinese : Language.English;
        titleContent = new GUIContent(GetLocalizedString("WindowTitle"));
    }

    private string GetLocalizedString(string key) => 
        localizedStrings.ContainsKey(key) ? localizedStrings[key][(int)currentLanguage] : key;
    #endregion
}