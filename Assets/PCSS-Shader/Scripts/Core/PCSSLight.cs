// PCSSLight.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace PCSS.Core
{
    [AddComponentMenu("PCSS/Light Core")]
    [ExecuteInEditMode]
    [RequireComponent(typeof(Light))]
    public class PCSSLight : MonoBehaviour
    {
        // VRCFury互換のパラメータ名
        private const string PCSS_PARAM_PREFIX = "PCSS_";
        private const string PCSS_ENABLED_PARAM = PCSS_PARAM_PREFIX + "Enabled";
        private const string PCSS_INTENSITY_PARAM = PCSS_PARAM_PREFIX + "Intensity";
        private const string PCSS_SOFTNESS_PARAM = PCSS_PARAM_PREFIX + "Softness";

        [Header("シャドウ設定")]
        public int resolution = 4096;
        public bool customShadowResolution = false;

        [Header("サンプリング設定")]
        [Range(1, 64)]
        public int Blocker_SampleCount = 16;
        [Range(1, 64)]
        public int PCF_SampleCount = 16;

        [Header("ノイズテクスチャ")]
        public Texture2D noiseTexture;

        [Header("ソフトネス設定")]
        [Range(0f, 7.5f)]
        public float Softness = 1f;
        [Range(0f, 5f)]
        public float SoftnessFalloff = 4f;

        [Header("バイアス設定")]
        [Range(0f, 0.15f)]
        public float MaxStaticGradientBias = .05f;
        [Range(0f, 1f)]
        public float Blocker_GradientBias = 0f;
        [Range(0f, 1f)]
        public float PCF_GradientBias = 1f;

        [Header("カスケード設定")]
        [Range(0f, 1f)]
        public float CascadeBlendDistance = .5f;

        [Header("その他の設定")]
        public bool supportOrthographicProjection;

        [Header("レンダリング設定")]
        public RenderTexture shadowRenderTexture;
        public RenderTextureFormat format = RenderTextureFormat.RFloat;
        public FilterMode filterMode = FilterMode.Bilinear;
        public enum antiAliasing
        {
            None = 1,
            Two = 2,
            Four = 4,
            Eight = 8,
        }
        public antiAliasing MSAA = antiAliasing.None;

        [Header("シェーダー設定")]
        public string shaderName = "liltoon/PCSS/PCSS";

        private LightEvent lightEvent = LightEvent.AfterShadowMap;
        private int shadowmapPropID;
        private CommandBuffer copyShadowBuffer;
        private Light lightComponent;
        private CommandBuffer commandBuffer;
        private RenderTexture shadowMap;
        private bool isSetup = false;

        // Added to support mirror state notification from PCSSLightPlugin
        private bool _isInMirror = false;
        
        private bool _isVRCFuryInitialized = false;
        private VRCAvatarDescriptor _avatarDescriptor;

        private void Awake()
        {
            if (!isSetup)
            {
                Setup();
            }
        }

        private void Reset()
        {
            Setup();
        }

        public void Setup()
        {
            try
            {
                lightComponent = GetComponent<Light>();
                if (lightComponent == null)
                {
                    Debug.LogError("Light component not found. PCSSLight requires a Light component.", this);
                    return;
                }

                // アバターのセットアップ
                _avatarDescriptor = GetComponentInParent<VRCAvatarDescriptor>();
                if (_avatarDescriptor == null)
                {
                    Debug.LogWarning("PCSSLight should be placed on an avatar with VRCAvatarDescriptor.", this);
                }

                // 基本設定の初期化
                if (noiseTexture == null)
                {
                    noiseTexture = new Texture2D(64, 64, TextureFormat.R8, false)
                    {
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Repeat
                    };
                    // ノイズテクスチャの生成（簡単なランダムノイズ）
                    Color[] pixels = new Color[64 * 64];
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        pixels[i] = new Color(Random.value, Random.value, Random.value, 1);
                    }
                    noiseTexture.SetPixels(pixels);
                    noiseTexture.Apply();
                }

                resolution = Mathf.ClosestPowerOfTwo(resolution);
                if (customShadowResolution)
                {
                    lightComponent.shadowCustomResolution = resolution;
                }

                // シャドウマップの設定
                shadowmapPropID = Shader.PropertyToID("_ShadowMap");
                CreateShadowRenderTexture();

                // コマンドバッファの設定
                SetupCommandBuffer();

                // VRCFury互換性の初期化
                InitializeVRCFuryCompatibility();

                // シェーダー値の更新
                UpdateShaderValues();

                isSetup = true;
                Debug.Log($"PCSS Light Core setup complete on {gameObject.name}", this);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in PCSS Light Setup on {gameObject.name}: {ex.Message}\nStackTrace: {ex.StackTrace}");
                isSetup = false;
            }
        }

        private void InitializeVRCFuryCompatibility()
        {
            if (_isVRCFuryInitialized) return;

            try
            {
                if (_avatarDescriptor != null)
                {
                    // シェーダーキーワードの設定
                    Shader.EnableKeyword("_PCSS_ON");
                    Shader.SetGlobalFloat(PCSS_ENABLED_PARAM, 1f);
                    Shader.SetGlobalFloat(PCSS_INTENSITY_PARAM, 1f);
                    Shader.SetGlobalFloat(PCSS_SOFTNESS_PARAM, Softness);

                    _isVRCFuryInitialized = true;
                    Debug.Log("VRCFury compatibility initialized successfully.", this);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error initializing VRCFury compatibility: {ex.Message}");
            }
        }

        public void CreateShadowRenderTexture()
        {
            if (shadowRenderTexture != null)
            {
                DestroyShadowRenderTexture();
            }

            try
            {
                shadowRenderTexture = new RenderTexture(resolution, resolution, 0, format)
                {
                    filterMode = filterMode,
                    useMipMap = false,
                    antiAliasing = (int)MSAA
                };
                shadowRenderTexture.Create();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error creating shadow render texture: {ex.Message}");
            }
        }

        public void DestroyShadowRenderTexture()
        {
            if (shadowRenderTexture != null)
            {
                try
                {
                    shadowRenderTexture.Release();
                    DestroyImmediate(shadowRenderTexture);
                    shadowRenderTexture = null;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error destroying shadow render texture: {ex.Message}");
                }
            }
        }

        public void UpdateShaderValues()
        {
            try
            {
                if (!_isVRCFuryInitialized)
                {
                    InitializeVRCFuryCompatibility();
                }

                // VRCFuryパラメータの更新
                if (_isVRCFuryInitialized)
                {
                    Shader.SetGlobalFloat(PCSS_ENABLED_PARAM, enabled ? 1f : 0f);
                    Shader.SetGlobalFloat(PCSS_INTENSITY_PARAM, lightComponent ? lightComponent.intensity : 0f);
                    Shader.SetGlobalFloat(PCSS_SOFTNESS_PARAM, Softness);
                }

                Shader.SetGlobalInt("Blocker_Samples", Blocker_SampleCount);
                Shader.SetGlobalInt("PCF_Samples", PCF_SampleCount);

                if (shadowRenderTexture)
                {
                    if (shadowRenderTexture.format != format || shadowRenderTexture.antiAliasing != (int)MSAA)
                        CreateShadowRenderTexture();
                    else
                    {
                        shadowRenderTexture.filterMode = filterMode;
                    }
                }

                // シェーダーパラメータの更新
                float scaledSoftness = Softness / 64f / Mathf.Sqrt(QualitySettings.shadowDistance);
                Shader.SetGlobalFloat("Softness", scaledSoftness);
                Shader.SetGlobalFloat("SoftnessFalloff", Mathf.Exp(SoftnessFalloff));
                SetFlag("USE_FALLOFF", SoftnessFalloff > Mathf.Epsilon);

                Shader.SetGlobalFloat("RECEIVER_PLANE_MIN_FRACTIONAL_ERROR", MaxStaticGradientBias);
                Shader.SetGlobalFloat("Blocker_GradientBias", Blocker_GradientBias);
                Shader.SetGlobalFloat("PCF_GradientBias", PCF_GradientBias);

                SetFlag("USE_CASCADE_BLENDING", CascadeBlendDistance > 0);
                Shader.SetGlobalFloat("CascadeBlendDistance", CascadeBlendDistance);

                SetFlag("USE_STATIC_BIAS", MaxStaticGradientBias > 0);
                SetFlag("USE_BLOCKER_BIAS", Blocker_GradientBias > 0);
                SetFlag("USE_PCF_BIAS", PCF_GradientBias > 0);

                if (noiseTexture)
                {
                    Shader.SetGlobalVector("NoiseCoords", new Vector4(1f / noiseTexture.width, 1f / noiseTexture.height, 0f, 0f));
                    Shader.SetGlobalTexture("_NoiseTexture", noiseTexture);
                }

                SetFlag("ORTHOGRAPHIC_SUPPORTED", supportOrthographicProjection);

                int maxSamples = Mathf.Max(Blocker_SampleCount, PCF_SampleCount);
                SetFlag("POISSON_32", maxSamples < 33);
                SetFlag("POISSON_64", maxSamples > 33);

                Matrix4x4 worldToLight = lightComponent.transform.worldToLocalMatrix;
                Matrix4x4 projection = Matrix4x4.Perspective(lightComponent.spotAngle, 1f, lightComponent.shadowNearPlane, lightComponent.range);
                
                Shader.SetGlobalMatrix("_WorldToLight", worldToLight);
                Shader.SetGlobalMatrix("_LightProjection", projection);
                Shader.SetGlobalFloat("_LightRange", lightComponent.range);
                Shader.SetGlobalFloat("_ShadowBias", lightComponent.shadowBias);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error updating shader values: {ex.Message}");
            }
        }

        public void UpdateCommandBuffer()
        {
            if (commandBuffer == null || lightComponent == null) return;

            commandBuffer.Clear();
            commandBuffer.SetGlobalTexture("_ShadowMap", BuiltinRenderTextureType.CurrentActive);
        }

        private void SetFlag(string shaderKeyword, bool value)
        {
            try
            {
                if (value)
                    Shader.EnableKeyword(shaderKeyword);
                else
                    Shader.DisableKeyword(shaderKeyword);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error setting shader keyword {shaderKeyword}: {ex.Message}");
            }
        }

        private void OnValidate()
        {
            if (isSetup)
            {
                UpdateShaderValues();
                UpdateCommandBuffer();
            }
        }

        private void OnEnable()
        {
            if (!isSetup)
            {
                Setup();
            }
            UpdateShaderValues();
        }

        private void OnDisable()
        {
            if (lightComponent && copyShadowBuffer != null)
            {
                lightComponent.RemoveCommandBuffer(lightEvent, copyShadowBuffer);
            }
            
            // VRCFuryパラメータをリセット
            if (_isVRCFuryInitialized)
            {
                Shader.SetGlobalFloat(PCSS_ENABLED_PARAM, 0f);
                Shader.SetGlobalFloat(PCSS_INTENSITY_PARAM, 0f);
            }
        }

        private void OnDestroy()
        {
            DestroyShadowRenderTexture();
            if (lightComponent && copyShadowBuffer != null)
            {
                lightComponent.RemoveCommandBuffer(lightEvent, copyShadowBuffer);
            }
        }

        // Added to support mirror state notification from PCSSLightPlugin
        public void SetMirrorState(bool inMirror)
        {
            if (_isInMirror != inMirror)
            {
                _isInMirror = inMirror;
                
                // ミラー内での描画設定を最適化
                if (_isInMirror)
                {
                    // ミラー内でのパフォーマンス最適化
                    Blocker_SampleCount = Mathf.Min(Blocker_SampleCount, 32);
                    PCF_SampleCount = Mathf.Min(PCF_SampleCount, 32);
                }
                
                UpdateShaderValues();
                UpdateCommandBuffer();
            }
        }

        private void SetupCommandBuffer()
        {
            if (copyShadowBuffer != null)
            {
                lightComponent.RemoveCommandBuffer(lightEvent, copyShadowBuffer);
            }

            copyShadowBuffer = new CommandBuffer();
            copyShadowBuffer.name = "PCSS Shadows";
            lightComponent.AddCommandBuffer(lightEvent, copyShadowBuffer);
        }

        public float GetParameterValue(string paramName)
        {
            if (paramName == PCSS_ENABLED_PARAM)
                return enabled ? 1f : 0f;
            if (paramName == PCSS_INTENSITY_PARAM)
                return lightComponent ? lightComponent.intensity : 0f;
            if (paramName == PCSS_SOFTNESS_PARAM)
                return Softness;
            return 0f;
        }
    }
}