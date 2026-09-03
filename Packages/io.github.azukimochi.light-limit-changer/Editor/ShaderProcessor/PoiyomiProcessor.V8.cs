using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace io.github.azukimochi;

internal partial class PoiyomiProcessor
{
    internal class V8 : PoiyomiProcessor
    {
        protected override int MinimumMajorVersion => 8;

        public override void ConfigureAnimation(in ConfigureAnimationContext context, AnimationClip motion)
        {
            if (context.ParameterInfo.Id is GeneralControlIDs.LightDirection)
            {
                // ライト方向は(0,0,0)にしてはいけない
                const float INITIAL_LIGHT_DIRECTION_X = 0.001f;
                const float INITIAL_LIGHT_DIRECTION_Y = 0.002f;
                const float INITIAL_LIGHT_DIRECTION_Z = 0.001f;

                float directionMode = Component.General.LightingControl.LightDirectionMode switch
                {
                    LightDirectionMode.Local => 1,
                    LightDirectionMode.World => 2,
                    _ => 0,
                };

                context.Renderers.AnimateAllFloat(motion, $"{MaterialAnimationKeyPrefix}_LightingDirectionMode",
                    AnimationUtils.LinearCurveZeroDisabled(initialValue: 0, directionMode));

                context.Renderers.AnimateAllFloat(motion, $"{MaterialAnimationKeyPrefix}{context.PropertyName}.x",
                    AnimationUtils.LinearCurveZeroDisabled(initialValue: INITIAL_LIGHT_DIRECTION_X, 0, 1000, 0, -1000, 0));

                context.Renderers.AnimateAllFloat(motion, $"{MaterialAnimationKeyPrefix}{context.PropertyName}.y",
                    AnimationUtils.LinearCurveZeroDisabled(initialValue: INITIAL_LIGHT_DIRECTION_Y, 0, 500, -500, 500, 0));

                context.Renderers.AnimateAllFloat(motion, $"{MaterialAnimationKeyPrefix}{context.PropertyName}.z",
                    AnimationUtils.LinearCurveZeroDisabled(initialValue: INITIAL_LIGHT_DIRECTION_Z, 1000, 0, -1000, 0, 1000));

                context.Renderers.AnimateAllFloat(motion, $"{MaterialAnimationKeyPrefix}{context.PropertyName}.w",
                    AnimationUtils.ConstantCurve(0));

                return;
            }

            var ctx = context;
            if (context.ParameterInfo.Id is GeneralControlIDs.ColorSaturation)
            {
                // Saturationパラメータの範囲は0～2だが、Poiyomiは-1～10なので変換する必要がある
                // 他シェーダーとの兼ね合いとかいろいろあるので-1～1の範囲にしておく
                ctx.Range -= new Vector2(1, 1);
            }

            base.ConfigureAnimation(ctx, motion);
        }

        public override void ConfigureLightDirectionControl(ReadOnlyMemory<Renderer> renderers, string propertyName, AnimationClip @default, AnimationClip x, AnimationClip y, AnimationClip z, AnimationClip w, AnimationClip other)
        {
            base.ConfigureLightDirectionControl(renderers, propertyName, @default, x, y, z, w, other);

            float directionMode = Component.General.LightingControl.LightDirectionMode switch
            {
                LightDirectionMode.Local => 1,
                LightDirectionMode.World => 2,
                _ => 0,
            };

            renderers.AnimateAllFloat(other, $"{MaterialAnimationKeyPrefix}_LightingDirectionMode", AnimationUtils.ConstantCurve(directionMode));
        }

        public override void OnMaterialCloned(IEnumerable<Material> materials)
        {
            try
            {
                Processor.SaveAssets(materials, force: true);
                ShaderOptimizer.UnlockMaterials(materials);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{LightLimitChanger.Title}] Failed to unlock poiyomi material. {ex.Message}\n\n{ex.StackTrace}");
            }
        }

