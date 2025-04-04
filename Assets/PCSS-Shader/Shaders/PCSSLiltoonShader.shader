Shader "liltoon/PCSS/PCSSLiltoon"
{
    Properties
    {
        // lilToonのプロパティ
        [lilToggle] _lilToonVersion ("lilToon Version", Int) = 37
        
        // メインカラー
        [lilHDR] [MainColor] _Color ("Color", Color) = (1,1,1,1)
        [MainTexture] _MainTex ("Texture", 2D) = "white" {}
        [lilUVAnim] _MainTex_ScrollRotate ("Angle|UV Animation|Scroll|Rotate", Vector) = (0,0,0,0)
        
        // PCSS設定
        [Header(PCSS Settings)]
        _PCSSEnabled ("PCSS Enabled", Float) = 1
        _PCSSIntensity ("PCSS Intensity", Range(0, 1)) = 1
        _PCSSBlockerSampleCount ("Blocker Sample Count", Range(1, 64)) = 16
        _PCSSPCFSampleCount ("PCF Sample Count", Range(1, 64)) = 16
        _PCSSoftness ("Softness", Range(0, 1)) = 0.5
        _PCSSSampleRadius ("Sample Radius", Range(0, 1)) = 0.02
        _PCSSMaxStaticGradientBias ("Static Gradient Bias", Range(0, 1)) = 0.05
        _PCSSBlockerGradientBias ("Blocker Gradient Bias", Range(0, 1)) = 0
        _PCSSPCFGradientBias ("PCF Gradient Bias", Range(0, 1)) = 1
        _PCSSCascadeBlendDistance ("Cascade Blend Distance", Range(0, 1)) = 0.5
        _PCSSNoiseTexture ("Noise Texture", 2D) = "black" {}
        
        // lilToonの他のプロパティ
        // シャドウ設定
        [lilToggle] _UseShadow ("Use Shadow", Int) = 1
        [lilToggle] _ShadowReceive ("Receive Shadow", Int) = 1
        _Shadow ("Shadow", Range(0, 1)) = 0.5
        _ShadowBorder ("Border", Range(0, 1)) = 0.5
        _ShadowBlur ("Blur", Range(0, 1)) = 0.1
        _Shadow2ndBorder ("2nd Border", Range(0, 1)) = 0.5
        _Shadow2ndBlur ("2nd Blur", Range(0, 1)) = 0.3
        _ShadowEnvStrength ("Environment Strength", Range(0, 1)) = 0
        _ShadowBorderColor ("Border Color", Color) = (1,0,0,1)
        _ShadowBorderRange ("Border Range", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags {"RenderType"="Opaque" "Queue"="Geometry"}
        LOD 100

        HLSLINCLUDE
        #pragma target 4.5
        #pragma require geometry
        #include "UnityCG.cginc"
        #include "AutoLight.cginc"
        #include "Lighting.cginc"
        #include "UnityPBSLighting.cginc"
        #include "Assets/lilToon/Shader/lil_common.hlsl"
        #include "PCSS.cginc"

        // PCSS変数
        float _PCSSEnabled;
        float _PCSSIntensity;
        int _PCSSBlockerSampleCount;
        int _PCSSPCFSampleCount;
        float _PCSSoftness;
        float _PCSSSampleRadius;
        float _PCSSMaxStaticGradientBias;
        float _PCSSBlockerGradientBias;
        float _PCSSPCFGradientBias;
        float _PCSSCascadeBlendDistance;
        sampler2D _PCSSNoiseTexture;
        float4 _PCSSNoiseCoords;

        struct v2f
        {
            float4 pos : SV_POSITION;
            float2 uv : TEXCOORD0;
            float3 normal : NORMAL;
            float4 worldPos : TEXCOORD1;
            UNITY_SHADOW_COORDS(2)
            UNITY_FOG_COORDS(3)
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        v2f vert(appdata_full v)
        {
            v2f o;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_OUTPUT(v2f, o);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

            o.pos = UnityObjectToClipPos(v.vertex);
            o.uv = v.texcoord.xy;
            o.normal = UnityObjectToWorldNormal(v.normal);
            o.worldPos = mul(unity_ObjectToWorld, v.vertex);
            UNITY_TRANSFER_SHADOW(o, o.uv);
            UNITY_TRANSFER_FOG(o, o.pos);
            return o;
        }

        float4 frag(v2f i) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

            // lilToonの基本設定
            float4 col = LIL_SAMPLE_2D(_MainTex, sampler_MainTex, i.uv) * _Color;
            
            // PCSSシャドウの計算
            float shadow = 1.0;
            if (_PCSSEnabled > 0.5 && _UseShadow)
            {
                shadow = CalculatePCSSShadow(i.worldPos, _PCSSBlockerSampleCount, _PCSSPCFSampleCount,
                    _PCSSoftness, _PCSSSampleRadius, _PCSSMaxStaticGradientBias,
                    _PCSSBlockerGradientBias, _PCSSPCFGradientBias, _PCSSCascadeBlendDistance,
                    _PCSSNoiseTexture, _PCSSNoiseCoords);
                
                shadow = lerp(1.0, shadow, _PCSSIntensity);
            }

            // lilToonのシャドウ設定との統合
            if (_UseShadow)
            {
                float lilShadow = LIL_SHADOW_ATTENUATION(i);
                shadow = min(shadow, lilShadow);
                
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = dot(i.normal, lightDir);
                
                float shadowStrength = 1.0 - _Shadow;
                float shadowBorder = _ShadowBorder;
                float shadowBlur = _ShadowBlur;
                
                float shadowFactor = smoothstep(shadowBorder - shadowBlur, shadowBorder + shadowBlur, NdotL);
                shadowFactor = lerp(shadowStrength, 1.0, shadowFactor);
                
                shadow *= shadowFactor;
            }

            // 最終カラーの計算
            col.rgb *= lerp(1.0, shadow, _ShadowReceive);
            
            // フォグの適用
            UNITY_APPLY_FOG(i.fogCoord, col);
            
            return col;
        }
        ENDHLSL

        Pass
        {
            Name "FORWARD"
            Tags {"LightMode"="ForwardBase"}

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma skip_variants LIGHTMAP_ON DYNAMICLIGHTMAP_ON LIGHTMAP_SHADOW_MIXING SHADOWS_SHADOWMASK DIRLIGHTMAP_COMBINED
            ENDHLSL
        }

        // シャドウキャスターパス
        Pass
        {
            Name "ShadowCaster"
            Tags {"LightMode"="ShadowCaster"}

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing

            struct v2f_shadow {
                V2F_SHADOW_CASTER;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f_shadow vertShadow(appdata_base v)
            {
                v2f_shadow o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 fragShadow(v2f_shadow i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDHLSL
        }
    }

    Fallback "lilToon/Lite"
    CustomEditor "lilToon.lilToonInspector"
} 