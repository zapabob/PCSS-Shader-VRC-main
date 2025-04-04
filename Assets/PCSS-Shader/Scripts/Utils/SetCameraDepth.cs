using UnityEngine;

namespace PCSSShader.Utils
{
    [ExecuteInEditMode]
    public class SetCameraDepthMode : MonoBehaviour
    {
        private void OnEnable()
        {
            var camera = GetComponent<Camera>();
            if (camera != null)
            {
                camera.depthTextureMode = DepthTextureMode.Depth;
            }
        }
    }
} 