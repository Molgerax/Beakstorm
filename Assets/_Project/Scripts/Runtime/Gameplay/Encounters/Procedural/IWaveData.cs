using System.Collections.Generic;

namespace Beakstorm.Gameplay.Encounters.Procedural
{
    public interface IWaveData : IEnumerable<IEnemySpawnData>
    {
        public int DangerRating();
        public int EnemyCount();
    }
}
