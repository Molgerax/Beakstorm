using System;
using System.IO;
using Beakstorm.Gameplay.Encounters.Procedural;
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
            path = "Assets/_Generated/TremblePrefabs/" + nameof(EnemySpawnPoint);
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
            if (enemySo.Prefab == null)
                return;
            
            GameObject enemyPrefab = enemySo.Prefab.gameObject;
            
            if (!enemyPrefab)
                return;

            if (!enemyPrefab.TryGetComponent(out EnemyController target))
                return;
            
            if (enemyPrefab.TryGetComponent(out TrembleEnemySpawn spawn))
                return;

            string prefabPath = AssetDatabase.GetAssetPath(enemyPrefab);
            if (!string.IsNullOrEmpty(prefabPath))
            {
                GameObject loadedPrefab = PrefabUtility.LoadPrefabContents(prefabPath);
                EnemyController e = loadedPrefab.GetComponent<EnemyController>();
                e.SetEnemySo(enemySo);
                PrefabUtility.SaveAsPrefabAsset(loadedPrefab, prefabPath);
                PrefabUtility.UnloadPrefabContents(loadedPrefab);
            }
            
            
            string folderPath = GetPathRoot();

            EnsureFolderExists(folderPath);
            
            string[] guids;

            guids = AssetDatabase.FindAssets(enemyPrefab.name + $"t:{nameof(EnemyController)}", new[] {folderPath});
            if (guids == null || guids.Length == 0)
            {
                guids = AssetDatabase.FindAssets("", new[] {folderPath});
            }

            foreach (string guid in guids)
            {
                string assetP = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetP);

                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(prefab);
                if (source == enemyPrefab)
                {
                    ApplyOnPrefabVariant(enemySo, prefab, source, assetP);
                    return;
                }
            }
            CreatePrefabVariant(enemySo, enemyPrefab, folderPath + "/" + enemyPrefab.name + "_Spawn.prefab");
        }
        
        static void CreatePrefabVariant(EnemySO enemy, GameObject source, string variantPath)
        {
            GameObject prefabVariant = new GameObject(source.name + "_Spawner");

            EnemySpawnPoint script = prefabVariant.AddComponent<EnemySpawnPoint>();
            script.Init(enemy, 0);
            
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.transform.SetParent(prefabVariant.transform, false);
            
            //PrefabUtility.ApplyAddedComponent(script, variantPath, InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAsset(prefabVariant, variantPath);
            CoreUtils.Destroy(prefabVariant);
        }

        static void EnsureFolderExists(string folderPath)
        {
            string fullPath = Application.dataPath + "/" + Path.GetRelativePath("Assets", folderPath);

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
        }
        
        static void ApplyOnPrefabVariant(EnemySO enemy, GameObject prefabVariant, GameObject source, string variantPath)
        {
            Debug.Log("Oops, shouldnt happen");
            return;
            
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
