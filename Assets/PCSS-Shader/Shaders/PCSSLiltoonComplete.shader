#ifndef LIL_LITE
#define LIL_LITE

#include "Includes/lil_pipeline.hlsl"
#include "Includes/lil_common.hlsl"
#include "PCSS.cginc"

//------------------------------------------------------------------------------------------------------------------------------
// Properties
PROPERTIES_BEGIN
    // PCSSプロパティ
    lilPCSSGroup(            PCSSSettings)
    lilPCSS(                _PCSSEnabled)
    lilPCSSInt(            _PCSSIntensity)
    lilPCSSBlocker(        _PCSSBlockerSampleCount)
    lilPCSSPCF(            _PCSSPCFSampleCount)
    lilPCSSSoftness(        _PCSSoftness)
    lilPCSSSampleRadius(    _PCSSampleRadius)
    lilPCSSBias(            _PCSSMaxStaticGradientBias)
    lilPCSSBlockerBias(    _PCSSBlockerGradientBias)
    lilPCSSPCFBias(        _PCSSPCFGradientBias)
    lilPCSSCascadeBlend(    _PCSSCascadeBlendDistance)
    lilPCSSNoise(            _PCSSNoiseTexture)

    // メインカラー
    lilColorGroup(         MainColorSettings)
    lilColor(             _Color)
    lilTex2D(             _MainTex)
    lilMainUVScrollRotate(_MainTex)
    lilColor(             _Color2nd)
    lilTex2D(             _MainTex2nd)
    lilColor(             _Color3rd)
    lilTex2D(             _MainTex3rd)

    // アルファマスク
    lilAlphaMaskGroup(    AlphaMaskSettings)
    lilAlphaMask(         _AlphaMask)

    // シャドウ
    lilShadowGroup(       ShadowSettings)
    lilShadow(            _UseShadow)
    lilShadowStrength(    _ShadowStrength)
    lilShadowBorder(      _ShadowBorder)
    lilShadowBlur(        _ShadowBlur)
    lilShadowColor(       _ShadowColor)
    lilShadow2nd(         _UseShadow2nd)
    lilShadowStrength2nd( _ShadowStrength2nd)
    lilShadowBorder2nd(   _ShadowBorder2nd)
    lilShadowBlur2nd(     _ShadowBlur2nd)
    lilShadowColor2nd(    _ShadowColor2nd)

    // リムライト
    lilRimlightGroup(     RimlightSettings)
    lilRimlight(          _UseRimlight)
    lilRimlightColor(     _RimlightColor)
    lilRimlightBorder(    _RimlightBorder)
    lilRimlightBlur(      _RimlightBlur)
    lilRimlightPower(     _RimlightFresnelPower)

    // 発光
    lilEmissionGroup(     EmissionSettings)
    lilEmission(          _UseEmission)
    lilEmissionColor(     _EmissionColor)
    lilEmissionMap(       _EmissionMap)
    lilEmissionBlend(     _EmissionBlend)
    lilEmissionMask(      _EmissionBlendMask)

    // アウトライン
    lilOutlineGroup(      OutlineSettings)
    lilOutline(           _UseOutline)
    lilOutlineColor(      _OutlineColor)
    lilOutlineWidth(      _OutlineWidth)
    lilOutlineMask(       _OutlineMask)
PROPERTIES_END

