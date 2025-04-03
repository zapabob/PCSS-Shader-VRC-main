// PCSSLiltoon.shader
Shader "lilToon/PCSS"
{
    Properties
    {
        //----------------------------------------------------------------------------------------------------------------------
        // Base
        [lilToggle]     _Invisible                  ("Invisible", Int) = 0
        [lilToggle]     _TransparentEnabled         ("Transparent Mode", Int) = 0
        [lilEnum]       _TransparentMode            ("Transparent Mode|Normal|OnePass|TwoPass|OnePass|TwoPass", Int) = 0
        [lilToggle]     _UseModularAvatar          ("Use Modular Avatar", Int) = 1
        [lilToggle]     _PreserveOnAutoFix         ("Preserve On AutoFix", Int) = 1

        //----------------------------------------------------------------------------------------------------------------------
        // PCSS Settings
        [lilPCSSHeader] _PCSSHeader                 ("PCSS", Int) = 0
        [NoScaleOffset] _PCSShadowMap              ("Shadow Map", 2D) = "white" {}
        [lilPCSSParams] _PCSSParams                ("PCSS Parameters", Vector) = (1.0, 0.1, 16, 16)
        _PCSSoftness                               ("Softness", Range(0, 7.5)) = 1.0
        _PCSSSampleRadius                         ("Sample Radius", Range(0, 1)) = 0.1
        _PCSSBlockerSampleCount                   ("Blocker Sample Count", Range(1, 64)) = 16
        _PCSSPCFSampleCount                      ("PCF Sample Count", Range(1, 64)) = 16
        _PCSSMaxStaticGradientBias               ("Max Static Gradient Bias", Range(0, 0.15)) = 0.05
        _PCSSBlockerGradientBias                 ("Blocker Gradient Bias", Range(0, 1)) = 0
        _PCSSPCFGradientBias                    ("PCF Gradient Bias", Range(0, 1)) = 1
        _PCSSCascadeBlendDistance               ("Cascade Blend Distance", Range(0, 1)) = 0.5
        [NoScaleOffset] _PCSSNoiseTexture        ("Noise Texture", 2D) = "black" {}
        [Toggle] _PCSSupportOrthographicProjection("Support Orthographic", Int) = 0
        _PCSShadowStrength                      ("Shadow Strength", Range(0, 1)) = 1.0

        //----------------------------------------------------------------------------------------------------------------------
        // Main
        [lilHDR] [MainColor] _Color               ("Color", Color) = (1,1,1,1)
        [MainTexture]   _MainTex                  ("Texture", 2D) = "white" {}
        [lilUVAnim]     _MainTex_ScrollRotate     ("Angle|UV Animation|Scroll|Rotate", Vector) = (0,0,0,0)
        [lilHSVG]       _MainTexHSVG              ("Hue|Saturation|Value|Gamma", Vector) = (0,1,1,1)
        [lilToggle]     _MainGradationStrength    ("Gradation Strength", Range(0, 1)) = 0
        [NoScaleOffset] _MainGradationTex         ("Gradation Map", 2D) = "white" {}
        [lilEnum]       _AlphaMaskMode            ("AlphaMask|", Int) = 0
        [NoScaleOffset] _AlphaMask                ("AlphaMask", 2D) = "white" {}
        
        //----------------------------------------------------------------------------------------------------------------------
        // Shadow
        [lilToggleLeft] _UseShadow                ("Use Shadow", Int) = 0
        [lilToggle]     _ShadowReceive            ("Receive Shadow", Int) = 0
        [lilShadowAO]   _ShadowAO                 ("AO", Vector) = (0,0,0,0)
        [lilShadowBorder] _ShadowBorder           ("Border", Range(0, 1)) = 0.5
        [lilShadowBlur]   _ShadowBlur             ("Blur", Range(0, 1)) = 0.1
        [lilShadowStrength] _ShadowStrength       ("Strength", Range(0, 1)) = 1
        [NoScaleOffset] _ShadowColorTex           ("Shadow Color", 2D) = "black" {}
        [lilShadowColor] _ShadowColor             ("Shadow Color", Color) = (0.7,0.75,0.85,1.0)

        //----------------------------------------------------------------------------------------------------------------------
        // MatCap
        [lilToggleLeft] _UseMatCap               ("Use MatCap", Int) = 0
        [lilMatCapUV]   _MatCapTex               ("MatCap Texture|UV Blend", 2D) = "white" {}
        [lilBlendMode]  _MatCapBlendMode         ("Blend Mode|", Int) = 1
        [lilToggle]     _MatCapZRotCancel        ("Z-axis rotation cancellation", Int) = 1
        [lilToggle]     _MatCapPerspective       ("Fix Perspective", Int) = 1
        [lilToggle]     _MatCapVRParallaxStrength ("VR Parallax Strength", Range(0, 1)) = 1

        //----------------------------------------------------------------------------------------------------------------------
        // Rim
        [lilToggleLeft] _UseRim                  ("Use Rim", Int) = 0
        [lilRimMode]    _RimMode                 ("Rim Mode|", Int) = 1
        [NoScaleOffset] _RimColorTex             ("Color", 2D) = "white" {}
        [lilHDR]        _RimColor                ("Color", Color) = (1,1,1,1)
        [lilRimWidth]   _RimBorder               ("Width", Range(0, 1)) = 0.5
        [lilRimBlur]    _RimBlur                 ("Blur", Range(0, 1)) = 0.1
        [lilRimFresnelPower] _RimFresnelPower    ("Fresnel Power", Range(0.01, 50)) = 3.0

        //----------------------------------------------------------------------------------------------------------------------
        // Emission
        [lilToggleLeft] _UseEmission             ("Use Emission", Int) = 0
        [lilEmissionMode] _EmissionMode          ("Mode|", Int) = 0
        [NoScaleOffset] _EmissionMap             ("Texture", 2D) = "white" {}
        [lilHDR]        _EmissionColor           ("Color", Color) = (1,1,1,1)
        [lilEmissionBlink] _EmissionBlink        ("Blink Strength|Blink Type|Blink Speed|Blink Offset", Vector) = (0,0,3.141593,0)

        //----------------------------------------------------------------------------------------------------------------------
        // Advanced
        [lilEnum]                 _Cull          ("Cull Mode|Off|Front|Back", Int) = 2
        [Enum(UnityEngine.Rendering.BlendMode)]     _SrcBlend    ("SrcBlend", Int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]     _DstBlend    ("DstBlend", Int) = 0
        [Enum(UnityEngine.Rendering.BlendMode)]     _SrcBlendAlpha ("SrcBlendAlpha", Int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]     _DstBlendAlpha ("DstBlendAlpha", Int) = 10
        [Enum(UnityEngine.Rendering.BlendOp)]       _BlendOp     ("BlendOp", Int) = 0
        [Enum(UnityEngine.Rendering.BlendOp)]       _BlendOpAlpha ("BlendOpAlpha", Int) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest     ("ZTest", Int) = 4
        [lilToggle]     _ZWrite                   ("ZWrite", Int) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilRef  ("Stencil Reference Value", Int) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Compare Function", Int) = 8
        [Enum(UnityEngine.Rendering.StencilOp)]      _StencilPass ("Stencil Pass", Int) = 0
        [Enum(UnityEngine.Rendering.StencilOp)]      _StencilFail ("Stencil Fail", Int) = 0
        [Enum(UnityEngine.Rendering.StencilOp)]      _StencilZFail ("Stencil ZFail", Int) = 0
        _OffsetFactor                             ("Offset Factor", Float) = 0
        _OffsetUnits                              ("Offset Units", Float) = 0
        [lilColorMask]  _ColorMask                ("Color Mask", Int) = 15
        [lilToggle]     _AlphaToMask              ("AlphaToMask", Int) = 0
    }

    HLSLINCLUDE
    #include "Lighting.cginc"
    #include "UnityCG.cginc"
    #include "AutoLight.cginc"
    #include "liltoon.hlsl"

    // PCSS関連の変数
    UNITY_DECLARE_SHADOWMAP(_PCSShadowMap);
    float4 _PCSSParams; // (Softness, SampleRadius, BlockerSampleCount, PCFSampleCount)
    float _PCSSoftness;
    float _PCSSSampleRadius;
    int _PCSSBlockerSampleCount;
    int _PCSSPCFSampleCount;
    float _PCSSMaxStaticGradientBias;
    float _PCSSBlockerGradientBias;
    float _PCSSPCFGradientBias;
    float _PCSSCascadeBlendDistance;
    float4 _PCSSNoiseCoords;
    sampler2D _PCSSNoiseTexture;
    int _PCSSupportOrthographicProjection;
    float _PCSShadowStrength;

    // PCSSのサンプリング関数
    float SamplePCSSShadowMap(float4 coords)
    {
        float shadow = 0;
        float2 texelSize = _PCSSNoiseCoords.xy;
        float2 rotation = tex2D(_PCSSNoiseTexture, coords.xy * _PCSSNoiseCoords.xy).xy;

        // Blocker search
        float blockerDepth = 0;
        int blockerCount = 0;
        
        UNITY_LOOP
        for(int i = 0; i < _PCSSBlockerSampleCount; i++)
        {
            float2 offset = lilGetPoissonSample(i) * _PCSSSampleRadius;
            offset = float2(offset.x * rotation.x - offset.y * rotation.y,
                          offset.x * rotation.y + offset.y * rotation.x);
            
            float depth = UNITY_SAMPLE_SHADOW(_PCSShadowMap, coords.xyz + float3(offset, 0));
            if(depth < coords.z)
            {
                blockerDepth += depth;
                blockerCount++;
            }
        }

        if(blockerCount > 0)
        {
            blockerDepth /= blockerCount;
            float penumbraSize = (coords.z - blockerDepth) * _PCSSoftness;
            
            UNITY_LOOP
            for(int i = 0; i < _PCSSPCFSampleCount; i++)
            {
                float2 offset = lilGetPoissonSample(i) * penumbraSize;
                offset = float2(offset.x * rotation.x - offset.y * rotation.y,
                              offset.x * rotation.y + offset.y * rotation.x);
                
                shadow += UNITY_SAMPLE_SHADOW(_PCSShadowMap, coords.xyz + float3(offset, 0));
            }
            shadow /= _PCSSPCFSampleCount;
        }
        else
        {
            shadow = 1;
        }

        return lerp(1, shadow, _PCSShadowStrength);
    }

    // モジュラーアバター対応のマクロ
    #define MODULAR_AVATAR_SUPPORT (defined(_USEMODULARAVATAR_ON) && defined(_PRESERVEONAUTOFIX_ON))

    ENDHLSL

    SubShader
    {
        Tags {"RenderType" = "Opaque" "Queue" = "Geometry"}

        UsePass "Hidden/liltoon/FORWARD"
        UsePass "Hidden/liltoon/FORWARD_ADD"
        UsePass "Hidden/liltoon/SHADOW_CASTER"
        UsePass "Hidden/liltoon/META"
        
        Pass
        {
            Name "PCSS_FORWARD"
            Tags {"LightMode" = "ForwardBase"}

            Stencil
            {
                Ref [_StencilRef]
                ReadMask 255
                WriteMask 255
                Comp [_StencilComp]
                Pass [_StencilPass]
                Fail [_StencilFail]
                ZFail [_StencilZFail]
            }

            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            BlendOp [_BlendOp], [_BlendOpAlpha]
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Cull [_Cull]
            ColorMask [_ColorMask]
            AlphaToMask [_AlphaToMask]
            Offset [_OffsetFactor], [_OffsetUnits]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma shader_feature_local _USEMODULARAVATAR_ON
            #pragma shader_feature_local _PRESERVEONAUTOFIX_ON

            #include "liltoon.hlsl"

            struct v2f : LIL_V2F_WITH_SHADOW
            {
                float4 positionCS : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                UNITY_FOG_COORDS(2)
                UNITY_SHADOW_COORDS(3)
                LIL_VERTEX_INPUT_INSTANCE_ID
                LIL_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata_full v)
            {
                v2f o;
                LIL_INITIALIZE_STRUCT(v2f, o);
                LIL_SETUP_INSTANCE_ID(v);
                LIL_TRANSFER_INSTANCE_ID(v, o);
                LIL_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = UnityObjectToClipPos(v.vertex);
                o.uv0 = v.texcoord.xy;
                o.positionWS = mul(unity_ObjectToWorld, v.vertex).xyz;
                
                #if MODULAR_AVATAR_SUPPORT
                    if(_UseModularAvatar)
                    {
                        o.positionWS = ApplyModularAvatarTransform(o.positionWS);
                    }
                #endif

                UNITY_TRANSFER_FOG(o,o.positionCS);
                UNITY_TRANSFER_SHADOW(o,v.texcoord1.xy);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                LIL_SETUP_INSTANCE_ID(i);
                LIL_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // liltoonの基本処理
                float4 col = LIL_SAMPLE_2D(_MainTex, sampler_MainTex, i.uv0);
                col *= _Color;

                #if MODULAR_AVATAR_SUPPORT
                    if(_UseModularAvatar && _PreserveOnAutoFix)
                    {
                        // PCSSシャドウの適用
                        float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                        float shadow = SamplePCSSShadowMap(shadowCoord);
                        col.rgb *= shadow;
                    }
                #endif

                // liltoonの後処理
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDHLSL
        }
    }
    
    Fallback "Hidden/liltoon"
    CustomEditor "lilToon.lilToonInspector"
}
