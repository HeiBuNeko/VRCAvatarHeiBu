#if VRC_SDK_VRCSDK3
using System;
using System.Collections.Generic;
using System.Linq;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using VRC.SDK3A.Editor;

namespace VRC.SDK3A.Editor
{
internal static class FaceTrackingAutoFill
{
    // Combined Blendshapes
    static string[] _EyeClosed = new string[] { "EyeClosed" };
    static string[] _EyeWide = new string[] { "EyeWide" };
    static string[] _EyeSquint = new string[] { "EyeSquint" };
    static string[] _EyeDilation = new string[] { "EyeDilation" };
    static string[] _EyeConstrict = new string[] { "EyeConstrict" };
    static string[] _BrowDownRight = new string[] { "BrowDownRight", "BROW_LOWERER_R" };
    static string[] _BrowDownLeft = new string[] { "BrowDownLeft", "BROW_LOWERER_L" };
    static string[] _BrowDown = new string[] { "BrowDown" };
    static string[] _BrowUpRight = new string[] { "BrowUpRight" };
    static string[] _BrowUpLeft = new string[] { "BrowUpLeft" };
    static string[] _BrowUp = new string[] { "BrowUp" };
    static string[] _NoseSneer = new string[] { "NoseSneer" };
    static string[] _NasalDilation = new string[] { "NasalDilation" };
    static string[] _NasalConstrict = new string[] { "NasalConstrict" };
    static string[] _CheekPuff = new string[] { "CheekPuff" };
    static string[] _CheekSuck = new string[] { "CheekSuck", "Cheek_Suck" };
    static string[] _CheekSquint = new string[] { "CheekSquint" };
    static string[] _LipSuckUpper = new string[] { "LipSuckUpper", "mouthRollUpper", "Mouth_Upper_Inside" };
    static string[] _LipSuckLower = new string[] { "LipSuckLower", "mouthRollLower", "Mouth_Lower_Inside" };
    static string[] _LipSuck = new string[] { "LipSuck" };
    static string[] _LipFunnelUpper = new string[] { "LipFunnelUpper", "Mouth_Upper_Overturn" };
    static string[] _LipFunnelLower = new string[] { "LipFunnelLower", "Mouth_Lower_Overturn" };
    static string[] _LipFunnel = new string[] { "LipFunnel", "mouthFunnel" };
    static string[] _LipPuckerUpper = new string[] { "LipPuckerUpper" };
    static string[] _LipPuckerLower = new string[] { "LipPuckerLower" };
    static string[] _LipPucker = new string[] { "LipPucker", "mouthPucker", "Mouth_Pout" };
    static string[] _MouthUpperUp = new string[] { "MouthUpperUp" };
    static string[] _MouthLowerDown = new string[] { "MouthLowerDown" };
    static string[] _MouthOpen = new string[] { "MouthOpen" };
    static string[] _MouthRight = new string[] { "MouthRight" };
    static string[] _MouthLeft = new string[] { "MouthLeft" };
    static string[] _MouthSmileRight = new string[] { "MouthSmileRight", "Mouth_Smile_Right", "LIP_CORNER_PULLER_R" };
    static string[] _MouthSmileLeft = new string[] { "MouthSmileLeft", "Mouth_Smile_Left", "LIP_CORNER_PULLER_L" };
    static string[] _MouthSmile = new string[] { "MouthSmile" };
    static string[] _MouthSadRight = new string[] { "MouthSadRight", "Mouth_Sad_Right" };
    static string[] _MouthSadLeft = new string[] { "MouthSadLeft", "Mouth_Sad_Left" };
    static string[] _MouthSad = new string[] { "MouthSad" };
    static string[] _MouthStretch = new string[] { "MouthStretch" };
    static string[] _MouthDimple = new string[] { "MouthDimple" };
    static string[] _MouthTightener = new string[] { "MouthTightener" };
    static string[] _MouthPress = new string[] { "MouthPress" };

    //Base Shapes - Eye Look
    static string[] _EyeLookUpRight = new string[] { "EyeLookUpRight", "Eye_Right_Look_Up", "EYES_LOOK_UP_R" };
    static string[] _EyeLookDownRight = new string[] { "EyeLookDownRight", "Eye_Right_Look_Down", "EYES_LOOK_DOWN_R" };
    static string[] _EyeLookInRight = new string[] { "EyeLookInRight", "Eye_Right_Left", "EYES_LOOK_IN_R" };
    static string[] _EyeLookOutRight = new string[] { "EyeLookOutRight", "Eye_Right_Right", "EYES_LOOK_OUT_R" };

