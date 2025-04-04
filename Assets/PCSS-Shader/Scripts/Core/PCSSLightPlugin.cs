// PCSSLightPlugin.cs
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using VRC.SDK3.Avatars.ScriptableObjects;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PCSS.Core
{
    [AddComponentMenu("PCSS/PCSS Light Plugin")]
    [ExecuteInEditMode]
    [RequireComponent(typeof(Light))]
    [RequireComponent(typeof(PCSSLight))]
    public class PCSSLightPlugin : MonoBehaviour
    {
        private const float PCSS_EFFECT_NEAR_DISTANCE = 10.0f;
        private const float PCSS_EFFECT_FADE_DISTANCE = 5.0f;
        private const float PCSS_EFFECT_FAR_DISTANCE = PCSS_EFFECT_NEAR_DISTANCE + PCSS_EFFECT_FADE_DISTANCE;
        private const int MAX_SHADOW_STRENGTH_CACHE = 100;
        private const string MIRROR_REFLECTION_LAYER_NAME = "MirrorReflection";
        private const string PCSS_ENABLED_PARAMETER_NAME = "PCSS_Light_Enabled";

        // VRCFury互換のパラメータ名
        private const string PCSS_PARAM_PREFIX = "PCSS_";
        private const string PCSS_ENABLED_PARAM = PCSS_PARAM_PREFIX + "Enabled";
        private const string PCSS_INTENSITY_PARAM = PCSS_PARAM_PREFIX + "Intensity";
        private const string PCSS_SOFTNESS_PARAM = PCSS_PARAM_PREFIX + "Softness";

        [Header("PCSS 設定")]
        [Tooltip("シャドウマップの解像度。カスタム解像度を使用する場合に適用されます。")]
        public int resolution = 4096;
        [Tooltip("Unityのデフォルト解像度ではなく、上記の解像度設定を使用します。")]
        public bool customShadowResolution = false;

        [Tooltip("影の生成をブロックするオブジェクト（ブロッカー）を検出するためのサンプル数。")]
        [Range(1, 64)]
        public int blockerSampleCount = 16;
        [Tooltip("PCF（Percentage-Closer Filtering）による影のエッジを滑らかにするためのサンプル数。")]
        [Range(1, 64)]
        public int PCFSampleCount = 16;

        [Tooltip("影の計算にランダム性を加えるためのノイズテクスチャ。")]
        public Texture2D noiseTexture;

        [Tooltip("影の全体的な柔らかさ。")]
        [Range(0f, 1f)]
        public float softness = 0.5f;
        [Tooltip("影のブロッキングを検出する範囲（半径）。")]
        [Range(0f, 1f)]
        public float sampleRadius = 0.02f;

        [Tooltip("静的な表面でのシャドウアクネ（自己遮蔽による縞模様）を軽減するためのバイアス。")]
        [Range(0f, 1f)]
        public float maxStaticGradientBias = 0.05f;
        [Tooltip("ブロッカー検索時のシャドウアクネを軽減するためのバイアス。")]
        [Range(0f, 1f)]
        public float blockerGradientBias = 0f;
        [Tooltip("PCF計算時のシャドウアクネを軽減するためのバイアス。")]
        [Range(0f, 1f)]
        public float PCFGradientBias = 1f;

        [Tooltip("カスケードシャドウマップを使用する場合の、異なるカスケード間のブレンド距離。")]
        [Range(0f, 1f)]
        public float cascadeBlendDistance = 0.5f;

        [Tooltip("正射影（Orthographic）カメラでの描画をサポートします。")]
        public bool supportOrthographicProjection;

        private RenderTexture _shadowRenderTexture;
        private readonly RenderTextureFormat _format = RenderTextureFormat.RFloat;
        private readonly FilterMode _filterMode = FilterMode.Bilinear;

        private int _shadowmapPropID;
        private CommandBuffer _copyShadowBuffer;

        private Light _lightComponent;
        private PCSSLight _pcssLight;
        private VRCAvatarDescriptor _localAvatarDescriptor;

        private readonly Dictionary<int, float> _shadowStrengthCache = new Dictionary<int, float>();
        private bool _isInitialized = false;
        private bool _isInMirror = false;
        private int _mirrorLayer = -1;

        private VRCExpressionParameters _expressionParameters;
        private bool _parametersInitialized = false;

        private void Awake()
        {
            _lightComponent = GetComponent<Light>();
            _pcssLight = GetComponent<PCSSLight>();
            _shadowmapPropID = Shader.PropertyToID("_PCSShadowMap");
            _mirrorLayer = LayerMask.NameToLayer(MIRROR_REFLECTION_LAYER_NAME);

            if (_pcssLight == null)
            {
                Debug.LogError("PCSSLight component not found. Disabling PCSSLightPlugin.", this);
                enabled = false;
            }
            if (_lightComponent == null)
            {
                Debug.LogError("Light component not found. Disabling PCSSLightPlugin.", this);
                enabled = false;
            }
        }

        private void Start()
        {
            if (enabled && !_isInitialized)
            {
                InitializePCSS();
                InitializeVRCFuryParameters();
            }
        }

        private void OnEnable()
        {
            if (enabled && !_isInitialized)
            {
                InitializePCSS();
            }
            else if (enabled && _isInitialized)
            {
                SetupLightComponentResources();
                AddCommandBuffer();
            }
        }

        private void OnDisable()
        {
            CleanupLightComponentResources();
            _isInitialized = false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_lightComponent == null) _lightComponent = GetComponent<Light>();
            if (_pcssLight == null) _pcssLight = GetComponent<PCSSLight>();

            if (isActiveAndEnabled && _lightComponent != null && _pcssLight != null)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this == null || _lightComponent == null || _pcssLight == null) return;

                    SetupLightComponentSettings();
                    RecreateRenderTextureIfNeeded();
                    UpdateShaderProperties();
                    UpdateCommandBuffer();
                    _pcssLight.Setup();
                    if (!_isInitialized) _isInitialized = true;
                    Debug.Log("PCSSLightPlugin settings validated and applied.", this);
                };
            }

            if (GetComponentInParent<VRCAvatarDescriptor>(true) == null)
            {
                Debug.LogWarning("PCSSLightPlugin is recommended to be on a GameObject within a VRCAvatarDescriptor hierarchy.", this);
            }
        }
