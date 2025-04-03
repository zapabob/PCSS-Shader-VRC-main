// PCSSLight.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using nadena.dev.modular_avatar.core;
using VRC.SDK3.Avatars.Components;

[AddComponentMenu("PCSS/PCSS Light")]
public class PCSSLight : MonoBehaviour
{
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
    [HideInInspector]
    public Light _light;
    private bool isSetup = false;

    private void OnEnable()
    {
        // モジュラーアバター対応のセットアップ
        SetupModularAvatarComponents();
    }

    private void SetupModularAvatarComponents()
    {
        // MAParameterの設定がまだない場合は追加
        var maParam = GetComponent<MAParameter>();
        if (maParam == null)
        {
            maParam = gameObject.AddComponent<MAParameter>();
            maParam.nameOrPrefix = "PCSS_Quality";
            maParam.syncType = VRCExpressionParameters.ValueType.Int;
            maParam.defaultValue = (int)MSAA;
            maParam.saved = true;
        }

        // MASaveParamの設定
        var maSave = GetComponent<MASaveParam>();
        if (maSave == null)
        {
            maSave = gameObject.AddComponent<MASaveParam>();
            maSave.parameter = "PCSS_Quality";
            maSave.defaultValue = (int)MSAA;
            maSave.syncWithOtherParams = true;
        }
    }

    private void Start()
    {
        if (!isSetup)
        {
            Setup();
        }
    }

    public void Setup()
    {
        try
        {
            _light = GetComponent<Light>();
            if (!_light)
            {
                Debug.LogError("Light component not found on PCSS Light object.");
                return;
            }

            resolution = Mathf.ClosestPowerOfTwo(resolution);
            if (customShadowResolution)
                _light.shadowCustomResolution = resolution;
            else
                _light.shadowCustomResolution = 0;

            shadowmapPropID = Shader.PropertyToID("_ShadowMap");

            if (copyShadowBuffer != null)
            {
                _light.RemoveCommandBuffer(lightEvent, copyShadowBuffer);
            }

            copyShadowBuffer = new CommandBuffer();
            copyShadowBuffer.name = "PCSS Shadows";

            _light.AddCommandBuffer(lightEvent, copyShadowBuffer);

            CreateShadowRenderTexture();
            UpdateShaderValues();
            UpdateCommandBuffer();

            isSetup = true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error in PCSS Light Setup: {ex.Message}");
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
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error updating shader values: {ex.Message}");
        }
    }

    public void UpdateCommandBuffer()
    {
        if (!_light)
            return;

        try
        {
            copyShadowBuffer.Clear();
            copyShadowBuffer.SetShadowSamplingMode(BuiltinRenderTextureType.CurrentActive, ShadowSamplingMode.RawDepth);
            copyShadowBuffer.Blit(BuiltinRenderTextureType.CurrentActive, shadowRenderTexture);
            copyShadowBuffer.SetGlobalTexture(shadowmapPropID, shadowRenderTexture);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error updating command buffer: {ex.Message}");
        }
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

    private void OnDisable()
    {
        if (_light && copyShadowBuffer != null)
        {
            _light.RemoveCommandBuffer(lightEvent, copyShadowBuffer);
        }
    }

    private void OnDestroy()
    {
        DestroyShadowRenderTexture();
        if (_light && copyShadowBuffer != null)
        {
            _light.RemoveCommandBuffer(lightEvent, copyShadowBuffer);
        }
    }
}