    static string[] _EyeLookUpLeft = new string[] { "EyeLookUpLeft", "Eye_Left_Look_Up", "EYES_LOOK_UP_L" };
    static string[] _EyeLookDownLeft = new string[] { "EyeLookDownLeft", "Eye_Left_Look_Down", "EYES_LOOK_DOWN_L" };
    static string[] _EyeLookInLeft = new string[] { "EyeLookInLeft", "Eye_Left_Right", "EYES_LOOK_IN_L" };
    static string[] _EyeLookOutLeft = new string[] { "EyeLookOutLeft", "Eye_Left_Left", "EYES_LOOK_OUT_L" };

    //Base Shapes - Eyes
    static string[] _EyeClosedRight = new string[] { "EyeClosedRight", "eyeBlinkRight", "Eye_Right_Blink", "EYES_CLOSED_R" };
    static string[] _EyeClosedLeft = new string[] { "EyeClosedLeft", "eyeBlinkLeft", "Eye_Left_Blink", "EYES_CLOSED_L" };
    static string[] _EyeWideRight = new string[] { "EyeWideRight", "Eye_Right_Wide", "EYES_WIDEN_R" };
    static string[] _EyeWideLeft = new string[] { "EyeWideLeft", "Eye_Left_Wide", "EYES_WIDEN_L" };
    static string[] _EyeSquintRight = new string[] { "EyeSquintRight", "EYES_SQUINT_R", "Eye_Right_squeeze" };
    static string[] _EyeSquintLeft = new string[] { "EyeSquintLeft", "EYES_SQUINT_L", "Eye_Left_squeeze" };
    static string[] _EyeDilationRight = new string[] { "EyeDilationRight", "Eye_Right_Dilation" };
    static string[] _EyeDilationLeft = new string[] { "EyeDilationLeft", "Eye_Left_Dilation" };
    static string[] _EyeConstrictRight = new string[] { "EyeConstrictRight", "Eye_Right_Constrict" };
    static string[] _EyeConstrictLeft = new string[] { "EyeConstrictLeft", "Eye_Left_Constrict" };

    //Base Shapes - Brows
    static string[] _BrowPinchRight = new string[] { "BrowPinchRight" };
    static string[] _BrowPinchLeft = new string[] { "BrowPinchLeft" };
    static string[] _BrowLowererRight = new string[] { "BrowLowererRight" };
    static string[] _BrowLowererLeft = new string[] { "BrowLowererLeft" };
    static string[] _BrowInnerUpRight = new string[] { "BrowInnerUpRight", "INNER_BROW_RAISER_R" };
    static string[] _BrowInnerUpLeft = new string[] { "BrowInnerUpLeft", "INNER_BROW_RAISER_L" };
    static string[] _BrowOuterUpRight = new string[] { "BrowOuterUpRight", "OUTER_BROW_RAISER_R" };
    static string[] _BrowOuterUpLeft = new string[] { "BrowOuterUpLeft", "OUTER_BROW_RAISER_L" };

    //Base Shapes - Nose
    static string[] _NoseSneerRight = new string[] { "NoseSneerRight", "NOSE_WRINKLER_R" };
    static string[] _NoseSneerLeft = new string[] { "NoseSneerLeft", "NOSE_WRINKLER_L" };
    static string[] _NasalDilationRight = new string[] { "NasalDilationRight" };
    static string[] _NasalDilationLeft = new string[] { "NasalDilationLeft" };
    static string[] _NasalConstrictRight = new string[] { "NasalConstrictRight" };
    static string[] _NasalConstrictLeft = new string[] { "NasalConstrictLeft" };

    //Base Shapes - Cheek
    static string[] _CheekSquintRight = new string[] { "CheekSquintRight", "CHEEK_RAISER_R" };
    static string[] _CheekSquintLeft = new string[] { "CheekSquintLeft", "CHEEK_RAISER_L" };
    static string[] _CheekPuffRight = new string[] { "CheekPuffRight", "Cheek_Puff_Right", "CHEEK_PUFF_R" };
    static string[] _CheekPuffLeft = new string[] { "CheekPuffLeft", "Cheek_Puff_Left", "CHEEK_PUFF_L" };
    static string[] _CheekSuckRight = new string[] { "CheekSuckRight", "CHEEK_SUCK_R" };
    static string[] _CheekSuckLeft = new string[] { "CheekSuckLeft", "CHEEK_SUCK_L" };

