using UnityEngine;

namespace TowerRpg.Core
{
    /// <summary>
    /// Giữ cho ĐẤU TRƯỜNG luôn nằm trong khung hình, ở mọi tỉ lệ màn hình.
    ///
    /// Camera trực giao khoá NỬA CHIỀU CAO (`orthographicSize`); bề ngang thấy được là
    /// `orthographicSize × aspect`. Màn hình càng CAO thì aspect càng nhỏ, tức thấy càng
    /// HẸP — ngược hẳn trực giác "máy to hơn thì thấy nhiều hơn".
    ///
    /// Số đo: `OrthoSize = 7,2` cho nửa bề ngang 4,05 ở 16:9 — vừa đủ cho quái ở bán kính
    /// 3,5 cộng nửa thân 0,5, dư đúng 1,2%. Ở 19,5:9 con số đó là 3,32 và ở 20:9 là 3,24,
    /// tức TÂM hai con quái hai bên nằm NGOÀI màn hình. Trên điện thoại thật hôm nay,
    /// người chơi đánh nhau với những con họ chỉ thấy một nửa.
    ///
    /// Sửa: nới `orthographicSize` vừa đủ để bề ngang cần thiết luôn lọt. KHÔNG BAO GIỜ
    /// thu nhỏ hơn cỡ thiết kế — màn hình rộng hơn thì thấy thoáng hơn, không phóng to.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class CameraFit : MonoBehaviour
    {
        [SerializeField] private Camera target;
        [SerializeField] private float designOrthoSize = 7.2f;

        private float _canNuaNgang = 4f;
        private float _aspectCu = -1f;
        private bool _ready;

        private void Awake() { if (target == null) target = GetComponent<Camera>(); }

        private void Start() => BalanceConfig.TryUse(this, b =>
        {
            // Suy từ chính số liệu cân bằng, không viết cứng: đổi enemy.spawnRadius trong
            // CSV mà camera không theo thì lại sinh ra đúng lỗi này lần nữa.
            _canNuaNgang = b.Get("enemy.spawnRadius") + b.Get("camera.marginX");
            _ready = true;
            Ap();
        });

        // Aspect đổi khi xoay máy hoặc ở chế độ cửa sổ. Rẻ: chỉ một phép chia mỗi khung hình.
        private void Update() { if (_ready) Ap(); }

        private void Ap()
        {
            if (target == null || !target.orthographic) return;
            float aspect = target.aspect;
            if (aspect <= 0f || Mathf.Approximately(aspect, _aspectCu)) return;
            _aspectCu = aspect;
            target.orthographicSize = Mathf.Max(designOrthoSize, _canNuaNgang / aspect);
        }
    }
}
