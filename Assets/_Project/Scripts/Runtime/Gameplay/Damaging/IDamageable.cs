namespace Beakstorm.Gameplay.Damaging
{
    public interface IDamageable
    {
        bool CanTakeDamage();
        void TakeDamage(int damage);
    }
}
