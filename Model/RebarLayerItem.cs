using Autodesk.Revit.DB.Structure;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Model
{
    /// <summary>
    /// Đại diện cho một lớp thép chính (trên hoặc dưới) trong danh sách động.
    /// Người dùng có thể thêm/xóa lớp tùy ý. Offset của từng lớp được tính
    /// tự động theo vị trí trong danh sách: 50 + 80 × index (mm).
    /// </summary>
    public partial class RebarLayerItem : ObservableObject
    {
        /// <summary>Loại thép (đường kính, vật liệu) cho lớp này.</summary>
        [ObservableProperty] private RebarBarType _barType;

        /// <summary>Số thanh thép trong lớp này.</summary>
        [ObservableProperty] private int _count = 2;
    }
}
