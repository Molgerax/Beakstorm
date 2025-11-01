using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Beakstorm.Rendering
{
    public static class BakeTexture3D
    {
        public static void RenderTextureToTexture3DAsset(RenderTexture renderTexture, string path)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(path))
                return;

            var output = RenderTextureToTexture3D(renderTexture);
            UnityEditor.AssetDatabase.CreateAsset(output, path);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(output);
#endif
        }

        public static Texture3D RenderTextureToTexture3DSubAsset(RenderTexture renderTexture, Object subAssetParent = null)
        {
#if UNITY_EDITOR
            if (subAssetParent == null)
                return null;
                
            string path = UnityEditor.AssetDatabase.GetAssetPath(subAssetParent);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"Object {subAssetParent} is no asset!");
                return null;
            }

            var output = RenderTextureToTexture3D(renderTexture);
            UnityEditor.AssetDatabase.AddObjectToAsset(output, subAssetParent);
            UnityEditor.AssetDatabase.SaveAssets();

            return output;
#endif
            return null;
        }
        
        public static Texture3D RenderTextureToTexture3D(RenderTexture renderTexture)
        {
            int width = renderTexture.width, height = renderTexture.height, depth = renderTexture.volumeDepth;

            int blockSize = (int)GraphicsFormatUtility.GetBlockSize(renderTexture.graphicsFormat);
            
            var a = new NativeArray<byte>(width * height * depth * blockSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory); 
            //change if format is not 8 bits (i was using R8_UNorm) (create a struct with 4 bytes etc)

            string name = renderTexture.name + "_Baked";
            Texture3D output = new Texture3D(width, height, depth, renderTexture.graphicsFormat, TextureCreationFlags.None);
            
            AsyncGPUReadback.RequestIntoNativeArray(ref a, renderTexture, 0, (_) =>
            {
                output.name = name;
                output.SetPixelData(a, 0);
                output.Apply(updateMipmaps: false, makeNoLongerReadable: true);
                
                a.Dispose();
                renderTexture.Release();
            }).WaitForCompletion();
            
            return output;
        }
    }
}
