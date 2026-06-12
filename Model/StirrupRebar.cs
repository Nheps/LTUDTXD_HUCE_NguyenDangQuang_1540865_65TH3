using Autodesk.Revit.DB.Structure;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Model
{
    /// <summary>
    /// Quản lý việc tính toán và tạo thép đai (stirrup) cho dầm.
    ///
    /// Với dầm khác tiết diện: mỗi nhịp tạo đai riêng theo đúng kích thước h/b của nhịp đó.
    ///
    /// Sơ đồ phân vùng trong mỗi nhịp:
    /// ┌────────────┬──────────────────────┬────────────┐
    /// │  Đai 2 đầu │      Đai giữa        │  Đai 2 đầu │
    /// │    L/4     │        L/2           │    L/4     │
    /// └────────────┴──────────────────────┴────────────┘
    /// </summary>
    public class StirrupRebar
    {
        /// <summary>Thông tin hình học của hệ dầm.</summary>
        private BeamInfo BeamInfo { get; set; }

        /// <summary>Loại thép đai vùng 2 đầu nhịp (bước dày hơn).</summary>
        private RebarBarType EndBarType { get; set; }

        /// <summary>Loại thép đai vùng giữa nhịp (bước thưa hơn).</summary>
        private RebarBarType CenterBarType { get; set; }

        /// <summary>Bước đai vùng 2 đầu (feet).</summary>
        private double EndSpacing { get; set; }

        /// <summary>Bước đai vùng giữa (feet).</summary>
        private double CenterSpacing { get; set; }

        /// <summary>Chiều dày lớp bảo vệ (feet).</summary>
        private double Cover { get; set; }

        /// <summary>Tài liệu Revit để tạo phần tử.</summary>
        private Document Document { get; set; }

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
        /// Tạo 4 đoạn thẳng tạo thành hình chữ nhật của một vòng đai tại vị trí
        /// <paramref name="position"/> trên trục dầm, sử dụng kích thước của <paramref name="span"/>.
        /// </summary>
        private List<Curve> GetStirrupCurves(XYZ position, SpanInfo span)
        {
            var halfWidth = span.Width / 2 - Cover;
            var topOffset = Cover;
            var bottomOffset = span.Height - Cover;

            var p1 = position.Add(halfWidth * (-BeamInfo.CrossDirection)).Add(topOffset * (-XYZ.BasisZ));
            var p2 = position.Add(halfWidth * BeamInfo.CrossDirection).Add(topOffset * (-XYZ.BasisZ));
            var p3 = position.Add(halfWidth * BeamInfo.CrossDirection).Add(bottomOffset * (-XYZ.BasisZ));
            var p4 = position.Add(halfWidth * (-BeamInfo.CrossDirection)).Add(bottomOffset * (-XYZ.BasisZ));

            return new List<Curve>
            {
                Line.CreateBound(p1, p2),
                Line.CreateBound(p2, p3),
                Line.CreateBound(p3, p4),
                Line.CreateBound(p4, p1)
            };
        }

        /// <summary>
        /// Tạo thực tế các đai thép trên Revit theo từng nhịp.
        /// Mỗi nhịp được chia thành 3 vùng (L/4, L/2, L/4) với kích thước đai
        /// khớp đúng tiết diện h/b của nhịp đó.
        /// </summary>
        public void RebarCreation()
        {
            var host = DirectShape.CreateElement(Document, new ElementId(BuiltInCategory.OST_StructuralFraming));

            foreach (var span in BeamInfo.Spans)
            {
                var spanLength = span.StartPoint.DistanceTo(span.EndPoint);
                var endZoneLength = spanLength / 4;
                var centerZoneLength = spanLength / 2;

                // Vùng 1: đai đầu nhịp (từ StartPoint → L/4)
                try
                {
                    var curves = GetStirrupCurves(span.StartPoint, span);
                    Document.CreateRebarMaximumSpacing(RebarStyle.StirrupTie, EndBarType, host,
                        BeamInfo.Direction, curves, endZoneLength, EndSpacing);
                }
                catch { }

                // Vùng 2: đai giữa nhịp (từ L/4 → 3L/4)
                try
                {
                    var zone2Start = span.StartPoint.Add(endZoneLength * BeamInfo.Direction);
                    var curves = GetStirrupCurves(zone2Start, span);
                    Document.CreateRebarMaximumSpacing(RebarStyle.StirrupTie, CenterBarType, host,
                        BeamInfo.Direction, curves, centerZoneLength, CenterSpacing);
                }
                catch { }

                // Vùng 3: đai cuối nhịp (từ 3L/4 → EndPoint)
                try
                {
                    var zone3Start = span.StartPoint.Add((endZoneLength + centerZoneLength) * BeamInfo.Direction);
                    var curves = GetStirrupCurves(zone3Start, span);
                    Document.CreateRebarMaximumSpacing(RebarStyle.StirrupTie, EndBarType, host,
                        BeamInfo.Direction, curves, endZoneLength, EndSpacing);
                }
                catch { }
            }
        }
    }
}
