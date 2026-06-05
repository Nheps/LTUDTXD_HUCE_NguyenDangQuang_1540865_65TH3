using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Model
{
    /// <summary>
    /// Lưu trữ toàn bộ thông tin hình học của một hoặc nhiều dầm được chọn.
    /// Các dầm phải cùng tiết diện (h, b) và nằm trên cùng một trục.
    /// Đơn vị nội bộ: feet (đơn vị Revit).
    /// </summary>
    public class BeamInfo
    {
        /// <summary>Danh sách các FamilyInstance dầm được chọn.</summary>
        public List<FamilyInstance> Families { get; }

        /// <summary>Chiều cao tiết diện dầm (feet).</summary>
        public double Height { get; private set; }

        /// <summary>Chiều rộng tiết diện dầm (feet).</summary>
        public double Width { get; private set; }

        /// <summary>
        /// Điểm đầu trục dầm — tại mép trên của dầm (top justification).
        /// Toàn bộ thép được tính offset từ điểm này.
        /// </summary>
        public XYZ StartPoint { get; private set; }

        /// <summary>Điểm cuối trục dầm — tại mép trên của dầm.</summary>
        public XYZ EndPoint { get; private set; }

        /// <summary>Vector đơn vị dọc theo trục dầm (từ Start → End).</summary>
        public XYZ Direction { get; private set; }

        /// <summary>
        /// Vector đơn vị vuông góc với trục dầm trong mặt phẳng ngang
        /// (tích có hướng của Direction × BasisZ).
        /// Dùng để phân bố thép theo chiều rộng.
        /// </summary>
        public XYZ CrossDirection { get; private set; }

        /// <summary>
        /// Khởi tạo BeamInfo từ danh sách dầm đã chọn.
        /// Tự động trích xuất kích thước h/b và xác định trục dầm.
        /// </summary>
        public BeamInfo(List<Element> elements)
        {
            if (elements == null) return;
            Families = elements.Select(x => x as FamilyInstance).ToList();
            GetHeightAndWidth();
            GetStartAndEndPoint();
        }

        /// <summary>
        /// Đọc tham số "h" (chiều cao) và "b" (chiều rộng) từ kiểu dầm.
        /// Nếu các dầm không đồng nhất tiết diện thì Height/Width giữ nguyên 0.
        /// </summary>
        private void GetHeightAndWidth()
        {
            var heights = new List<double>();
            var widths = new List<double>();

            foreach (var beam in Families)
            {
                var type = beam.Document.GetElement(beam.GetTypeId());
                var h = type.LookupParameter("h").AsDouble(); // chiều cao (feet)
                var w = type.LookupParameter("b").AsDouble(); // chiều rộng (feet)
                heights.Add(h);
                widths.Add(w);
            }

            // Loại bỏ trùng lặp — tất cả dầm phải có cùng tiết diện
            heights = heights.Distinct().ToList();
            widths = widths.Distinct().ToList();

            // Chỉ gán nếu tất cả dầm có cùng h và b
            if (heights.Count != 1 || widths.Count != 1) return;
            Height = heights[0];
            Width = widths[0];
        }

        /// <summary>
        /// Xác định điểm đầu, điểm cuối và các vector phương của cụm dầm.
        /// Khi có nhiều dầm liên tiếp, lấy điểm đầu của dầm đầu tiên
        /// và điểm cuối của dầm cuối cùng theo chiều Direction.
        /// </summary>
        private void GetStartAndEndPoint()
        {
            // Lấy hướng trục từ dầm đầu tiên
            Direction = ((Families[0].Location as LocationCurve)?.Curve as Line)?.Direction;

            // CrossDirection = hướng ngang (vuông góc trục dầm, trong mặt phẳng nằm ngang)
            CrossDirection = Direction!.CrossProduct(XYZ.BasisZ);

            // Thu thập tất cả đường trục của các dầm
            var curves = new List<Curve>();
            foreach (var beam in Families)
            {
                var locationCurve = (beam.Location as LocationCurve)?.Curve;
                curves.Add(locationCurve);
            }

            // Sắp xếp dầm theo chiều Direction (từ nhỏ đến lớn)
            var curs = curves.OrderBy(x => (x as Line)?.Direction.DotProduct(Direction) ?? 0);
            if (!curs.Any()) return;

            // Điểm đầu = điểm 0 của dầm đầu tiên; điểm cuối = điểm 1 của dầm cuối cùng
            StartPoint = curs.First().GetEndPoint(0);
            EndPoint = curs.Last().GetEndPoint(1);
        }
    }
}
