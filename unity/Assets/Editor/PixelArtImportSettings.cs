using UnityEditor;
using UnityEngine;

namespace TowerRpg.EditorTools
{
    /// <summary>
    /// Ép thiết lập import đúng cho pixel art với mọi texture nằm dưới Assets/Art/.
    ///
    /// VÌ SAO CẦN: mặc định Unity import texture với lọc Bilinear và nén — cả hai đều
    /// phá pixel art. Sprite 16x16 của bộ Ninja Adventure sẽ nhoè và bẩn từng điểm ảnh.
    /// Chỉnh tay 1.915 file là không tưởng, nên ép ở đây.
    ///
    /// Chạy TỰ ĐỘNG mỗi lần import. Muốn áp lại cho file đã import:
    /// chuột phải thư mục Art -> Reimport.
    /// </summary>
    public sealed class PixelArtImportSettings : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/Art/";
        private const int PixelsPerUnit = 16;   // kích thước ô của bộ asset

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot)) return;

            TextureImporter importer = (TextureImporter)assetImporter;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;          // KHÔNG được đổi sang Bilinear
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
        }
    }
}
