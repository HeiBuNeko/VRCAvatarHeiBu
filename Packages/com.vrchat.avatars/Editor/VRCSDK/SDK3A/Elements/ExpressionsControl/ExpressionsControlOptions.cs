
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.ScriptableObjects;
using ExpressionsMenu = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu;
using ExpressionControl = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;


namespace VRC.SDK3A.Editor.Elements
{
    public class ExpressionsControlOptions : VisualElement
    {

        public new class UxmlFactory : UxmlFactory<ExpressionsControlOptions, UxmlTraits> { }

        public static bool ShowControlTypeHelpBox
        {
            get => SessionState.GetBool("ShowControlHelpBox", true);
            set => SessionState.SetBool("ShowControlHelpBox", value);
        }

        private ExpressionsControlLabel ControlNameField;
        private EnumField ControlTypeField;
        private ExpressionsControlParameterField ControlParameterField;
        private Slider ValueSlider;
        private SliderInt ValueIntSlider;
        private Toggle ValueToggle;
        private HelpBox ValueValidationWarningHelpBox;
        private VisualElement ControlTypeHelpBoxContainer;
        private Button TypeInfoButton;
        private HelpBox ControlTypeHelpBox;
        private VisualElement MenuTypesContainer;
        private VRCCreateObjectField SubMenuObjectField;
        private VisualElement TwoAxisPuppetContainer;
        private ExpressionsControlParameterField HorizontalParam;
        private ExpressionsControlParameterField VerticalParam;
        private ExpressionsControlLabel UpLabel;
        private ExpressionsControlLabel RightLabel;
        private ExpressionsControlLabel DownLabel;
        private ExpressionsControlLabel LeftLabel;
        private VisualElement FourAxisPuppetContainer;
        private ExpressionsControlParameterField FourAxisUpParam;
        private ExpressionsControlParameterField FourAxisRightParam;
        private ExpressionsControlParameterField FourAxisDownParam;
        private ExpressionsControlParameterField FourAxisLeftParam;
        private ExpressionsControlLabel FourAxisUpLabel;
        private ExpressionsControlLabel FourAxisRightLabel;
        private ExpressionsControlLabel FourAxisDownLabel;
        private ExpressionsControlLabel FourAxisLeftLabel;
        private ExpressionsControlParameterField RotationParamField;

        public ExpressionsMenu Menu;

        private SerializedProperty propControl;
        private SerializedProperty propName;
        private SerializedProperty propIcon;
        private SerializedProperty propType;
        private SerializedProperty propSubMenu;
        private SerializedProperty propControlParameter;
        private SerializedProperty propControlParameterName;
        private SerializedProperty propValue;

        private bool HasAnySelectedParameter => ControlParameterField.Parameter != null && ControlParameterField.Index > 0;


        #region HelpBox Text

        private const string HELPBOX_BUTTON =
            "Click or hold to activate. The button remains active for a minimum 0.2s.\n" +
            "While active, the (Parameter) is set to {0}.\n" +
            "When inactive, the (Parameter) is reset to {1}.";

        private const string HELPBOX_TOGGLE =
            "Click to toggle on or off.\n" +
            "When turned on, the (Parameter) is set to {0}.\n" +
            "When turned off, the (Parameter) is reset to {1}.";

        private const string HELPBOX_SUBMENU =
            "Opens another expression menu.\n" +
            "When opened, the (Parameter) is set to {0}.\n" +
            "When closed, (Parameter) is reset to {1}.";

        private const string HELPBOX_TWO_AXIS_PUPPET =
            "Puppet menu that maps the joystick to two parameters (-1 to +1).\n" +
            "When opened, the (Parameter) is set to {0}.\n" +
            "When closed, (Parameter) is reset to {1}.";

        private const string HELPBOX_FOUR_AXIS_PUPPET =
            "Puppet menu that maps the joystick to four parameters (0 to 1).\n" +
            "When opened, the (Parameter) is set to {0}.\n" +
            "When closed, (Parameter) is reset to {1}.";

