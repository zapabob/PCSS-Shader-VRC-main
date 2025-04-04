// PCSSLight.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
// using nadena.dev.modular_avatar.core; // Removed as MA setup is handled by PCSSLightPlugin
using VRC.SDK3.Avatars.Components;

namespace PCSS.Core // Changed namespace to PCSS.Core
{
    [AddComponentMenu("PCSS/PCSS Light")]
    [RequireComponent(typeof(Light))]
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
        private Light lightComponent;
        private CommandBuffer commandBuffer;
        private RenderTexture shadowMap;
        private bool isSetup = false;

        // Added to support mirror state notification from PCSSLightPlugin
        private bool _isInMirror = false;
        
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
                lightComponent = GetComponent<Light>();
                if (lightComponent == null)
                {
                    Debug.LogError("Light component not found on PCSS Light object.");
                    return;
                }

                resolution = Mathf.ClosestPowerOfTwo(resolution);
                if (customShadowResolution)
                    lightComponent.shadowCustomResolution = resolution;
                else
                    lightComponent.shadowCustomResolution = 0;

                shadowmapPropID = Shader.PropertyToID("_ShadowMap");

                if (copyShadowBuffer != null)
                {
                    lightComponent.RemoveCommandBuffer(lightEvent, copyShadowBuffer);
                }

                copyShadowBuffer = new CommandBuffer();
                copyShadowBuffer.name = "PCSS Shadows";

                lightComponent.AddCommandBuffer(lightEvent, copyShadowBuffer);

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

        private void OnDisable()
        {
            if (lightComponent && copyShadowBuffer != null)
            {
                lightComponent.RemoveCommandBuffer(lightEvent, copyShadowBuffer);
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
                // If mirror state changes, we may need to update rendering
                // This is a simplified implementation - add mirror-specific logic as needed
                UpdateShaderValues();
                UpdateCommandBuffer();
            }
        }
    }
}