        public override void NormalizeMaterial(Material material)
        {
            ConfigureAnimationFlags(material);

            if (Component.General.ColorControl.Saturation.Enable)
            {
                material.SetFloat("_MainColorAdjustToggle", 1f);
                material.EnableKeyword("COLOR_GRADING_HDR");
            }

            if (Component.Poiyomi.Backlight.Enable)
            {
                material.SetFloat("_BacklightEnabled", 1f);
                material.EnableKeyword($"POI_BACKLIGHT");
            }

            if (Component.Poiyomi.SSAO.Enable)
            {
                material.SetFloat("_SSAOEnabled", 1f);
                material.EnableKeyword("POI_SSAO");
            }

            if (NeedSkipTextureBake(material))
                return;

            var bakeMat = GetBakeMaterial();
            if (bakeMat == null) 
                return;
            bakeMat.name = $"LLC BAKE MAT {material.name} {GUID.Generate()}";
            using var disposer = AssetDisposer.Create(bakeMat);

            bool flag = false;

            var maintex = material.Get<Texture>("_MainTex", Texture2D.whiteTexture);
            flag |= NormalizeMainColor(material, bakeMat);
            flag |= NormalizeSaturation(material, bakeMat);
            flag &= maintex is Texture2D;

            if (!flag)
                return;

            bakeMat.SetTexture("_MainTex", maintex);
            var baked = Processor.TextureBaker.GetOrBake(maintex, bakeMat);
            baked.name = $"LLC BAKED {material.name}";

            if (Processor.Component.General.AllowForceOverrideSameReference)
                TextureBaker.ReplaceTextures(material, maintex, baked);
            material.SetTexture("_MainTex", baked);
        }

        private bool NormalizeMainColor(Material material, Material bakeMaterial)
        {
            var colorCtrl = Processor.Component.General.ColorControl;
            if (!colorCtrl.ColorTemperature.Enable)
                return false;

            var color = material.GetColor("_Color");
            if (color == Color.white)
                return false;
            bakeMaterial.SetColor("_Color", color);
            material.SetColor("_Color", Color.white);

            return true;
        }

        private bool NormalizeSaturation(Material material, Material bakeMaterial)
        {
            var satProp = Processor.Component.General.ColorControl.Saturation;
            if (!satProp.Enable)
                return false;

            var saturation = material.Get("_Saturation", 0f);
            if (saturation == 0)
                return false;
            var maskTex = material.Get<Texture2D>("_MainColorAdjustTexture", null);
            bakeMaterial.SetFloat("_MainColorAdjustToggle", 1f);
            bakeMaterial.EnableKeyword($"_MAINCOLORADJUSTTOGGLE_ON");
            bakeMaterial.EnableKeyword("COLOR_GRADING_HDR");
            bakeMaterial.SetTexture("_MainColorAdjustTexture", maskTex);
            bakeMaterial.SetFloat("_Saturation", saturation);

            if (maskTex != null)
                material.SetTexture("_MainColorAdjustTexture", Processor.TextureBaker.EditMaskTexture(maskTex, additive: new Vector4(0, 0, 1, 0)));
            material.SetFloat("_Saturation", 0f);

            return true;
        }

        public void ConfigureAnimationFlags(Material material)
        {
            if(Component.General.LightingControl.LightDirection.Enable && Component.General.LightingControl.LightDirection.Animation)
                material.SetOverrideTag("_LightingDirectionModeAnimated", "1");
            
            foreach(var parameterInfo in Metadata.AllParameters)
            {
                var parameter = parameterInfo.Get(Component);
                if (!parameter.IsAnimated)
                    continue;

                if (!parameterInfo.ShaderFeatures.IsEmpty && !parameterInfo.ShaderFeatures.Contains(BuiltinSupportedShaders.Poiyomi))
                    continue;

                var propertyNames = parameterInfo.GetPropertyNames(this);

                foreach (var propertyName in propertyNames)
                {
                    material.SetOverrideTag($"{propertyName}Animated", "1");
                }
            }

        }

        public override void OverrideMaterialValue(in OverrideMaterialValueContext context)
        {
            if (context.PropertyName == "_Saturation")
            {
                var value = context.ParameterInfo.Get(Component).GetValueDirect<float>();
                value -= 1;
                foreach (var mat in context.Materials)
                {
                    mat.SetFloat("_Saturation", value); 
                }
                
                return;
            }

            base.OverrideMaterialValue(context);
        }
        
        public override void ModifyExpressionMenuTree(Transform menuRoot)
        {
            if (Component.Poiyomi.Backlight.AddLightDirectionMenu)
            {
                var lightDirection = menuRoot.Find("Lighting/LightDirection");
                var backlight = menuRoot.Find("Poiyomi/Backlight");
                if (lightDirection != null && backlight != null && backlight.Find("LightDirection") == null)
                {
                    Object.Instantiate(lightDirection.gameObject, backlight).name = lightDirection.name;
                }
            }
        }

        public override int GetHashCode() => HashCode.Combine(8, base.GetHashCode());
    }
}