        private const string HELPBOX_RADIAL_PUPPET =
            "Puppet menu that sets a value based on joystick rotation. (0 to 1)\n" +
            "When opened, the (Parameter) is set to {0}.\n" +
            "When closed, (Parameter) is reset to {1}.";

        private string _assignedControlHelpBoxTextFormat = string.Empty;

        private const string HELPBOX_ZERO_VALUE_WARNING_NUMERIC =
            "The Value set when this control is activated should usually be a value other than zero, so it doesn't match the value set when the control is deactivated. Otherwise, the control may not behave correctly.";

        private const string HELPBOX_ZERO_VALUE_WARNING_BOOLEAN =
            "The parameter will be set to false when this control is both activated and deactivated. Under normal use, the Value option should be ticked for Boolean parameters.";

        private const string HELPBOX_VALUE_UNDER_MINIMUM =
            "The Value set when this control is activated is lower than its minimum value of {0}. The value will be clamped.";

        private const string HELPBOX_VALUE_OVER_MAXIMUM =
            "The Value set when this control is activated is higher than its maximum value of {0}. The value will be clamped.";

        #endregion


        public ExpressionsControlOptions()
        {
            VisualTreeAsset uxml = Resources.Load<VisualTreeAsset>("VRCExpressionsControlOptions");
            uxml.CloneTree(this);
            QueryAllElements();
            BindCallbacks();
        }

        public ExpressionsControlOptions(SerializedProperty property, VRCExpressionsMenu menu)
        {
            VisualTreeAsset uxml = Resources.Load<VisualTreeAsset>("VRCExpressionsControlOptions");
            uxml.CloneTree(this);
            Menu = menu;

            QueryAllElements();

            BindProperty(property);

            BindCallbacks();
            TypeInfoButtonClicked();
            UpdateValueSliders();
        }

        private void QueryAllElements()
        {
            ControlNameField = this.Q<ExpressionsControlLabel>("ControlNameField");
            ControlTypeHelpBoxContainer = this.Q<VisualElement>("ControlTypeHelpBoxContainer");
            TypeInfoButton = this.Q<Button>("TypeInfoButton");
            ControlTypeField = this.Q<EnumField>("ControlTypeField");
            ControlParameterField = this.Q<ExpressionsControlParameterField>("ControlParameterField");
            ValueSlider = this.Q<Slider>("ValueSlider");
            ValueIntSlider = this.Q<SliderInt>("ValueIntSlider");
            ValueToggle = this.Q<Toggle>("ValueToggle");
            ValueValidationWarningHelpBox = this.Q<HelpBox>("ValueValidationWarningHelpBox");

            MenuTypesContainer = this.Q<VisualElement>("MenuTypesContainer");
            SubMenuObjectField = this.Q<VRCCreateObjectField>("SubMenuObjectField");
            TwoAxisPuppetContainer = this.Q<VisualElement>("TwoAxisPuppetContainer");
            HorizontalParam = this.Q<ExpressionsControlParameterField>("HorizontalParam");
            VerticalParam = this.Q<ExpressionsControlParameterField>("VerticalParam");
            UpLabel = this.Q<ExpressionsControlLabel>("UpLabel");
            RightLabel = this.Q<ExpressionsControlLabel>("RightLabel");
            DownLabel = this.Q<ExpressionsControlLabel>("DownLabel");
            LeftLabel = this.Q<ExpressionsControlLabel>("LeftLabel");
            FourAxisPuppetContainer = this.Q<VisualElement>("FourAxisPuppetContainer");
            FourAxisUpParam = this.Q<ExpressionsControlParameterField>("FourAxisUpParam");
            FourAxisRightParam = this.Q<ExpressionsControlParameterField>("FourAxisRightParam");
            FourAxisDownParam = this.Q<ExpressionsControlParameterField>("FourAxisDownParam");
            FourAxisLeftParam = this.Q<ExpressionsControlParameterField>("FourAxisLeftParam");
            FourAxisUpLabel = this.Q<ExpressionsControlLabel>("FourAxisUpLabel");
            FourAxisRightLabel = this.Q<ExpressionsControlLabel>("FourAxisRightLabel");
            FourAxisDownLabel = this.Q<ExpressionsControlLabel>("FourAxisDownLabel");
            FourAxisLeftLabel = this.Q<ExpressionsControlLabel>("FourAxisLeftLabel");
            RotationParamField = this.Q<ExpressionsControlParameterField>("RotationParamField");
        }

