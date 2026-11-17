#if VRC_SDK_VRCSDK3
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Profiling;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using static VRC.SDK3A.Editor.FaceTrackingDrawerUtility;

namespace VRC.SDK3A.Editor
{
    /// <summary>
    /// Edits a Blendshapes Set wherever it is serialized. Blendshape pickers use an ancestor Avatar
    /// Descriptor when available; standalone assets can store names supplied by later build steps.
    /// </summary>
    [CustomPropertyDrawer(typeof(VRCAvatarDescriptor.FaceTrackingBlendshapesSet))]
    public class FaceTrackingBlendshapesSetDrawer : PropertyDrawer
    {
        const float Padding = 10f;
        const float MaxScrollHeight = 280f;
        const float HelpHeight = 60f;
        static readonly string[] Tabs = { "All", "Eyes", "Brows", "Nose", "Cheek", "Jaw", "Lip", "Mouth", "Tongue", "Throat", "Visemes" };
        static readonly string[] TrackingKeyNames = FaceTrackingPickers.KeyNames;
        static readonly GUIContent[] TrackingTitles = Array.ConvertAll(TrackingKeyNames, name => new GUIContent(name));
        static readonly GUIContent[] VisemeTitles = Array.ConvertAll(FaceTrackingPickers.VisemeNames, name => new GUIContent(name));
        static readonly string[] MultiplierActions = { "Set All", "Scale All", "Add to All", "Cancel" };
        const int AllTab = 0;
        const int VisemeTab = 10;
        static readonly ProfilerMarker DrawMarker = new ProfilerMarker("VRC.FaceTracking.BlendshapesSetDrawer");
        
        private const string VISEME_PREFIX = "visemeBlendshape";
        private const string FACETRACKING_PREFIX = "faceTrackingBlendshape";
        private const string NAMES_SUFFIX = "Names";
        private const string MULTS_SUFFIX = "Multipliers";
        
        private const string VISEME_NAMES = VISEME_PREFIX + NAMES_SUFFIX;
        private const string VISEME_MULTS = VISEME_PREFIX + MULTS_SUFFIX;
        private const string FACETRACKING_NAMES = FACETRACKING_PREFIX + NAMES_SUFFIX;
        private const string FACETRACKING_MULTS = FACETRACKING_PREFIX + MULTS_SUFFIX;

        class ViewState
        {
            public int tab;
            public string search = "";
            public string autoFillFilter = "";
            public Vector2 scroll;
            public bool editMultipliers;
            public string multiplier = "1";
            public readonly List<MappingRow> rows = new List<MappingRow>();
            public float contentHeight;
            public readonly SetBindings bindings = new SetBindings();
            public FaceTrackingEditorContext context;
            public FaceTrackingEditorContext.MeshCatalog catalog;
            public int rowsVersion = -1;
            public int rowsTab = -1;
            public string rowsSearch;
        }

        class MappingFields
        {
            public SerializedProperty name, multiplier;
        }

        // Each set of shapes gets its own bindings to mesh blendshapes
        // A set is fully contained in a single `property` serialized property
        // With all its mappings stored in a nested array of properties
        class SetBindings
        {
            public SerializedProperty property, name, weight;
            public MappingFields[] tracking, visemes;
            
            int _revision = -1;
            int _tick = -1;
            
            (
                SerializedProperty faceTrackingBlendshapeNames, 
                SerializedProperty faceTrackingBlendshapeMultipliers, 
                SerializedProperty visemeBlendshapeNames, 
                SerializedProperty visemeBlendshapeMultipliers
            ) _arrays;
            
