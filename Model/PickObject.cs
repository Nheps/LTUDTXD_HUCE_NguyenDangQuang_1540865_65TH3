using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.UI;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Model
{
    /// <summary>
    /// Các phương thức hỗ trợ chọn dầm từ giao diện Revit.
    /// </summary>
    public static class PickObject
    {
        /// <summary>
        /// Yêu cầu người dùng chọn một hoặc nhiều dầm trên mô hình Revit.
        /// Chỉ cho phép chọn phần tử thuộc danh mục Structural Framing (dầm).
        /// Sau khi chọn, kiểm tra tất cả dầm phải có cùng tiết diện h và b.
        /// </summary>
        /// <returns>
        /// Danh sách dầm hợp lệ, hoặc null nếu không chọn gì
        /// hoặc các dầm không đồng nhất tiết diện.
        /// </returns>
        public static List<Element> PickBeams(this UIDocument uiDocument)
        {
            // Cho phép người dùng chọn nhiều phần tử, lọc qua BeamFilter
            var eles = uiDocument.Selection.PickObjects(ObjectType.Element, new BeamFilter())
              .Select(x => uiDocument.Document.GetElement(x) as Element).ToList();

            if (!eles.Any()) return null;

            // Kiểm tra tất cả dầm có cùng tiết diện không
            return eles.CheckBeams() ? eles : null;
        }

        /// <summary>
        /// Kiểm tra danh sách dầm: tất cả phải có cùng chiều cao h và chiều rộng b.
        /// Điều này cần thiết vì tool chỉ hỗ trợ bố trí thép cho dầm đồng nhất tiết diện.
        /// </summary>
        /// <returns>true nếu tất cả dầm có cùng h và b, ngược lại false.</returns>
        private static bool CheckBeams(this List<Element> eles)
        {
            var heights = new List<double>();
            var widths = new List<double>();

            foreach (var beam in eles)
            {
                var type = beam.Document.GetElement(beam.GetTypeId());
                var h = type.LookupParameter("h").AsDouble(); // chiều cao dầm
                var w = type.LookupParameter("b").AsDouble(); // chiều rộng dầm
                heights.Add(h);
                widths.Add(w);
            }

            // Nếu sau khi loại trùng còn đúng 1 giá trị → tất cả dầm cùng tiết diện
            var enumerable = heights.Distinct().ToList();
            var distinct = widths.Distinct().ToList();
            return enumerable.Count == 1 && distinct.Count == 1;
        }
    }

    /// <summary>
    /// Bộ lọc chọn phần tử: chỉ cho phép chọn dầm kết cấu (Structural Framing).
    /// Được dùng khi gọi PickObjects để hạn chế người dùng chỉ chọn dầm.
    /// </summary>
    public class BeamFilter : ISelectionFilter
    {
        /// <summary>
        /// Cho phép chọn phần tử nếu:
        ///   - Là FamilyInstance (cấu kiện họ)
        ///   - Có LocationCurve (nằm trên đường trục)
        ///   - Thuộc danh mục OST_StructuralFraming (dầm/cột/thanh kết cấu)
        /// </summary>
        public bool AllowElement(Element elem)
        {
            // Chỉ xét FamilyInstance
            if (elem is not FamilyInstance familyInstance) return false;

            // Phải có đường trục (LocationCurve) — loại bỏ cột hoặc cấu kiện điểm
            if ((familyInstance.Location as LocationCurve)?.Curve == null) return false;

            // Kiểm tra thuộc danh mục dầm kết cấu
            return familyInstance.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming;
        }

        /// <summary>
        /// Không sử dụng lọc theo Reference trong tool này.
        /// </summary>
        public bool AllowReference(Reference reference, XYZ position)
        {
            throw new NotImplementedException();
        }
    }
}
