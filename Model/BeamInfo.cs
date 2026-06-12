using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Model
{
    /// <summary>
    /// Lưu trữ toàn bộ thông tin hình học của một hoặc nhiều dầm được chọn.
    /// Hỗ trợ cả dầm cùng tiết diện và dầm khác tiết diện.
    /// Đơn vị nội bộ: feet (đơn vị Revit).
    /// </summary>
    public class BeamInfo
    {
        /// <summary>Danh sách các FamilyInstance dầm được chọn.</summary>
        public List<FamilyInstance> Families { get; }

        /// <summary>Danh sách thông tin từng nhịp (đã sắp xếp theo chiều dầm).</summary>
        public List<SpanInfo> Spans { get; private set; } = new();

        /// <summary>Chiều cao lớn nhất trong các nhịp (feet) — dùng cho canvas preview.</summary>
        public double Height { get; private set; }

        /// <summary>Chiều rộng lớn nhất trong các nhịp (feet) — dùng cho canvas preview.</summary>
        public double Width { get; private set; }

        /// <summary>Chiều rộng nhỏ nhất trong các nhịp (feet) — dùng phân bố thép trên liên tục.</summary>
        public double MinWidth { get; private set; }

        /// <summary>
        /// Điểm đầu trục toàn bộ hệ dầm — tại mép trên của dầm đầu tiên.
        /// Toàn bộ thép trên được tính offset từ điểm này.
        /// </summary>
        public XYZ StartPoint { get; private set; }

        /// <summary>Điểm cuối trục toàn bộ hệ dầm — tại mép trên của dầm cuối cùng.</summary>
        public XYZ EndPoint { get; private set; }

        /// <summary>Vector đơn vị dọc theo trục dầm (từ Start → End).</summary>
        public XYZ Direction { get; private set; }

        /// <summary>
        /// Vector đơn vị vuông góc với trục dầm trong mặt phẳng ngang.
        /// Dùng để phân bố thép theo chiều rộng.
        /// </summary>
        public XYZ CrossDirection { get; private set; }

        /// <summary>
        /// Khởi tạo BeamInfo từ danh sách dầm đã chọn.
        /// Tự động trích xuất kích thước h/b của từng nhịp và xác định trục dầm.
        /// </summary>
        public BeamInfo(List<Element> elements)
        {
            if (elements == null) return;
            Families = elements.Select(x => x as FamilyInstance).ToList();
            BuildSpans();
            ComputeExtents();
        }

        /// <summary>
        /// Xây dựng danh sách nhịp (Spans), xác định hướng trục và điểm đầu/cuối.
        /// Sắp xếp các dầm theo chiều tăng dần của tọa độ dọc trục.
        /// </summary>
        private void BuildSpans()
        {
            if (!Families.Any()) return;

            Direction = ((Families[0].Location as LocationCurve)?.Curve as Line)?.Direction;
            if (Direction == null) return;

            CrossDirection = Direction.CrossProduct(XYZ.BasisZ);

            // Sắp xếp dầm theo vị trí điểm đầu chiếu lên trục Direction
            Spans = Families
                .Select(b =>
                {
                    var curve = (b.Location as LocationCurve)?.Curve;
                    var type = b.Document.GetElement(b.GetTypeId());
                    return new SpanInfo
                    {
                        Family = b,
                        StartPoint = curve?.GetEndPoint(0),
                        EndPoint = curve?.GetEndPoint(1),
                        Height = type.LookupParameter("h").AsDouble(),
                        Width = type.LookupParameter("b").AsDouble()
                    };
                })
                .OrderBy(s => s.StartPoint?.DotProduct(Direction) ?? 0)
                .ToList();

            StartPoint = Spans.First().StartPoint;
            EndPoint = Spans.Last().EndPoint;
        }

        /// <summary>
        /// Tính chiều cao/rộng lớn nhất và nhỏ nhất từ danh sách nhịp.
        /// </summary>
        private void ComputeExtents()
        {
            if (!Spans.Any()) return;
            Height = Spans.Max(s => s.Height);
            Width = Spans.Max(s => s.Width);
            MinWidth = Spans.Min(s => s.Width);
        }
    }
}
