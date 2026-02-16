using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Gameplay.Player
{
    [PointEntity("player_spawn", "info", prefab: "info_player_prefab")]
    [DefaultExecutionOrder(-50)]
    public class PlayerStartPosition : MonoBehaviour, IOnImportFromMapEntity
    {
        private static Vector3 _startPosition;
        private static Quaternion _startRotation = Quaternion.identity;

        public static void SetPlayer(Transform t)
        {
            t.SetPositionAndRotation(_startPosition, _startRotation);
        }

        private void Awake()
        {
            _startPosition = transform.position;
            _startRotation = transform.rotation;
        }

        public void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            transform.rotation = Quaternion.LookRotation(transform.right, transform.up);
        }
    }
}
