namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Model
{
    /// <summary>
    /// Thông tin hình học của một nhịp dầm đơn lẻ trong hệ dầm liên tục.
    /// Đơn vị: feet (đơn vị nội bộ Revit).
    /// </summary>
    public class SpanInfo
    {
        /// <summary>FamilyInstance dầm ứng với nhịp này.</summary>
        public FamilyInstance Family { get; set; }

        /// <summary>Điểm đầu trục nhịp (mép trên).</summary>
        public XYZ StartPoint { get; set; }

        /// <summary>Điểm cuối trục nhịp (mép trên).</summary>
        public XYZ EndPoint { get; set; }

        /// <summary>Chiều cao tiết diện nhịp này (feet).</summary>
        public double Height { get; set; }

        /// <summary>Chiều rộng tiết diện nhịp này (feet).</summary>
        public double Width { get; set; }
    }
}