        private void BindCallbacks()
        {
            // HelpBoxes must be added manually and cannot be created in UIBuilder.
            ControlTypeHelpBox = new HelpBox { messageType = HelpBoxMessageType.Info };
            ControlTypeHelpBoxContainer.Add(ControlTypeHelpBox);
            ControlParameterField.changed += HandleParameterChanged;

            TypeInfoButton.clicked += () =>
            {
                ShowControlTypeHelpBox = !ShowControlTypeHelpBox;
                TypeInfoButtonClicked();
            };

            ControlTypeField.RegisterValueChangedCallback(ControlTypeChanged);

            ControlTypeChanged(null);
        }

        public void BindProperty(SerializedProperty prop)
        {
            propControl = prop;

            propName = propControl.FindPropertyRelative(nameof(ExpressionControl.name));
            propIcon = propControl.FindPropertyRelative(nameof(ExpressionControl.icon));
            propType = propControl.FindPropertyRelative(nameof(ExpressionControl.type));
            propSubMenu = propControl.FindPropertyRelative(nameof(ExpressionControl.subMenu));
            propControlParameter = propControl.FindPropertyRelative(nameof(ExpressionControl.parameter));
            propControlParameterName = propControlParameter.FindPropertyRelative(nameof(ExpressionControl.Parameter.name));
            propValue = propControl.FindPropertyRelative(nameof(ExpressionControl.value));

            ControlNameField.BindProperty(propName, propIcon);
            ControlTypeField.BindProperty(propType);
            ControlParameterField.BindParameter(propControlParameterName, Menu.Parameters);

            BindValueFields();
        }

