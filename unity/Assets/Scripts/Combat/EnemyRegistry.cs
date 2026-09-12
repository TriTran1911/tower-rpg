using System.Collections.Generic;
using UnityEngine;

namespace TowerRpg.Combat
{
    /// <summary>
    /// Danh sách quái đang sống, để AutoAttack tìm mục tiêu gần nhất mà không gọi
    /// FindObjectsByType mỗi khung hình (§9.3).
    /// </summary>
    public static class EnemyRegistry
    {
        private static readonly List<Component> Alive = new List<Component>();

        public static int Count => Alive.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() => Alive.Clear();

        public static void Register(Component enemy)
        {
            if (enemy != null && !Alive.Contains(enemy)) Alive.Add(enemy);
        }

        public static void Unregister(Component enemy)
        {
            if (enemy != null) Alive.Remove(enemy);
        }

        /// <summary>
        /// Quái còn sống gần nhất trong tầm, hoặc null.
        /// Lưu danh sách dưới dạng Component chứ không phải interface: chỉ khi đó phép so
        /// sánh null mới dùng toán tử của UnityEngine.Object và bắt được đối tượng đã bị huỷ.
        /// </summary>
        public static IDamageable Nearest(Vector3 from, float maxRange)
        {
            IDamageable best = null;
            float bestSqr = maxRange * maxRange;

            for (int i = Alive.Count - 1; i >= 0; i--)
            {
                Component component = Alive[i];

                if (component == null) { Alive.RemoveAt(i); continue; }
                if (component is not IDamageable candidate || !candidate.IsAlive) continue;

                float sqr = (candidate.Position - from).sqrMagnitude;
                if (sqr > bestSqr) continue;

                bestSqr = sqr;
                best = candidate;
            }

            return best;
        }

        /// <summary>Huỷ mọi quái còn sống. Dùng khi bày lại đợt quái.</summary>
        public static void ClearAll()
        {
            for (int i = Alive.Count - 1; i >= 0; i--)
            {
                Component component = Alive[i];
                if (component != null) UnityEngine.Object.Destroy(component.gameObject);
            }

            Alive.Clear();
        }
    }
}