            // Initializes the mapping list for a specific blendshapes
            // Handles both face tracking and viseme mappings
            public bool Bind(SerializedProperty next, bool initialize)
            {
                // A list deletion can invalidate the cached property; check the revision before accessing it.
                bool changed = _revision != FaceTrackingEditorContext.BindingsRevision || property == null ||
                               property.serializedObject != next.serializedObject ||
                               property.propertyPath != next.propertyPath;

                // Avoid re-binding every frame. Only re-bind on init or empty mappings
                if (!changed && _tick == FaceTrackingEditorContext.Tick && (!initialize || tracking != null))
                    return false;

                _tick = FaceTrackingEditorContext.Tick;

                // Ensure that the shape lists are of an appropriate size.
                // If not, recreate them from the serialized data below
                if (!changed)
                {
                    changed |= _arrays.faceTrackingBlendshapeNames?.arraySize != (int) VRCAvatarDescriptor.FaceTrackingKey.Max;
                    changed |= _arrays.faceTrackingBlendshapeMultipliers?.arraySize != (int) VRCAvatarDescriptor.FaceTrackingKey.Max;
                    changed |= _arrays.visemeBlendshapeNames?.arraySize != (int) VRC_AvatarDescriptor.Viseme.Count;
                    changed |= _arrays.visemeBlendshapeMultipliers?.arraySize != (int) VRC_AvatarDescriptor.Viseme.Count;
                }

                if (initialize)
                {
                    InitializeMappings(next, FACETRACKING_PREFIX, (int) VRCAvatarDescriptor.FaceTrackingKey.Max);
                    InitializeMappings(next, VISEME_PREFIX, (int) VRC_AvatarDescriptor.Viseme.Count);
                }

                changed |= initialize && tracking == null;
                if (!changed)
                    return false;

                property = next.Copy();
                name = property.FindPropertyRelative("setName");
                weight = property.FindPropertyRelative("setWeight");
                _arrays = (
                    property.FindPropertyRelative(FACETRACKING_NAMES),
                    property.FindPropertyRelative(FACETRACKING_MULTS),
                    property.FindPropertyRelative(VISEME_NAMES),
                    property.FindPropertyRelative(VISEME_MULTS)
                );
                _revision = FaceTrackingEditorContext.BindingsRevision;
                tracking = initialize ? BindMappings(property, FACETRACKING_NAMES, FACETRACKING_MULTS) : null;
                visemes = initialize ? BindMappings(property, VISEME_NAMES, VISEME_MULTS) : null;
                return true;
            }

            // Loads serialized data for visemes or face tracking shapes
            static MappingFields[] BindMappings(SerializedProperty property, string namePropName, string multiplierPropName)
            {
                var names = property.FindPropertyRelative(namePropName);
                var multipliers = property.FindPropertyRelative(multiplierPropName);
                var fields = new MappingFields[names.arraySize];
                for (int i = 0; i < fields.Length; i++)
                {
                    fields[i] = new MappingFields { name = names.GetArrayElementAtIndex(i), multiplier = multipliers.GetArrayElementAtIndex(i) };
                }
                return fields;
            }
        }

        struct MappingRow
        {
            public int index;
            public bool viseme;
            public string text;
            public bool help;
            public bool disabled;
            public GUIContent title;
            public float Height => help ? HelpHeight : Step;
        }

        // Unity can reuse a drawer for multiple list entries; navigation must belong to each property.
        readonly Dictionary<(int target, string path), ViewState> _states = new Dictionary<(int, string), ViewState>();

        /// <summary>
        /// Fetches or initializes a view state for the provided blendshape set
        /// </summary>
        /// <param name="property">Serialized property for the set</param>
        /// <param name="embedded">When `True` - only one ViewState per object is maintained. Should be `False` when used outside VRC inspectors.</param>
        /// <param name="initialize">Initializes the shape mappings from serialized data</param>
        /// <returns></returns>
        ViewState GetState(SerializedProperty property, bool embedded, bool initialize)
        {
            // disambiguate between inspectors for multiple objects
            // VRC inspectors only have one blendshape set view state per descriptor (only one set is shown at a time).
            var key = (property.serializedObject.targetObject.GetInstanceID(), embedded ? "$inspector" : property.propertyPath);
            if (!_states.TryGetValue(key, out var state))
            {
                _states.Add(key, state = new ViewState());
            }
            
            if (state.bindings.Bind(property, initialize))
            {
                state.context = FaceTrackingEditorContext.ForProperty(property);
                state.rowsVersion = -1;
            }
            
            var catalog = state.context.Catalog;
            if (state.catalog != catalog)
                state.rowsVersion = -1;
            
            state.catalog = catalog;
            return state;
        }

        // Was the set name edited in this GUI pass
        // Used to communicate the name changes up the GUI stack for validation
        internal bool NameEdited { get; private set; }
        
        // Initialize the default state for all blendshape set drawers
        internal void RevealInspectorMappings()
        {
            foreach (var state in _states.Values)
            {
                state.tab = 0;
                state.search = "";
                state.scroll = Vector2.zero;
                state.rowsVersion = -1;
            }
        }

