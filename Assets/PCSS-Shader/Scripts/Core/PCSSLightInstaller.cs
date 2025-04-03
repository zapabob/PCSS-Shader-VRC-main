using UnityEngine;
using VRC.SDKBase;
using nadena.dev.modular_avatar.core;
using VRC.SDK3.Avatars.Components;

namespace PCSSShader.Core
{
    [AddComponentMenu("PCSS/PCSS Light Installer")]
    public class PCSSLightInstaller : MonoBehaviour
    {
        [Header("モジュラーアバター設定")]
        [SerializeField] private bool preserveOnAutoFix = true;
        [SerializeField] private bool syncWithMirror = true;

        private void Start()
        {
            SetupPCSSLight();
        }

        private void SetupPCSSLight()
        {
            try
            {
                var pcssLight = gameObject.AddComponent<PCSSLight>();
                if (pcssLight != null)
                {
                    pcssLight.Setup();
                    var plugin = gameObject.AddComponent<PCSSLightPlugin>();
                    
                    // モジュラーアバター用の永続化設定
                    var maComponent = gameObject.AddComponent<MAComponent>();
                    maComponent.pathMode = VRCAvatarDescriptor.PathMode.Absolute;
                    maComponent.ignoreObjectScale = true;

                    // AutoFIX対策
                    if (preserveOnAutoFix)
                    {
                        var maParam = gameObject.AddComponent<MAParameter>();
                        maParam.nameOrPrefix = "PCSS_Enabled";
                        maParam.syncType = VRCExpressionParameters.ValueType.Bool;
                        maParam.defaultValue = 1;
                        maParam.saved = true;

                        // ミラー同期対策
                        if (syncWithMirror)
                        {
                            var maMerge = gameObject.AddComponent<MAMergeAnimator>();
                            maMerge.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
                            maMerge.pathMode = VRCAvatarDescriptor.PathMode.Absolute;
                            maMerge.deleteAttachedAnimator = true;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error setting up PCSS Light: {ex.Message}");
            }
        }

        private void OnValidate()
        {
            // コンポーネントの依存関係チェック
            var avatar = GetComponent<VRCAvatarDescriptor>();
            if (avatar == null)
            {
                Debug.LogWarning("VRCAvatarDescriptorが見つかりません。アバターのルートに配置してください。");
            }
        }
    }
}