        private void ControlTypeChanged(ChangeEvent<System.Enum> evt)
        {
            bool fromChangeCallback = evt != null;

            int undoGroup = 0;
            if (fromChangeCallback)
            {
                undoGroup = Undo.GetCurrentGroup();
            }

            ExpressionControl.ControlType controlType =
                (ExpressionControl.ControlType)propControl.FindPropertyRelative(nameof(ExpressionControl.type))
                    .enumValueFlag;

            SerializedProperty propSubParameters =
                propControl.FindPropertyRelative(nameof(ExpressionControl.subParameters));
            SerializedProperty propLabels = propControl.FindPropertyRelative(nameof(ExpressionControl.labels));


            switch (controlType)
            {
                default:
                case ExpressionControl.ControlType.Button:
                    MenuTypesContainer.style.display = DisplayStyle.None;

                    _assignedControlHelpBoxTextFormat = HELPBOX_BUTTON;
                    break;
                case ExpressionControl.ControlType.Toggle:
                    MenuTypesContainer.style.display = DisplayStyle.None;

                    _assignedControlHelpBoxTextFormat = HELPBOX_TOGGLE;
                    break;
                case ExpressionControl.ControlType.SubMenu:
                    MenuTypesContainer.style.display = DisplayStyle.Flex;
                    SubMenuObjectField.style.display = DisplayStyle.Flex;
                    TwoAxisPuppetContainer.style.display = DisplayStyle.None;
                    FourAxisPuppetContainer.style.display = DisplayStyle.None;
                    RotationParamField.style.display = DisplayStyle.None;

                    _assignedControlHelpBoxTextFormat = HELPBOX_SUBMENU;

                    SubMenuObjectField.BindProperty(propSubMenu, typeof(ExpressionsMenu), "SubMenu");
                    break;
                case ExpressionControl.ControlType.TwoAxisPuppet:
                    MenuTypesContainer.style.display = DisplayStyle.Flex;
                    SubMenuObjectField.style.display = DisplayStyle.None;
                    TwoAxisPuppetContainer.style.display = DisplayStyle.Flex;
                    FourAxisPuppetContainer.style.display = DisplayStyle.None;
                    RotationParamField.style.display = DisplayStyle.None;

                    _assignedControlHelpBoxTextFormat = HELPBOX_TWO_AXIS_PUPPET;

                    propSubParameters.arraySize = 2;
                    propLabels.arraySize = 4;
                    propControl.serializedObject.ApplyModifiedProperties();

                    SerializedProperty propHorizontal = propSubParameters.GetArrayElementAtIndex(0);
                    SerializedProperty propVertical = propSubParameters.GetArrayElementAtIndex(1);

                    HorizontalParam.BindParameter(
                        propHorizontal.FindPropertyRelative(nameof(ExpressionControl.Parameter.name)),
                        Menu.Parameters,
                        false);

                    VerticalParam.BindParameter(
                        propVertical.FindPropertyRelative(nameof(ExpressionControl.Parameter.name)),
                        Menu.Parameters,
                        false);

                    UpLabel.BindProperty(propLabels.GetArrayElementAtIndex(0));
                    RightLabel.BindProperty(propLabels.GetArrayElementAtIndex(1));
                    DownLabel.BindProperty(propLabels.GetArrayElementAtIndex(2));
                    LeftLabel.BindProperty(propLabels.GetArrayElementAtIndex(3));
                    break;
                case ExpressionControl.ControlType.FourAxisPuppet:
                    MenuTypesContainer.style.display = DisplayStyle.Flex;
                    SubMenuObjectField.style.display = DisplayStyle.None;
                    TwoAxisPuppetContainer.style.display = DisplayStyle.None;
                    FourAxisPuppetContainer.style.display = DisplayStyle.Flex;
                    RotationParamField.style.display = DisplayStyle.None;

                    _assignedControlHelpBoxTextFormat = HELPBOX_FOUR_AXIS_PUPPET;

                    propSubParameters.arraySize = 4;
                    propLabels.arraySize = 4;
                    propControl.serializedObject.ApplyModifiedProperties();

                    SerializedProperty propUp = propSubParameters.GetArrayElementAtIndex(0);
                    SerializedProperty propRight = propSubParameters.GetArrayElementAtIndex(1);
                    SerializedProperty propDown = propSubParameters.GetArrayElementAtIndex(2);
                    SerializedProperty propLeft = propSubParameters.GetArrayElementAtIndex(3);
                    FourAxisUpParam.BindParameter(
                        propUp.FindPropertyRelative(nameof(ExpressionControl.Parameter.name)),
                        Menu.Parameters,
                        false);
                    FourAxisRightParam.BindParameter(
                        propRight.FindPropertyRelative(nameof(ExpressionControl.Parameter.name)),
                        Menu.Parameters,
                        false);
                    FourAxisDownParam.BindParameter(
                        propDown.FindPropertyRelative(nameof(ExpressionControl.Parameter.name)),
                        Menu.Parameters,
                        false);
                    FourAxisLeftParam.BindParameter(
                        propLeft.FindPropertyRelative(nameof(ExpressionControl.Parameter.name)),
                        Menu.Parameters,
                        false);

                    FourAxisUpLabel.BindProperty(propLabels.GetArrayElementAtIndex(0));
                    FourAxisRightLabel.BindProperty(propLabels.GetArrayElementAtIndex(1));
                    FourAxisDownLabel.BindProperty(propLabels.GetArrayElementAtIndex(2));
                    FourAxisLeftLabel.BindProperty(propLabels.GetArrayElementAtIndex(3));
                    break;
                case ExpressionControl.ControlType.RadialPuppet:
                    MenuTypesContainer.style.display = DisplayStyle.Flex;
                    SubMenuObjectField.style.display = DisplayStyle.None;
                    TwoAxisPuppetContainer.style.display = DisplayStyle.None;
                    FourAxisPuppetContainer.style.display = DisplayStyle.None;
                    RotationParamField.style.display = DisplayStyle.Flex;

                    _assignedControlHelpBoxTextFormat = HELPBOX_RADIAL_PUPPET;

                    propSubParameters.arraySize = 1;
                    propLabels.arraySize = 0;
                    propControl.serializedObject.ApplyModifiedProperties();

                    SerializedProperty propRotation = propSubParameters.GetArrayElementAtIndex(0);
                    RotationParamField.BindParameter(
                        propRotation.FindPropertyRelative(nameof(ExpressionControl.Parameter.name)),
                        Menu.Parameters,
                        false);
                    break;
            }

            if (controlType != ExpressionControl.ControlType.SubMenu)
            {
                // Clear the submenu reference. This avoids unused submenus being dragged into asset builds
                // and avoids validation checking through stale submenus that are actually unused.
                propSubMenu.objectReferenceValue = null;
                propControl.serializedObject.ApplyModifiedProperties();
            }

            if (fromChangeCallback)
            {
                // Collapse into the undo operation before this one, as the type change has already happened.
                Undo.CollapseUndoOperations(undoGroup - 1);
            }

            RefreshControlHelpBox();
        }

