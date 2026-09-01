using UnityEngine;
using VRC.SDKBase;

namespace satania.tabakosystem
{
    public class TabakoScript : MonoBehaviour, IEditorOnly
    {
        public enum Gesture
        {
            [InspectorName("Neutral")] neutral,
            [InspectorName("Fist")] fist,
            [InspectorName("HandOpen")] handopen,
            [InspectorName("FingerPoint")] fingerpoint,
            [InspectorName("Victory")] victory,
            [InspectorName("RockNRoll")] rocknroll,
            [InspectorName("HandGun")] handgun,
            [InspectorName("ThumbsUp")] thumbsup,
        }
        public Gesture gesture_case = Gesture.thumbsup;
        public Gesture gesture_lighter = Gesture.rocknroll;
        public Gesture gesture_fire = Gesture.fist;
        public Gesture gesture_restore = Gesture.rocknroll;
        public Gesture gesture_smoke = Gesture.victory;
        public Gesture gesture_swap = Gesture.thumbsup;

        public enum MaterialColor
        {
            black_en = 0,
            black_jp,
            white_en,
            white_jp,
            custom
        }

        public MaterialColor case_mat = MaterialColor.black_en;
        public MaterialColor tabako_mat = MaterialColor.black_en;
        public MaterialColor lighter_mat = MaterialColor.black_en;
        public Material custom_case_mat;
        public Material custom_tabako_mat;
        public Material custom_lighter_mat;

        public enum LighterAudio
        {
            v1_Real,
            v1_1,
            custom,
            none
        }

        public LighterAudio _audio = LighterAudio.v1_Real;
        public AudioClip _clip;
    }
}