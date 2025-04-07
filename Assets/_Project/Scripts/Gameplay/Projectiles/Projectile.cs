using UnityEngine;
using UnityEngine.Pool;

namespace Beakstorm.Gameplay.Projectiles
{
    public class Projectile : MonoBehaviour
    {
        private IObjectPool<Projectile> _objectPool;
        public IObjectPool<Projectile> ObjectPool { set => _objectPool = value; }

        public void Deactivate()
        {
            _objectPool.Release(this);
        }
    }
}
