using Autodesk.Revit.DB.Structure;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Model
{
    /// <summary>
    /// Quản lý việc tính toán và tạo thép đai (stirrup) cho dầm.
    ///
    /// Sơ đồ phân vùng theo tiêu chuẩn Việt Nam:
    /// ┌────────────┬──────────────────────┬────────────┐
    /// │  Đai 2 đầu │      Đai giữa        │  Đai 2 đầu │
    /// │    L/4     │        L/2           │    L/4     │
    /// └────────────┴──────────────────────┴────────────┘
    ///
    /// Mỗi đai là một vòng thép hình chữ nhật khép kín trong mặt cắt ngang,
    /// cách mép bê tông một khoảng bằng lớp bảo vệ (cover).
    /// Các đai được phân bố dọc trục dầm với bước (spacing) theo từng vùng.
    /// </summary>
    public class StirrupRebar
    {
        /// <summary>Thông tin hình học của dầm.</summary>
        private BeamInfo BeamInfo { get; set; }

        /// <summary>Loại thép đai vùng 2 đầu dầm (bước dày hơn).</summary>
        private RebarBarType EndBarType { get; set; }

        /// <summary>Loại thép đai vùng giữa dầm (bước thưa hơn).</summary>
        private RebarBarType CenterBarType { get; set; }

        /// <summary>Bước đai vùng 2 đầu (feet) — khoảng cách tim đến tim giữa các đai.</summary>
        private double EndSpacing { get; set; }

        /// <summary>Bước đai vùng giữa (feet) — khoảng cách tim đến tim giữa các đai.</summary>
        private double CenterSpacing { get; set; }

        /// <summary>Chiều dày lớp bảo vệ (feet) — khoảng cách từ mặt bê tông đến tim đai.</summary>
        private double Cover { get; set; }

        /// <summary>Tài liệu Revit để tạo phần tử.</summary>
        private Document Document { get; set; }

        /// <summary>
        /// Khởi tạo StirrupRebar với đầy đủ thông số đai.
        /// </summary>
        /// <param name="centerBarType">Loại thép đai giữa dầm.</param>
        /// <param name="endBarType">Loại thép đai 2 đầu dầm.</param>
        /// <param name="beamInfo">Thông tin hình học dầm.</param>
        /// <param name="cover">Chiều dày lớp bảo vệ (mm).</param>
        /// <param name="centerSpacing">Bước đai vùng giữa (mm).</param>
        /// <param name="endSpacing">Bước đai vùng 2 đầu (mm).</param>
        public StirrupRebar(RebarBarType centerBarType, RebarBarType endBarType,
            BeamInfo beamInfo, double cover, double centerSpacing, double endSpacing)
        {
            BeamInfo = beamInfo;
            CenterBarType = centerBarType;
            EndBarType = endBarType;
            Cover = cover.MmToFeet();
            CenterSpacing = centerSpacing.MmToFeet();
            EndSpacing = endSpacing.MmToFeet();
            Document = beamInfo.Families[0].Document;
        }

        /// <summary>
        /// Tạo 4 đoạn thẳng tạo thành hình chữ nhật của một vòng đai
        /// tại vị trí <paramref name="position"/> trên trục dầm.
        ///
        /// Hình dạng đai (nhìn mặt cắt ngang):
        ///   p1 ──── p2
        ///   │        │
        ///   p4 ──── p3
        ///
        /// Kích thước đai = (Width - 2×Cover) × (Height - 2×Cover)
        /// </summary>
        /// <param name="position">Điểm tham chiếu trên trục dầm (tại mép trên).</param>
        private List<Curve> GetStirrupCurves(XYZ position)
        {
            // Nửa chiều rộng bên trong tính từ trục dầm đến tim đai
            var halfWidth = BeamInfo.Width / 2 - Cover;

            // Khoảng cách từ mép trên xuống tim đai trên = Cover
            var topOffset = Cover;

            // Khoảng cách từ mép trên xuống tim đai dưới = Height - Cover
            var bottomOffset = BeamInfo.Height - Cover;

            // Tính 4 góc của đai trong không gian 3D
            var p1 = position.Add(halfWidth * (-BeamInfo.CrossDirection)).Add(topOffset * (-XYZ.BasisZ));    // góc trên trái
            var p2 = position.Add(halfWidth * BeamInfo.CrossDirection).Add(topOffset * (-XYZ.BasisZ));       // góc trên phải
            var p3 = position.Add(halfWidth * BeamInfo.CrossDirection).Add(bottomOffset * (-XYZ.BasisZ));    // góc dưới phải
            var p4 = position.Add(halfWidth * (-BeamInfo.CrossDirection)).Add(bottomOffset * (-XYZ.BasisZ)); // góc dưới trái

            return new List<Curve>
            {
                Line.CreateBound(p1, p2), // cạnh trên
                Line.CreateBound(p2, p3), // cạnh phải
                Line.CreateBound(p3, p4), // cạnh dưới
                Line.CreateBound(p4, p1)  // cạnh trái
            };
        }

        /// <summary>
        /// Tạo thực tế các đai thép trên Revit theo 3 vùng phân bố.
        ///
        /// Quy trình:
        ///   1. Tính chiều dài từng vùng (L/4, L/2, L/4)
        ///   2. Với mỗi vùng: tạo 1 hình đai tại điểm đầu vùng
        ///   3. Dùng CreateRebarMaximumSpacing để Revit tự nhân bản đai theo bước spacing
        ///
        /// DirectShape được dùng làm host vì Revit yêu cầu đai phải có phần tử chứa.
        /// </summary>
        public void RebarCreation()
        {
            // Tạo DirectShape làm phần tử chứa cho thép đai
            var host = DirectShape.CreateElement(Document, new ElementId(BuiltInCategory.OST_StructuralFraming));

            var beamLength = BeamInfo.StartPoint.DistanceTo(BeamInfo.EndPoint);
            var endZoneLength = beamLength / 4;     // chiều dài mỗi vùng đầu = L/4
            var centerZoneLength = beamLength / 2;  // chiều dài vùng giữa = L/2

            // --- Vùng 1: đai đầu dầm (từ StartPoint → L/4) ---
            try
            {
                var curves = GetStirrupCurves(BeamInfo.StartPoint);
                // Normal = Direction: đai nằm trong mặt phẳng vuông góc trục dầm,
                // Revit phân bố dọc theo Direction
                Document.CreateRebarMaximumSpacing(RebarStyle.StirrupTie, EndBarType, host,
                    BeamInfo.Direction, curves, endZoneLength, EndSpacing);
            }
            catch { }

            // --- Vùng 2: đai giữa dầm (từ L/4 → 3L/4) ---
            try
            {
                // Điểm đầu vùng giữa = StartPoint dịch theo Direction một đoạn L/4
                var zone2Start = BeamInfo.StartPoint.Add(endZoneLength * BeamInfo.Direction);
                var curves = GetStirrupCurves(zone2Start);
                Document.CreateRebarMaximumSpacing(RebarStyle.StirrupTie, CenterBarType, host,
                    BeamInfo.Direction, curves, centerZoneLength, CenterSpacing);
            }
            catch { }

            // --- Vùng 3: đai cuối dầm (từ 3L/4 → EndPoint) ---
            try
            {
                // Điểm đầu vùng cuối = StartPoint dịch theo Direction một đoạn 3L/4
                var zone3Start = BeamInfo.StartPoint.Add((endZoneLength + centerZoneLength) * BeamInfo.Direction);
                var curves = GetStirrupCurves(zone3Start);
                Document.CreateRebarMaximumSpacing(RebarStyle.StirrupTie, EndBarType, host,
                    BeamInfo.Direction, curves, endZoneLength, EndSpacing);
            }
            catch { }
        }
    }
}
