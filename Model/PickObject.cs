using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
        /// Các dầm phải có cùng chiều rộng b (cho phép khác chiều cao h).
        /// </summary>
        /// <returns>Danh sách dầm hợp lệ, hoặc null nếu không chọn gì hoặc chiều rộng không đồng nhất.</returns>
        public static List<Element> PickBeams(this UIDocument uiDocument)
        {
            var eles = uiDocument.Selection.PickObjects(ObjectType.Element, new BeamFilter())
              .Select(x => uiDocument.Document.GetElement(x) as Element).ToList();

            if (!eles.Any()) return null;

            // Kiểm tra tất cả dầm có cùng chiều rộng b
            var widths = eles.Select(e => e.Document.GetElement(e.GetTypeId()).LookupParameter("b").AsDouble())
                             .Distinct().ToList();

            if (widths.Count != 1)
            {
                MessageBox.Show("Các dầm phải có cùng chiều rộng b!", "Lỗi chọn dầm");
                return null;
            }

            return eles;
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
