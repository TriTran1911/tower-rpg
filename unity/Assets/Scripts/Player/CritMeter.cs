using TowerRpg.Core;
using UnityEngine;

namespace TowerRpg.Player
{
    /// <summary>
    /// Thanh dồn chí mạng (§5.4). HOÀN TOÀN XÁC ĐỊNH — không có xác suất ở đâu cả.
    /// Cứ N đòn thì đòn thứ N chắc chắn chí mạng.
    ///
    /// QUAN TRỌNG: thanh KHÔNG reset khi người chơi di chuyển. Đó là chiến thuật chữ ký
    /// của §5.4 — lùi lại, chờ, rồi dồn đòn chí mạng vào lúc boss hở sườn.
    /// Đừng "dọn dẹp" nó thành reset-khi-di-chuyển.
    /// </summary>
    public sealed class CritMeter : MonoBehaviour
    {
        private int _charge;
        private int _size;
        private bool _ready;

        public int Size => _size;

        /// <summary>Đòn tiếp theo có phải chí mạng không — dùng cho UI báo trước.</summary>
        public bool IsReady => _ready && _charge >= _size - 1;

        /// <summary>Mức đầy 0..1 để vẽ thanh.</summary>
        public float Fill01 => _ready && _size > 1 ? Mathf.Clamp01((float)_charge / (_size - 1)) : 0f;

        private void Start() => BalanceConfig.TryUse(this, ApplyBalance);

        private void ApplyBalance(BalanceConfig balance)
        {
            _size = balance.GetInt("crit.meterSize");

            // Không im lặng kẹp về 1: meterSize <= 1 nghĩa là mọi đòn đều chí mạng,
            // gần như chắc chắn là lỗi cấu hình chứ không phải ý đồ.
            if (_size <= 1)
            {
                Debug.LogError($"[CritMeter] crit.meterSize = {_size}. Phải >= 2, nếu không mọi đòn " +
                               "đều là chí mạng. Sửa m1-balance.csv.", this);
                enabled = false;
                return;
            }

            _ready = true;
        }

        /// <summary>
        /// Gọi đúng một lần cho mỗi đòn đánh. Trả về true nếu đòn NÀY là chí mạng.
        /// Với Size = 5: đòn 1,2,3,4 nạp thanh; đòn 5 chí mạng rồi reset.
        /// </summary>
        public bool RegisterAttack()
        {
            if (!_ready) return false;

            if (_charge >= _size - 1)
            {
                _charge = 0;
                return true;
            }

            _charge++;
            return false;
        }
    }
}
