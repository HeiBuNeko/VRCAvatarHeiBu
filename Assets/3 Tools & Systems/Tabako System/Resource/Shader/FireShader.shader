//シェーダー名: FireShader
//著作権者: さたにあしょっぴんぐ
//ライセンス: vn3 License

//ライセンスの詳細に関しては、同梱されている "vn3" フォルダ内のPDFを参照してください。

Shader "Satania Shopping/FireShader"
{
    Properties
    {
        // プロパティの定義
        _MainTex("Texture", 2D) = "white" {}
    }

        SubShader
        {
            // サブシェーダーの定義
            Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
            Blend DstAlpha DstAlpha
            LOD 100
            Cull off

            Pass
            {
                CGPROGRAM
                // コンパイル指令やインクルード
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_fog
                #include "UnityCG.cginc"

                // 入力構造体の定義
                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                // 出力構造体の定義
                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    UNITY_FOG_COORDS(1)
                    float4 vertex : SV_POSITION;
                };

                // プロパティ変数の宣言
                sampler2D _MainTex;
                float4 _MainTex_ST;

                // ランダムな2次元値を生成する関数
                fixed2 random2(fixed2 st)
                {
                    st = fixed2(dot(st, fixed2(523.7, 853.2)),
                                dot(st, fixed2(337.4, 149.8)));
                    return -1.0 + 2.0 * frac(sin(st) * 43758.5453123);
                }

                // パーリンノイズを生成する関数
                float perlinNoise(fixed2 st)
                {
                    // 床関数と小数部分を計算
                    fixed2 p = floor(st);
                    fixed2 f = frac(st);
                    fixed2 u = f * f * (3.0 - 2.0 * f);

                    // 周囲のランダムな値を取得
                    float v00 = random2(p + fixed2(0,0));
                    float v10 = random2(p + fixed2(1,0));
                    float v01 = random2(p + fixed2(0,1));
                    float v11 = random2(p + fixed2(1,1));

                    // 補間してパーリンノイズを計算
                    return lerp(lerp(dot(v00, f - fixed2(0, 0)), dot(v10, f - fixed2(1, 0)), u.x),
                        lerp(dot(v01, f - fixed2(0, 1)), dot(v11, f - fixed2(1, 1)), u.x),
                        u.y) + 0.5f;
                }

                // 値を再マップする関数
                float Remap(float value, float from1, float to1, float from2, float to2)
                {
                    return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
                }

                // 頂点シェーダー
                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                    UNITY_TRANSFER_FOG(o, o.vertex);
                    return o;
                }

                // フラグメントシェーダー
                fixed4 frag(v2f i) : SV_Target
                {
                    // UV座標のオフセットを適用
                    i.uv = float2(i.uv.x, i.uv.y);

                    // パーリンノイズを生成してオフセットに適用
                    fixed pn = perlinNoise(fixed2(i.uv.x + sin(_Time.y), i.uv.y - _Time.y) * 2);
                    pn = Remap(pn, 0, 1, -1, 1);
                    fixed2 noisedUV = fixed2(i.uv.x + pn * 0.01, i.uv.y + pn * 0.2);

                    // テクスチャから色をサンプリングし、乗算と調整を行う
                    fixed4 col = tex2D(_MainTex, noisedUV) * 1.0 * fixed4(1,1,1,1);

                    // アルファ値の調整
                    col.a = (col.r + col.g + col.b) / 3;
                    if (col.a < 0.01)
                        discard;

                    UNITY_APPLY_FOG(i.fogCoord, col);
                    return col;
                }
                ENDCG
        }
    }
}