        private void BindValueFields()
        {
            ValueSlider.RegisterValueChangedCallback(evt =>
            {
                UpdatePropValue(evt.newValue);
                ValueIntSlider.SetValueWithoutNotify(Mathf.FloorToInt(evt.newValue));
            });

            ValueIntSlider.RegisterValueChangedCallback(evt =>
            {
                UpdatePropValue(evt.newValue);
                ValueSlider.SetValueWithoutNotify(evt.newValue);
            });

            ValueToggle.RegisterValueChangedCallback(evt =>
            {
                UpdatePropValue(evt.newValue ? 1.0f : 0.0f);
            });
        }

        private void TypeInfoButtonClicked()
        {
            ControlTypeHelpBox.style.display = ShowControlTypeHelpBox ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void HandleParameterChanged()
        {
            if (HasAnySelectedParameter)
            {
                byte bitWidth = ControlParameterField.Parameter.bitWidth;
                VRCExpressionParameters.Parameter.ValueRepresentation representation = ControlParameterField.Parameter.GetRepresentationByType();

                switch (ControlParameterField.Parameter.valueType)
                {
                    case VRCExpressionParameters.ValueType.Int:
                        ValueSlider.style.display = DisplayStyle.None;
                        ValueIntSlider.style.display = DisplayStyle.Flex;
                        ValueToggle.style.display = DisplayStyle.None;

                        int minIntValue = representation == VRCExpressionParameters.Parameter.ValueRepresentation.Signed ? -(1 << (bitWidth - 1)) : 0;
                        int maxIntValue = representation == VRCExpressionParameters.Parameter.ValueRepresentation.Signed ? (1 << (bitWidth - 1)) - 1 : (1 << bitWidth) - 1;
                        ValueIntSlider.lowValue = minIntValue;
                        ValueIntSlider.highValue = maxIntValue;

                        int clampedIntValue = Mathf.Clamp(Mathf.FloorToInt(propValue.floatValue), minIntValue, maxIntValue);
                        ValueIntSlider.SetValueWithoutNotify(clampedIntValue);
                        UpdatePropValue(clampedIntValue);

                        break;
                    case VRCExpressionParameters.ValueType.Float:
                        ValueSlider.style.display = DisplayStyle.Flex;
                        ValueIntSlider.style.display = DisplayStyle.None;
                        ValueToggle.style.display = DisplayStyle.None;

                        float minFloatValue = representation == VRCExpressionParameters.Parameter.ValueRepresentation.Signed ? -1.0f : 0.0f;
                        float maxFloatValue = 1.0f;
                        ValueSlider.lowValue = minFloatValue;
                        ValueSlider.highValue = maxFloatValue;

                        float clampedFloatValue = Mathf.Clamp(propValue.floatValue, minFloatValue, maxFloatValue);
                        ValueSlider.SetValueWithoutNotify(clampedFloatValue);
                        UpdatePropValue(clampedFloatValue);

                        break;
                    default:
                    case VRCExpressionParameters.ValueType.Bool:
                        ValueSlider.style.display = DisplayStyle.None;
                        ValueIntSlider.style.display = DisplayStyle.None;
                        ValueToggle.style.display = DisplayStyle.Flex;

                        // There's an advanced community use case for false->false toggles, so ValueToggle can be false with a warning.
                        // Most users should be able to just leave Value as true.
                        UpdatePropValue(ValueToggle.value ? 1.0f : 0.0f);

                        break;
                }
            }
            else
            {
                ValueSlider.style.display = DisplayStyle.None;
                ValueIntSlider.style.display = DisplayStyle.None;
                ValueToggle.style.display = DisplayStyle.None;
                ValueValidationWarningHelpBox.style.display = DisplayStyle.None;
            }

            RefreshControlHelpBox();
        }

        private void UpdateValueSliders()
        {
            ValueSlider.SetValueWithoutNotify(propValue.floatValue);
            ValueIntSlider.SetValueWithoutNotify(Mathf.FloorToInt(propValue.floatValue));
            ValueToggle.SetValueWithoutNotify(propValue.floatValue > 0f);

            RefreshValueValidationHelpBox();
        }

        private void UpdatePropValue(float newValue)
        {
            propValue.floatValue = newValue;
            propControl.serializedObject.ApplyModifiedProperties();

            RefreshValueValidationHelpBox();
        }

        private void RefreshControlHelpBox()
        {
            bool isBooleanParameter = HasAnySelectedParameter && ControlParameterField.Parameter.valueType == VRCExpressionParameters.ValueType.Bool;
            ControlTypeHelpBox.text = string.Format(_assignedControlHelpBoxTextFormat, "(Value)", isBooleanParameter ? "false" : "zero");
        }

        private void RefreshValueValidationHelpBox()
        {
            if (HasAnySelectedParameter)
            {
                string warningText = string.Empty;
                if (propValue.floatValue == 0.0f)
                {
                    bool isBooleanParameter = ControlParameterField.Parameter.valueType == VRCExpressionParameters.ValueType.Bool;
                    warningText = isBooleanParameter ? HELPBOX_ZERO_VALUE_WARNING_BOOLEAN : HELPBOX_ZERO_VALUE_WARNING_NUMERIC;
                }
                else
                {
                    VRCExpressionParameters.Parameter.ValueRepresentation representation = ControlParameterField.Parameter.GetRepresentationByType();
                    bool signed = representation == VRCExpressionParameters.Parameter.ValueRepresentation.Signed;
                    switch (ControlParameterField.Parameter.valueType)
                    {
                        case VRCExpressionParameters.ValueType.Int:
                            byte bitWidth = ControlParameterField.Parameter.bitWidth;
                            int minIntValue = signed ? -(1 << (bitWidth - 1)) : 0;
                            int maxIntValue = signed ? (1 << (bitWidth - 1)) - 1 : (1 << bitWidth) - 1;
                            if (propValue.floatValue < minIntValue)
                            {
                                warningText = string.Format(HELPBOX_VALUE_UNDER_MINIMUM, minIntValue);
                            }
                            else if (propValue.floatValue > maxIntValue)
                            {
                                warningText = string.Format(HELPBOX_VALUE_OVER_MAXIMUM, maxIntValue);
                            }
                            break;
                        case VRCExpressionParameters.ValueType.Float:
                            float minFloatValue = signed ? -1.0f : 0.0f;
                            float maxFloatValue = 1.0f;
                            if (propValue.floatValue < minFloatValue)
                            {
                                warningText = string.Format(HELPBOX_VALUE_UNDER_MINIMUM, minFloatValue);
                            }
                            else if (propValue.floatValue > maxFloatValue)
                            {
                                warningText = string.Format(HELPBOX_VALUE_OVER_MAXIMUM, maxFloatValue);
                            }
                            break;
                    }
                }

                ValueValidationWarningHelpBox.text = warningText;
                ValueValidationWarningHelpBox.style.display = !string.IsNullOrEmpty(warningText) ? DisplayStyle.Flex : DisplayStyle.None;
            }
            else
            {
                ValueValidationWarningHelpBox.style.display = DisplayStyle.None;
            }
        }
    }
}