#endif

        private void Update()
        {
            if (!_isInitialized || !enabled) return;

            try
            {
                bool currentIsInMirror = IsInMirror();
                if (currentIsInMirror != _isInMirror)
                {
                    _isInMirror = currentIsInMirror;
                    _pcssLight.SetMirrorState(_isInMirror);
                }

                _pcssLight.UpdateShaderValues();
                _pcssLight.UpdateCommandBuffer();

                UpdateShaderProperties();
                UpdateCommandBuffer();

                UpdatePCSSEffectBasedOnDistance();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during PCSSLightPlugin Update: {ex.Message}\n{ex.StackTrace}", this);
            }
        }

        private void InitializePCSS()
        {
            if (_isInitialized || !enabled) return;
            if (_lightComponent == null || _pcssLight == null)
            {
                Debug.LogError("Cannot initialize PCSS: Missing Light or PCSSLight component.", this);
                enabled = false;
                return;
            }

            Debug.Log("Initializing PCSSLightPlugin...", this);
            try
            {
                _pcssLight.Setup();
                SetupLightComponentSettings();
                SetupLightComponentResources();
                AddCommandBuffer();
                _isInitialized = true;
                Debug.Log("PCSSLightPlugin Initialized Successfully.", this);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize PCSSLightPlugin: {ex.Message}\n{ex.StackTrace}", this);
                CleanupLightComponentResources();
                _isInitialized = false;
                enabled = false;
            }
        }

        private void SetupLightComponentSettings()
        {
            if (_lightComponent == null) return;

            if (customShadowResolution)
            {
                resolution = Mathf.Max(32, resolution);
                _lightComponent.shadowCustomResolution = resolution;
            }
            else
            {
                _lightComponent.shadowCustomResolution = 0;
            }
            if (_lightComponent.shadows == LightShadows.None)
            {
                Debug.LogWarning("PCSSLightPlugin requires shadows to be enabled on the Light component. Setting to Hard Shadows.", this);
                _lightComponent.shadows = LightShadows.Hard;
            }
        }

        private void SetupLightComponentResources()
        {
            if (_lightComponent == null) return;

            if (_copyShadowBuffer == null)
            {
                _copyShadowBuffer = new CommandBuffer { name = "PCSS Shadow Copy" };
            }

            RecreateRenderTextureIfNeeded();
        }

        private void AddCommandBuffer()
        {
            if (_lightComponent != null && _copyShadowBuffer != null)
            {
                _lightComponent.RemoveCommandBuffer(LightEvent.AfterShadowMap, _copyShadowBuffer);
                _lightComponent.AddCommandBuffer(LightEvent.AfterShadowMap, _copyShadowBuffer);
            }
        }

        private void CleanupLightComponentResources()
        {
            if (_lightComponent != null && _copyShadowBuffer != null)
            {
                try
                {
                    _lightComponent.RemoveCommandBuffer(LightEvent.AfterShadowMap, _copyShadowBuffer);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Ignoring error removing command buffer: {e.Message}");
                }
            }

            CleanupRenderTexture();

            _shadowStrengthCache.Clear();
        }

        private int GetCurrentShadowResolution()
        {
            if (_lightComponent == null) return 0;
            
            if (_lightComponent.shadowCustomResolution > 0)
                return _lightComponent.shadowCustomResolution;
                
            switch (QualitySettings.shadowResolution)
            {
                case ShadowResolution.Low:
                    return 1024;
                case ShadowResolution.Medium:
                    return 2048;
                case ShadowResolution.High:
                    return 4096;
                case ShadowResolution.VeryHigh:
                    return 8192;
                default:
                    return 2048;
            }
        }

        private void RecreateRenderTextureIfNeeded()
        {
            if (_lightComponent == null) return;

            int currentResolution = GetCurrentShadowResolution();
            if (currentResolution <= 0) {
                currentResolution = 512;
                Debug.LogWarning("Could not determine valid shadow resolution. Using fallback: " + currentResolution, this);
            }

            bool needsRecreation = _shadowRenderTexture == null ||
                                   _shadowRenderTexture.width != currentResolution ||
                                   _shadowRenderTexture.height != currentResolution ||
                                   _shadowRenderTexture.format != _format ||
                                   !_shadowRenderTexture.IsCreated();

            if (needsRecreation)
            {
                CleanupRenderTexture();

                try
                {
                    _shadowRenderTexture = new RenderTexture(currentResolution, currentResolution, 0, _format)
                    {
                        filterMode = _filterMode,
                        useMipMap = false,
                        autoGenerateMips = false,
                        name = "PCSS Shadow Copy RT " + GetInstanceID()
                    };

                    if (!_shadowRenderTexture.Create())
                    {
                        Debug.LogError("Failed to create PCSS shadow RenderTexture.", this);
                        CleanupRenderTexture();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error creating PCSS shadow RenderTexture: {e.Message}\n{e.StackTrace}", this);
                    CleanupRenderTexture();
                }
            }
        }

        private void CleanupRenderTexture()
        {
            if (_shadowRenderTexture != null)
            {
                if (_shadowRenderTexture.IsCreated())
                {
                    _shadowRenderTexture.Release();
                }
                if (Application.isPlaying)
                    Destroy(_shadowRenderTexture);
                else
                    DestroyImmediate(_shadowRenderTexture);

                _shadowRenderTexture = null;
            }
        }

        private void UpdateShaderProperties()
        {
            if (!enabled) return;
            try
            {
                Shader.SetGlobalInt("_PCSSBlockerSampleCount", blockerSampleCount);
                Shader.SetGlobalInt("_PCSSPCFSampleCount", PCFSampleCount);
                Shader.SetGlobalFloat("_PCSSoftness", softness);
                Shader.SetGlobalFloat("_PCSSSampleRadius", sampleRadius);
                Shader.SetGlobalFloat("_PCSSMaxStaticGradientBias", maxStaticGradientBias);
                Shader.SetGlobalFloat("_PCSSBlockerGradientBias", blockerGradientBias);
                Shader.SetGlobalFloat("_PCSSPCFGradientBias", PCFGradientBias);
                Shader.SetGlobalFloat("_PCSSCascadeBlendDistance", cascadeBlendDistance);
                Shader.SetGlobalInt("_PCSSupportOrthographicProjection", supportOrthographicProjection ? 1 : 0);

                if (noiseTexture != null)
                {
                    Vector4 noiseCoords = Vector4.zero;
                    if (noiseTexture.width > 0 && noiseTexture.height > 0)
                    {
                        noiseCoords = new Vector4(1.0f / noiseTexture.width, 1.0f / noiseTexture.height, 0f, 0f);
                    }
                    Shader.SetGlobalVector("_PCSSNoiseCoords", noiseCoords);
                    Shader.SetGlobalTexture("_PCSSNoiseTexture", noiseTexture);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error updating global shader properties: {e.Message}\n{e.StackTrace}", this);
            }
        }

        private void UpdateCommandBuffer()
        {
            if (!enabled || _copyShadowBuffer == null || _shadowRenderTexture == null || !_shadowRenderTexture.IsCreated())
            {
                return;
            }

            try
            {
                _copyShadowBuffer.Clear();
                _copyShadowBuffer.SetShadowSamplingMode(BuiltinRenderTextureType.CurrentActive, ShadowSamplingMode.RawDepth);
                _copyShadowBuffer.Blit(BuiltinRenderTextureType.CurrentActive, new RenderTargetIdentifier(_shadowRenderTexture));
                _copyShadowBuffer.SetGlobalTexture(_shadowmapPropID, new RenderTargetIdentifier(_shadowRenderTexture));
            }
            catch (Exception e)
            {
                Debug.LogError($"Error updating PCSS command buffer: {e.Message}\n{e.StackTrace}", this);
            }
        }

        private void UpdatePCSSEffectBasedOnDistance()
        {
            float shadowStrength = 1.0f;
            try
            {
                if (_localAvatarDescriptor == null)
                {
                    _localAvatarDescriptor = GetLocalPlayerAvatarDescriptor();
                }

                if (_localAvatarDescriptor != null)
                {
                    float distance = Vector3.Distance(transform.position, _localAvatarDescriptor.transform.position);
                    shadowStrength = CalculateShadowStrength(distance);
                }

                // VRCFuryパラメータの更新
                SetParameterValue(PCSS_INTENSITY_PARAM, shadowStrength);
                SetParameterValue(PCSS_SOFTNESS_PARAM, softness);
                
                Shader.SetGlobalFloat("_PCSShadowStrength", shadowStrength);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error updating PCSS effect: {e.Message}\n{e.StackTrace}", this);
                Shader.SetGlobalFloat("_PCSShadowStrength", 1.0f);
            }
        }

        private float CalculateShadowStrength(float distance)
        {
            if (distance <= PCSS_EFFECT_NEAR_DISTANCE)
                return 1.0f;
            if (distance >= PCSS_EFFECT_FAR_DISTANCE)
                return 0.0f;
            
            float t = Mathf.InverseLerp(PCSS_EFFECT_NEAR_DISTANCE, PCSS_EFFECT_FAR_DISTANCE, distance);
            return Mathf.Lerp(1.0f, 0.0f, t);
        }

        public void SetParameterValue(string paramName, float value)
        {
            try
            {
                if (_expressionParameters != null)
                {
                    foreach (var param in _expressionParameters.parameters)
                    {
                        if (param.name == paramName)
                        {
                            param.defaultValue = value;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error setting parameter {paramName}: {ex.Message}", this);
            }
        }

        private VRCAvatarDescriptor GetLocalPlayerAvatarDescriptor()
        {
            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            if (localPlayer == null) return null;

            VRCAvatarDescriptor[] allDescriptors = FindObjectsOfType<VRCAvatarDescriptor>();
            foreach (VRCAvatarDescriptor descriptor in allDescriptors)
            {
                var vrcPlayer = descriptor.GetComponentInParent<VRC.SDKBase.VRCPlayerApi>();
                if (vrcPlayer != null && vrcPlayer.playerId == localPlayer.playerId)
                {
                    return descriptor;
                }
            }

            return null;
        }

        private bool IsInMirror()
        {
            bool inVR = Networking.LocalPlayer?.IsUserInVR() ?? false;
            bool onMirrorLayer = (_mirrorLayer != -1) && (gameObject.layer == _mirrorLayer);

            return inVR && onMirrorLayer;
        }

        private void InitializeVRCFuryParameters()
        {
            if (_parametersInitialized) return;

            try
            {
                var avatar = GetComponentInParent<VRCAvatarDescriptor>();
                if (avatar != null && avatar.expressionParameters != null)
                {
                    _expressionParameters = avatar.expressionParameters;
                    
                    // パラメータの追加
                    AddParameterIfNotExists(PCSS_ENABLED_PARAM, VRCExpressionParameters.ValueType.Bool, 1f);
                    AddParameterIfNotExists(PCSS_INTENSITY_PARAM, VRCExpressionParameters.ValueType.Float, 1f);
                    AddParameterIfNotExists(PCSS_SOFTNESS_PARAM, VRCExpressionParameters.ValueType.Float, softness);

                    _parametersInitialized = true;
                    Debug.Log("VRCFury parameters initialized successfully.", this);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error initializing VRCFury parameters: {ex.Message}\n{ex.StackTrace}", this);
            }
        }

        private void AddParameterIfNotExists(string paramName, VRCExpressionParameters.ValueType valueType, float defaultValue)
        {
#if UNITY_EDITOR
            if (_expressionParameters == null) return;

            bool paramExists = false;
            foreach (var param in _expressionParameters.parameters)
            {
                if (param.name == paramName)
                {
                    paramExists = true;
                    break;
                }
            }

            if (!paramExists)
            {
                var newParams = new VRCExpressionParameters.Parameter[_expressionParameters.parameters.Length + 1];
                Array.Copy(_expressionParameters.parameters, newParams, _expressionParameters.parameters.Length);
                
                newParams[newParams.Length - 1] = new VRCExpressionParameters.Parameter
                {
                    name = paramName,
                    valueType = valueType,
                    defaultValue = defaultValue,
                    saved = true
                };

                _expressionParameters.parameters = newParams;
                EditorUtility.SetDirty(_expressionParameters);
            }
#endif
        }
    }
}