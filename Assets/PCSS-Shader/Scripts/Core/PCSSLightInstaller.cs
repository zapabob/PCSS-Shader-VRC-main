using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Avatars.Components;
using PCSS.Core;

namespace PCSS.Core
{
    [AddComponentMenu("PCSS/PCSS Light Installer")]
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VRCAvatarDescriptor))]
    public class PCSSLightInstaller : MonoBehaviour
    {
        [SerializeField]
        private bool _autoSetup = true;

        // VRCFury互換のパラメータ
        private const string PCSS_PARAM_PREFIX = "PCSS_";
        private const string PCSS_ENABLED_PARAM = PCSS_PARAM_PREFIX + "Enabled";
        private const string PCSS_INTENSITY_PARAM = PCSS_PARAM_PREFIX + "Intensity";

        [SerializeField, Range(0f, 1f)]
        private float _defaultIntensity = 1f;

        private void OnEnable()
        {
            if (_autoSetup && Application.isPlaying)
            {
                SetupPCSSLight();
            }
        }

        private void SetupPCSSLight()
        {
            try
            {
                // Lightコンポーネントが存在しなければ追加
                var light = gameObject.GetComponent<Light>();
                if (light == null)
                {
                    light = gameObject.AddComponent<Light>();
                    light.type = LightType.Directional;
                    light.shadows = LightShadows.Soft;
                }

                // PCSSLightコンポーネントを追加
                var pcssLight = gameObject.GetComponent<PCSSLight>() ?? gameObject.AddComponent<PCSSLight>();
                if (pcssLight != null)
                {
                    pcssLight.Setup();

                    // PCSSLightPluginコンポーネントを追加
                    var plugin = gameObject.GetComponent<PCSSLightPlugin>() ?? gameObject.AddComponent<PCSSLightPlugin>();
                    if (plugin != null)
                    {
                        SetupVRCFuryParameters(plugin);
                        Debug.Log("PCSS Light setup complete with lilToon and VRCFury compatibility.", this);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error setting up PCSS Light: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private void SetupVRCFuryParameters(PCSSLightPlugin plugin)
        {
            try
            {
                // VRCFuryパラメータの設定
                var avatar = GetComponent<VRCAvatarDescriptor>();
                if (avatar != null)
                {
                    // パラメータの永続性を確保
                    plugin.enabled = true;
                    plugin.SetParameterValue(PCSS_ENABLED_PARAM, 1f);
                    plugin.SetParameterValue(PCSS_INTENSITY_PARAM, _defaultIntensity);

                    // シェーダーキーワードの設定
                    Shader.EnableKeyword("_PCSS_ON");
                    Shader.SetGlobalFloat("_PCSSEnabled", 1f);
                    Shader.SetGlobalFloat("_PCSSIntensity", _defaultIntensity);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error setting up VRCFury parameters: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private void OnValidate()
        {
            // アバターのルートに配置されているか確認
            var avatar = GetComponent<VRCAvatarDescriptor>();
            if (avatar == null)
            {
                Debug.LogWarning("VRCAvatarDescriptorが見つかりません。アバターのルートに配置してください。");
                return;
            }

            // lilToon互換性チェック
            if (!Application.isPlaying)
            {
                var light = GetComponent<Light>();
                if (light != null && light.shadows == LightShadows.None)
                {
                    Debug.LogWarning("シャドウが無効になっています。PCSSの機能を使用するにはシャドウを有効にしてください。");
                }
            }

            // VRCFury互換性チェック
            ValidateVRCFuryCompatibility();
        }

        private void ValidateVRCFuryCompatibility()
        {
            try
            {
                var plugin = GetComponent<PCSSLightPlugin>();
                if (plugin != null)
                {
                    // シェーダーキーワードとパラメータの整合性チェック
                    bool isEnabled = Shader.IsKeywordEnabled("_PCSS_ON");
                    float intensity = Shader.GetGlobalFloat("_PCSSIntensity");

                    if (!isEnabled)
                    {
                        Debug.LogWarning("PCSSシェーダーキーワードが無効です。VRCFuryの自動修正により無効化された可能性があります。");
                    }

                    if (Mathf.Approximately(intensity, 0f))
                    {
                        Debug.LogWarning("PCSSの強度が0に設定されています。VRCFuryの自動修正により変更された可能性があります。");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error validating VRCFury compatibility: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("PCSS/Setup PCSS Light")]
        private static void SetupPCSSLightMenuItem()
        {
            var selection = UnityEditor.Selection.activeGameObject;
            if (selection == null)
            {
                Debug.LogError("ゲームオブジェクトが選択されていません。");
                return;
            }

            var installer = selection.GetComponent<PCSSLightInstaller>();
            if (installer == null)
            {
                installer = selection.AddComponent<PCSSLightInstaller>();
            }

            installer.SetupPCSSLight();
        }
#endif
    }
}
