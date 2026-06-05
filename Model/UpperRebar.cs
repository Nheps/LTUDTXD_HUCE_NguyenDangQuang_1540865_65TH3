using Autodesk.Revit.DB.Structure;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Model
{
    /// <summary>
    /// Quản lý việc tính toán tọa độ và tạo thép chính phía TRÊN của dầm.
    /// Hỗ trợ tối đa 3 lớp thép trên (Top1, Top2, Top3) với offset lần lượt 50/130/210mm từ mép trên.
    /// Nếu có chiều dài neo (anchor > 0), thép sẽ có dạng chữ L ở 2 đầu (uốn xuống thêm).
    /// </summary>
    public class UpperRebar
    {
        /// <summary>Lớp thép trên cần tạo (Top1/Top2/Top3).</summary>
        private RebarBeamType RebarBeamType { get; set; }

        /// <summary>Điểm đầu thanh thép (được tính toán lại trong RebarAnalys).</summary>
        private XYZ Start { get; set; }

        /// <summary>Điểm cuối thanh thép (được tính toán lại trong RebarAnalys).</summary>
        private XYZ End { get; set; }

        /// <summary>Chiều dài đoạn neo thép uốn xuống tại 2 đầu dầm (feet). 0 = không có neo.</summary>
        private double Anchor { get; set; }

        /// <summary>
        /// Danh sách đường cong của từng thanh thép.
        /// Mỗi phần tử là danh sách Curve của một thanh (1 đoạn thẳng hoặc 3 đoạn nếu có neo).
        /// </summary>
        private List<List<Curve>> Curves { get; set; } = new();

        /// <summary>Số lượng thanh thép cần đặt trong lớp này.</summary>
        private int Quantity { get; set; } = 0;

        /// <summary>Loại thép (đường kính, vật liệu) được chọn từ giao diện.</summary>
        private RebarBarType RebarBarType { get; set; }

        /// <summary>Tài liệu Revit để tạo phần tử.</summary>
        private Document Document { get; set; }

        /// <summary>Thông tin hình học của dầm.</summary>
        private BeamInfo BeamInfo { get; set; }

        /// <summary>
        /// Khởi tạo và tự động tính toán tọa độ các thanh thép trên.
        /// </summary>
        /// <param name="rebarBeamType">Lớp thép (Top1/Top2/Top3).</param>
        /// <param name="rebarBarType">Loại thép từ Revit.</param>
        /// <param name="beamInfo">Thông tin dầm đã chọn.</param>
        /// <param name="anchor">Chiều dài neo tại 2 đầu (mm). 0 = không có neo.</param>
        /// <param name="quatity">Số lượng thanh thép cần đặt.</param>
        public UpperRebar(RebarBeamType rebarBeamType, RebarBarType rebarBarType, BeamInfo beamInfo, double anchor, int quatity)
        {
            RebarBeamType = rebarBeamType;
            BeamInfo = beamInfo;
            RebarBarType = rebarBarType;
            Start = BeamInfo.StartPoint;
            End = BeamInfo.EndPoint;
            Anchor = anchor.MmToFeet();
            Quantity = quatity;
            Document = BeamInfo.Families.FirstOrDefault().Document;

            // Tính toán tọa độ thực tế của từng thanh thép
            RebarAnalys();
        }

        /// <summary>
        /// Tính toán tọa độ từng thanh thép dựa trên lớp thép và chiều dài neo.
        ///
        /// Bước 1 — Offset theo chiều đứng (Z) tùy lớp:
        ///   Top1: -50mm,  Top2: -130mm,  Top3: -210mm  (tính từ mép trên dầm)
        ///
        /// Bước 2 — Phân bố ngang: dàn đều (Quantity-1) khoảng trong chiều rộng
        ///   (Width - 100mm), bắt đầu từ mép trái (cách mép 50mm).
        ///
        /// Bước 3 — Hình dạng thanh thép:
        ///   • Anchor = 0: thanh thẳng từ Start → End
        ///   • Anchor > 0: dạng chữ U ngược gồm 3 đoạn:
        ///       đoạn neo đầu (thẳng đứng xuống) → đoạn ngang → đoạn neo cuối (thẳng đứng xuống)
        /// </summary>
        private void RebarAnalys()
        {
            if (Quantity == 0) return;

            // Xác định offset dọc theo chiều cao tuỳ lớp thép
            switch (RebarBeamType)
            {
                case RebarBeamType.Top1:
                    // Lớp 1: cách mép trên 50mm (lớp bảo vệ)
                    Start = Start.Add(50.0.MmToFeet() * -XYZ.BasisZ);
                    End = End.Add(50.0.MmToFeet() * -XYZ.BasisZ);
                    break;
                case RebarBeamType.Top2:
                    // Lớp 2: cách mép trên 130mm
                    Start = Start.Add(130.0.MmToFeet() * -XYZ.BasisZ);
                    End = End.Add(130.0.MmToFeet() * -XYZ.BasisZ);
                    break;
                case RebarBeamType.Top3:
                    // Lớp 3: cách mép trên 210mm
                    Start = Start.Add(210.0.MmToFeet() * -XYZ.BasisZ);
                    End = End.Add(210.0.MmToFeet() * -XYZ.BasisZ);
                    break;
                default:
                    break;
            }

            // Phân bố ngang: khoảng cách giữa tim các thanh ngoài cùng = Width - 100mm
            var width = BeamInfo.Width - 100.0.MmToFeet();
            var distance = width / (Quantity - 1); // bước phân bố giữa các thanh

            // Dịch chuyển về phía trái để thanh đầu tiên ở mép trái
            Start = Start.Add(width / 2 * -BeamInfo.CrossDirection);
            End = End.Add(width / 2 * -BeamInfo.CrossDirection);

            if (Anchor == 0)
            {
                // Không có neo: mỗi thanh là một đoạn thẳng Start → End
                for (int i = 0; i < Quantity; i++)
                {
                    var p1 = Start.Add(i * distance * BeamInfo.CrossDirection);
                    var p2 = End.Add(i * distance * BeamInfo.CrossDirection);
                    List<Curve> curves = new List<Curve>() { Line.CreateBound(p1, p2) };
                    Curves.Add(curves);
                }
            }
            else
            {
                // Có neo: mỗi thanh có 3 đoạn — neo đầu (↓) + ngang + neo cuối (↓)
                // Điểm cuối neo = xuống thêm Anchor từ vị trí thép ngang
                var start = Start.Add(Anchor * -XYZ.BasisZ); // điểm đáy neo đầu
                var end = End.Add(Anchor * -XYZ.BasisZ);     // điểm đáy neo cuối

                for (int i = 0; i < Quantity; i++)
                {
                    var p1 = start.Add(i * distance * BeamInfo.CrossDirection); // đáy neo đầu
                    var p2 = Start.Add(i * distance * BeamInfo.CrossDirection); // góc quay đầu
                    var p3 = End.Add(i * distance * BeamInfo.CrossDirection);   // góc quay cuối
                    var p4 = end.Add(i * distance * BeamInfo.CrossDirection);   // đáy neo cuối

                    // 3 đoạn: p1→p2 (neo đầu) + p2→p3 (đoạn ngang) + p3→p4 (neo cuối)
                    List<Curve> curves = new List<Curve>()
                    {
                        Line.CreateBound(p1, p2),
                        Line.CreateBound(p2, p3),
                        Line.CreateBound(p3, p4)
                    };
                    Curves.Add(curves);
                }
            }
        }

        /// <summary>
        /// Tạo thực tế các thanh thép trên Revit từ danh sách đường cong đã tính.
        /// Dùng DirectShape làm host vì thép không gắn trực tiếp vào dầm FamilyInstance.
        /// Bỏ qua lỗi từng thanh để không làm gián đoạn toàn bộ quá trình tạo thép.
        /// </summary>
        public void RebarCreation()
        {
            // Tạo DirectShape làm phần tử chứa (host) cho thép
            var host = DirectShape.CreateElement(Document, new ElementId(BuiltInCategory.OST_StructuralFraming));

            foreach (var curves in Curves)
            {
                try
                {
                    // Normal = CrossDirection: mặt phẳng thép vuông góc với chiều rộng dầm
                    Document.CreateRebarSingle(RebarStyle.Standard, RebarBarType, host, BeamInfo.CrossDirection, curves);
                }
                catch (Exception)
                {
                    // Bỏ qua lỗi cá biệt (ví dụ: đường cong quá ngắn) và tiếp tục
                }
            }
        }
    }
}