        internal void ResetInspectorView()
        {
            foreach (var state in _states.Values)
            {
                state.tab = 0;
                state.rowsVersion = -1;
            }
        }
        internal void DrawInspectorGUI(SerializedProperty property)
        {
            NameEdited = false;
            Rect rect = EditorGUILayout.GetControlRect(false, GetPropertyHeight(property, GUIContent.none));
            OnGUI(rect, property, GUIContent.none);
        }

        public override bool CanCacheInspectorGUI(SerializedProperty property) => false;
        
        // If used from a UIElements context - the property drawer will be wrapped in an IMGUI Container
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
            => FaceTrackingDrawerUtility.CreatePropertyGUI(this, property);

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            bool embedded = label == GUIContent.none || string.IsNullOrEmpty(label.text);
            if (!embedded && !property.isExpanded)
                return Line;
            
            var state = GetState(property, embedded, false);
            RefreshRows(state);
            float body = BodyHeight(state);
            return embedded ? body - Padding * 2 - Step : Step + Padding + body + state.context.AuthoringHeight(true);
        }

        static float BodyHeight(ViewState state)
            => Padding * 3 + Step * 9 + Mathf.Min(MaxScrollHeight, state.contentHeight);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using var sample = DrawMarker.Auto();
            EditorGUI.BeginProperty(position, label, property);
            int indent = EditorGUI.indentLevel;
            try
            {
                // Embedded mode is mainly used for drawing inside VRC inspectors, which renders a single blendshape set at a time.
                // In this mode, the foldout is omitted and the set is always expanded.
                bool embedded = label == GUIContent.none || string.IsNullOrEmpty(label.text);
                if (!embedded)
                    property.isExpanded = EditorGUI.Foldout(TakeLine(ref position), property.isExpanded, label, true);
                
                if (!embedded && !property.isExpanded)
                    return;
                
                position = EditorGUI.IndentedRect(position);
                EditorGUI.indentLevel = 0;
                var state = GetState(property, embedded, true);
                
                // Draw extra fields required for authoring outside the Avatar Descriptor when not in embedded mode 
                if (!embedded)
                {
                    state.context.DrawAuthoring(ref position, true);
                }
                
                var catalog = state.context.Catalog;
                if (state.catalog != catalog)
                    state.rowsVersion = -1;
                
                state.catalog = catalog;
                // Refresh rows early so we can calculate proper height for non-embedded cases
                RefreshRows(state);
                // Non-embedded mode draws a header for the current blendshape set w/ copy-paste buttons
                // Embedded mode relies on the Avatar Descriptor inspector for this
                if (!embedded)
                {
                    var box = new Rect(position.x, position.y + Padding, position.width, BodyHeight(state));
                    GUI.Box(box, GUIContent.none, GUI.skin.box);
                    position = new Rect(box.x + Padding, box.y + Padding, box.width - Padding * 2, box.height - Padding * 2);
                    var header = TakeLine(ref position);
                    EditorGUI.LabelField(header, "Blendshapes Set", EditorStyles.boldLabel);
                    if (GUI.Button(new Rect(header.xMax - 134f, header.y, 64f, Line), CopyContent))
                    {
                        Copy(property, false);
                    }
                    if (GUI.Button(new Rect(header.xMax - 64f, header.y, 64f, Line), PasteContent) &&
                        Paste(property, false) >= 0)
                    {
                        return;
                    }
                }
                var name = state.bindings.name;
                using (var c = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUI.PropertyField(TakeLine(ref position), name, new GUIContent("Blendshapes Set Name", "Name used to reference this set from Animator State Behaviors"));
                    if (c.changed)
                    {
                        NameEdited = true;
                        if (name.stringValue.Length > 100)
                        {
                            name.stringValue = name.stringValue.Substring(0, 100);
                        }
                        FaceTrackingEditorContext.Invalidate();
                    }
                }
                
                EditorGUI.Slider(TakeLine(ref position), state.bindings.weight, 0f, 1f, new GUIContent("Default Weight", "Initial weight for this set at avatar load. If another set with the same name exists at a higher index, this will be forced to 0."));
                position.y += 5f;
                state.autoFillFilter = EditorGUI.TextField(TakeLine(ref position), new GUIContent("Auto-Fill Filter", "Only auto-fill blendshape names containing this text."), state.autoFillFilter);
                position.y += 5f;
                
                var buttons = TakeLine(ref position);
                using (new EditorGUI.DisabledScope(catalog.Meshes.Count == 0))
                {
                    if (GUI.Button(new Rect(buttons.x, buttons.y, buttons.width / 2 - 2f, Line), "Auto-Fill Blendshapes"))
                        AutoFill(property, state);
                }

                if (GUI.Button(new Rect(buttons.x + buttons.width / 2 + 2f, buttons.y, buttons.width / 2 - 2f, Line),
                        "Clear Blendshapes")
                    && ConfirmClear(property, state))
                {
                    ClearMappings(property, state);
                }

                state.tab = GUI.Toolbar(TakeLine(ref position), state.tab, Tabs, EditorStyles.toolbarButton);
                state.search = EditorGUI.TextField(TakeLine(ref position), state.search, GUI.skin.FindStyle("ToolbarSearchTextField") ?? EditorStyles.toolbarTextField);
                DrawMultiplierTools(TakeLine(ref position), property, state);
                // Refresh the rows again in case search or tab has changed
                RefreshRows(state);
                float scrollHeight = Mathf.Min(MaxScrollHeight, state.contentHeight);
                float scrollbar = state.contentHeight > scrollHeight ? GUI.skin.verticalScrollbar.fixedWidth : 0f;
                var columns = TakeLine(ref position);
                columns.width -= scrollbar;
                DrawColumnHeaders(columns);

                var viewport = new Rect(position.x, position.y, position.width, scrollHeight);
                var content = new Rect(0f, 0f, viewport.width - scrollbar, state.contentHeight);
                using (var sv = new GUI.ScrollViewScope(viewport, state.scroll, content))
                {
                    state.scroll = sv.scrollPosition;
                    foreach (var row in state.rows)
                    {
                        var rowRect = new Rect(0f, content.y, content.width, row.Height);
                        try
                        {
                            if (row.help)
                            {
                                EditorGUI.HelpBox(rowRect, row.text, MessageType.Info);
                            }
                            else if (row.index < 0)
                            {
                                EditorGUI.LabelField(rowRect, row.text, EditorStyles.boldLabel);
                            }
                            else
                            {
                                DrawMappingRow(rowRect, property, row, state);
                            }
                        }
                        catch (ExitGUIException)
                        {
                            // let Unity stop the current GUI pass if a control calls GUIUtility.ExitGUI()
                            throw;
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                        // to prevent rows from overlapping if something throws, this has to stay outside the try/catch
                        content.y += row.Height;
                    }
                }
            }
            finally
            {
                EditorGUI.indentLevel = indent;
                EditorGUI.EndProperty();
            }
        }

