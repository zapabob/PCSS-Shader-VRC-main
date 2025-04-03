using UnityEngine;
using VRC.SDKBase;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using VRC.SDK3.Avatars.Components;

[assembly: ExportsPlugin(typeof(PCSSLightPlugin))]
namespace PCSSShader
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
                var pcssLight = GetComponent<PCSSLight>();
                if (pcssLight)
                {
                    pcssLight.Setup();
                    var plugin = gameObject.AddComponent<PCSSLightPlugin>();
                    VRCObjectSync.AddNativePlugin(plugin);
                    
                    // モジュラーアバター用の永続化設定
                    var maComponent = gameObject.AddComponent<MAComponent>();
                    maComponent.pathMode = VRCAvatarDescriptor.PathMode.Absolute;
                    maComponent.ignoreObjectScale = true;

                    // AutoFix対策
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
                else
                {
                    Debug.LogError("PCSSLight component not found on the game object.");
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

    public class PCSSLightPlugin : SDKUnityNativePluginBase
    {
        private PCSSLight pcssLight;
        private bool isInitialized = false;
        private bool isInMirror = false;

        private void Start()
        {
            pcssLight = GetComponent<PCSSLight>();
            if (!isInitialized)
            {
                InitializePCSSLight();
            }
        }

        private void InitializePCSSLight()
        {
            if (pcssLight != null)
            {
                pcssLight.Setup();
                isInitialized = true;
            }
        }

        public override void OnDestroy()
        {
            try
            {
                if (pcssLight != null)
                {
                    pcssLight.DestroyShadowRenderTexture();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error destroying PCSS Light shadow render texture: {ex.Message}");
            }
        }

        public override void OnUpdate()
        {
            try
            {
                if (pcssLight != null)
                {
                    // ミラー内での処理を最適化
                    var currentIsInMirror = Networking.LocalPlayer?.IsUserInVR() == true && 
                                          gameObject.layer == LayerMask.NameToLayer("MirrorReflection");
                    
                    if (currentIsInMirror != isInMirror)
                    {
                        isInMirror = currentIsInMirror;
                        pcssLight.Setup(); // ミラー状態が変わった時に再セットアップ
                    }

                    pcssLight.UpdateShaderValues();
                    pcssLight.UpdateCommandBuffer();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error updating PCSS Light: {ex.Message}");
            }
        }
    }
}
