#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace DynaMak.Utility
{
    public class ClearShaderCache : MonoBehaviour
    {
        [MenuItem("Tools/Clear shader cache")]
        public static void ClearShaderCache_Command()
        {
            var shaderCachePath = Path.Combine(Application.dataPath, "../Library/ShaderCache");
            Directory.Delete(shaderCachePath, true);
        }
    }
}
#endif