        // Re-builds the list of blendshape mappings to display (Key, Blendshape, Multiplier)
        // Also calculates the up-to-date height of the content
        static void RefreshRows(ViewState state)
        {
            var catalog = state.catalog;
            // Short-circuit if the state is unchanged
            if (state.rowsVersion == catalog.MappingsVersion && state.rowsTab == state.tab && state.rowsSearch == state.search)
                return;
            
            state.rowsVersion = catalog.MappingsVersion;
            state.rowsTab = state.tab;
            state.rowsSearch = state.search;
            state.rows.Clear();
            bool visemesAvailable = catalog.VisemesAvailable;
            
            switch (state.tab)
            {
                case AllTab or VisemeTab when visemesAvailable:
                {
                    state.rows.Add(new MappingRow { index = -1, text = "Visemes" });
                    for (int i = 0; i < (int)VRC_AvatarDescriptor.Viseme.Count; i++)
                    {
                        string title = VisemeTitles[i].text;
                        if (MatchesSearch(title, state.search))
                            state.rows.Add(new MappingRow { index = i, viseme = true, text = title, title = VisemeTitles[i] });
                    }

                    break;
                }
                case VisemeTab:
                    state.rows.Add(new MappingRow { index = -1, help = true, text = "Visemes require LipSync Mode 'Viseme BlendShape Face Tracking'." });
                    break;
            }

            if (state.tab != VisemeTab)
            {
                bool anyKeys = false;
                foreach (var category in FaceTrackingPickers.Categories)
                {
                    if (state.tab != 0 && state.tab != category.tab)
                        continue;
                    bool headingAdded = false;
                    foreach (int i in category.keys)
                    {
                        // This limits the displayed keys to Gaze/Blink only
                        if (!catalog.UseAllKeys && !VRCAvatarDescriptor.IsBasicFaceTrackingKey(i))
                            continue;
                        
                        string title = TrackingKeyNames[i];
                        if (!MatchesSearch(title, state.search))
                            continue;
                        
                        // Only render category headings on the "All" tab, once per category
                        if (state.tab == AllTab && !headingAdded)
                        {
                            state.rows.Add(new MappingRow { index = -1, text = category.label });
                            headingAdded = true;
                        }
                        
                        bool disabled = IsEyeMappingDisabled(catalog, i);
                        // Handle disabled eyelook shapes
                        // The index comparison is used to avoid adding the same help text for every eyelook shape in the category
                        if (disabled && i == (int)VRCAvatarDescriptor.FaceTrackingKey.EyeLookOutRight)
                            state.rows.Add(new MappingRow { index = -1, help = true, text = "Eye gaze blendshapes are disabled because Eye Gaze Mode is Bone Rotation." });
                        
                        // Handle disabled blink shapes
                        if (disabled && i == (int)VRCAvatarDescriptor.FaceTrackingKey.EyeClosedRight)
                            state.rows.Add(new MappingRow { index = -1, help = true, text = "Eye closed blendshapes are disabled because Eyelid Type is Bones or Blendshapes." });
                        
                        state.rows.Add(new MappingRow { index = i, text = title, title = TrackingTitles[i], disabled = disabled });
                        anyKeys = true;
                    }
                }


                if (!anyKeys)
                {
                    state.rows.Add(new MappingRow
                    {
                        index = -1, help = true, text = !catalog.UseAllKeys && string.IsNullOrEmpty(state.search)
                            ? "All tracking keys in this category require 'Use All Tracking Keys'."
                            : "No results found."
                    });
                }
            }
            state.contentHeight = 0f;
            foreach (var row in state.rows)
            {
                state.contentHeight += row.Height;
            }
        }

