using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using PCSS.Core;

namespace PCSS.Core
{
    [AddComponentMenu("PCSS/Setup")]
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class PCSSLightInstaller : MonoBehaviour
    {
        [SerializeField]
        private bool _autoSetup = true;

        // VRCFury互換のパラメータ
        private const string PCSS_PARAM_PREFIX = "PCSS_";
        private const string PCSS_ENABLED_PARAM = PCSS_PARAM_PREFIX + "Enabled";
        private const string PCSS_INTENSITY_PARAM = PCSS_PARAM_PREFIX + "Intensity";
        
        // AutoFIX対策のための追加パラメータ
        private const string PCSS_AUTOFIX_RESISTANCE_KEY = "_PCSSAutoFixResistance";
        private const string PCSS_SHADER_KEYWORD = "_PCSS_ON";

        [SerializeField, Range(0f, 1f)]
        private float _defaultIntensity = 1f;

        [Header("アニメーション設定")]
        [SerializeField]
        private string _toggleParameterName = "PCSSLightToggle";
        
        [SerializeField]
        private string _menuPath = "PCSS Light/Toggle";

        private VRCExpressionParameters _expressionParameters;
        private Light _light;
        private PCSSLight _pcssLight;

        private void Awake()
        {
            if (_autoSetup)
            {
                SetupPCSSLight();
            }
        }

        private void Start()
        {
            if (_autoSetup)
            {
                SetupPCSSLight();
            }
        }

        private void Reset()
        {
            SetupPCSSLight();
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
                
                // Lightの基本設定
                _light.type = LightType.Directional;
                _light.shadows = LightShadows.Soft;
                _light.intensity = _defaultIntensity;

                // 2. PCSSLightコンポーネントのセットアップ
                _pcssLight = gameObject.GetComponent<PCSSLight>();
                if (_pcssLight == null)
                {
                    _pcssLight = gameObject.AddComponent<PCSSLight>();
                }

                if (_pcssLight != null)
                {
                    _pcssLight.Setup();
                    Debug.Log($"PCSS Light setup complete on {gameObject.name}", this);
                }
                else
                {
                    Debug.LogError($"Failed to add PCSSLight component to {gameObject.name}", this);
                    return;
                }

                // 3. VRChat関連のセットアップ
                var avatar = GetComponentInParent<VRCAvatarDescriptor>();
                if (avatar != null)
                {
                    SetupVRChatComponents();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error setting up PCSS Light on {gameObject.name}: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private void SetupVRChatComponents()
        {
            try
            {
                var avatar = GetComponentInParent<VRCAvatarDescriptor>();
                if (avatar != null)
                {
                    SetupToggleParameter();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error setting up VRChat components: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private void SetupVRCFuryParameters(PCSSLightPlugin plugin)
        {
            try
            {
                var avatar = GetComponentInParent<VRCAvatarDescriptor>();
                if (avatar != null)
                {
                    // パラメータの永続性を確保
                    plugin.enabled = true;
                    plugin.SetParameterValue(PCSS_ENABLED_PARAM, 1f);
                    plugin.SetParameterValue(PCSS_INTENSITY_PARAM, _defaultIntensity);
                    plugin.SetParameterValue(_toggleParameterName, 1f);

                    // シェーダーキーワードの設定
                    Shader.EnableKeyword(PCSS_SHADER_KEYWORD);
                    Shader.SetGlobalFloat("_PCSSEnabled", 1f);
                    Shader.SetGlobalFloat("_PCSSIntensity", _defaultIntensity);
                    Shader.SetGlobalFloat(PCSS_AUTOFIX_RESISTANCE_KEY, 1f);

                    // VRCFuryのパラメータ設定
                    SetupToggleParameter();
                    
                    // コマンドバッファを使ったAutoFIX耐性の設定
                    SetupAutoFixResistance();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error setting up VRCFury parameters: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        // AutoFIX耐性のための追加設定
        private void SetupAutoFixResistance()
        {
            try
            {
                if (_light != null && _pcssLight != null)
                {
                    // コマンドバッファの設定
                    CommandBuffer commandBuffer = new CommandBuffer();
                    commandBuffer.name = "PCSS AutoFix Resistance";
                    
                    // シェーダーキーワードと重要なグローバル変数をコマンドバッファに追加
                    commandBuffer.EnableShaderKeyword(PCSS_SHADER_KEYWORD);
                    commandBuffer.SetGlobalFloat(PCSS_ENABLED_PARAM, 1f);
                    commandBuffer.SetGlobalFloat(PCSS_INTENSITY_PARAM, _defaultIntensity);
                    commandBuffer.SetGlobalFloat(PCSS_AUTOFIX_RESISTANCE_KEY, 1f);
                    
                    // ライトイベントにコマンドバッファを追加
                    _light.AddCommandBuffer(LightEvent.BeforeScreenspaceMask, commandBuffer);
                    _light.AddCommandBuffer(LightEvent.AfterShadowMap, commandBuffer);
                    
                    Debug.Log("PCSS AutoFix resistance setup complete.", this);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error setting up AutoFix resistance: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private void SetupToggleParameter()
        {
#if UNITY_EDITOR
            try
            {
                var avatar = GetComponentInParent<VRCAvatarDescriptor>();
                if (avatar != null && avatar.expressionParameters != null)
                {
                    _expressionParameters = avatar.expressionParameters;
                    
                    // トグルパラメータの追加
                    var param = new VRCExpressionParameters.Parameter
                    {
                        name = _toggleParameterName,
                        valueType = VRCExpressionParameters.ValueType.Bool,
                        defaultValue = 1f,
                        saved = true  // パラメータを保存して永続化
                    };

                    // AutoFIX耐性のためのパラメータを追加
                    var pcssEnabledParam = new VRCExpressionParameters.Parameter
                    {
                        name = PCSS_ENABLED_PARAM,
                        valueType = VRCExpressionParameters.ValueType.Bool,
                        defaultValue = 1f,
                        saved = true  // パラメータを保存して永続化
                    };

                    var pcssIntensityParam = new VRCExpressionParameters.Parameter
                    {
                        name = PCSS_INTENSITY_PARAM,
                        valueType = VRCExpressionParameters.ValueType.Float,
                        defaultValue = _defaultIntensity,
                        saved = true  // パラメータを保存して永続化
                    };

                    // 既存のパラメータをチェック
                    bool toggleExists = false;
                    bool pcssEnabledExists = false;
                    bool pcssIntensityExists = false;
                    
                    for (int i = 0; i < _expressionParameters.parameters.Length; i++)
                    {
                        if (_expressionParameters.parameters[i].name == _toggleParameterName)
                        {
                            toggleExists = true;
                        }
                        if (_expressionParameters.parameters[i].name == PCSS_ENABLED_PARAM)
                        {
                            pcssEnabledExists = true;
                        }
                        if (_expressionParameters.parameters[i].name == PCSS_INTENSITY_PARAM)
                        {
                            pcssIntensityExists = true;
                        }
                    }

                    // 新しいパラメータの数を計算
                    int newParamCount = _expressionParameters.parameters.Length;
                    if (!toggleExists) newParamCount++;
                    if (!pcssEnabledExists) newParamCount++;
                    if (!pcssIntensityExists) newParamCount++;
                    
                    // パラメータが存在しない場合は追加
                    if (!toggleExists || !pcssEnabledExists || !pcssIntensityExists)
                    {
                        var newParams = new VRCExpressionParameters.Parameter[newParamCount];
                        System.Array.Copy(_expressionParameters.parameters, newParams, _expressionParameters.parameters.Length);
                        
                        int index = _expressionParameters.parameters.Length;
                        
                        if (!toggleExists)
                        {
                            newParams[index++] = param;
                        }
                        
                        if (!pcssEnabledExists)
                        {
                            newParams[index++] = pcssEnabledParam;
                        }
                        
                        if (!pcssIntensityExists)
                        {
                            newParams[index++] = pcssIntensityParam;
                        }
                        
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

            var avatarDescriptor = selection.GetComponent<VRCAvatarDescriptor>();
            if (avatarDescriptor == null)
            {
                Debug.LogError("選択されたオブジェクトにVRCAvatarDescriptorがありません。アバターのルートオブジェクトを選択してください。");
                return;
            }

            var installer = selection.GetComponent<PCSSLightInstaller>();
            if (installer == null)
            {
                installer = selection.AddComponent<PCSSLightInstaller>();
            }

            installer.SetupPCSSLight();
            UnityEditor.EditorUtility.SetDirty(selection);
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
                if (_pcssLight != null)
                {
                    // シェーダーキーワードとパラメータの整合性チェック
                    bool isEnabled = Shader.IsKeywordEnabled(PCSS_SHADER_KEYWORD);
                    float intensity = Shader.GetGlobalFloat("_PCSSIntensity");
                    float toggleValue = _pcssLight.GetParameterValue(_toggleParameterName);
                    float autoFixResistance = Shader.GetGlobalFloat(PCSS_AUTOFIX_RESISTANCE_KEY);

                    if (!isEnabled)
                    {
                        Debug.LogWarning("PCSSシェーダーキーワードが無効です。VRCFuryの自動修正により無効化された可能性があります。");
                        Shader.EnableKeyword(PCSS_SHADER_KEYWORD);
                    }

                    if (Mathf.Approximately(intensity, 0f))
                    {
                        Debug.LogWarning("PCSSの強度が0に設定されています。VRCFuryの自動修正により変更された可能性があります。");
                        Shader.SetGlobalFloat("_PCSSIntensity", _defaultIntensity);
                    }
                    
                    if (autoFixResistance < 0.5f)
                    {
                        Debug.LogWarning("PCSS AutoFix耐性が無効化されています。AutoFIXにより変更された可能性があります。");
                        Shader.SetGlobalFloat(PCSS_AUTOFIX_RESISTANCE_KEY, 1f);
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
