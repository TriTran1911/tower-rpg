using System;
using System.Collections;
using TowerRpg.Core;
using UnityEngine;

namespace TowerRpg.Progression
{
    /// <summary>
    /// Quét nhanh — §5.2. Tầng đã dọn một lần thì bấm nút là nhận thưởng, không đánh lại.
    ///
    /// ⚠️ CHÚ THÍCH CŨ Ở ĐÂY ĐÃ SAI, VÀ CÁI SAI ĐÓ CHE MẤT MỘT LỖI ĐANG SỐNG.
    /// Tôi từng viết "can-bang.xlsx chỉ mô hình hoá Mảnh theo việc LEO — chưa từng tính
    /// farm". SAI. Ô 'Thông số'!B32 = 2, nhãn "Hệ số cày lại (quét nhanh)", chú thích
    /// "1.0 = chỉ clear mỗi tầng một lần"; và 'Đường cong tầng'!E = D × B32, tức ngân sách
    /// Mảnh lũy kế LUÔN gấp đôi Mảnh leo. Cột cấp trang bị kỳ vọng cũng suy từ đó.
    ///
    /// Nghĩa là bảng tính CÓ cho farm, và cho ĐÚNG 2×. Mã này thì đang giao VÔ HẠN:
    /// 133 Mảnh/giây khi quét so với 9,6 khi đánh tay — gấp 13,9 lần, max cả bốn ô lên
    /// cấp 10 chỉ mất 2,6 phút giữ nút ở tầng 1. Hồi 3 giây KHÔNG chặn được gì vì phần
    /// thưởng bám theo HighestCleared và vòng lặp chạy song song với đánh tay.
    ///
    /// CHƯA SỬA — nằm ở Việc 2 của docs/KE-HOACH-NHIP-DO.md. Cách sửa đã biết: trần
    /// ngân sách 2× ShardReward mỗi tầng, hết trần thì nút chuyển "HẾT NGÂN SÁCH".
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

                // Cắt phần thưởng cuối cho vừa ngân sách còn lại, thay vì bỏ luôn lượt:
                // bỏ lượt thì người chơi mất công chờ 3 giây mà không nhận gì.
                float budget = GameState.Instance.SweepBudgetLeft;
                if (budget <= 0f) break;

                float reward = Mathf.Min(GameState.Instance.ShardReward(floor), budget);
                GameState.Instance.AddShards(reward, fromSweep: true);
                GameState.Instance.Save();
                Swept?.Invoke(floor, reward);
                Progress?.Invoke(0f);
            }
            _loop = null;
        }

        private void OnDisable() => Stop();
    }
}