        static bool MatchesSearch(string name, string search) => string.IsNullOrEmpty(search) || name.Contains(search, StringComparison.OrdinalIgnoreCase);

        // Check if current avatar configuration allows for Gaze or Blink blendshapes to be mapped
        // As all shapes are in a fixed order, this is addressed by key index
        static bool IsEyeMappingDisabled(FaceTrackingEditorContext.MeshCatalog catalog, int index)
        {
            if (index >= (int)VRCAvatarDescriptor.FaceTrackingKey.EyeLookOutRight && index <= (int)VRCAvatarDescriptor.FaceTrackingKey.EyeLookDownLeft)
                return catalog.GazeDisabled;
            return (index == (int)VRCAvatarDescriptor.FaceTrackingKey.EyeClosedRight || index == (int)VRCAvatarDescriptor.FaceTrackingKey.EyeClosedLeft)
                && catalog.BlinkDisabled;
        }

        // Look up a property for the provided key row
        static SerializedProperty MappingProperty(SerializedProperty set, MappingRow row, string suffix)
            => set.FindPropertyRelative((row.viseme ? VISEME_PREFIX : FACETRACKING_PREFIX) + suffix).GetArrayElementAtIndex(row.index);

        static void Columns(Rect row, out Rect key, out Rect blendshape, out Rect multiplier)
        {
            key = new Rect(row.x, row.y, Mathf.Min(180f, row.width * 0.35f), Line);
            multiplier = new Rect(row.xMax - 60f, row.y, 60f, Line);
            float start = key.xMax + 4f;
            blendshape = new Rect(start, row.y, multiplier.x - start - 4f, Line);
        }

        static void DrawColumnHeaders(Rect row)
        {
            Columns(row, out var key, out var blendshape, out var multiplier);
            EditorGUI.LabelField(key, "Tracking Key", EditorStyles.boldLabel);
            EditorGUI.LabelField(blendshape, "Blendshape", EditorStyles.boldLabel);
            EditorGUI.LabelField(multiplier, new GUIContent("Multiplier", "Tracking strength multiplier"), EditorStyles.boldLabel);
        }