//------------------------------------------------------------------------------------------------------------------------------
// Pass
PASS_BEGIN(FORWARD)
    HLSLPROGRAM
    #pragma vertex vert
    #pragma fragment frag
    #pragma multi_compile_fwdbase
    #pragma multi_compile_fog
    #pragma multi_compile_instancing
    #pragma skip_variants LIGHTMAP_ON DYNAMICLIGHTMAP_ON LIGHTMAP_SHADOW_MIXING SHADOWS_SHADOWMASK DIRLIGHTMAP_COMBINED
    #pragma multi_compile _ _EMISSION
    #pragma multi_compile _ _RIMLIGHT
    #pragma multi_compile _ _OUTLINE
    #pragma multi_compile _ _NORMAL_MAP
    #pragma multi_compile _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON

    #include "Includes/lil_pass_forward.hlsl"

    struct v2f
    {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
        float3 normal : NORMAL;
        float4 worldPos : TEXCOORD1;
        UNITY_SHADOW_COORDS(2)
        UNITY_FOG_COORDS(3)
        lilLightingInput lightingInput;
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
        o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
        o.normal = UnityObjectToWorldNormal(v.normal);
        o.worldPos = mul(unity_ObjectToWorld, v.vertex);
        o.lightingInput = lilInitLightingInput(v);
        UNITY_TRANSFER_SHADOW(o, o.uv);
        UNITY_TRANSFER_FOG(o, o.pos);
        return o;
    }

    float4 frag(v2f i) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

        // 基本色
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

        // lilToonライティング
        lilLightingData lightingData = lilInitLightingData(i.lightingInput);
        lightingData.shadowAtten = min(shadow, LIL_SHADOW_ATTENUATION(i));
        float3 lighting = lilCalculateLighting(lightingData);

        // リムライト
        #if defined(_RIMLIGHT)
        if (_UseRimlight)
        {
            float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
            float rimDot = 1.0 - saturate(dot(viewDir, i.normal));
            float rimPower = pow(rimDot, _RimlightFresnelPower);
            float rim = smoothstep(_RimlightBorder - _RimlightBlur, _RimlightBorder + _RimlightBlur, rimPower);
            col.rgb = lerp(col.rgb, _RimlightColor.rgb, rim * _RimlightColor.a);
        }
        #endif

        // 発光
        #if defined(_EMISSION)
        if (_UseEmission)
        {
            float4 emission = LIL_SAMPLE_2D(_EmissionMap, sampler_EmissionMap, i.uv) * _EmissionColor;
            float emissionMask = LIL_SAMPLE_2D(_EmissionBlendMask, sampler_EmissionBlendMask, i.uv).r;
            col.rgb = lerp(col.rgb, emission.rgb, emission.a * emissionMask * _EmissionBlend);
        }
        #endif

        // 最終カラーの計算
        col.rgb *= lighting;
        
        // フォグの適用
        UNITY_APPLY_FOG(i.fogCoord, col);
        
        return col;
    }
    ENDHLSL
PASS_END

// アウトラインパス
PASS_BEGIN(OUTLINE)
    Tags {"LightMode" = "ForwardBase"}
    Cull Front
    
    HLSLPROGRAM
    #pragma vertex vertOutline
    #pragma fragment fragOutline
    #pragma multi_compile_fwdbase
    #pragma multi_compile_fog
    #pragma multi_compile_instancing
    
    struct v2f_outline
    {
        float4 pos : SV_POSITION;
        UNITY_FOG_COORDS(0)
        float4 color : TEXCOORD1;
        UNITY_VERTEX_INPUT_INSTANCE_ID
        UNITY_VERTEX_OUTPUT_STEREO
    };

    v2f_outline vertOutline(appdata_full v)
    {
        v2f_outline o;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_INITIALIZE_OUTPUT(v2f_outline, o);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

        float outlineWidth = _OutlineWidth;
        #if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED) || defined(UNITY_STEREO_EYEINDEX_POST_VERTEX)
            outlineWidth *= 0.5;
        #endif
            
        float3 normalOS = normalize(v.normal);
        float3 outlinePos = v.vertex.xyz + normalOS * outlineWidth;
        o.pos = UnityObjectToClipPos(float4(outlinePos, 1));
        
        o.color = _OutlineColor;
        
        UNITY_TRANSFER_FOG(o,o.pos);
        return o;
    }

    float4 fragOutline(v2f_outline i) : SV_Target
    {
        float4 col = i.color;
        UNITY_APPLY_FOG(i.fogCoord, col);
        return col;
    }
    ENDHLSL
PASS_END

// シャドウキャスターパス
PASS_BEGIN(SHADOW_CASTER)
    Tags {"LightMode" = "ShadowCaster"}
    
    HLSLPROGRAM
    #pragma vertex vertShadow
    #pragma fragment fragShadow
    #pragma multi_compile_shadowcaster
    #pragma multi_compile_instancing
    
    struct v2f_shadow
    {
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
PASS_END

SubShader
{
    Tags {"RenderType"="Opaque" "Queue"="Geometry"}
    LOD 100

    UsePass "Hidden/ltspass_lite/FORWARD"
    UsePass "Hidden/ltspass_lite/FORWARD_ADD"
    UsePass "Hidden/ltspass_lite/SHADOW_CASTER"
    UsePass "Hidden/ltspass_lite/META"
}

Fallback "lilToon/Lite"
CustomEditor "lilToon.lilToonInspector" 