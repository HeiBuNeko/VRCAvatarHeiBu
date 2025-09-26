using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace nHaruka.PCSS4VRC
{
    public class PCSS4VRC_ParameterSetter : EditorWindow
    {
        private VRCAvatarDescriptor avatarDescriptor;
        private int isEng = 0;

        List<Material> materials;

        Color _DropShadowColor = Color.black;
        float _ShadowClamp = 0;
        float _ShadowNormalBias = 0.0025f;
        float _EnvLightStrength = 0.2f;
        float _ShadowDistance = 10;
        float _ShadowDensity = 0.1f;

        float Softness = 0.0015f;
        float SoftnessFalloff = 1.0f;

        [MenuItem("nHaruka/PCSS For VRC Parameter Configurator")]
        public static void Init()
        {
            var window = GetWindowWithRect<PCSS4VRC_ParameterSetter>(new Rect(0, 0, 600, 360));
            window.Show();
        }

        private void OnGUI()
        {
            GUIStyle style = new GUIStyle(EditorStyles.largeLabel);
            style.fontSize = 14;
            style.normal.textColor = Color.white;
            style.wordWrap = true;
            style.fontStyle = FontStyle.Normal;

            GUILayout.Label("PCSS4VRCパラメーター簡単設定ツール", style);

            GUILayout.Space(10);

            isEng = GUILayout.SelectionGrid(isEng, new string[] { "Japanese", "English" }, 2, GUI.skin.button);

            GUILayout.Space(10);

            EditorGUI.BeginChangeCheck();

            avatarDescriptor =
                (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", avatarDescriptor, typeof(VRCAvatarDescriptor), true);
            GUILayout.Space(10);

            
            if (EditorGUI.EndChangeCheck())
            {
                if (avatarDescriptor != null)
                {
                    materials = new List<Material>();
                    var renderers = avatarDescriptor.GetComponentsInChildren<Renderer>();
                    foreach (Renderer renderer in renderers)
                    {
                        foreach (Material material in renderer.sharedMaterials)
                        {
                            if (material != null && material.shader.name.Contains("PCSS"))
                            {
                                materials.Add(material);
                            }
                        }
                    }
                }
            }
            

            EditorGUI.BeginChangeCheck();

            if (isEng == 0)
            {
               
                Softness = EditorGUILayout.Slider(new GUIContent("近距離での影のぼかし具合", "射影物に近い影がぼんやりするようになります。"), Softness, 0.0f, 0.005f);

                GUILayout.Space(5);

                SoftnessFalloff = EditorGUILayout.Slider(new GUIContent("遠距離での影のぼかし具合", "射影物から遠い影がぼんやりするようになります。"), SoftnessFalloff, 0.0f, 2f);

                GUILayout.Space(5);

                _DropShadowColor = EditorGUILayout.ColorField(new GUIContent("影の色", "影の色を加算します。"), _DropShadowColor);

                GUILayout.Space(5);

                _ShadowClamp = EditorGUILayout.Slider(new GUIContent("影の2値化(アニメ調用途想定)", "この値をしきい値に影を2値化します。0で無効。アニメ調用途想定"), _ShadowClamp, 0, 1);

                GUILayout.Space(5);

                _ShadowNormalBias = EditorGUILayout.Slider(new GUIContent("Shadow Normal Bias", "シャドウアクネ（影が縞模様になる現象）を抑えるために影を法線方向にずらします。シャドウアクネが出たり、影の位置に違和感があれば調整してください。"), _ShadowNormalBias, 0, 0.005f);

                GUILayout.Space(5);

                _EnvLightStrength = EditorGUILayout.Slider(new GUIContent("ワールドライトの影響度", "リアル影システムの光源以外のライトの影響度です。影部分も影響を受けるので、影部分がワールドライトに照らされると影が薄くなったりします。"), _EnvLightStrength, 0, 1f);

                GUILayout.Space(5);

                _ShadowDistance = EditorGUILayout.Slider(new GUIContent("影を無効化する距離", "この距離以上離れると影が表示されなくなります。0にするとこの機能を無効化します。単位はメートル。"), _ShadowDistance, 0, 50f);
                
                GUILayout.Space(5); 
                
                _ShadowDensity = EditorGUILayout.Slider(new GUIContent("影の濃さ", "影の濃さ"), _ShadowDensity, 0, 1f);
            }
            else
            {
                
                Softness = EditorGUILayout.Slider(new GUIContent("Blurring of shadows at close range", "Shadows close to the projectile will be blurred."), Softness, 0.0f, 0.005f);

                GUILayout.Space(5);

                    SoftnessFalloff = EditorGUILayout.Slider(new GUIContent("Blurring of shadows at long range", "Shadows far from the projectile become blurred."), SoftnessFalloff, 0.0f, 2f);


                GUILayout.Space(5);

                _DropShadowColor = EditorGUILayout.ColorField(new GUIContent("Shadow Color", "Adds shadow color."), _DropShadowColor);

                GUILayout.Space(5);

                _ShadowClamp = EditorGUILayout.Slider(new GUIContent("Shadow binarization(for anime like tone)", "This value is used as a threshold to binarize shadows. 0 is disabled. Assumed for anime like tone use."), _ShadowClamp, 0, 1);

                GUILayout.Space(5);

                _ShadowNormalBias = EditorGUILayout.Slider(new GUIContent("Shadow Normal Bias", "Shift the shadow in the normal direction to suppress shadow acne (a phenomenon in which shadows become striped). If shadow acne appears or the position of the shadow is incongruous, adjust it."), _ShadowNormalBias, 0, 0.005f);

                GUILayout.Space(5);

                _EnvLightStrength = EditorGUILayout.Slider(new GUIContent("World Light Influence", "This is the degree of influence of lights other than the light source in the PCSS4VRC. Shadow areas are also affected, so if shadow areas are illuminated by world lights, the shadows may become lighter."), _EnvLightStrength, 0, 1f);

                GUILayout.Space(5);

                _ShadowDistance = EditorGUILayout.Slider(new GUIContent("Distance to disable shadows", "Shadows are not displayed at distances greater than this distance; a value of 0 disables this feature. Units are in meters."), _ShadowDistance, 0, 50f);

                GUILayout.Space(5);

                _ShadowDensity = EditorGUILayout.Slider(new GUIContent("Shadow Density", ""), _ShadowDensity, 0, 1f);
            }

            if (EditorGUI.EndChangeCheck())
            {
                for (int i = 0; i < materials.Count; i++)
                {

                    if (!materials[i].IsPropertyLocked("Softness"))
                    {
                        materials[i].SetFloat("Softness", Softness);
                    }
                    if (!materials[i].IsPropertyLocked("SoftnessFalloff"))
                    {
                        materials[i].SetFloat("SoftnessFalloff", SoftnessFalloff);
                    }

                    if (!materials[i].IsPropertyLocked("_DropShadowColor"))
                    {
                        materials[i].SetColor("_DropShadowColor", _DropShadowColor);
                    }
                    if (!materials[i].IsPropertyLocked("_ShadowClamp"))
                    {
                        materials[i].SetFloat("_ShadowClamp", _ShadowClamp);
                    }
                    if (!materials[i].IsPropertyLocked("_ShadowNormalBias"))
                    {
                        materials[i].SetFloat("_ShadowNormalBias", _ShadowNormalBias);
                    }
                    if (!materials[i].IsPropertyLocked("_EnvLightStrength"))
                    {
                        materials[i].SetFloat("_EnvLightStrength", _EnvLightStrength);
                    }
                    if (!materials[i].IsPropertyLocked("_ShadowDistance"))
                    {
                        materials[i].SetFloat("_ShadowDistance", _ShadowDistance);
                    }
                    if (!materials[i].IsPropertyLocked("_ShadowDensity"))
                    {
                        materials[i].SetFloat("_ShadowDensity", _ShadowDensity);
                    }

                    EditorUtility.SetDirty(materials[i]);
                }
                AssetDatabase.SaveAssets();
            }

            GUILayout.Space(5);

            GUIStyle style2 = new GUIStyle(EditorStyles.largeLabel);
            style2.fontSize = 14;
            style2.normal.textColor = Color.white;
            style2.wordWrap = true;
            style2.fontStyle = FontStyle.Normal;
            if (isEng == 0)
            {
                GUILayout.Label("※より細かい設定（マスクテクスチャやバイアス設定など）を行いたい場合は、各マテリアルのカスタムプロパティを参照してください。",style2);
            }
            else
            {
                GUILayout.Label("Note : For more advanced settings (mask texture, bias settings, etc.), please refer to the custom properties of your material.", style2);
            }
        }
    }
}