        static void DrawMappingRow(Rect rowRect, SerializedProperty set, MappingRow row, ViewState state)
        {
            Columns(rowRect, out var keyRect, out var blendshapeRect, out var multiplierRect);
            EditorGUI.LabelField(keyRect, row.title);
            if (Event.current.type == EventType.ContextClick && keyRect.Contains(Event.current.mousePosition))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Copy Tracking Key Name"), false, () => EditorGUIUtility.systemCopyBuffer = row.text);
                var copiedSet = set.Copy();
                menu.AddItem(new GUIContent("Copy Blendshapes Set"), false, () => Copy(copiedSet, false));
                menu.ShowAsContext();
                Event.current.Use();
            }
            using (new EditorGUI.DisabledScope(row.disabled))
            {
                var fields = row.viseme ? state.bindings.visemes[row.index] : state.bindings.tracking[row.index];
                DrawBlendshapePicker(blendshapeRect, fields.name, state.catalog, row.title);
                var multiplier = fields.multiplier;
                multiplierRect.xMin = multiplierRect.xMax - 40f;
                EditorGUI.BeginChangeCheck();
                float value = EditorGUI.FloatField(multiplierRect, multiplier.floatValue);
                if (EditorGUI.EndChangeCheck())
                    multiplier.floatValue = Mathf.Clamp(value, 0f, 10f);
            }
        }

        static bool ConfirmClear(SerializedProperty property, ViewState state)
        {
            string name = state.bindings.name.stringValue;
            string label = name;
            if (ArrayElement(property, out _, out int index))
                label = string.IsNullOrEmpty(name) ? (index + 1).ToString() : (index + 1) + ": " + name;
            
            if (state.tab == VisemeTab)
                return EditorUtility.DisplayDialog("Clear Visemes", "Clear all viseme mappings for Set " + label + "?", "Clear", "Cancel");
            
            bool all = state.tab == AllTab && string.IsNullOrEmpty(state.search);
            return EditorUtility.DisplayDialog(
                all ? "Clear ALL Face Tracking Blendshapes" : "Clear Face Tracking Blendshapes",
                all
                    ? "Clear ALL face tracking blendshape mappings for Set " + label + "?"
                    : "Clear face tracking blendshape mappings shown below for Set " + label + "?", "Clear", "Cancel");
        }

        /// <summary>
        /// Get mappings for the current blendshape set based on the <paramref name="state"/> for bulk actions like auto-fill or clear.
        /// These will be filtered by the current tab and search string.
        /// </summary>
        /// <param name="state">Current view state</param>
        /// <param name="autoFill">Restricts face tracking keys to the basic set unless "Use All Keys" is enabled</param>
        /// <param name="clear">Returns viseme keys even if the descriptor doesn't have them anymore. This allows clearing viseme keys even after changing the Viseme mode. Used in clearing</param>
        /// <returns></returns>
        static IEnumerable<MappingRow> SelectedMappings(ViewState state, bool autoFill = false, bool clear = false)
        {
            if ((clear || state.catalog.VisemesAvailable) && (state.tab == AllTab || state.tab == VisemeTab))
            {
                for (int i = 0; i < (int)VRC_AvatarDescriptor.Viseme.Count; i++)
                {
                    if (MatchesSearch(VisemeTitles[i].text, state.search))
                        yield return new MappingRow { index = i, viseme = true };
                }
            }
            
            if (state.tab == VisemeTab) yield break;
            
            // Returns face tracking keys matching the following conditions:
            // - Key is a part of the current tracking category, based on the tab, unless on "All" tab
            // - Key matches the search filter
            // - Key is a part of a basic face tracking key set, or "Use All Keys" is set in the descriptor
            // - (but for clear-all, or multiplier set-all, ignore the basic key limit)
            foreach (var category in FaceTrackingPickers.Categories)
            {
                if (state.tab != 0 && state.tab != category.tab) continue;
                
                foreach (int i in category.keys)
                {
                    // Auto fill should not include non-basic keys unless "Use All Keys" is set
                    if (MatchesSearch(TrackingKeyNames[i], state.search) && (!autoFill || state.catalog.UseAllKeys || VRCAvatarDescriptor.IsBasicFaceTrackingKey(i)))
                        yield return new MappingRow { index = i };
                }
            }
        }

        /// <summary>
        /// Clears the mappings for the current view: blendshape set, tab, and search filter
        /// </summary>
        static void ClearMappings(SerializedProperty set, ViewState state)
        {
            Undo.SetCurrentGroupName("Clear Blendshapes");
            foreach (var row in SelectedMappings(state, clear: true))
            {
                if (row.index < 0)
                    continue;
                MappingProperty(set, row, NAMES_SUFFIX).stringValue = "";
                MappingProperty(set, row, MULTS_SUFFIX).floatValue = 1f;
            }
            state.rowsVersion = -1;
        }

        /// <summary>
        /// Batch-edit multipliers for the current view: blendshape set, tab, and search filter
        /// </summary>
        static void DrawMultiplierTools(Rect row, SerializedProperty set, ViewState state)
        {
            if (!state.editMultipliers)
            {
                row.xMin = Mathf.Max(row.x, row.xMax - 150f);
                if (GUI.Button(row, "Edit Multipliers..."))
                {
                    state.editMultipliers = true;
                    state.multiplier = "1";
                }
                return;
            }
            float buttonWidth = Mathf.Min(70f, row.width / 7f);
            float inputWidth = Mathf.Min(60f, row.width * 0.18f);
            float labelWidth = row.width - inputWidth - buttonWidth * 4f - 16f;
            EditorGUI.LabelField(new Rect(row.x, row.y, labelWidth, Line), "Multipliers:", EditorStyles.boldLabel);
            float inputStart = row.x + labelWidth;
            state.multiplier = EditorGUI.TextField(new Rect(inputStart, row.y, inputWidth, Line), state.multiplier);
            for (int i = 0; i < MultiplierActions.Length; i++)
            {
                if (!GUI.Button(new Rect(inputStart + inputWidth + 4f + i * (buttonWidth + 4f), row.y, buttonWidth, Line), new GUIContent(MultiplierActions[i], MultiplierActions[i])))
                    continue;
                if (i < 3 && float.TryParse(state.multiplier, out float value))
                {
                    Undo.SetCurrentGroupName("Edit Blendshape Multipliers");
                    foreach (var mapping in SelectedMappings(state))
                    {
                        if (mapping.index < 0)
                            continue;
                        var multiplier = MappingProperty(set, mapping, MULTS_SUFFIX);
                        multiplier.floatValue = Mathf.Clamp(i == 0 ? value : i == 1 ? multiplier.floatValue * value : multiplier.floatValue + value, 0f, 10f);
                    }
                }
                state.editMultipliers = false;
                GUIUtility.keyboardControl = 0;
            }
        }

        /// <summary>
        /// Autofill blendshape names for the current view: blendshape set, tab, and search filter
        /// </summary>
        static void AutoFill(SerializedProperty set, ViewState state)
        {
            string[] names = state.catalog.GetBlendshapeNames();
            Undo.SetCurrentGroupName("Auto-Fill Blendshapes");
            foreach (var row in SelectedMappings(state, true))
            {
                if (row.index < 0)
                    continue;
                var name = MappingProperty(set, row, NAMES_SUFFIX);
                if (!string.IsNullOrEmpty(name.stringValue))
                    continue;
                int found;
                if (row.viseme)
                {
                    string viseme = ((VRC_AvatarDescriptor.Viseme)row.index).ToString().ToLowerInvariant();
                    found = FaceTrackingAutoFill.FindVisemeBlendshape(names, viseme, state.autoFillFilter);
                }
                else
                {
                    found = FaceTrackingAutoFill.FindFaceTrackingBlendshape(names, (VRCAvatarDescriptor.FaceTrackingKey)row.index, state.autoFillFilter);
                }
                if (found < 0)
                    continue;
                
                name.stringValue = names[found];
                MappingProperty(set, row, MULTS_SUFFIX).floatValue = 1f;
            }
            state.rowsVersion = -1;
        }

        /// <summary>
        /// Initializes serialized arrays of shape names and multipliers for a provided blendshape set
        /// </summary>
        internal static void InitializeMappings(SerializedProperty property, string prefix, int count)
        {
            var names = property.FindPropertyRelative(prefix + NAMES_SUFFIX);
            var multipliers = property.FindPropertyRelative(prefix + MULTS_SUFFIX);
            
            int oldNames = names.arraySize;
            if (oldNames != count) names.arraySize = count;
            for (int i = oldNames; i < count; i++)
            {
                names.GetArrayElementAtIndex(i).stringValue = "";
            }
            
            int oldMultipliers = multipliers.arraySize;
            if (oldMultipliers != count) multipliers.arraySize = count;
            for (int i = oldMultipliers; i < count; i++)
            {
                multipliers.GetArrayElementAtIndex(i).floatValue = 1f;
            }
        }
    }
}
#endif
