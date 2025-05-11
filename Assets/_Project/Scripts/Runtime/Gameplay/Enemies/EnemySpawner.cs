using UltEvents;
using UnityEngine;

namespace Beakstorm.Gameplay.Enemies
{
    [ExecuteInEditMode]
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyController enemyPrefab;

        [SerializeField] private UltEvent onSpawnEnemy;
        [SerializeField] public UltEvent onDefeat;

        [SerializeField] private WaitCondition waitCondition;
        [SerializeField] private float spawnDelay;

        public bool IsDefeated;

        private EnemyController _enemy;

        private AwaitableCompletionSource _completionSource = new AwaitableCompletionSource();

        public void SpawnEnemy()
        {
            if (IsDefeated)
                return;

            _enemy = Instantiate(enemyPrefab, transform.position, transform.rotation);
            _enemy.Spawn(this);
            _completionSource.Reset();
            IsDefeated = false;

            onSpawnEnemy?.Invoke();
        }

        public void OnDefeat()
        {
            IsDefeated = true;
            _completionSource.SetResult();
            onDefeat?.Invoke();
        }

        public Awaitable GetWaitCondition()
        {
            if (waitCondition == WaitCondition.Null)
                return Awaitable.EndOfFrameAsync();

            if (waitCondition == WaitCondition.WaitForDelay)
                return Awaitable.WaitForSecondsAsync(spawnDelay);

            if (waitCondition == WaitCondition.WaitUntilDefeated)
            {
                return _completionSource.Awaitable;
            }

            return Awaitable.EndOfFrameAsync();
        }


        enum WaitCondition
        {
            Null = 0,
            WaitForDelay = 1,
            WaitUntilDefeated = 2
        }

        private EnemyController _preview;

        private void OnEnable()
        {
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
            DestroyImmediate(_preview);
        }
    }
}