    //Base Shapes - Jaw
    static string[] _JawOpen = new string[] { "JawOpen", "Jaw_Open", "JAW_DROP" };
    static string[] _JawRight = new string[] { "JawRight", "Jaw_Right", "JAW_SIDEWAYS_RIGHT" };
    static string[] _JawLeft = new string[] { "JawLeft", "Jaw_Left", "JAW_SIDEWAYS_LEFT" };
    static string[] _JawForward = new string[] { "JawForward", "Jaw_Forward", "JAW_THRUST" };
    static string[] _JawBackward = new string[] { "JawBackward" };
    static string[] _JawClench = new string[] { "JawClench" };
    static string[] _JawMandibleRaise = new string[] { "JawMandibleRaise" };
    static string[] _MouthClosed = new string[] { "MouthClosed", "mouthClose", "LIPS_TOWARD" };

    //Base Shapes - Lip
    static string[] _LipSuckUpperRight = new string[] { "LipSuckUpperRight", "LIP_SUCK_RT" };
    static string[] _LipSuckUpperLeft = new string[] { "LipSuckUpperLeft", "LIP_SUCK_LT" };
    static string[] _LipSuckLowerRight = new string[] { "LipSuckLowerRight", "LIP_SUCK_RB" };
    static string[] _LipSuckLowerLeft = new string[] { "LipSuckLowerLeft", "LIP_SUCK_LB" };
    static string[] _LipSuckCornerRight = new string[] { "LipSuckCornerRight" };
    static string[] _LipSuckCornerLeft = new string[] { "LipSuckCornerLeft" };
    static string[] _LipFunnelUpperRight = new string[] { "LipFunnelUpperRight", "LIP_FUNNELER_RT" };
    static string[] _LipFunnelUpperLeft = new string[] { "LipFunnelUpperLeft", "LIP_FUNNELER_LT" };
    static string[] _LipFunnelLowerRight = new string[] { "LipFunnelLowerRight", "LIP_SUCK_RB" };
    static string[] _LipFunnelLowerLeft = new string[] { "LipFunnelLowerLeft", "LIP_SUCK_LB" };
    static string[] _LipPuckerUpperRight = new string[] { "LipPuckerUpperRight", "LIP_PUCKER_R" };
    static string[] _LipPuckerUpperLeft = new string[] { "LipPuckerUpperLeft", "LIP_PUCKER_L" };
    static string[] _LipPuckerLowerRight = new string[] { "LipPuckerLowerRight", "LIP_PUCKER_R" };
    static string[] _LipPuckerLowerLeft = new string[] { "LipPuckerLowerLeft", "LIP_PUCKER_L" };

