
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace VRC.SDK3.Editor
{
    public class ExpressionParameterField : VisualElement
    {
        private SerializedProperty property;
        private SerializedProperty propName;
        private SerializedProperty propType;
        private SerializedProperty propValue;
        private SerializedProperty propUseCustomLerpDuration;
        private SerializedProperty propCustomLerpDuration;
        private SerializedProperty propBitWidth;
        private SerializedProperty propFlippedRepresentation;
        private SerializedProperty propSaved;
        private SerializedProperty propSynced;

        private readonly TextField NameField;
        private readonly EnumField TypeField;
        private readonly VisualElement DefaultBoolContainer;
        private readonly Toggle DefaultBoolField;
        private readonly FloatField DefaultFloatField;
        private readonly IntegerField DefaultIntField;
        private readonly Toggle SavedField;
        private readonly Toggle SyncedField;

        private readonly VisualElement AdvancedOptionsContainer;
        private readonly DropdownField LerpDurationTypeField;
        private readonly FloatField CustomLerpDurationField;
        private readonly IntegerField BitWidthField;
        private readonly EnumField RepresentationField;
        private readonly Label InfoLabel;

        private const string LerpDurationTypeOptionOff = "Default";
        private const string LerpDurationTypeOptionOn = "Custom";

        private bool isRefreshing;

        private static readonly StringBuilder InfoBuilder = new StringBuilder();


        public ExpressionParameterField()
        {
            VisualTreeAsset uxml = Resources.Load<VisualTreeAsset>("VRCExpressionParameterField");
            uxml.CloneTree(this);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            NameField = this.Q<TextField>("NameField");
            TypeField = this.Q<EnumField>("TypeField");
            DefaultBoolContainer = this.Q<VisualElement>("DefaultBoolContainer");
            DefaultBoolField = this.Q<Toggle>("DefaultBoolField");
            DefaultFloatField = this.Q<FloatField>("DefaultFloatField");
            DefaultIntField = this.Q<IntegerField>("DefaultIntField");
            SavedField = this.Q<Toggle>("SavedField");
            SyncedField = this.Q<Toggle>("SyncedField");

            AdvancedOptionsContainer = this.Q<VisualElement>("AdvancedOptionsContainer");
            LerpDurationTypeField = this.Q<DropdownField>("DurationTypeField");
            CustomLerpDurationField = this.Q<FloatField>("DurationField");
            BitWidthField = this.Q<IntegerField>("BitWidthField");
            RepresentationField = this.Q<EnumField>("RepresentationField");
            InfoLabel = this.Q<Label>("InfoLabel");

            TypeField.RegisterValueChangedCallback(evt =>
            {
                // Check to see if the property has been disposed/deleted.
                if (evt.newValue == null) return;

                // Set default bit width and representation when type changes
                VRCExpressionParameters.ValueType newType = (VRCExpressionParameters.ValueType)evt.newValue;
                switch (newType)
                {
                    case VRCExpressionParameters.ValueType.Bool:
                        propBitWidth.intValue = 1;
                        break;
                    case VRCExpressionParameters.ValueType.Int:
                    case VRCExpressionParameters.ValueType.Float:
                        propBitWidth.intValue = 8;
                        break;
                }
                propFlippedRepresentation.boolValue = false;
                property.serializedObject.ApplyModifiedProperties();

                DisplayValueSlider();
                RefreshValues();
            });

            DefaultBoolField.RegisterValueChangedCallback(evt =>
            {
                propValue.floatValue = evt.newValue ? 1 : 0;
                property.serializedObject.ApplyModifiedProperties();
                RefreshValues();
            });
            DefaultFloatField.RegisterValueChangedCallback(evt =>
            {
                ClampDefaultFloat(evt.newValue);
                property.serializedObject.ApplyModifiedProperties();

                RefreshValues();
            });
            DefaultIntField.RegisterValueChangedCallback(evt =>
            {
                ClampDefaultInt(evt.newValue);
                property.serializedObject.ApplyModifiedProperties();

                RefreshValues();
            });
            LerpDurationTypeField.RegisterValueChangedCallback(evt =>
            {
                propUseCustomLerpDuration.boolValue = evt.newValue.Equals(LerpDurationTypeOptionOn);
                property.serializedObject.ApplyModifiedProperties();
                RefreshValues();
            });
            CustomLerpDurationField.RegisterValueChangedCallback(evt =>
            {
                propCustomLerpDuration.floatValue = Mathf.Clamp(evt.newValue, VRCExpressionParameters.Parameter.MinLerpDuration, VRCExpressionParameters.Parameter.MaxLerpDuration);
                property.serializedObject.ApplyModifiedProperties();
            });
            BitWidthField.RegisterValueChangedCallback(evt =>
            {
                propBitWidth.intValue = evt.newValue;
                property.serializedObject.ApplyModifiedProperties();
                RefreshInfoLabel();
            });
            BitWidthField.RegisterCallback<FocusOutEvent>(_ =>
            {
                propBitWidth.intValue = Mathf.Clamp(propBitWidth.intValue, VRCExpressionParameters.Parameter.MinBitWidth, VRCExpressionParameters.Parameter.MaxBitWidth);
                if ((VRCExpressionParameters.ValueType)propType.intValue == VRCExpressionParameters.ValueType.Int)
                {
                    ClampDefaultInt((int)propValue.floatValue); // Changing bit-width may restrict range for integers.
                }
                property.serializedObject.ApplyModifiedProperties();
                RefreshValues();
            });
            RepresentationField.RegisterValueChangedCallback(evt =>
            {
                if (isRefreshing) return; // Ignore cyclic callbacks. BitWidthField uses the FocusOutEvent instead.
                if (evt.newValue == null) return;

                // Convert enum selection to flippedRepresentation bool
                VRCExpressionParameters.Parameter.ValueRepresentation selectedRep = (VRCExpressionParameters.Parameter.ValueRepresentation)evt.newValue;
                VRCExpressionParameters.ValueType valueType = (VRCExpressionParameters.ValueType)propType.intValue;
                propFlippedRepresentation.boolValue = IsRepresentationFlipped(valueType, selectedRep);
                if ((VRCExpressionParameters.ValueType)propType.intValue == VRCExpressionParameters.ValueType.Int)
                {
                    ClampDefaultInt((int)propValue.floatValue);
                }
                else if ((VRCExpressionParameters.ValueType)propType.intValue == VRCExpressionParameters.ValueType.Float)
                {
                    ClampDefaultFloat(propValue.floatValue);
                }
                property.serializedObject.ApplyModifiedProperties();

                RefreshValues();
            });

            void ClampDefaultFloat(float newValue)
            {
                bool isSigned = GetDisplayRepresentation(VRCExpressionParameters.ValueType.Float, propFlippedRepresentation.boolValue) == VRCExpressionParameters.Parameter.ValueRepresentation.Signed;

                float minFloatValue = isSigned ? -1.0f : 0.0f;
                float maxFloatValue = 1.0f;
                propValue.floatValue = Mathf.Clamp(newValue, minFloatValue, maxFloatValue);
            }

            void ClampDefaultInt(int newValue)
            {
                bool isSigned = GetDisplayRepresentation(VRCExpressionParameters.ValueType.Int, propFlippedRepresentation.boolValue) == VRCExpressionParameters.Parameter.ValueRepresentation.Signed;

                int minIntValue = isSigned ? -(1 << (propBitWidth.intValue - 1)) : 0;
                int maxIntValue = isSigned ? (1 << (propBitWidth.intValue - 1)) - 1 : (1 << propBitWidth.intValue) - 1;
                propValue.floatValue = Mathf.Clamp(newValue, minIntValue, maxIntValue);
            }
        }

        private void OnAttachToPanel(AttachToPanelEvent _)
        {
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent _)
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        private void OnUndoRedoPerformed()
        {
            if (property == null || property.serializedObject == null)
                return;

            property.serializedObject.Update();
            RefreshValues();
            DisplayValueSlider();
        }

        public void BindProperty(SerializedProperty prop)
        {
            property = prop;
            
            propName = property.FindPropertyRelative(nameof(VRCExpressionParameters.Parameter.name));
            propType = property.FindPropertyRelative(nameof(VRCExpressionParameters.Parameter.valueType));
            propValue = property.FindPropertyRelative(nameof(VRCExpressionParameters.Parameter.defaultValue));
            propUseCustomLerpDuration = property.FindPropertyRelative(nameof(VRCExpressionParameters.Parameter.useCustomLerpDuration));
            propCustomLerpDuration = property.FindPropertyRelative(nameof(VRCExpressionParameters.Parameter.customLerpDuration));
            propBitWidth = property.FindPropertyRelative(nameof(VRCExpressionParameters.Parameter.bitWidth));
            propFlippedRepresentation = property.FindPropertyRelative(nameof(VRCExpressionParameters.Parameter.flippedRepresentation));
            propSaved = property.FindPropertyRelative(nameof(VRCExpressionParameters.Parameter.saved));
            propSynced = property.FindPropertyRelative(nameof(VRCExpressionParameters.Parameter.networkSynced));

            NameField.BindProperty(propName);
            TypeField.BindProperty(propType);
            SavedField.BindProperty(propSaved);
            SyncedField.BindProperty(propSynced);
            CustomLerpDurationField.BindProperty(propCustomLerpDuration);

            RefreshValues();
            DisplayValueSlider();
        }
        
        private void RefreshValues()
        {
            if (isRefreshing)
                return;
            isRefreshing = true;

            VRCExpressionParameters.ValueType valueType = (VRCExpressionParameters.ValueType)propType.intValue;
            bool isSigned = GetDisplayRepresentation(valueType, propFlippedRepresentation.boolValue) == VRCExpressionParameters.Parameter.ValueRepresentation.Signed;

            DefaultBoolField.SetValueWithoutNotify(propValue.floatValue != 0);

            float minFloatValue = isSigned ? -1.0f : 0.0f;
            float maxFloatValue = 1.0f;
            DefaultFloatField.SetValueWithoutNotify(Mathf.Clamp(propValue.floatValue, minFloatValue, maxFloatValue));

            switch (valueType)
            {
                case VRCExpressionParameters.ValueType.Bool:
                    propBitWidth.intValue = 1;
                    BitWidthField.SetEnabled(false);
                    RepresentationField.SetEnabled(false);
                    break;
                case VRCExpressionParameters.ValueType.Int:
                case VRCExpressionParameters.ValueType.Float:
                    if (propBitWidth.intValue < VRCExpressionParameters.Parameter.MinBitWidth || propBitWidth.intValue > VRCExpressionParameters.Parameter.MaxBitWidth)
                    {
                        // This covers for new zero default parameters so they default to a bit width of 8 instead of 0.
                        propBitWidth.intValue = 8;
                    }
                    BitWidthField.SetEnabled(true);
                    RepresentationField.SetEnabled(true);
                    break;
            }

            property.serializedObject.ApplyModifiedProperties();

            int minIntValue = isSigned ? -(1 << (propBitWidth.intValue - 1)) : 0;
            int maxIntValue = isSigned ? (1 << (propBitWidth.intValue - 1)) - 1 : (1 << propBitWidth.intValue) - 1;
            DefaultIntField.SetValueWithoutNotify((int)Mathf.Clamp(propValue.floatValue, minIntValue, maxIntValue));

            // Booleans never lerp.
            if (valueType == VRCExpressionParameters.ValueType.Bool)
            {
                propUseCustomLerpDuration.boolValue = false;
                LerpDurationTypeField.SetEnabled(false);
            }
            else
            {
                LerpDurationTypeField.SetEnabled(true);
            }

            LerpDurationTypeField.value = propUseCustomLerpDuration.boolValue ? LerpDurationTypeOptionOn : LerpDurationTypeOptionOff;
            CustomLerpDurationField.visible = propUseCustomLerpDuration.boolValue;

            RefreshInfoLabel();
            BitWidthField.SetValueWithoutNotify(propBitWidth.intValue);
            RepresentationField.SetValueWithoutNotify(GetDisplayRepresentation(valueType, propFlippedRepresentation.boolValue));

            isRefreshing = false;
        }

        private void RefreshInfoLabel()
        {
            InfoBuilder.Clear();

            int clampedBitWidth = Mathf.Clamp(propBitWidth.intValue, VRCExpressionParameters.Parameter.MinBitWidth, VRCExpressionParameters.Parameter.MaxBitWidth);
            VRCExpressionParameters.ValueType valueType = (VRCExpressionParameters.ValueType)propType.intValue;
            bool isSigned = GetDisplayRepresentation(valueType, propFlippedRepresentation.boolValue) == VRCExpressionParameters.Parameter.ValueRepresentation.Signed;
            switch ((VRCExpressionParameters.ValueType)propType.intValue)
            {
                case VRCExpressionParameters.ValueType.Bool:
                    InfoBuilder.Append("True or False");
                    break;
                case VRCExpressionParameters.ValueType.Float:
                    InfoBuilder.Append("Range: ");
                    float minFloatValue = isSigned ? -1.0f : 0.0f;
                    float maxFloatValue = 1.0f;
                    InfoBuilder.Append($"[{minFloatValue},{maxFloatValue}]");
                    InfoBuilder.Append("  Precision: ");
                    int numIntervals = (1 << clampedBitWidth) - 2;
                    float floatRange = maxFloatValue - minFloatValue;
                    float step = floatRange / numIntervals;
                    InfoBuilder.Append($"{step:G4}");
                    break;
                case VRCExpressionParameters.ValueType.Int:
                    InfoBuilder.Append("Range: ");
                    int minIntValue = isSigned ? -(1 << (clampedBitWidth - 1)) : 0;
                    int maxIntValue = isSigned ? (1 << (clampedBitWidth - 1)) - 1 : (1 << clampedBitWidth) - 1;
                    InfoBuilder.Append($"[{minIntValue},{maxIntValue}]");
                    break;
            }

            InfoLabel.text = InfoBuilder.ToString();
        }

        private void DisplayValueSlider()
        {
            switch ((VRCExpressionParameters.ValueType)propType.intValue)
            {
                default:
                case VRCExpressionParameters.ValueType.Bool:
                    DefaultBoolContainer.style.display = DisplayStyle.Flex;
                    DefaultFloatField.style.display = DisplayStyle.None;
                    DefaultIntField.style.display = DisplayStyle.None;
                    break;
                case VRCExpressionParameters.ValueType.Float:
                    DefaultBoolContainer.style.display = DisplayStyle.None;
                    DefaultFloatField.style.display = DisplayStyle.Flex;
                    DefaultIntField.style.display = DisplayStyle.None;
                    break;
                case VRCExpressionParameters.ValueType.Int:
                    DefaultBoolContainer.style.display = DisplayStyle.None;
                    DefaultFloatField.style.display = DisplayStyle.None;
                    DefaultIntField.style.display = DisplayStyle.Flex;
                    break;
            }
        }

        public void SetAdvancedOptionsVisible(bool vis)
        {
            AdvancedOptionsContainer.style.display = vis ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static VRCExpressionParameters.Parameter.ValueRepresentation GetDisplayRepresentation(VRCExpressionParameters.ValueType valueType, bool flipped)
        {
            // Float default is Signed, Int default is Unsigned
            // flipped=true inverts the default
            bool isSigned = valueType switch
            {
                VRCExpressionParameters.ValueType.Float => !flipped,
                VRCExpressionParameters.ValueType.Int => flipped,
                _ => false // Bool is always Unsigned
            };
            return isSigned
                ? VRCExpressionParameters.Parameter.ValueRepresentation.Signed
                : VRCExpressionParameters.Parameter.ValueRepresentation.Unsigned;
        }

        private static bool IsRepresentationFlipped(VRCExpressionParameters.ValueType valueType, VRCExpressionParameters.Parameter.ValueRepresentation representation)
        {
            // Returns true if representation differs from type's default
            bool isSigned = representation == VRCExpressionParameters.Parameter.ValueRepresentation.Signed;
            return valueType switch
            {
                VRCExpressionParameters.ValueType.Float => !isSigned, // Float default is Signed, so flipped if Unsigned
                VRCExpressionParameters.ValueType.Int => isSigned, // Int default is Unsigned, so flipped if Signed
                _ => false // Bool is always Unsigned, never flipped
            };
        }
    }
}