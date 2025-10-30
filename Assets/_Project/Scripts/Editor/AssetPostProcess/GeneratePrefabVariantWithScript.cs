using System;
using Beakstorm.Gameplay.Enemies;
using Beakstorm.Mapping.Tremble;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Beakstorm.AssetPostProcess.Editor
{
    public class GeneratePrefabVariantWithScript : AssetPostprocessor
    {
        public static string GetPathRoot()
        {
            string path = "Assets/_Generated/" + nameof(EnemyController) + "/" + nameof(TrembleEnemySpawn);
            return path;
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                EnemySO enemySo = AssetDatabase.LoadAssetAtPath<EnemySO>(path);
                ProcessAsset(enemySo);
            }
        }

        static void ProcessAsset(EnemySO enemySo)
        {
            if (enemySo == null)
                    return;
            
            GameObject loadedPrefab = enemySo.Prefab.gameObject;
            
            if (!loadedPrefab)
                return;

            if (!loadedPrefab.TryGetComponent(out EnemyController target))
                return;
            
            if (loadedPrefab.TryGetComponent(out TrembleEnemySpawn spawn))
                return;

            string folderPath = GetPathRoot();
            
            string[] guids;

            guids = AssetDatabase.FindAssets(loadedPrefab.name + $"t:{nameof(EnemyController)}", new[] {folderPath});
            if (guids == null || guids.Length == 0)
            {
                guids = AssetDatabase.FindAssets("", new[] {folderPath});
            }

            foreach (string guid in guids)
            {
                string assetP = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetP);

                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(prefab);
                if (source == loadedPrefab)
                {
                    ApplyOnPrefabVariant(enemySo, prefab, source, assetP);
                    return;
                }
            }
            CreatePrefabVariant(enemySo, loadedPrefab, folderPath + "/" + loadedPrefab.name + ".prefab");
        }
        
        static void CreatePrefabVariant(EnemySO enemy, GameObject source, string variantPath)
        {
            GameObject prefabVariant = (GameObject)PrefabUtility.InstantiatePrefab(source);
            
            Debug.Log(prefabVariant);
            
            TrembleEnemySpawn script = prefabVariant.AddComponent<TrembleEnemySpawn>();
            script.Enemy = enemy;
            PrefabUtility.ApplyAddedComponent(script, variantPath, InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAsset(prefabVariant, variantPath);
            CoreUtils.Destroy(prefabVariant);
        }

        static void ApplyOnPrefabVariant(EnemySO enemy, GameObject prefabVariant, GameObject source, string variantPath)
        {
            if (prefabVariant.TryGetComponent(out TrembleEnemySpawn script))
                return;
            
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            script = instance.AddComponent<TrembleEnemySpawn>();
            script.Enemy = enemy;
            PrefabUtility.ApplyAddedComponent(script, variantPath, InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAsset(prefabVariant, variantPath);
        }
    }
}
