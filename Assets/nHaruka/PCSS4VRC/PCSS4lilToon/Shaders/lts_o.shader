Shader "Hidden/lilToonOutline"
{
    Properties
    {
        //----------------------------------------------------------------------------------------------------------------------
        // Dummy
        _DummyProperty ("", Float) = 0
        _DummyProperty ("", Float) = 0
        _DummyProperty ("", Float) = 0
        _DummyProperty ("", Float) = 0
        _DummyProperty ("", Float) = 0
        _DummyProperty ("", Float) = 0
        _DummyProperty ("", Float) = 0
        [Space(1000)]
        _DummyProperty ("", Float) = 0

        //----------------------------------------------------------------------------------------------------------------------
        // Base
             _Invisible                  ("", Int) = 0
                        _AsUnlit                    ("", Range(0, 1)) = 0
                        _Cutoff                     ("", Range(-0.001,1.001)) = 0.5
                        _SubpassCutoff              ("", Range(0,1)) = 0.5
             _FlipNormal                 ("", Int) = 0
             _ShiftBackfaceUV            ("", Int) = 0
                        _BackfaceForceShadow        ("", Range(0,1)) = 0
                _BackfaceColor              ("", Color) = (0,0,0,0)
                        _VertexLightStrength        ("", Range(0,1)) = 0
                        _LightMinLimit              ("", Range(0,1)) = 0.05
                        _LightMaxLimit              ("", Range(0,10)) = 1
                        _BeforeExposureLimit        ("", Float) = 10000
                        _MonochromeLighting         ("", Range(0,1)) = 0
                        _AlphaBoostFA               ("", Range(1,100)) = 10
                        _lilDirectionalLightStrength ("", Range(0,1)) = 1
              _LightDirectionOverride     ("", Vector) = (0.001,0.002,0.001,0)
                        _AAStrength                 ("", Range(0, 1)) = 1
             _UseDither                  ("", Int) = 0
         _DitherTex                  ("", 2D) = "white" {}
                        _DitherMaxValue             ("", Float) = 255
                        _EnvRimBorder               ("", Range(0, 3)) = 3.0
                        _EnvRimBlur                 ("", Range(0, 1)) = 0.35

        //----------------------------------------------------------------------------------------------------------------------
        // Main
         [MainColor] _Color                 ("", Color) = (1,1,1,1)
        [MainTexture]   _MainTex                    ("", 2D) = "white" {}
             _MainTex_ScrollRotate       ("", Vector) = (0,0,0,0)
               _MainTexHSVG                ("", Vector) = (0,1,1,1)
                        _MainGradationStrength      ("", Range(0, 1)) = 0
         _MainGradationTex           ("", 2D) = "white" {}
         _MainColorAdjustMask        ("", 2D) = "white" {}

        //----------------------------------------------------------------------------------------------------------------------
        // Main2nd
         _UseMain2ndTex              ("", Int) = 0
                _Color2nd                   ("", Color) = (1,1,1,1)
                        _Main2ndTex                 ("", 2D) = "white" {}
              _Main2ndTexAngle            ("", Float) = 0
             _Main2ndTex_ScrollRotate    ("", Vector) = (0,0,0,0)
               _Main2ndTex_UVMode          ("", Int) = 0
               _Main2ndTex_Cull            ("", Int) = 0
          _Main2ndTexDecalAnimation   ("", Vector) = (1,1,1,30)
           _Main2ndTexDecalSubParam    ("", Vector) = (1,1,0,1)
             _Main2ndTexIsDecal          ("", Int) = 0
             _Main2ndTexIsLeftOnly       ("", Int) = 0
             _Main2ndTexIsRightOnly      ("", Int) = 0
             _Main2ndTexShouldCopy       ("", Int) = 0
             _Main2ndTexShouldFlipMirror ("", Int) = 0
             _Main2ndTexShouldFlipCopy   ("", Int) = 0
             _Main2ndTexIsMSDF           ("", Int) = 0
         _Main2ndBlendMask           ("", 2D) = "white" {}
               _Main2ndTexBlendMode        ("", Int) = 0
               _Main2ndTexAlphaMode        ("", Int) = 0
                        _Main2ndEnableLighting      ("", Range(0, 1)) = 1
                        _Main2ndDissolveMask        ("", 2D) = "white" {}
                        _Main2ndDissolveNoiseMask   ("", 2D) = "gray" {}
             _Main2ndDissolveNoiseMask_ScrollRotate ("", Vector) = (0,0,0,0)
                        _Main2ndDissolveNoiseStrength ("", float) = 0.1
                _Main2ndDissolveColor       ("", Color) = (1,1,1,1)
           _Main2ndDissolveParams      ("", Vector) = (0,0,0.5,0.1)
          _Main2ndDissolvePos         ("", Vector) = (0,0,0,0)
               _Main2ndDistanceFade        ("", Vector) = (0.1,0.01,0,0)

        //----------------------------------------------------------------------------------------------------------------------
        // Main3rd
         _UseMain3rdTex              ("", Int) = 0
                _Color3rd                   ("", Color) = (1,1,1,1)
                        _Main3rdTex                 ("", 2D) = "white" {}
              _Main3rdTexAngle            ("", Float) = 0
             _Main3rdTex_ScrollRotate    ("", Vector) = (0,0,0,0)
               _Main3rdTex_UVMode          ("", Int) = 0
               _Main3rdTex_Cull            ("", Int) = 0
          _Main3rdTexDecalAnimation   ("", Vector) = (1,1,1,30)
           _Main3rdTexDecalSubParam    ("", Vector) = (1,1,0,1)
             _Main3rdTexIsDecal          ("", Int) = 0
             _Main3rdTexIsLeftOnly       ("", Int) = 0
             _Main3rdTexIsRightOnly      ("", Int) = 0
             _Main3rdTexShouldCopy       ("", Int) = 0
             _Main3rdTexShouldFlipMirror ("", Int) = 0
             _Main3rdTexShouldFlipCopy   ("", Int) = 0
             _Main3rdTexIsMSDF           ("", Int) = 0
         _Main3rdBlendMask           ("", 2D) = "white" {}
               _Main3rdTexBlendMode        ("", Int) = 0
               _Main3rdTexAlphaMode        ("", Int) = 0
                        _Main3rdEnableLighting      ("", Range(0, 1)) = 1
                        _Main3rdDissolveMask        ("", 2D) = "white" {}
                        _Main3rdDissolveNoiseMask   ("", 2D) = "gray" {}
             _Main3rdDissolveNoiseMask_ScrollRotate ("", Vector) = (0,0,0,0)
                        _Main3rdDissolveNoiseStrength ("", float) = 0.1
                _Main3rdDissolveColor       ("", Color) = (1,1,1,1)
           _Main3rdDissolveParams      ("", Vector) = (0,0,0.5,0.1)
          _Main3rdDissolvePos         ("", Vector) = (0,0,0,0)
               _Main3rdDistanceFade        ("", Vector) = (0.1,0.01,0,0)

        //----------------------------------------------------------------------------------------------------------------------
        // Alpha Mask
          _AlphaMaskMode              ("", Int) = 0
                        _AlphaMask                  ("", 2D) = "white" {}
                        _AlphaMaskScale             ("", Float) = 1
                        _AlphaMaskValue             ("", Float) = 0

        //----------------------------------------------------------------------------------------------------------------------
        // NormalMap
         _UseBumpMap                 ("", Int) = 0
        [Normal]        _BumpMap                    ("", 2D) = "bump" {}
                        _BumpScale                  ("", Range(-10,10)) = 1

        //----------------------------------------------------------------------------------------------------------------------
        // NormalMap 2nd
         _UseBump2ndMap              ("", Int) = 0
        [Normal]        _Bump2ndMap                 ("", 2D) = "bump" {}
               _Bump2ndMap_UVMode          ("", Int) = 0
                        _Bump2ndScale               ("", Range(-10,10)) = 1
         _Bump2ndScaleMask           ("", 2D) = "white" {}

        //----------------------------------------------------------------------------------------------------------------------
        // Anisotropy
         _UseAnisotropy              ("", Int) = 0
        [Normal]        _AnisotropyTangentMap       ("", 2D) = "bump" {}
                        _AnisotropyScale            ("", Range(-1,1)) = 1
         _AnisotropyScaleMask        ("", 2D) = "white" {}
                        _AnisotropyTangentWidth     ("", Range(0,10)) = 1
                        _AnisotropyBitangentWidth   ("", Range(0,10)) = 1
                        _AnisotropyShift            ("", Range(-10,10)) = 0
                        _AnisotropyShiftNoiseScale  ("", Range(-1,1)) = 0
                        _AnisotropySpecularStrength ("", Range(0,10)) = 1
                        _Anisotropy2ndTangentWidth  ("", Range(0,10)) = 1
                        _Anisotropy2ndBitangentWidth ("", Range(0,10)) = 1
                        _Anisotropy2ndShift         ("", Range(-10,10)) = 0
                        _Anisotropy2ndShiftNoiseScale ("", Range(-1,1)) = 0
                        _Anisotropy2ndSpecularStrength ("", Range(0,10)) = 0
                        _AnisotropyShiftNoiseMask   ("", 2D) = "white" {}
             _Anisotropy2Reflection      ("", Int) = 0
             _Anisotropy2MatCap          ("", Int) = 0
             _Anisotropy2MatCap2nd       ("", Int) = 0

        //----------------------------------------------------------------------------------------------------------------------
        // Backlight
         _UseBacklight               ("", Int) = 0
                _BacklightColor             ("", Color) = (0.85,0.8,0.7,1.0)
         _BacklightColorTex          ("", 2D) = "white" {}
                        _BacklightMainStrength      ("", Range(0, 1)) = 0
                        _BacklightNormalStrength    ("", Range(0, 1)) = 1.0
                        _BacklightBorder            ("", Range(0, 1)) = 0.35
                        _BacklightBlur              ("", Range(0, 1)) = 0.05
                        _BacklightDirectivity       ("", Float) = 5.0
                        _BacklightViewStrength      ("", Range(0, 1)) = 1
             _BacklightReceiveShadow     ("", Int) = 1
             _BacklightBackfaceMask      ("", Int) = 1

        //----------------------------------------------------------------------------------------------------------------------
        // Shadow
         _UseShadow                  ("", Int) = 0
                        _ShadowStrength             ("", Range(0, 1)) = 1
         _ShadowStrengthMask         ("", 2D) = "white" {}
                _ShadowStrengthMaskLOD      ("", Range(0, 1)) = 0
         _ShadowBorderMask           ("", 2D) = "white" {}
                _ShadowBorderMaskLOD        ("", Range(0, 1)) = 0
         _ShadowBlurMask             ("", 2D) = "white" {}
                _ShadowBlurMaskLOD          ("", Range(0, 1)) = 0
               _ShadowAOShift              ("", Vector) = (1,0,1,0)
                 _ShadowAOShift2             ("", Vector) = (1,0,1,0)
             _ShadowPostAO               ("", Int) = 0
               _ShadowColorType            ("", Int) = 0
                        _ShadowColor                ("", Color) = (0.82,0.76,0.85,1.0)
         _ShadowColorTex             ("", 2D) = "black" {}
                        _ShadowNormalStrength       ("", Range(0, 1)) = 1.0
                        _ShadowBorder               ("", Range(0, 1)) = 0.5
                        _ShadowBlur                 ("", Range(0, 1)) = 0.1
                        _ShadowReceive              ("", Range(0, 1)) = 0
                        _Shadow2ndColor             ("", Color) = (0.68,0.66,0.79,1)
         _Shadow2ndColorTex          ("", 2D) = "black" {}
                        _Shadow2ndNormalStrength    ("", Range(0, 1)) = 1.0
                        _Shadow2ndBorder            ("", Range(0, 1)) = 0.15
                        _Shadow2ndBlur              ("", Range(0, 1)) = 0.1
                        _Shadow2ndReceive           ("", Range(0, 1)) = 0
                        _Shadow3rdColor             ("", Color) = (0,0,0,0)
         _Shadow3rdColorTex          ("", 2D) = "black" {}
                        _Shadow3rdNormalStrength    ("", Range(0, 1)) = 1.0
                        _Shadow3rdBorder            ("", Range(0, 1)) = 0.25
                        _Shadow3rdBlur              ("", Range(0, 1)) = 0.1
                        _Shadow3rdReceive           ("", Range(0, 1)) = 0
                        _ShadowBorderColor          ("", Color) = (1,0.1,0,1)
                        _ShadowBorderRange          ("", Range(0, 1)) = 0.08
                        _ShadowMainStrength         ("", Range(0, 1)) = 0
                        _ShadowEnvStrength          ("", Range(0, 1)) = 0
               _ShadowMaskType             ("", Int) = 0
                        _ShadowFlatBorder           ("", Range(-2, 2)) = 1
                        _ShadowFlatBlur             ("", Range(0.001, 2)) = 1

        //----------------------------------------------------------------------------------------------------------------------
        // Rim Shade
         _UseRimShade                ("", Int) = 0
                        _RimShadeColor              ("", Color) = (0.5,0.5,0.5,1.0)
         _RimShadeMask               ("", 2D) = "white" {}
                        _RimShadeNormalStrength     ("", Range(0, 1)) = 1.0
                        _RimShadeBorder             ("", Range(0, 1)) = 0.5
                        _RimShadeBlur               ("", Range(0, 1)) = 1.0
        _RimShadeFresnelPower     ("", Range(0.01, 50)) = 1.0

        //----------------------------------------------------------------------------------------------------------------------
        // Reflection
         _UseReflection              ("", Int) = 0
        // Smoothness
                        _Smoothness                 ("", Range(0, 1)) = 1
         _SmoothnessTex              ("", 2D) = "white" {}
        // Metallic
        [Gamma]         _Metallic                   ("", Range(0, 1)) = 0
         _MetallicGlossMap           ("", 2D) = "white" {}
        // Reflectance
        [Gamma]         _Reflectance                ("", Range(0, 1)) = 0.04
        // Reflection
                        _GSAAStrength               ("", Range(0, 1)) = 0
             _ApplySpecular              ("", Int) = 1
             _ApplySpecularFA            ("", Int) = 1
             _SpecularToon               ("", Int) = 1
                        _SpecularNormalStrength     ("", Range(0, 1)) = 1.0
                        _SpecularBorder             ("", Range(0, 1)) = 0.5
                        _SpecularBlur               ("", Range(0, 1)) = 0.0
             _ApplyReflection            ("", Int) = 0
                        _ReflectionNormalStrength   ("", Range(0, 1)) = 1.0
                _ReflectionColor            ("", Color) = (1,1,1,1)
         _ReflectionColorTex         ("", 2D) = "white" {}
             _ReflectionApplyTransparency ("", Int) = 1
         _ReflectionCubeTex          ("", Cube) = "black" {}
                _ReflectionCubeColor        ("", Color) = (0,0,0,1)
             _ReflectionCubeOverride     ("", Int) = 0
                        _ReflectionCubeEnableLighting ("", Range(0, 1)) = 1
               _ReflectionBlendMode        ("", Int) = 1

        //----------------------------------------------------------------------------------------------------------------------
        // MatCap
         _UseMatCap                  ("", Int) = 0
                _MatCapColor                ("", Color) = (1,1,1,1)
                        _MatCapTex                  ("", 2D) = "white" {}
                        _MatCapMainStrength         ("", Range(0, 1)) = 0
              _MatCapBlendUV1             ("", Vector) = (0,0,0,0)
             _MatCapZRotCancel           ("", Int) = 1
             _MatCapPerspective          ("", Int) = 1
                        _MatCapVRParallaxStrength   ("", Range(0, 1)) = 1
                        _MatCapBlend                ("", Range(0, 1)) = 1
         _MatCapBlendMask            ("", 2D) = "white" {}
                        _MatCapEnableLighting       ("", Range(0, 1)) = 1
                        _MatCapShadowMask           ("", Range(0, 1)) = 0
             _MatCapBackfaceMask         ("", Int) = 0
                        _MatCapLod                  ("", Range(0, 10)) = 0
               _MatCapBlendMode            ("", Int) = 1
             _MatCapApplyTransparency    ("", Int) = 1
                        _MatCapNormalStrength       ("", Range(0, 1)) = 1.0
             _MatCapCustomNormal         ("", Int) = 0
        [Normal]        _MatCapBumpMap              ("", 2D) = "bump" {}
                        _MatCapBumpScale            ("", Range(-10,10)) = 1

        //----------------------------------------------------------------------------------------------------------------------
        // MatCap 2nd
         _UseMatCap2nd               ("", Int) = 0
                _MatCap2ndColor             ("", Color) = (1,1,1,1)
                        _MatCap2ndTex               ("", 2D) = "white" {}
                        _MatCap2ndMainStrength      ("", Range(0, 1)) = 0
              _MatCap2ndBlendUV1          ("", Vector) = (0,0,0,0)
             _MatCap2ndZRotCancel        ("", Int) = 1
             _MatCap2ndPerspective       ("", Int) = 1
                        _MatCap2ndVRParallaxStrength ("", Range(0, 1)) = 1
                        _MatCap2ndBlend             ("", Range(0, 1)) = 1
         _MatCap2ndBlendMask         ("", 2D) = "white" {}
                        _MatCap2ndEnableLighting    ("", Range(0, 1)) = 1
                        _MatCap2ndShadowMask        ("", Range(0, 1)) = 0
             _MatCap2ndBackfaceMask      ("", Int) = 0
                        _MatCap2ndLod               ("", Range(0, 10)) = 0
               _MatCap2ndBlendMode         ("", Int) = 1
             _MatCap2ndApplyTransparency ("", Int) = 1
                        _MatCap2ndNormalStrength    ("", Range(0, 1)) = 1.0
             _MatCap2ndCustomNormal      ("", Int) = 0
        [Normal]        _MatCap2ndBumpMap           ("", 2D) = "bump" {}
                        _MatCap2ndBumpScale         ("", Range(-10,10)) = 1

        //----------------------------------------------------------------------------------------------------------------------
        // Rim
         _UseRim                     ("", Int) = 0
                _RimColor                   ("", Color) = (0.66,0.5,0.48,1)
         _RimColorTex                ("", 2D) = "white" {}
                        _RimMainStrength            ("", Range(0, 1)) = 0
                        _RimNormalStrength          ("", Range(0, 1)) = 1.0
                        _RimBorder                  ("", Range(0, 1)) = 0.5
                        _RimBlur                    ("", Range(0, 1)) = 0.65
        _RimFresnelPower          ("", Range(0.01, 50)) = 3.5
                        _RimEnableLighting          ("", Range(0, 1)) = 1
                        _RimShadowMask              ("", Range(0, 1)) = 0.5
             _RimBackfaceMask            ("", Int) = 1
                        _RimVRParallaxStrength      ("", Range(0, 1)) = 1
             _RimApplyTransparency       ("", Int) = 1
                        _RimDirStrength             ("", Range(0, 1)) = 0
                        _RimDirRange                ("", Range(-1, 1)) = 0
                        _RimIndirRange              ("", Range(-1, 1)) = 0
                _RimIndirColor              ("", Color) = (1,1,1,1)
                        _RimIndirBorder             ("", Range(0, 1)) = 0.5
                        _RimIndirBlur               ("", Range(0, 1)) = 0.1
               _RimBlendMode               ("", Int) = 1

        //----------------------------------------------------------------------------------------------------------------------
        // Glitter
         _UseGlitter                 ("", Int) = 0
               _GlitterUVMode              ("", Int) = 0
                _GlitterColor               ("", Color) = (1,1,1,1)
                        _GlitterColorTex            ("", 2D) = "white" {}
               _GlitterColorTex_UVMode     ("", Int) = 0
                        _GlitterMainStrength        ("", Range(0, 1)) = 0
                        _GlitterNormalStrength      ("", Range(0, 1)) = 1.0
                        _GlitterScaleRandomize      ("", Range(0, 1)) = 0
             _GlitterApplyShape          ("", Int) = 0
                        _GlitterShapeTex            ("", 2D) = "white" {}
               _GlitterAtras               ("", Vector) = (1,1,0,0)
             _GlitterAngleRandomize      ("", Int) = 0
         _GlitterParams1             ("", Vector) = (256,256,0.16,50)
         _GlitterParams2             ("", Vector) = (0.25,0,0,0)
                        _GlitterPostContrast        ("", Float) = 1
                        _GlitterSensitivity         ("", Float) = 0.25
                        _GlitterEnableLighting      ("", Range(0, 1)) = 1
                        _GlitterShadowMask          ("", Range(0, 1)) = 0
             _GlitterBackfaceMask        ("", Int) = 0
             _GlitterApplyTransparency   ("", Int) = 1
                        _GlitterVRParallaxStrength  ("", Range(0, 1)) = 0

        //----------------------------------------------------------------------------------------------------------------------
        // Emmision
         _UseEmission                ("", Int) = 0
        [HDR]   _EmissionColor              ("", Color) = (1,1,1,1)
                        _EmissionMap                ("", 2D) = "white" {}
             _EmissionMap_ScrollRotate   ("", Vector) = (0,0,0,0)
               _EmissionMap_UVMode         ("", Int) = 0
                        _EmissionMainStrength       ("", Range(0, 1)) = 0
                        _EmissionBlend              ("", Range(0,1)) = 1
                        _EmissionBlendMask          ("", 2D) = "white" {}
             _EmissionBlendMask_ScrollRotate ("", Vector) = (0,0,0,0)
               _EmissionBlendMode          ("", Int) = 1
              _EmissionBlink              ("", Vector) = (0,0,3.141593,0)
             _EmissionUseGrad            ("", Int) = 0
         _EmissionGradTex            ("", 2D) = "white" {}
                        _EmissionGradSpeed          ("", Float) = 1
                        _EmissionParallaxDepth      ("", float) = 0
                        _EmissionFluorescence       ("", Range(0,1)) = 0
        // Gradation
         _egci ("", Int) = 2
         _egai ("", Int) = 2
         _egc0 ("", Color) = (1,1,1,0)
         _egc1 ("", Color) = (1,1,1,1)
         _egc2 ("", Color) = (1,1,1,0)
         _egc3 ("", Color) = (1,1,1,0)
         _egc4 ("", Color) = (1,1,1,0)
         _egc5 ("", Color) = (1,1,1,0)
         _egc6 ("", Color) = (1,1,1,0)
         _egc7 ("", Color) = (1,1,1,0)
         _ega0 ("", Color) = (1,0,0,0)
         _ega1 ("", Color) = (1,0,0,1)
         _ega2 ("", Color) = (1,0,0,0)
         _ega3 ("", Color) = (1,0,0,0)
         _ega4 ("", Color) = (1,0,0,0)
         _ega5 ("", Color) = (1,0,0,0)
         _ega6 ("", Color) = (1,0,0,0)
         _ega7 ("", Color) = (1,0,0,0)

        //----------------------------------------------------------------------------------------------------------------------
        // Emmision2nd
         _UseEmission2nd             ("", Int) = 0
        [HDR]   _Emission2ndColor           ("", Color) = (1,1,1,1)
                        _Emission2ndMap             ("", 2D) = "white" {}
             _Emission2ndMap_ScrollRotate ("", Vector) = (0,0,0,0)
               _Emission2ndMap_UVMode      ("", Int) = 0
                        _Emission2ndMainStrength    ("", Range(0, 1)) = 0
                        _Emission2ndBlend           ("", Range(0,1)) = 1
                        _Emission2ndBlendMask       ("", 2D) = "white" {}
             _Emission2ndBlendMask_ScrollRotate ("", Vector) = (0,0,0,0)
               _Emission2ndBlendMode       ("", Int) = 1
              _Emission2ndBlink           ("", Vector) = (0,0,3.141593,0)
             _Emission2ndUseGrad         ("", Int) = 0
         _Emission2ndGradTex         ("", 2D) = "white" {}
                        _Emission2ndGradSpeed       ("", Float) = 1
                        _Emission2ndParallaxDepth   ("", float) = 0
                        _Emission2ndFluorescence    ("", Range(0,1)) = 0
        // Gradation
         _e2gci ("", Int) = 2
         _e2gai ("", Int) = 2
         _e2gc0 ("", Color) = (1,1,1,0)
         _e2gc1 ("", Color) = (1,1,1,1)
         _e2gc2 ("", Color) = (1,1,1,0)
         _e2gc3 ("", Color) = (1,1,1,0)
         _e2gc4 ("", Color) = (1,1,1,0)
         _e2gc5 ("", Color) = (1,1,1,0)
         _e2gc6 ("", Color) = (1,1,1,0)
         _e2gc7 ("", Color) = (1,1,1,0)
         _e2ga0 ("", Color) = (1,0,0,0)
         _e2ga1 ("", Color) = (1,0,0,1)
         _e2ga2 ("", Color) = (1,0,0,0)
         _e2ga3 ("", Color) = (1,0,0,0)
         _e2ga4 ("", Color) = (1,0,0,0)
         _e2ga5 ("", Color) = (1,0,0,0)
         _e2ga6 ("", Color) = (1,0,0,0)
         _e2ga7 ("", Color) = (1,0,0,0)

        //----------------------------------------------------------------------------------------------------------------------
        // Parallax
         _UseParallax                ("", Int) = 0
             _UsePOM                     ("", Int) = 0
         _ParallaxMap                ("", 2D) = "gray" {}
                        _Parallax                   ("", float) = 0.02
                        _ParallaxOffset             ("", float) = 0.5

        //----------------------------------------------------------------------------------------------------------------------
        // Distance Fade
                _DistanceFadeColor          ("", Color) = (0,0,0,1)
               _DistanceFade               ("", Vector) = (0.1,0.01,0,0)
               _DistanceFadeMode           ("", Int) = 0
                _DistanceFadeRimColor       ("", Color) = (0,0,0,0)
        _DistanceFadeRimFresnelPower ("", Range(0.01, 50)) = 5.0

        //----------------------------------------------------------------------------------------------------------------------
        // AudioLink
         _UseAudioLink               ("", Int) = 0
               _AudioLinkDefaultValue      ("", Vector) = (0.0,0.0,2.0,0.75)
               _AudioLinkUVMode            ("", Int) = 1
         _AudioLinkUVParams          ("", Vector) = (0.25,0,0,0.125)
               _AudioLinkStart             ("", Vector) = (0.0,0.0,0.0,0.0)
                        _AudioLinkMask              ("", 2D) = "blue" {}
             _AudioLinkMask_ScrollRotate ("", Vector) = (0,0,0,0)
               _AudioLinkMask_UVMode       ("", Int) = 0
             _AudioLink2Main2nd          ("", Int) = 0
             _AudioLink2Main3rd          ("", Int) = 0
             _AudioLink2Emission         ("", Int) = 0
             _AudioLink2EmissionGrad     ("", Int) = 0
             _AudioLink2Emission2nd      ("", Int) = 0
             _AudioLink2Emission2ndGrad  ("", Int) = 0
             _AudioLink2Vertex           ("", Int) = 0
               _AudioLinkVertexUVMode      ("", Int) = 1
         _AudioLinkVertexUVParams    ("", Vector) = (0.25,0,0,0.125)
               _AudioLinkVertexStart       ("", Vector) = (0.0,0.0,0.0,0.0)
          _AudioLinkVertexStrength    ("", Vector) = (0.0,0.0,0.0,1.0)
             _AudioLinkAsLocal           ("", Int) = 0
         _AudioLinkLocalMap          ("", 2D) = "black" {}
            _AudioLinkLocalMapParams    ("", Vector) = (120,1,0,0)

        //----------------------------------------------------------------------------------------------------------------------
        // Dissolve
                        _DissolveMask               ("", 2D) = "white" {}
                        _DissolveNoiseMask          ("", 2D) = "gray" {}
             _DissolveNoiseMask_ScrollRotate ("", Vector) = (0,0,0,0)
                        _DissolveNoiseStrength      ("", float) = 0.1
                _DissolveColor              ("", Color) = (1,1,1,1)
           _DissolveParams             ("", Vector) = (0,0,0.5,0.1)
          _DissolvePos                ("", Vector) = (0,0,0,0)

        //----------------------------------------------------------------------------------------------------------------------
        // ID Mask
        // _IDMaskCompile will enable compilation of IDMask-related systems. For compatibility, setting certain
        // parameters to non-zero values will also enable the IDMask feature, but this enable switch ensures that
        // animator-controlled IDMasked meshes will be compiled correctly. Note that this _only_ controls compilation,
        // and is ignored at runtime.
        [ToggleUI]      _IDMaskCompile              ("", Int) = 0
               _IDMaskFrom                 ("", Int) = 8
        [ToggleUI]      _IDMask1                    ("", Int) = 0
        [ToggleUI]      _IDMask2                    ("", Int) = 0
        [ToggleUI]      _IDMask3                    ("", Int) = 0
        [ToggleUI]      _IDMask4                    ("", Int) = 0
        [ToggleUI]      _IDMask5                    ("", Int) = 0
        [ToggleUI]      _IDMask6                    ("", Int) = 0
        [ToggleUI]      _IDMask7                    ("", Int) = 0
        [ToggleUI]      _IDMask8                    ("", Int) = 0
        [ToggleUI]      _IDMaskIsBitmap             ("", Int) = 0
                        _IDMaskIndex1               ("", Int) = 0
                        _IDMaskIndex2               ("", Int) = 0
                        _IDMaskIndex3               ("", Int) = 0
                        _IDMaskIndex4               ("", Int) = 0
                        _IDMaskIndex5               ("", Int) = 0
                        _IDMaskIndex6               ("", Int) = 0
                        _IDMaskIndex7               ("", Int) = 0
                        _IDMaskIndex8               ("", Int) = 0

        [ToggleUI]      _IDMaskControlsDissolve     ("", Int) = 0
        [ToggleUI]      _IDMaskPrior1               ("", Int) = 0
        [ToggleUI]      _IDMaskPrior2               ("", Int) = 0
        [ToggleUI]      _IDMaskPrior3               ("", Int) = 0
        [ToggleUI]      _IDMaskPrior4               ("", Int) = 0
        [ToggleUI]      _IDMaskPrior5               ("", Int) = 0
        [ToggleUI]      _IDMaskPrior6               ("", Int) = 0
        [ToggleUI]      _IDMaskPrior7               ("", Int) = 0
        [ToggleUI]      _IDMaskPrior8               ("", Int) = 0

        //----------------------------------------------------------------------------------------------------------------------
        // UDIM Discard
         _UDIMDiscardCompile         ("", Int) = 0
               _UDIMDiscardUV              ("", Int) = 0
               _UDIMDiscardMode            ("", Int) = 0
             _UDIMDiscardRow3_3          ("", Int) = 0
             _UDIMDiscardRow3_2          ("", Int) = 0
             _UDIMDiscardRow3_1          ("", Int) = 0
             _UDIMDiscardRow3_0          ("", Int) = 0
             _UDIMDiscardRow2_3          ("", Int) = 0
             _UDIMDiscardRow2_2          ("", Int) = 0
             _UDIMDiscardRow2_1          ("", Int) = 0
             _UDIMDiscardRow2_0          ("", Int) = 0
             _UDIMDiscardRow1_3          ("", Int) = 0
             _UDIMDiscardRow1_2          ("", Int) = 0
             _UDIMDiscardRow1_1          ("", Int) = 0
             _UDIMDiscardRow1_0          ("", Int) = 0
             _UDIMDiscardRow0_3          ("", Int) = 0
             _UDIMDiscardRow0_2          ("", Int) = 0
             _UDIMDiscardRow0_1          ("", Int) = 0
             _UDIMDiscardRow0_0          ("", Int) = 0

        //----------------------------------------------------------------------------------------------------------------------
        // Outline
                _OutlineColor               ("", Color) = (0.6,0.56,0.73,1)
                        _OutlineTex                 ("", 2D) = "white" {}
             _OutlineTex_ScrollRotate    ("", Vector) = (0,0,0,0)
               _OutlineTexHSVG             ("", Vector) = (0,1,1,1)
                _OutlineLitColor            ("", Color) = (1.0,0.2,0,0)
             _OutlineLitApplyTex         ("", Int) = 0
                        _OutlineLitScale            ("", Float) = 10
                        _OutlineLitOffset           ("", Float) = -8
             _OutlineLitShadowReceive    ("", Int) = 0
            _OutlineWidth               ("", Range(0,1)) = 0.08
         _OutlineWidthMask           ("", 2D) = "white" {}
                        _OutlineFixWidth            ("", Range(0,1)) = 0.5
               _OutlineVertexR2Width       ("", Int) = 0
             _OutlineDeleteMesh          ("", Int) = 0
        [Normal] _OutlineVectorTex   ("", 2D) = "bump" {}
               _OutlineVectorUVMode        ("", Int) = 0
                        _OutlineVectorScale         ("", Range(-10,10)) = 1
                        _OutlineEnableLighting      ("", Range(0, 1)) = 1
                        _OutlineZBias               ("", Float) = 0
             _OutlineDisableInVR         ("", Int) = 0

        //----------------------------------------------------------------------------------------------------------------------
        // Tessellation
                        _TessEdge                   ("", Range(1, 100)) = 10
                        _TessStrength               ("", Range(0, 1)) = 0.5
                        _TessShrink                 ("", Range(0, 1)) = 0.0
              _TessFactorMax              ("", Range(1, 8)) = 3

        //----------------------------------------------------------------------------------------------------------------------
        // For Multi
         _UseOutline                 ("", Int) = 0
               _TransparentMode            ("", Int) = 0
             _UseClippingCanceller       ("", Int) = 0
             _AsOverlay                  ("", Int) = 0

        //----------------------------------------------------------------------------------------------------------------------
        // Save (Unused)
                                       _BaseColor          ("", Color) = (1,1,1,1)
                                       _BaseMap            ("", 2D) = "white" {}
                                       _BaseColorMap       ("", 2D) = "white" {}
                                       _lilToonVersion     ("", Int) = 0

        //----------------------------------------------------------------------------------------------------------------------
        // VRChat
        _Ramp ("", 2D) = "white" {}

        //----------------------------------------------------------------------------------------------------------------------
        // Advanced
                                               _Cull               ("", Int) = 2
                 _SrcBlend           ("", Int) = 1
                 _DstBlend           ("", Int) = 0
                 _SrcBlendAlpha      ("", Int) = 1
                 _DstBlendAlpha      ("", Int) = 10
                   _BlendOp            ("", Int) = 0
                   _BlendOpAlpha       ("", Int) = 0
                 _SrcBlendFA         ("", Int) = 1
                 _DstBlendFA         ("", Int) = 1
                 _SrcBlendAlphaFA    ("", Int) = 0
                 _DstBlendAlphaFA    ("", Int) = 1
                   _BlendOpFA          ("", Int) = 4
                   _BlendOpAlphaFA     ("", Int) = 4
                                             _ZClip              ("", Int) = 1
                                             _ZWrite             ("", Int) = 1
           _ZTest              ("", Int) = 4
                                              _StencilRef         ("", Range(0, 255)) = 0
                                              _StencilReadMask    ("", Range(0, 255)) = 255
                                              _StencilWriteMask   ("", Range(0, 255)) = 255
           _StencilComp        ("", Float) = 8
                 _StencilPass        ("", Float) = 0
                 _StencilFail        ("", Float) = 0
                 _StencilZFail       ("", Float) = 0
                                                        _OffsetFactor       ("", Float) = 0
                                                        _OffsetUnits        ("", Float) = 0
                                          _ColorMask          ("", Int) = 15
                                             _AlphaToMask        ("", Int) = 0
                                                        _lilShadowCasterBias ("", Float) = 0

        //----------------------------------------------------------------------------------------------------------------------
        // Outline Advanced
                                               _OutlineCull                ("", Int) = 1
                 _OutlineSrcBlend            ("", Int) = 1
                 _OutlineDstBlend            ("", Int) = 0
                 _OutlineSrcBlendAlpha       ("", Int) = 1
                 _OutlineDstBlendAlpha       ("", Int) = 10
                   _OutlineBlendOp             ("", Int) = 0
                   _OutlineBlendOpAlpha        ("", Int) = 0
                 _OutlineSrcBlendFA          ("", Int) = 1
                 _OutlineDstBlendFA          ("", Int) = 1
                 _OutlineSrcBlendAlphaFA     ("", Int) = 0
                 _OutlineDstBlendAlphaFA     ("", Int) = 1
                   _OutlineBlendOpFA           ("", Int) = 4
                   _OutlineBlendOpAlphaFA      ("", Int) = 4
                                             _OutlineZClip               ("", Int) = 1
                                             _OutlineZWrite              ("", Int) = 1
           _OutlineZTest               ("", Int) = 2
                                              _OutlineStencilRef          ("", Range(0, 255)) = 0
                                              _OutlineStencilReadMask     ("", Range(0, 255)) = 255
                                              _OutlineStencilWriteMask    ("", Range(0, 255)) = 255
           _OutlineStencilComp         ("", Float) = 8
                 _OutlineStencilPass         ("", Float) = 0
                 _OutlineStencilFail         ("", Float) = 0
                 _OutlineStencilZFail        ("", Float) = 0
                                                        _OutlineOffsetFactor        ("", Float) = 0
                                                        _OutlineOffsetUnits         ("", Float) = 0
                                          _OutlineColorMask           ("", Int) = 15
                                             _OutlineAlphaToMask         ("", Int) = 0
    }

    SubShader
    {
        Tags {"RenderType" = "Opaque" "Queue" = "Geometry"}
        UsePass "Hidden/ltspass_opaque/FORWARD"
        UsePass "Hidden/ltspass_opaque/FORWARD_OUTLINE"
        UsePass "Hidden/ltspass_opaque/FORWARD_ADD"
        UsePass "Hidden/ltspass_opaque/FORWARD_ADD_OUTLINE"
        UsePass "Hidden/ltspass_opaque/SHADOW_CASTER_OUTLINE"
        UsePass "Hidden/ltspass_opaque/META"
        Pass
        {
            Tags { "LightMode" = "Never" }

            HLSLPROGRAM
            // Unity strips unused UV channels from meshes; unfortunately, in 2022.3.13f1, Unity fails to detect that UV channels
            // are used when they are referenced from a pass included via `UsePass`. This fake pass is #included directly into
            // each shader to work around this; because this has an invalid lightmode set, it will never actually be executed.
            //
            // Unity bug report ID: IN-60271
            #pragma vertex vert
            #pragma fragment frag

            // For some reason, using struct appdata from lil_common_appdata doesn't work as a workaround...
            //#include "Includes/lil_pipeline_brp.hlsl"
            CBUFFER_START(UnityPerMaterial)
            #include "Packages/jp.lilxyzw.liltoon/Shader/Includes/lil_common_input_opt.hlsl"
            #if defined(LIL_CUSTOM_PROPERTIES)
                LIL_CUSTOM_PROPERTIES
            #endif
            CBUFFER_END
            //#include "Includes/lil_common.hlsl"
            //#include "Includes/lil_common_appdata.hlsl"


            struct appdata
            {
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float2 uv3 : TEXCOORD3;

                float2 uv4 : TEXCOORD4;
                float2 uv5 : TEXCOORD5;
                float2 uv6 : TEXCOORD6;
                float2 uv7 : TEXCOORD7;

                float4 color        : COLOR;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                #if !defined(SHADER_API_MOBILE) && !defined(SHADER_API_GLES)
                uint vertexID       : SV_VertexID;
                #endif

                float4 pos : POSITION;
            };

            struct v2f
            {
                float4 pos : POSITION;
                float4 col : TEXCOORD0;
            };

            struct v2f vert(struct appdata input)
            {
                struct v2f output;
                // Don't actually render to the screen, but pass UV-derived data all the way down to the fragment
                // shader so it shows up as an input in the compiled shader program.
                output.pos = float4(0,0,0,1);
                output.col = float4(input.uv, input.uv1) + float4(input.uv2, input.uv3)
                  + float4(input.uv4, input.uv5) + float4(input.uv6, input.uv7)
                  + input.color + float4(input.normalOS, 1) + input.tangentOS;

                #if !defined(SHADER_API_MOBILE) && !defined(SHADER_API_GLES)
                output.col.a += input.vertexID;
                #endif

                return output;
            }

            float4 frag(v2f i) : SV_Target
            {
                return i.col;
            }
            ENDHLSL
        }
    }
    Fallback "Unlit/Texture"

    CustomEditor "lilToon.lilToonInspector"
}

