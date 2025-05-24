using System;
using UnityEngine;

namespace Beakstorm.Gameplay.Enemies
{
    [ExecuteInEditMode]
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyController enemyPrefab;

        [HideInInspector] public bool isDefeated;

        public event Action OnDefeatAction;
        private EnemyController _enemy;

        public void SpawnEnemy()
        {
            if (isDefeated)
                return;

            _enemy = Instantiate(enemyPrefab, transform.position, transform.rotation);
            _enemy.Spawn(this);
            isDefeated = false;
        }

        public void OnDefeat()
        {
            isDefeated = true;
            OnDefeatAction?.Invoke();
        }

#if UNITY_EDITOR
        private EnemyController _preview;

        private void OnEnable()
        {
            if (UnityEditor.EditorApplication.isPlaying)
                return;
                
            if (!_preview)
            {
                _preview = Instantiate(enemyPrefab, transform.position, transform.rotation, transform);
                UnityEditor.SceneVisibilityManager.instance.DisablePicking(_preview.gameObject, true);

                HideAndDontSaveRecursive(_preview.transform);
            }
        }

        private void HideAndDontSaveRecursive(Transform t)
        {
            t.gameObject.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;

            for (int i = 0; i < t.childCount; i++)
            {
                HideAndDontSaveRecursive(t.GetChild(i));
            }
        }

        private void OnDisable()
        {
            if (UnityEditor.EditorApplication.isPlaying)
                return;

            DestroyImmediate(_preview);
        }
#endif
    }
}