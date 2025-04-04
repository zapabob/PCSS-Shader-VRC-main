using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
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

        [Header("アニメーション設定")]
        [SerializeField]
        private string _toggleParameterName = "PCSSLightToggle";
        
        [SerializeField]
        private string _menuPath = "PCSS Light/Toggle";

        private VRCExpressionParameters _expressionParameters;
        private Light _light;
        private PCSSLightPlugin _plugin;

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
                // 1. Lightコンポーネントのセットアップ
                _light = gameObject.GetComponent<Light>();
                if (_light == null)
                {
                    _light = gameObject.AddComponent<Light>();
                }
                
                // Lightコンポーネントの設定
                _light.type = LightType.Directional;
                _light.shadows = LightShadows.Soft;

                // 2. PCSSLightコンポーネントのセットアップ
                var pcssLight = gameObject.GetComponent<PCSSLight>();
                if (pcssLight == null)
                {
                    pcssLight = gameObject.AddComponent<PCSSLight>();
                }

                if (pcssLight != null)
                {
                    pcssLight.Setup();

                    // 3. PCSSLightPluginコンポーネントのセットアップ
                    _plugin = gameObject.GetComponent<PCSSLightPlugin>();
                    if (_plugin == null)
                    {
                        _plugin = gameObject.AddComponent<PCSSLightPlugin>();
                    }

                    if (_plugin != null)
                    {
                        SetupVRCFuryParameters(_plugin);
                        SetupToggleParameter();
                        Debug.Log("PCSS Light setup complete with lilToon and VRCFury compatibility.", this);
                    }
                    else
                    {
                        Debug.LogError("Failed to add PCSSLightPlugin component.", this);
                    }
                }
                else
                {
                    Debug.LogError("Failed to add PCSSLight component.", this);
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
                var avatar = GetComponent<VRCAvatarDescriptor>();
                if (avatar != null)
                {
                    // パラメータの永続性を確保
                    plugin.enabled = true;
                    plugin.SetParameterValue(PCSS_ENABLED_PARAM, 1f);
                    plugin.SetParameterValue(PCSS_INTENSITY_PARAM, _defaultIntensity);
                    plugin.SetParameterValue(_toggleParameterName, 1f);

                    // シェーダーキーワードの設定
                    Shader.EnableKeyword("_PCSS_ON");
                    Shader.SetGlobalFloat("_PCSSEnabled", 1f);
                    Shader.SetGlobalFloat("_PCSSIntensity", _defaultIntensity);

                    // VRCFuryのパラメータ設定
                    SetupToggleParameter();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error setting up VRCFury parameters: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private void SetupToggleParameter()
        {
#if UNITY_EDITOR
            try
            {
                var avatar = GetComponent<VRCAvatarDescriptor>();
                if (avatar != null && avatar.expressionParameters != null)
                {
                    _expressionParameters = avatar.expressionParameters;
                    
                    // トグルパラメータの追加
                    var param = new VRCExpressionParameters.Parameter
                    {
                        name = _toggleParameterName,
                        valueType = VRCExpressionParameters.ValueType.Bool,
                        defaultValue = 1f,
                        saved = true
                    };

                    // 既存のパラメータをチェック
                    bool paramExists = false;
                    for (int i = 0; i < _expressionParameters.parameters.Length; i++)
                    {
                        if (_expressionParameters.parameters[i].name == _toggleParameterName)
                        {
                            paramExists = true;
                            break;
                        }
                    }

                    // パラメータが存在しない場合は追加
                    if (!paramExists)
                    {
                        var newParams = new VRCExpressionParameters.Parameter[_expressionParameters.parameters.Length + 1];
                        System.Array.Copy(_expressionParameters.parameters, newParams, _expressionParameters.parameters.Length);
                        newParams[newParams.Length - 1] = param;
                        _expressionParameters.parameters = newParams;
                        UnityEditor.EditorUtility.SetDirty(_expressionParameters);
                    }

                    // メニューアイテムの作成
                    CreateToggleMenuItem();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error setting up toggle parameter: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
#endif
        }

#if UNITY_EDITOR
        private void CreateToggleMenuItem()
        {
            try
            {
                var avatar = GetComponent<VRCAvatarDescriptor>();
                if (avatar != null && avatar.expressionsMenu != null)
                {
                    var menu = avatar.expressionsMenu;
                    
                    // 既存のメニューアイテムをチェック
                    bool menuExists = false;
                    foreach (var control in menu.controls)
                    {
                        if (control.parameter.name == _toggleParameterName)
                        {
                            menuExists = true;
                            break;
                        }
                    }

                    // メニューアイテムが存在しない場合は追加
                    if (!menuExists && menu.controls.Count < VRCExpressionsMenu.MAX_CONTROLS)
                    {
                        var control = new VRCExpressionsMenu.Control
                        {
                            name = "PCSS Light",
                            type = VRCExpressionsMenu.Control.ControlType.Toggle,
                            parameter = new VRCExpressionsMenu.Control.Parameter { name = _toggleParameterName },
                            value = 1f
                        };

                        menu.controls.Add(control);
                        UnityEditor.EditorUtility.SetDirty(menu);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error creating toggle menu item: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

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
                if (_plugin != null)
                {
                    // シェーダーキーワードとパラメータの整合性チェック
                    bool isEnabled = Shader.IsKeywordEnabled("_PCSS_ON");
                    float intensity = Shader.GetGlobalFloat("_PCSSIntensity");
                    float toggleValue = _plugin.GetParameterValue(_toggleParameterName);

                    if (!isEnabled)
                    {
                        Debug.LogWarning("PCSSシェーダーキーワードが無効です。VRCFuryの自動修正により無効化された可能性があります。");
                    }

                    if (Mathf.Approximately(intensity, 0f))
                    {
                        Debug.LogWarning("PCSSの強度が0に設定されています。VRCFuryの自動修正により変更された可能性があります。");
                    }

                    // ライトの状態を同期
                    if (_light != null)
                    {
                        _light.enabled = toggleValue > 0.5f;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error validating VRCFury compatibility: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }
    }
}