    //Base Shapes - Mouth
    static string[] _MouthUpperUpRight = new string[] { "MouthUpperUpRight", "Mouth_Upper_Up_Right", "UPPER_LIP_RAISER_R" };
    static string[] _MouthUpperUpLeft = new string[] { "MouthUpperUpLeft", "Mouth_Upper_Up_Left", "UPPER_LIP_RAISER_L" };
    static string[] _MouthUpperDeepenRight = new string[] { "MouthUpperDeepenRight" };
    static string[] _MouthUpperDeepenLeft = new string[] { "MouthUpperDeepenLeft" };
    static string[] _MouthLowerDownRight = new string[] { "MouthLowerDownRight", "Mouth_Lower_Down_Right", "LOWER_LIP_DEPRESSOR_R" };
    static string[] _MouthLowerDownLeft = new string[] { "MouthLowerDownLeft", "Mouth_Lower_Down_Left", "LOWER_LIP_DEPRESSOR_L" };
    static string[] _MouthUpperRight = new string[] { "MouthUpperRight" };
    static string[] _MouthUpperLeft = new string[] { "MouthUpperLeft" };
    static string[] _MouthLowerRight = new string[] { "MouthLowerRight" };
    static string[] _MouthLowerLeft = new string[] { "MouthLowerLeft" };
    static string[] _MouthCornerPullRight = new string[] { "MouthCornerPullRight" };
    static string[] _MouthCornerPullLeft = new string[] { "MouthCornerPullLeft" };
    static string[] _MouthCornerSlantRight = new string[] { "MouthCornerSlantRight" };
    static string[] _MouthCornerSlantLeft = new string[] { "MouthCornerSlantLeft" };
    static string[] _MouthFrownRight = new string[] { "MouthFrownRight", "LIP_CORNER_DEPRESSOR_R" };
    static string[] _MouthFrownLeft = new string[] { "MouthFrownLeft", "LIP_CORNER_DEPRESSOR_L" };
    static string[] _MouthStretchRight = new string[] { "MouthStretchRight", "LIP_STRETCHER_R" };
    static string[] _MouthStretchLeft = new string[] { "MouthStretchLeft", "LIP_STRETCHER_L" };
    static string[] _MouthDimpleRight = new string[] { "MouthDimpleRight", "DIMPLER_R" };
    static string[] _MouthDimpleLeft = new string[] { "MouthDimpleLeft", "DIMPLER_L" };
    static string[] _MouthRaiserUpper = new string[] { "MouthRaiserUpper", "mouthShrugUpper", "CHIN_RAISER_T" };
    static string[] _MouthRaiserLower = new string[] { "MouthRaiserLower", "mouthShrugLower", "Mouth_Lower_Overlay", "CHIN_RAISER_B" };
    static string[] _MouthPressRight = new string[] { "MouthPressRight", "LIP_PRESSOR_R" };
    static string[] _MouthPressLeft = new string[] { "MouthPressLeft", "LIP_PRESSOR_L" };
    static string[] _MouthTightenerRight = new string[] { "MouthTightenerRight", "LIP_TIGHTENER_R" };
    static string[] _MouthTightenerLeft = new string[] { "MouthTightenerLeft", "LIP_TIGHTENER_L" };

    //Base Shapes - Tongue
    static string[] _TongueOut = new string[] { "TongueOut", "Tongue_LongStep1", "Tongue_LongStep2", "TONGUE_OUT" };
    static string[] _TongueUp = new string[] { "TongueUp", "Tongue_Up" };
    static string[] _TongueDown = new string[] { "TongueDown", "Tongue_Down" };
    static string[] _TongueRight = new string[] { "TongueRight", "Tongue_Right" };
    static string[] _TongueLeft = new string[] { "TongueLeft", "Tongue_Left" };
    static string[] _TongueRoll = new string[] { "TongueRoll", "Tongue_Roll" };
    static string[] _TongueBendDown = new string[] { "TongueBendDown" };
    static string[] _TongueCurlUp = new string[] { "TongueCurlUp" };
    static string[] _TongueSquish = new string[] { "TongueSquish" };
    static string[] _TongueFlat = new string[] { "TongueFlat" };
    static string[] _TongueTwistRight = new string[] { "TongueTwistRight" };
    static string[] _TongueTwistLeft = new string[] { "TongueTwistLeft" };

    //Base Shapes - Throat
    static string[] _SoftPalateClose = new string[] { "SoftPalateClose" };
    static string[] _ThroatSwallow = new string[] { "ThroatSwallow" };
    static string[] _NeckFlexRight = new string[] { "NeckFlexRight" };
    static string[] _NeckFlexLeft = new string[] { "NeckFlexLeft" };

