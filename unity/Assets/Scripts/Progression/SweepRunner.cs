using System;
using System.Collections;
using TowerRpg.Core;
using UnityEngine;

namespace TowerRpg.Progression
{
    /// <summary>
    /// Quét nhanh — §5.2. Tầng đã dọn một lần thì bấm nút là nhận thưởng, không đánh lại.
    ///
    /// CÓ HỒI CHIÊU, và đó là một quyết định chứ không phải thiếu sót: §5.2 cho phép bấm
    /// lại, nhưng can-bang.xlsx chỉ mô hình hoá Mảnh theo việc LEO — chưa từng tính farm.
    /// Quét tức thời bấm được vô hạn thì Mảnh vô hạn, và toàn bộ đường cong chi phí ở §5.6
    /// mất nghĩa. 3 giây mỗi lượt so với ~45 giây đánh tay vẫn là nén 15 lần — đủ để
    /// "farm không thành cực hình" mà không phá kinh tế.
    /// </summary>
    public sealed class SweepRunner : MonoBehaviour
    {
        public event Action<int, float> Swept;    // tầng, Mảnh nhận được
        public event Action<float> Progress;      // 0..1 của lượt quét đang chạy

        private float _seconds = 3f;
        private bool _ready;
        private Coroutine _loop;

        public bool Running => _loop != null;

        /// <summary>Tầng sẽ quét: tầng cao nhất từng dọn sạch — tầng béo nhất người chơi có.</summary>
        public int TargetFloor =>
            GameState.Instance != null ? Mathf.Max(1, GameState.Instance.HighestCleared) : 1;

        public bool CanSweep =>
            GameState.Instance != null && GameState.Instance.Ready &&
            GameState.Instance.CanSweep(TargetFloor);

        private void Start() => BalanceConfig.TryUse(this, b =>
        {
            _seconds = Mathf.Max(0.1f, b.Get("sweep.seconds"));
            _ready = true;
        });

        public bool Toggle()
        {
            if (!_ready || !CanSweep) return false;

            if (_loop != null) { Stop(); return false; }
            _loop = StartCoroutine(Loop());
            return true;
        }

        public void Stop()
        {
            if (_loop != null) StopCoroutine(_loop);
            _loop = null;
            Progress?.Invoke(0f);
        }

        private IEnumerator Loop()
        {
            while (CanSweep)
            {
                float t = 0f;
                while (t < _seconds)
                {
                    t += Time.deltaTime;
                    Progress?.Invoke(Mathf.Clamp01(t / _seconds));
                    yield return null;
                }

                int floor = TargetFloor;
                float reward = GameState.Instance.ShardReward(floor);
                GameState.Instance.AddShards(reward);
                GameState.Instance.Save();
                Swept?.Invoke(floor, reward);
                Progress?.Invoke(0f);
            }
            _loop = null;
        }

        private void OnDisable() => Stop();
    }
}
