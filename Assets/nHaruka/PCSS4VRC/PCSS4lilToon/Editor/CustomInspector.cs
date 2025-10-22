#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace lilToon
{
    public class PCSS4lilToonInspector : lilToonInspector
    {
        // Custom properties
        MaterialProperty Softness;
        MaterialProperty _EnvLightStrength;
        MaterialProperty _CastMaskTex;
        MaterialProperty _ReceiveMaskTex;
        MaterialProperty _ShadowNormalBias;
        MaterialProperty _ShadowCasterBias;
        MaterialProperty _ShadowCasterBiasOffset;
        MaterialProperty _ShadowBiasMaskStrength;
        MaterialProperty _ShadowBiasMaskTexture;
        MaterialProperty SoftnessFalloff;
        //MaterialProperty FalloffOffset;
        MaterialProperty Blocker_Samples;
        MaterialProperty PCF_Samples;
        MaterialProperty Blocker_Rotation;
        MaterialProperty PCF_Rotation;
        MaterialProperty SoftnessRange;
        MaterialProperty Blocker_GradientBias;
        MaterialProperty PCF_GradientBias;
        MaterialProperty _ShadowDistance;
        MaterialProperty _IsOn;
        MaterialProperty _DropShadowColor;
        MaterialProperty _EnvLightLevelTexture;
        MaterialProperty _EnvLightAdjustLevel;
        MaterialProperty _ShadowcoordzOffset;
        MaterialProperty _MinusNormalOffset;
        MaterialProperty _PlusNormalOffset;
        MaterialProperty _ShadingThreshold;
        MaterialProperty _ShadingBlurRadius;
        MaterialProperty _ShadingCutOffThreshold;
        MaterialProperty _ShadingCutOffBlurRadius;
        //MaterialProperty _EnableSurfaceSmoothing;
        MaterialProperty _ShadowClamp;
        MaterialProperty _ShadowDensity;
        MaterialProperty _ReceiveMaskStrength;
        MaterialProperty _CastMaskStrength;
        MaterialProperty _IgnoreCookieTexture;
        MaterialProperty _BlendOpFA;
        MaterialProperty _ShadowColorOverrideTexture;
        MaterialProperty _ShadowColorOverrideStrength;
        MaterialProperty _InterpolationStrength;
        MaterialProperty PenumbraWithMaxSamples;
        MaterialProperty MaxDistance;
        MaterialProperty _NormalMapStrength;

        private static bool isShowCustomProperties = true;
        private const string shaderName = "PCSS4VRC/PCSS4lilToon";
        bool advSettings = false;
        bool overridable_BlendOpFA = false;

        protected override void LoadCustomProperties(MaterialProperty[] props, Material material)
        {
            isCustomShader = true;

            // If you want to change rendering modes in the editor, specify the shader here
            ReplaceToCustomShaders();
            //isShowRenderMode = !material.shader.name.Contains("Optional");

            // If not, set isShowRenderMode to false
            isShowRenderMode = true;

            //LoadCustomLanguage("");
            Softness = FindProperty("Softness", props);
            //FalloffOffset = FindProperty("FalloffOffset", props);
            _EnvLightStrength = FindProperty("_EnvLightStrength", props);
            _ShadowNormalBias = FindProperty("_ShadowNormalBias", props);
            _ShadowCasterBias = FindProperty("_ShadowCasterBias", props);
            _ShadowCasterBiasOffset = FindProperty("_ShadowCasterBiasOffset", props);
            _ShadowBiasMaskTexture = FindProperty("_ShadowBiasMaskTexture", props);
            _ShadowBiasMaskStrength = FindProperty("_ShadowBiasMaskStrength", props);
            _ReceiveMaskTex = FindProperty("_ReceiveMaskTex", props);
            _CastMaskTex = FindProperty("_CastMaskTex", props);
            SoftnessFalloff = FindProperty("SoftnessFalloff", props);
            //Blocker_Rotation = FindProperty("Blocker_Rotation", props);
            //PCF_Rotation = FindProperty("PCF_Rotation", props);
            SoftnessRange = FindProperty("SoftnessRange", props);
            MaxDistance = FindProperty("MaxDistance", props);
            Blocker_GradientBias = FindProperty("Blocker_GradientBias", props);
            PCF_GradientBias = FindProperty("PCF_GradientBias", props);
            PenumbraWithMaxSamples = FindProperty("PenumbraWithMaxSamples", props);
            _IsOn = FindProperty("_IsOn", props);
            _ShadowDistance = FindProperty("_ShadowDistance", props);
            _DropShadowColor = FindProperty("_DropShadowColor", props);
            _EnvLightAdjustLevel = FindProperty("_EnvLightAdjustLevel", props);
            _EnvLightLevelTexture = FindProperty("_EnvLightLevelTexture", props);
            _ShadowcoordzOffset = FindProperty("_ShadowcoordzOffset", props);
            _MinusNormalOffset = FindProperty("_MinusNormalOffset", props);
            _PlusNormalOffset = FindProperty("_PlusNormalOffset", props);
            _ShadingThreshold = FindProperty("_ShadingThreshold", props);
            _ShadingBlurRadius = FindProperty("_ShadingBlurRadius", props);
            _ShadingCutOffThreshold = FindProperty("_ShadingCutOffThreshold", props);
            _ShadingCutOffBlurRadius = FindProperty("_ShadingCutOffBlurRadius", props);
            //_EnableSurfaceSmoothing = FindProperty("_EnableSurfaceSmoothing", props);
            _ShadowClamp = FindProperty("_ShadowClamp", props);
            _ShadowDensity = FindProperty("_ShadowDensity", props);
            _CastMaskStrength = FindProperty("_CastMaskStrength", props);
            _ReceiveMaskStrength = FindProperty("_ReceiveMaskStrength", props);
            Blocker_Samples = FindProperty("Blocker_Samples", props);
            PCF_Samples = FindProperty("PCF_Samples", props);
            _IgnoreCookieTexture = FindProperty("_IgnoreCookieTexture", props);
            _BlendOpFA = FindProperty("_BlendOpFA", props);
            _ShadowColorOverrideTexture = FindProperty("_ShadowColorOverrideTexture", props);
            _ShadowColorOverrideStrength = FindProperty("_ShadowColorOverrideStrength", props);
            _InterpolationStrength = FindProperty("_InterpolationStrength", props);
            _NormalMapStrength = FindProperty("_NormalMapStrength", props);

            if (_EnvLightLevelTexture.textureValue == null)
            {
                var EnvLightLevelTexture = AssetDatabase.LoadAssetAtPath<CustomRenderTexture>("Assets/nHaruka/PCSS4VRC/EnvLightLevelSensor/EnvLightColor.asset");
                if (EnvLightLevelTexture != null)
                {
                    _EnvLightLevelTexture.textureValue = EnvLightLevelTexture;
                }
            }

            if (_IgnoreCookieTexture.textureValue == null)
            {
                var ignoreCookieTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/nHaruka/PCSS4VRC/Soft.png");
                if (ignoreCookieTexture != null)
                {
                    _IgnoreCookieTexture.textureValue = ignoreCookieTexture;
                }
            }
        }

        protected override void DrawCustomProperties(Material material)
        {
            // GUIStyles Name   Description
            // ---------------- ------------------------------------
            // boxOuter         outer box
            // boxInnerHalf     inner box
            // boxInner         inner box without label
            // customBox        box (similar to unity default box)
            // customToggleFont label for box

            isShowCustomProperties = Foldout("PCSS Shadow Settings", "Shadow Settings", isShowCustomProperties);
            if(isShowCustomProperties)
            {
                EditorGUILayout.BeginVertical(boxOuter);
                EditorGUILayout.BeginVertical(boxInner);

                EditorGUILayout.LabelField("※マウスオーバーすると説明が表示されます。");
                m_MaterialEditor.ShaderProperty(_IsOn, new GUIContent("On", "切るとリアル影を無効化します。"));

                EditorGUILayout.EndVertical();
                EditorGUILayout.LabelField(GetLoc("PCSS Settings"), customToggleFont);
                EditorGUILayout.BeginVertical(boxInnerHalf);

                m_MaterialEditor.ShaderProperty(Blocker_Samples, new GUIContent("Test Samplers", "影のサンプリング数です。上げるほど影のぼかし処理の精度が上がりますが、負荷も増えます。16以上は視覚上あまり違いはないです。"));
                m_MaterialEditor.ShaderProperty(PCF_Samples, new GUIContent("Filter Samplers", "シャドウフィルターのサンプリング数です。NGSS_TEST_SAMPLERSの倍にしとくといいです。こちらも32以上は視覚上あまり違いはないです。"));
                m_MaterialEditor.ShaderProperty(Softness, new GUIContent("Softness","影の柔らかさです。"));
                m_MaterialEditor.ShaderProperty(SoftnessFalloff, new GUIContent("SoftnessFalloff","影が遠くなるほどぼやけるようになります。"));
                //m_MaterialEditor.ShaderProperty(FalloffOffset, new GUIContent("FalloffOffset", "SoftnessFalloffのオフセット。至近距離でもシャープにならないときに。"));
                m_MaterialEditor.ShaderProperty(SoftnessRange, new GUIContent("SoftnessRange", "影を柔らかくする範囲。"));
                m_MaterialEditor.ShaderProperty(MaxDistance, new GUIContent("MaxDistance", "影を柔らかくする上限距離。"));
                //m_MaterialEditor.ShaderProperty(Blocker_Rotation, new GUIContent("Blocker_Rotation", "ぼかし処理を回転させるか。"));
                //m_MaterialEditor.ShaderProperty(PCF_Rotation, new GUIContent("PCF_Rotation", "ぼかし処理を回転させるか。"));
                m_MaterialEditor.ShaderProperty(Blocker_GradientBias, new GUIContent("Blocker_GradientBias", ""));
                m_MaterialEditor.ShaderProperty(PCF_GradientBias, new GUIContent("PCF_GradientBias", ""));
                m_MaterialEditor.ShaderProperty(PenumbraWithMaxSamples, new GUIContent("PenumbraWithMaxSamples", ""));

                if (Blocker_Samples.floatValue > PCF_Samples.floatValue / 2)
                {

                    Blocker_Samples.floatValue = PCF_Samples.floatValue / 2;
                }
                Blocker_Samples.floatValue = Mathf.CeilToInt(Blocker_Samples.floatValue / 4) * 4;
                PCF_Samples.floatValue = Mathf.CeilToInt(PCF_Samples.floatValue / 4) * 4;

                EditorGUILayout.EndVertical();
                EditorGUILayout.LabelField(GetLoc("General Settings"), customToggleFont);
                EditorGUILayout.BeginVertical(boxInnerHalf);

                m_MaterialEditor.ShaderProperty(_ShadowNormalBias, new GUIContent("Shadow Normal Bias", "シャドウアクネ（影が縞模様になる現象）を抑えるために影を法線方向にずらします。シャドウアクネが出たり、影の位置に違和感があれば調整してください。"));
                m_MaterialEditor.ShaderProperty(_ShadowCasterBias, new GUIContent("Shadow Caster Bias", ""));
                m_MaterialEditor.ShaderProperty(_ShadowCasterBiasOffset, new GUIContent("Shadow Caster Bias Offset", ""));
                m_MaterialEditor.ShaderProperty(_EnvLightStrength, new GUIContent("Other Light Influence", "リアル影システムの光源以外のライトの影響度です。影部分も影響を受けるので、影がOffワールドライトに照らされると影が薄くなったりします。"));
                m_MaterialEditor.ShaderProperty(_ShadowDistance, new GUIContent("Shadow Distance", "影を無効化する距離です。この距離以上離れると影が表示されなくなります。0にするとこの機能を無効化します。単位はメートル"));
                m_MaterialEditor.ShaderProperty(_ShadowClamp, new GUIContent("Shadow Clamp", "この値をしきい値に影を2値化します。0で無効。アニメ調用途想定（Shadow Clampを0.85、Shadow Softnessはどちらも0.02、Shadow Normal Biasは0に近くを推奨）"));
                m_MaterialEditor.ShaderProperty(_ShadowDensity, new GUIContent("Shadow Density", "影の濃さ"));
                m_MaterialEditor.ShaderProperty(_NormalMapStrength, new GUIContent("NormalMap Strength", "影に対するノーマルマップの影響度"));
                m_MaterialEditor.ShaderProperty(_DropShadowColor, new GUIContent("Shadow Color", "影の色を加算します。"));
                m_MaterialEditor.ShaderProperty(_EnvLightAdjustLevel, new GUIContent("Auto Light Color Adjusting Level", "自動ライトカラー調整機能の強さです。"));

                EditorGUILayout.EndVertical();
                EditorGUILayout.LabelField(GetLoc("Mask Settings"), customToggleFont);
                EditorGUILayout.BeginVertical(boxInnerHalf);

                m_MaterialEditor.ShaderProperty(_ReceiveMaskTex, new GUIContent("_ReceiveMaskTex", "ここにマスクテクスチャをいれると、影を受けなくする(常に明るくなる)ことができます。"));
                m_MaterialEditor.ShaderProperty(_ReceiveMaskStrength, new GUIContent("ReceiveMaskStrength", "ReceiveMaskの強さ調整用です。"));
                m_MaterialEditor.ShaderProperty(_CastMaskTex, new GUIContent("_CastMaskTex", "ここにマスクテクスチャをいれると、マスクした個所が他のオブジェクトに影を落とさなくなります。"));
                m_MaterialEditor.ShaderProperty(_CastMaskStrength, new GUIContent("_CastMaskStrength", "CastMaskの強さ調整用です。"));
                m_MaterialEditor.ShaderProperty(_ShadowColorOverrideTexture, new GUIContent("_ShadowColorOverrideTexture", "ここにテクスチャをいれると、テクスチャの色で影の色を上書きします。"));
                m_MaterialEditor.ShaderProperty(_ShadowColorOverrideStrength, new GUIContent("_ShadowColorOverrideStrength", "_ShadowColorOverrideTextureの強さ調整用です。"));
                m_MaterialEditor.ShaderProperty(_ShadowBiasMaskTexture, new GUIContent("_ShadowBiasMaskTexture", "ここにテクスチャをいれると、各種Bias（R:ShadowCasterBias, G:ShadowNormalBias, B:NormalOffset）の強さを濃さに応じて軽減します。"));
                m_MaterialEditor.ShaderProperty(_ShadowBiasMaskStrength, new GUIContent("_ShadowBiasMaskStrength", "_ShadowBiasMaskTextureの強さ調整用です。"));

                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.wordWrap = true;

                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(boxInnerHalf);
                advSettings = Foldout("Advanced Settings", advSettings);
                if (advSettings)
                {
                    EditorGUILayout.LabelField(GetLoc("Advanced Settings"), customToggleFont);

                    EditorGUILayout.LabelField("※影のアーティファクト軽減のための調整項目です。影の出方が大きく変わるので、もし調整する場合は慎重に行ってください。", style);
                    //m_MaterialEditor.ShaderProperty(_EnableSurfaceSmoothing, new GUIContent("Enable Surface Smoothing", "法線を利用して、影をスムージングします。アーティファクトが大幅に軽減されます。"));
                    m_MaterialEditor.ShaderProperty(_InterpolationStrength, new GUIContent("Interpolation Strength", "法線を利用して、影をスムージングします。アーティファクトが大幅に軽減されます。"));
                    m_MaterialEditor.ShaderProperty(_ShadowcoordzOffset, new GUIContent("_Shadowcoord Z Offset", "ShadowcoordのZオフセット。全体的にアーティファクトが軽減されますが、影の位置が奥にずれます。"));
                    m_MaterialEditor.ShaderProperty(_MinusNormalOffset, new GUIContent("Minus Normal Offset", "マイナス方向のノーマルオフセット。法線方向（面の向き）が_ShadingThresholdよりも光源からの角度が大きい部分の影が強くなります。"));
                    m_MaterialEditor.ShaderProperty(_PlusNormalOffset, new GUIContent("Plus Normal Offset", "プラス方向のノーマルオフセット。法線方向（面の向き）がShadingThresholdよりも光源からの角度が小さい部分の影が強くなります。"));
                    m_MaterialEditor.ShaderProperty(_ShadingThreshold, new GUIContent("Shading Threshold", "プラス方向とマイナス方向の境界のしきい値。0で光源方向と並行、0.5で直交、1で逆行です。"));
                    m_MaterialEditor.ShaderProperty(_ShadingBlurRadius, new GUIContent("Shading Blur Radius", "マイナスオフセットのブラー量。_ShadingThresholdの境界を基準に、ブラーをかけます。"));
                    m_MaterialEditor.ShaderProperty(_ShadingCutOffThreshold, new GUIContent("Shading CutOff Threshold", "プラスオフセットおよび_ShadowcoordzOffsetの境界のしきい値。このしきい値以上の部分にはプラスオフセットおよび_ShadowcoordzOffsetを適用しません。"));
                    m_MaterialEditor.ShaderProperty(_ShadingCutOffBlurRadius, new GUIContent("Shading CutOff Blur Radius", "プラス境界のブラー量。_ShadingCutOffBlurRadiusの境界を基準に、ブラーをかけます。"));

                    GUILayout.Space(6);

                    EditorGUILayout.LabelField("その他", style);
                    m_MaterialEditor.ShaderProperty(_EnvLightLevelTexture, new GUIContent("_EnvLightLevelTexture", "ワールドライトの色を取得するためのテクスチャです。特に理由がなければ弄らないでください。"));


                    overridable_BlendOpFA = GUILayout.Toggle(overridable_BlendOpFA, "ライト合成モードを上書きしない");

                    GUILayout.Space(6);

                    if (GUILayout.Button("Advanced Propertyをデフォルトに戻す"))
                    {
                       // material.SetFloat("_EnableSurfaceSmoothing", material.shader.GetPropertyDefaultFloatValue(material.shader.FindPropertyIndex("_EnableSurfaceSmoothing")));
                        material.SetFloat("_InterpolationStrength", material.shader.GetPropertyDefaultFloatValue(material.shader.FindPropertyIndex("_InterpolationStrength")));
                        material.SetFloat("_ShadowcoordzOffset", material.shader.GetPropertyDefaultFloatValue(material.shader.FindPropertyIndex("_ShadowcoordzOffset")));
                        material.SetFloat("_MinusNormalOffset", material.shader.GetPropertyDefaultFloatValue(material.shader.FindPropertyIndex("_MinusNormalOffset")));
                        material.SetFloat("_PlusNormalOffset", material.shader.GetPropertyDefaultFloatValue(material.shader.FindPropertyIndex("_PlusNormalOffset")));
                        material.SetFloat("_ShadingThreshold", material.shader.GetPropertyDefaultFloatValue(material.shader.FindPropertyIndex("_ShadingThreshold")));
                        material.SetFloat("_ShadingBlurRadius", material.shader.GetPropertyDefaultFloatValue(material.shader.FindPropertyIndex("_ShadingBlurRadius")));
                        material.SetFloat("_ShadingCutOffThreshold", material.shader.GetPropertyDefaultFloatValue(material.shader.FindPropertyIndex("_ShadingCutOffThreshold")));
                        material.SetFloat("_ShadingCutOffBlurRadius", material.shader.GetPropertyDefaultFloatValue(material.shader.FindPropertyIndex("_ShadingCutOffBlurRadius")));
                        overridable_BlendOpFA = false;
                    }
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical(boxInner);

                if (!overridable_BlendOpFA)
                {
                    _BlendOpFA.floatValue = 0;
                }

                if (GUILayout.Button("ApplyProperty All PCSS Material(without MaskTex)"))
                {
                    SetPropertyGlobal();
                }
                EditorGUILayout.LabelField("※(Unity2022限定)ロックしたプロパティは同期から除外することができます。", style);

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }
        }
        void SetPropertyGlobal()
        {
            foreach(Material mat in  Resources.FindObjectsOfTypeAll(typeof(Material)))
            {
               if(mat.shader.name.Contains("PCSS"))
                {
#if UNITY_2022_3_OR_NEWER
                    if (!mat.IsPropertyLocked("_IsOn"))
                    {
                        mat.SetFloat("_IsOn", (int)_IsOn.floatValue);
                    }
                    if (!mat.IsPropertyLocked("Softness"))
                    {
                        mat.SetFloat("Softness", Softness.floatValue);
                    }
                    //if (!mat.IsPropertyLocked("FalloffOffset"))
                    //{
                    //    mat.SetFloat("FalloffOffset", FalloffOffset.floatValue);
                    //}
                    if (!mat.IsPropertyLocked("SoftnessRange"))
                    {
                        mat.SetFloat("SoftnessRange", SoftnessRange.floatValue);
                    }
                    if (!mat.IsPropertyLocked("MaxDistance"))
                    {
                        mat.SetFloat("MaxDistance", MaxDistance.floatValue);
                    }
                    if (!mat.IsPropertyLocked("_EnvLightStrength"))
                    {
                        mat.SetFloat("_EnvLightStrength", _EnvLightStrength.floatValue);
                    }
                    if (!mat.IsPropertyLocked("_ShadowNormalBias"))
                    {
                        mat.SetFloat("_ShadowNormalBias", _ShadowNormalBias.floatValue);
                    }
                    if (!mat.IsPropertyLocked("_ShadowCasterBias"))
                    {
                        mat.SetFloat("_ShadowCasterBias", _ShadowCasterBias.floatValue);
                    }
                    if (!mat.IsPropertyLocked("_ShadowCasterBiasOffset"))
                    {
                        mat.SetFloat("_ShadowCasterBiasOffset", _ShadowCasterBiasOffset.floatValue);
                    }
                    if (!mat.IsPropertyLocked("SoftnessFalloff"))
                    {
                        mat.SetFloat("SoftnessFalloff", SoftnessFalloff.floatValue);
                    }
                    if (!mat.IsPropertyLocked("_ShadowDistance"))
                    {
                        mat.SetFloat("_ShadowDistance", _ShadowDistance.floatValue);
                    }
                    if (!mat.IsPropertyLocked("_DropShadowColor"))
                    {
                        mat.SetColor("_DropShadowColor", _DropShadowColor.colorValue);
                    }
                    if (!mat.IsPropertyLocked("_EnvLightAdjustLevel"))
                    {
                        mat.SetFloat("_EnvLightAdjustLevel", _EnvLightAdjustLevel.floatValue);
                    }     
                    if (!mat.IsPropertyLocked("_ShadowcoordzOffset"))
                    {
                        mat.SetFloat("_ShadowcoordzOffset", _ShadowcoordzOffset.floatValue);
                    }
                    if (!mat.IsPropertyLocked("_MinusNormalOffset"))
                    {
                        mat.SetFloat("_MinusNormalOffset", _MinusNormalOffset.floatValue);
                    }
                    if (!mat.IsPropertyLocked("_PlusNormalOffset"))
                    {
                        mat.SetFloat("_PlusNormalOffset", _PlusNormalOffset.floatValue);
                    }
                    if (!mat.IsPropertyLocked("_ShadingThreshold"))
                    {
                        mat.SetFloat("_ShadingThreshold", _ShadingThreshold.floatValue);
                    }
                    if (!mat.IsPropertyLocked("_ShadingBlurRadius"))
                    {
                        mat.SetFloat("_ShadingBlurRadius", _ShadingBlurRadius.floatValue);
                    }
                    if (!mat.IsPropertyLocked("_ShadingCutOffThreshold"))
                    {
                        mat.SetFloat("_ShadingCutOffThreshold", _ShadingCutOffThreshold.floatValue);
                    }
                    if (!mat.IsPropertyLocked("_ShadingCutOffBlurRadius"))
                    {
                        mat.SetFloat("_ShadingCutOffBlurRadius", _ShadingCutOffBlurRadius.floatValue);
                    }
                    //if (!mat.IsPropertyLocked("_EnableSurfaceSmoothing"))
                    //{
                    //    mat.SetFloat("_EnableSurfaceSmoothing", _EnableSurfaceSmoothing.floatValue);
                    //}
                    if (!mat.IsPropertyLocked("_ShadowClamp"))
                    {
                        mat.SetFloat("_ShadowClamp", _ShadowClamp.floatValue);
                    }
                    if (!mat.IsPropertyLocked("Blocker_Samples"))
                    {
                        mat.SetFloat("Blocker_Samples", Blocker_Samples.floatValue);
                    }
                    if (!mat.IsPropertyLocked("PCF_Samples"))
                    {
                        mat.SetFloat("PCF_Samples", PCF_Samples.floatValue);
                    }
                    if (!mat.IsPropertyLocked("_ShadowDensity"))
                    {
                        mat.SetFloat("_ShadowDensity", _ShadowDensity.floatValue);
                    }
                    //if (!mat.IsPropertyLocked("Blocker_Rotation"))
                    //{
                    //    mat.SetFloat("Blocker_Rotation", Blocker_Rotation.floatValue);
                    //}
                    //if (!mat.IsPropertyLocked("PCF_Rotation"))
                    //{
                    //    mat.SetFloat("PCF_Rotation", PCF_Rotation.floatValue);
                    //}
                    if (!mat.IsPropertyLocked("_InterpolationStrength"))
                    {
                        mat.SetFloat("_InterpolationStrength", _InterpolationStrength.floatValue);
                    }
                    if (!mat.IsPropertyLocked("PenumbraWithMaxSamples"))
                    {
                        mat.SetFloat("PenumbraWithMaxSamples", PenumbraWithMaxSamples.floatValue);
                    }
                    if (!mat.IsPropertyLocked("Blocker_GradientBias"))
                    {
                        mat.SetFloat("Blocker_GradientBias", Blocker_GradientBias.floatValue);
                    }
                    if (!mat.IsPropertyLocked("PCF_GradientBias"))
                    {
                        mat.SetFloat("PCF_GradientBias", PCF_GradientBias.floatValue);
                    }
                    if (!mat.IsPropertyLocked("_NormalMapStrength"))
                    {
                        mat.SetFloat("_NormalMapStrength", _NormalMapStrength.floatValue);
                    }

#else
                    mat.SetFloat("_IsOn", (int)_IsOn.floatValue);
                    mat.SetFloat("SimplePCSS_Softness", SimplePCSS_Softness.floatValue);
                    mat.SetFloat("_EnvLightStrength", _EnvLightStrength.floatValue);
                    mat.SetFloat("_ShadowNormalBias", _ShadowNormalBias.floatValue);
                    mat.SetFloat("SimplePCSS_SoftnessFalloff", SimplePCSS_SoftnessFalloff.floatValue);
                    mat.SetFloat("_ShadowDistance", _ShadowDistance.floatValue);
                    mat.SetColor("_DropShadowColor", _DropShadowColor.colorValue);
                    mat.SetFloat("_EnvLightAdjustLevel", _EnvLightAdjustLevel.floatValue);

                    mat.SetFloat("_ShadowcoordzOffset", _ShadowcoordzOffset.floatValue);
                    mat.SetFloat("_MinusNormalOffset", _MinusNormalOffset.floatValue);
                    mat.SetFloat("_PlusNormalOffset", _PlusNormalOffset.floatValue);
                    mat.SetFloat("_ShadingThreshold", _ShadingThreshold.floatValue);
                    mat.SetFloat("_ShadingBlurRadius", _ShadingBlurRadius.floatValue);
                    mat.SetFloat("_ShadingCutOffThreshold", _ShadingCutOffThreshold.floatValue);
                    mat.SetFloat("_ShadingCutOffBlurRadius", _ShadingCutOffBlurRadius.floatValue);
                    mat.SetFloat("_EnableSurfaceSmoothing", _EnableSurfaceSmoothing.floatValue);
                    mat.SetFloat("_ShadowClamp", _ShadowClamp.floatValue);
                    mat.SetFloat("Blocker_Samples", Blocker_Samples.floatValue);
                    mat.SetFloat("PCF_Samples", PCF_Samples.floatValue);
#endif
                }
            }
        }

        protected override void ReplaceToCustomShaders()
        {
            lts = Shader.Find(shaderName + "/lilToon");
            ltsc = Shader.Find("Hidden/" + shaderName + "/Cutout");
            ltst = Shader.Find("Hidden/" + shaderName + "/Transparent");
            ltsot = Shader.Find("Hidden/" + shaderName + "/OnePassTransparent");
            ltstt = Shader.Find("Hidden/" + shaderName + "/TwoPassTransparent");

            ltso = Shader.Find("Hidden/" + shaderName + "/OpaqueOutline");
            ltsco = Shader.Find("Hidden/" + shaderName + "/CutoutOutline");
            ltsto = Shader.Find("Hidden/" + shaderName + "/TransparentOutline");
            ltsoto = Shader.Find("Hidden/" + shaderName + "/OnePassTransparentOutline");
            ltstto = Shader.Find("Hidden/" + shaderName + "/TwoPassTransparentOutline");


            ltsoo = Shader.Find(shaderName + "/lilToon");
            ltscoo = Shader.Find(shaderName + "/lilToon");
            ltstoo = Shader.Find(shaderName + "/lilToon");

            ltstess = Shader.Find("Hidden/" + shaderName + "/Tessellation/Opaque");
            ltstessc = Shader.Find("Hidden/" + shaderName + "/Tessellation/Cutout");
            ltstesst = Shader.Find("Hidden/" + shaderName + "/Tessellation/Transparent");
            ltstessot = Shader.Find("Hidden/" + shaderName + "/Tessellation/OnePassTransparent");
            ltstesstt = Shader.Find("Hidden/" + shaderName + "/Tessellation/TwoPassTransparent");

            ltstesso = Shader.Find("Hidden/" + shaderName + "/Tessellation/OpaqueOutline");
            ltstessco = Shader.Find("Hidden/" + shaderName + "/Tessellation/CutoutOutline");
            ltstessto = Shader.Find("Hidden/" + shaderName + "/Tessellation/TransparentOutline");
            ltstessoto = Shader.Find("Hidden/" + shaderName + "/Tessellation/OnePassTransparentOutline");
            ltstesstto = Shader.Find("Hidden/" + shaderName + "/Tessellation/TwoPassTransparentOutline");

            ltsl = Shader.Find(shaderName + "/lilToonLite");
            ltslc = Shader.Find("Hidden/" + shaderName + "/Lite/Cutout");
            ltslt = Shader.Find("Hidden/" + shaderName + "/Lite/Transparent");
            ltslot = Shader.Find("Hidden/" + shaderName + "/Lite/OnePassTransparent");
            ltsltt = Shader.Find("Hidden/" + shaderName + "/Lite/TwoPassTransparent");

            ltslo = Shader.Find("Hidden/" + shaderName + "/Lite/OpaqueOutline");
            ltslco = Shader.Find("Hidden/" + shaderName + "/Lite/CutoutOutline");
            ltslto = Shader.Find("Hidden/" + shaderName + "/Lite/TransparentOutline");
            ltsloto = Shader.Find("Hidden/" + shaderName + "/Lite/OnePassTransparentOutline");
            ltsltto = Shader.Find("Hidden/" + shaderName + "/Lite/TwoPassTransparentOutline");

            //ltsref = Shader.Find(shaderName + "/lilToon");
            //ltsrefb = Shader.Find(shaderName + "/lilToon");
            //ltsfur = Shader.Find("Hidden/" + shaderName + "/Fur");
            //ltsfurc = Shader.Find("Hidden/" + shaderName + "/FurCutout");
            //ltsfurtwo = Shader.Find("Hidden/" + shaderName + "/FurTwoPass");
            //ltsfuro = Shader.Find(shaderName + "/lilToon");
            //ltsfuroc = Shader.Find(shaderName + "/lilToon");
            //ltsfurotwo = Shader.Find(shaderName + "/lilToon");
            //ltsgem = Shader.Find(shaderName + "/lilToon");
            //ltsfs = Shader.Find(shaderName + "/lilToon");

            //ltsover = Shader.Find(shaderName + "/lilToon");
            //ltsoover = Shader.Find(shaderName + "/lilToon");
            //ltslover = Shader.Find(shaderName + "/lilToon");
            //ltsloover = Shader.Find(shaderName + "/lilToon");

            ltsm = Shader.Find(shaderName + "/lilToonMulti");
            ltsmo = Shader.Find("Hidden/" + shaderName + "/MultiOutline");
            //ltsmref = Shader.Find(shaderName + "/lilToon");
            //ltsmfur = Shader.Find("Hidden/" + shaderName + "/MultiFur");
            //ltsmgem = Shader.Find(shaderName + "/lilToon");
        }

        // You can create a menu like this

        [MenuItem("Assets/PCSS4lilToon/Convert material to PCSS4lilToon", false, 1100)]
        private static void ConvertMaterialToCustomShaderMenu()
        {
            if(Selection.objects.Length == 0) return;
            PCSS4lilToonInspector inspector = new PCSS4lilToonInspector();
            for(int i = 0; i < Selection.objects.Length; i++)
            {
                if (Selection.objects[i] is Material)
                {
                    inspector.ConvertMaterialToCustomShader((Material)Selection.objects[i]);

                    if (((Material)Selection.objects[i]).shader.name.Contains("lil", System.StringComparison.OrdinalIgnoreCase) && ((Material)Selection.objects[i]).shader.name.Contains("PCSS", System.StringComparison.OrdinalIgnoreCase))
                    {

                        var EnvLightLevelTexture = AssetDatabase.LoadAssetAtPath<CustomRenderTexture>("Assets/nHaruka/PCSS4VRC/EnvLightLevelSensor/EnvLightColor.asset");
                        if (EnvLightLevelTexture != null)
                        {
                            ((Material)Selection.objects[i]).SetTexture("_EnvLightLevelTexture", EnvLightLevelTexture);
                        }

                        var ignoreCookieTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/nHaruka/PCSS4VRC/Soft.png");
                        if (ignoreCookieTexture != null)
                        {
                            ((Material)Selection.objects[i]).SetTexture("_IgnoreCookieTexture", ignoreCookieTexture);
                        }

                    ((Material)Selection.objects[i]).SetFloat("_BlendOpFA", 0);
                        if (((Material)Selection.objects[i]).shader.name.Contains("Transparent"))
                        {
                            ((Material)Selection.objects[i]).SetColor("_DropShadowColor", new Color(0.5f, 0.5f, 0.5f));
                        }
                    }
                }
            }
        }

        public void ConvertMaterialProxy(Material material)
        {
            this.ConvertMaterialToCustomShader(material);

            if (material.shader.name.Contains("lil", System.StringComparison.OrdinalIgnoreCase) && material.shader.name.Contains("PCSS", System.StringComparison.OrdinalIgnoreCase))
            {
                var EnvLightLevelTexture = AssetDatabase.LoadAssetAtPath<CustomRenderTexture>("Assets/nHaruka/PCSS4VRC/EnvLightLevelSensor/EnvLightColor.asset");
                if (EnvLightLevelTexture != null)
                {
                    material.SetTexture("_EnvLightLevelTexture", EnvLightLevelTexture);
                }

                var ignoreCookieTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/nHaruka/PCSS4VRC/Soft.png");
                if (ignoreCookieTexture != null)
                {
                    material.SetTexture("_IgnoreCookieTexture", ignoreCookieTexture);
                }

                material.SetFloat("_BlendOpFA", 0);
                if (material.shader.name.Contains("Transparent"))
                {
                    material.SetColor("_DropShadowColor", new Color(0.5f, 0.5f, 0.5f));
                }
            }
        }
    }
}
#endif