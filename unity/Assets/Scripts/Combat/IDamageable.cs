using UnityEngine;

namespace TowerRpg.Combat
{
    public interface IDamageable
    {
        bool IsAlive { get; }
        Vector3 Position { get; }
        void TakeDamage(float amount, bool isCrit);
    }
}