    /// <summary>
    /// Returns a list of standardized blendshape names that correspond to the given FaceTrackingKey
    /// </summary>
    internal static IEnumerable<string> GetNamesForFaceTrackingKey(VRCAvatarDescriptor.FaceTrackingKey value)
    {
        switch (value)
        {
            //Eye Look
            case VRCAvatarDescriptor.FaceTrackingKey.EyeLookUpRight: return _EyeLookUpRight;
            case VRCAvatarDescriptor.FaceTrackingKey.EyeLookDownRight: return _EyeLookDownRight;
            case VRCAvatarDescriptor.FaceTrackingKey.EyeLookInRight: return _EyeLookInRight;
            case VRCAvatarDescriptor.FaceTrackingKey.EyeLookOutRight: return _EyeLookOutRight;
            case VRCAvatarDescriptor.FaceTrackingKey.EyeLookUpLeft: return _EyeLookUpLeft;
            case VRCAvatarDescriptor.FaceTrackingKey.EyeLookDownLeft: return _EyeLookDownLeft;
            case VRCAvatarDescriptor.FaceTrackingKey.EyeLookInLeft: return _EyeLookInLeft;
            case VRCAvatarDescriptor.FaceTrackingKey.EyeLookOutLeft: return _EyeLookOutLeft;
            //Eyes
            case VRCAvatarDescriptor.FaceTrackingKey.EyeClosedRight: return _EyeClosedRight.Concat(_EyeClosed);
            case VRCAvatarDescriptor.FaceTrackingKey.EyeClosedLeft: return _EyeClosedLeft.Concat(_EyeClosed);
            case VRCAvatarDescriptor.FaceTrackingKey.EyeDilationRight: return _EyeDilationRight.Concat(_EyeDilation);
            case VRCAvatarDescriptor.FaceTrackingKey.EyeDilationLeft: return _EyeDilationLeft.Concat(_EyeDilation);
            case VRCAvatarDescriptor.FaceTrackingKey.EyeConstrictRight: return _EyeConstrictRight.Concat(_EyeConstrict);
            case VRCAvatarDescriptor.FaceTrackingKey.EyeConstrictLeft: return _EyeConstrictLeft.Concat(_EyeConstrict);
            case VRCAvatarDescriptor.FaceTrackingKey.EyeSquintRight: return _EyeSquintRight.Concat(_EyeSquint);
            case VRCAvatarDescriptor.FaceTrackingKey.EyeSquintLeft: return _EyeSquintLeft.Concat(_EyeSquint);
            case VRCAvatarDescriptor.FaceTrackingKey.EyeWideRight: return _EyeWideRight.Concat(_EyeWide);
            case VRCAvatarDescriptor.FaceTrackingKey.EyeWideLeft: return _EyeWideLeft.Concat(_EyeWide);
            //Brows
            case VRCAvatarDescriptor.FaceTrackingKey.BrowPinchRight: return _BrowPinchRight.Concat(_BrowDownRight).Concat(_BrowDown);
            case VRCAvatarDescriptor.FaceTrackingKey.BrowPinchLeft: return _BrowPinchLeft.Concat(_BrowDownLeft).Concat(_BrowDown);
            case VRCAvatarDescriptor.FaceTrackingKey.BrowLowererRight: return _BrowLowererRight.Concat(_BrowDownRight).Concat(_BrowDown);
            case VRCAvatarDescriptor.FaceTrackingKey.BrowLowererLeft: return _BrowLowererLeft.Concat(_BrowDownLeft).Concat(_BrowDown);
            case VRCAvatarDescriptor.FaceTrackingKey.BrowInnerUpRight: return _BrowInnerUpRight.Concat(_BrowUpRight).Concat(_BrowUp);
            case VRCAvatarDescriptor.FaceTrackingKey.BrowInnerUpLeft: return _BrowInnerUpLeft.Concat(_BrowUpLeft).Concat(_BrowUp);
            case VRCAvatarDescriptor.FaceTrackingKey.BrowOuterUpRight: return _BrowOuterUpRight.Concat(_BrowUpRight).Concat(_BrowUp);
            case VRCAvatarDescriptor.FaceTrackingKey.BrowOuterUpLeft: return _BrowOuterUpLeft.Concat(_BrowUpLeft).Concat(_BrowUp);
            //Nose
            case VRCAvatarDescriptor.FaceTrackingKey.NoseSneerRight: return _NoseSneerRight.Concat(_NoseSneer);
            case VRCAvatarDescriptor.FaceTrackingKey.NoseSneerLeft: return _NoseSneerLeft.Concat(_NoseSneer);
            case VRCAvatarDescriptor.FaceTrackingKey.NasalDilationRight: return _NasalDilationRight.Concat(_NasalDilation);
            case VRCAvatarDescriptor.FaceTrackingKey.NasalDilationLeft: return _NasalDilationLeft.Concat(_NasalDilation);
            case VRCAvatarDescriptor.FaceTrackingKey.NasalConstrictRight: return _NasalConstrictRight.Concat(_NasalConstrict);
            case VRCAvatarDescriptor.FaceTrackingKey.NasalConstrictLeft: return _NasalConstrictLeft.Concat(_NasalConstrict);
            //Cheek
            case VRCAvatarDescriptor.FaceTrackingKey.CheekSquintRight: return _CheekSquintRight.Concat(_CheekSquint);
            case VRCAvatarDescriptor.FaceTrackingKey.CheekSquintLeft: return _CheekSquintLeft.Concat(_CheekSquint);
            case VRCAvatarDescriptor.FaceTrackingKey.CheekPuffRight: return _CheekPuffRight.Concat(_CheekPuff);
            case VRCAvatarDescriptor.FaceTrackingKey.CheekPuffLeft: return _CheekPuffLeft.Concat(_CheekPuff);
            case VRCAvatarDescriptor.FaceTrackingKey.CheekSuckRight: return _CheekSuckRight.Concat(_CheekSuck);
            case VRCAvatarDescriptor.FaceTrackingKey.CheekSuckLeft: return _CheekSuckLeft.Concat(_CheekSuck);
            //Jaw
            case VRCAvatarDescriptor.FaceTrackingKey.JawOpen: return _JawOpen;
            case VRCAvatarDescriptor.FaceTrackingKey.JawRight: return _JawRight;
            case VRCAvatarDescriptor.FaceTrackingKey.JawLeft: return _JawLeft;
            case VRCAvatarDescriptor.FaceTrackingKey.JawForward: return _JawForward;
            case VRCAvatarDescriptor.FaceTrackingKey.JawBackward: return _JawBackward;
            case VRCAvatarDescriptor.FaceTrackingKey.JawClench: return _JawClench;
            case VRCAvatarDescriptor.FaceTrackingKey.JawMandibleRaise: return _JawMandibleRaise;
            case VRCAvatarDescriptor.FaceTrackingKey.MouthClosed: return _MouthClosed;
            //Lip
            case VRCAvatarDescriptor.FaceTrackingKey.LipSuckUpperRight: return _LipSuckUpperRight.Concat(_LipSuckUpper).Concat(_LipSuck);
            case VRCAvatarDescriptor.FaceTrackingKey.LipSuckUpperLeft: return _LipSuckUpperLeft.Concat(_LipSuckUpper).Concat(_LipSuck);
            case VRCAvatarDescriptor.FaceTrackingKey.LipSuckLowerRight: return _LipSuckLowerRight.Concat(_LipSuckLower).Concat(_LipSuck);
            case VRCAvatarDescriptor.FaceTrackingKey.LipSuckLowerLeft: return _LipSuckLowerLeft.Concat(_LipSuckLower).Concat(_LipSuck);
            case VRCAvatarDescriptor.FaceTrackingKey.LipSuckCornerRight: return _LipSuckCornerRight;
            case VRCAvatarDescriptor.FaceTrackingKey.LipSuckCornerLeft: return _LipSuckCornerLeft;
            case VRCAvatarDescriptor.FaceTrackingKey.LipFunnelUpperRight: return _LipFunnelUpperRight.Concat(_LipFunnelUpper).Concat(_LipFunnel);
            case VRCAvatarDescriptor.FaceTrackingKey.LipFunnelUpperLeft: return _LipFunnelUpperLeft.Concat(_LipFunnelUpper).Concat(_LipFunnel);
            case VRCAvatarDescriptor.FaceTrackingKey.LipFunnelLowerRight: return _LipFunnelLowerRight.Concat(_LipFunnelLower).Concat(_LipFunnel);
            case VRCAvatarDescriptor.FaceTrackingKey.LipFunnelLowerLeft: return _LipFunnelLowerLeft.Concat(_LipFunnelLower).Concat(_LipFunnel);
            case VRCAvatarDescriptor.FaceTrackingKey.LipPuckerUpperRight: return _LipPuckerUpperRight.Concat(_LipPuckerUpper).Concat(_LipPucker);
            case VRCAvatarDescriptor.FaceTrackingKey.LipPuckerUpperLeft: return _LipPuckerUpperLeft.Concat(_LipPuckerUpper).Concat(_LipPucker);
            case VRCAvatarDescriptor.FaceTrackingKey.LipPuckerLowerRight: return _LipPuckerLowerRight.Concat(_LipPuckerLower).Concat(_LipPucker);
            case VRCAvatarDescriptor.FaceTrackingKey.LipPuckerLowerLeft: return _LipPuckerLowerLeft.Concat(_LipPuckerLower).Concat(_LipPucker);
            //Mouth
            case VRCAvatarDescriptor.FaceTrackingKey.MouthUpperUpRight: return _MouthUpperUpRight.Concat(_MouthUpperUp);
            case VRCAvatarDescriptor.FaceTrackingKey.MouthUpperUpLeft: return _MouthUpperUpLeft.Concat(_MouthUpperUp);
            case VRCAvatarDescriptor.FaceTrackingKey.MouthUpperDeepenRight: return _MouthUpperDeepenRight;
            case VRCAvatarDescriptor.FaceTrackingKey.MouthUpperDeepenLeft: return _MouthUpperDeepenLeft;
            case VRCAvatarDescriptor.FaceTrackingKey.MouthLowerDownRight: return _MouthLowerDownRight.Concat(_MouthLowerDown);
            case VRCAvatarDescriptor.FaceTrackingKey.MouthLowerDownLeft: return _MouthLowerDownLeft.Concat(_MouthLowerDown);
            case VRCAvatarDescriptor.FaceTrackingKey.MouthUpperRight: return _MouthUpperRight.Concat(_MouthRight);
            case VRCAvatarDescriptor.FaceTrackingKey.MouthUpperLeft: return _MouthUpperLeft.Concat(_MouthLeft);
            case VRCAvatarDescriptor.FaceTrackingKey.MouthLowerRight: return _MouthLowerRight.Concat(_MouthRight);
            case VRCAvatarDescriptor.FaceTrackingKey.MouthLowerLeft: return _MouthLowerLeft.Concat(_MouthLeft);
            case VRCAvatarDescriptor.FaceTrackingKey.MouthCornerPullRight: return _MouthCornerPullRight.Concat(_MouthSmileRight).Concat(_MouthSmile);
            case VRCAvatarDescriptor.FaceTrackingKey.MouthCornerPullLeft: return _MouthCornerPullLeft.Concat(_MouthSmileLeft).Concat(_MouthSmile);
            case VRCAvatarDescriptor.FaceTrackingKey.MouthCornerSlantRight: return _MouthCornerSlantRight.Concat(_MouthSmileRight).Concat(_MouthSmile);
            case VRCAvatarDescriptor.FaceTrackingKey.MouthCornerSlantLeft: return _MouthCornerSlantLeft.Concat(_MouthSmileLeft).Concat(_MouthSmile);
            case VRCAvatarDescriptor.FaceTrackingKey.MouthFrownRight: return _MouthFrownRight.Concat(_MouthSadRight).Concat(_MouthSad);
            case VRCAvatarDescriptor.FaceTrackingKey.MouthFrownLeft: return _MouthFrownLeft.Concat(_MouthSadLeft).Concat(_MouthSad);
            case VRCAvatarDescriptor.FaceTrackingKey.MouthStretchRight: return _MouthStretchRight.Concat(_MouthSadRight).Concat(_MouthSad).Concat(_MouthStretch);
            case VRCAvatarDescriptor.FaceTrackingKey.MouthStretchLeft: return _MouthStretchLeft.Concat(_MouthSadLeft).Concat(_MouthSad).Concat(_MouthStretch);
            case VRCAvatarDescriptor.FaceTrackingKey.MouthDimpleRight: return _MouthDimpleRight.Concat(_MouthDimple);
            case VRCAvatarDescriptor.FaceTrackingKey.MouthDimpleLeft: return _MouthDimpleLeft.Concat(_MouthDimple);
            case VRCAvatarDescriptor.FaceTrackingKey.MouthRaiserUpper: return _MouthRaiserUpper;
            case VRCAvatarDescriptor.FaceTrackingKey.MouthRaiserLower: return _MouthRaiserLower;
            case VRCAvatarDescriptor.FaceTrackingKey.MouthPressRight: return _MouthPressRight;
            case VRCAvatarDescriptor.FaceTrackingKey.MouthPressLeft: return _MouthPressLeft;
            case VRCAvatarDescriptor.FaceTrackingKey.MouthTightenerRight: return _MouthTightenerRight;
            case VRCAvatarDescriptor.FaceTrackingKey.MouthTightenerLeft: return _MouthTightenerLeft;
            //Tongue
            case VRCAvatarDescriptor.FaceTrackingKey.TongueOut: return _TongueOut;
            case VRCAvatarDescriptor.FaceTrackingKey.TongueUp: return _TongueUp;
            case VRCAvatarDescriptor.FaceTrackingKey.TongueDown: return _TongueDown;
            case VRCAvatarDescriptor.FaceTrackingKey.TongueRight: return _TongueRight;
            case VRCAvatarDescriptor.FaceTrackingKey.TongueLeft: return _TongueLeft;
            case VRCAvatarDescriptor.FaceTrackingKey.TongueRoll: return _TongueRoll;
            case VRCAvatarDescriptor.FaceTrackingKey.TongueBendDown: return _TongueBendDown;
            case VRCAvatarDescriptor.FaceTrackingKey.TongueCurlUp: return _TongueCurlUp;
            case VRCAvatarDescriptor.FaceTrackingKey.TongueSquish: return _TongueSquish;
            case VRCAvatarDescriptor.FaceTrackingKey.TongueFlat: return _TongueFlat;
            case VRCAvatarDescriptor.FaceTrackingKey.TongueTwistRight: return _TongueTwistRight;
            case VRCAvatarDescriptor.FaceTrackingKey.TongueTwistLeft: return _TongueTwistLeft;
            //Throat
            case VRCAvatarDescriptor.FaceTrackingKey.SoftPalateClose: return _SoftPalateClose;
            case VRCAvatarDescriptor.FaceTrackingKey.ThroatSwallow: return _ThroatSwallow;
            case VRCAvatarDescriptor.FaceTrackingKey.NeckFlexRight: return _NeckFlexRight;
            case VRCAvatarDescriptor.FaceTrackingKey.NeckFlexLeft: return _NeckFlexLeft;
        }

        return null;
    }

    /// <summary>
    /// Search the best matching blendshape for a given FaceTrackingKey
    /// </summary>
    internal static int FindFaceTrackingBlendshape(string[] names, VRCAvatarDescriptor.FaceTrackingKey key, string filter)
    {
        var searches = GetNamesForFaceTrackingKey(key);
        // short-circuit on an unsupported key
        if (searches == null)
            return -1;
        
        string keyName = key.ToString();
        var isFilterEmpty = string.IsNullOrWhiteSpace(filter);
        // UE names (`keyName`) take priority over case-insensitive aliases such as ARKit camelCase names.
        for (int i = 0; i < names.Length; i++)
        {
            if ((isFilterEmpty || names[i].Contains(filter, StringComparison.OrdinalIgnoreCase))
                && string.Equals(names[i], keyName, StringComparison.Ordinal))
                return Array.IndexOf(names, names[i]);
        }
        
        // If UE search failed, search for any of the aliases in a case-insensitive manner.
        foreach (string search in searches)
        {
            for (int i = 0; i < names.Length; i++)
            {
                if ((isFilterEmpty || names[i].Contains(filter, StringComparison.OrdinalIgnoreCase))
                    && names[i].Equals(search, StringComparison.OrdinalIgnoreCase))
                    return Array.IndexOf(names, names[i]); // First duplicate blendshape name wins.
            }
        }
        return -1;
    }

    /// <summary>
    /// Search for the best matching blendshape for a given viseme
    /// Performing a full scan for the best match before going down to more generic matches
    /// </summary>
    internal static int FindVisemeBlendshape(string[] names, string viseme, string filter)
    {
        var isFilterEmpty = string.IsNullOrWhiteSpace(filter);
        return (FindFirstIndex(names, s => (isFilterEmpty || s.Contains(filter, StringComparison.OrdinalIgnoreCase)) && s.ToLowerInvariant() == "vrc.v_" + viseme)
            ?? FindFirstIndex(names, s => (isFilterEmpty || s.Contains(filter, StringComparison.OrdinalIgnoreCase)) && s.ToLowerInvariant() == "v_" + viseme)
            ?? FindFirstIndex(names, s => (isFilterEmpty || s.Contains(filter, StringComparison.OrdinalIgnoreCase)) && s.ToLowerInvariant().EndsWith(viseme))
            ?? FindFirstIndex(names, s => (isFilterEmpty || s.Contains(filter, StringComparison.OrdinalIgnoreCase)) && s.ToLowerInvariant() == viseme)
            ?? FindFirstIndex(names, s => (isFilterEmpty || s.Contains(filter, StringComparison.OrdinalIgnoreCase)) && s.ToLowerInvariant().Contains(viseme))).GetValueOrDefault(-1);
    }

    static int? FindFirstIndex(string[] names, Func<string, bool> match)
    {
        for (int i = 0; i < names.Length; i++)
        {
            if (match(names[i]))
                return i;
        }
        return null;
    }
}
}
#endif
