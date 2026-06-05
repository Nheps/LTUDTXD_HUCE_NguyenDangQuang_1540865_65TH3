using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB.Structure;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Model
{
    /// <summary>
    /// Tập hợp các phương thức mở rộng để tạo cốt thép (Rebar) trong Revit.
    /// Bao bọc Revit API giúp code nghiệp vụ gọn hơn.
    /// Tất cả phương thức đều dùng hook Left-Left (móc thép trái-trái).
    /// </summary>
    public static class RebarUtils
    {
        /// <summary>
        /// Tạo một thanh thép đơn lẻ (không phân bố).
        /// Dùng cho thép chính khi chỉ cần một thanh tại vị trí xác định.
        /// </summary>
        /// <param name="document">Tài liệu Revit hiện hành.</param>
        /// <param name="rebarStyle">Kiểu thép: Standard (thẳng) hoặc StirrupTie (đai).</param>
        /// <param name="rebarBarType">Loại thép (đường kính, vật liệu).</param>
        /// <param name="host">Phần tử chứa cốt thép (dùng DirectShape làm host).</param>
        /// <param name="normal">Vector pháp tuyến mặt phẳng chứa thép — cũng là chiều phân bố.</param>
        /// <param name="curves">Danh sách đường cong định nghĩa hình dạng thanh thép.</param>
        public static Rebar CreateRebarSingle(this Document document, RebarStyle rebarStyle, RebarBarType rebarBarType, Element host, XYZ normal,
        List<Curve> curves)
        {
            var rebar = Rebar.CreateFromCurves(document, rebarStyle, rebarBarType, null, null, host, normal, curves, RebarHookOrientation.Left,
                RebarHookOrientation.Left, true, true);
            return rebar;
        }

        /// <summary>
        /// Tạo dãy thép với số lượng cố định, phân bố đều trên một khoảng dài <paramref name="lengthArr"/>.
        /// Dùng khi biết chính xác số thanh thép cần đặt.
        /// </summary>
        /// <param name="lengthArr">Tổng chiều dài phân bố (feet).</param>
        /// <param name="quantity">Số thanh thép cần tạo.</param>
        public static Rebar CreateRebarFixedNumber(this Document document, RebarStyle rebarStyle, RebarBarType rebarBarType, Element host, XYZ normal,
            List<Curve> curves, double lengthArr, int quantity)
        {
            var rebar = Rebar.CreateFromCurves(document, rebarStyle, rebarBarType, null, null, host, normal, curves, RebarHookOrientation.Left,
                RebarHookOrientation.Left, true, true);
            if (rebar == null || quantity <= 0 || !(lengthArr > 0.0)) return rebar;

            // Phân bố cố định số lượng: Revit tự tính khoảng cách đều
            rebar.GetShapeDrivenAccessor().SetLayoutAsFixedNumber(quantity, lengthArr, true, true, true);
            rebar.IncludeFirstBar = true;
            rebar.IncludeLastBar = true;

            return rebar;
        }

        /// <summary>
        /// Tạo dãy thép với bước cách tối đa <paramref name="spacing"/>, phân bố trên <paramref name="lengthArr"/>.
        /// Revit tự xác định số lượng thanh sao cho khoảng cách không vượt quá spacing.
        /// Dùng cho thép đai (stirrup) khi chỉ biết bước đai.
        /// </summary>
        /// <param name="lengthArr">Tổng chiều dài vùng phân bố (feet).</param>
        /// <param name="spacing">Bước cách tối đa giữa các thanh (feet).</param>
        public static Rebar CreateRebarMaximumSpacing(this Document document, RebarStyle rebarStyle, RebarBarType rebarBarType, Element host, XYZ normal,
            List<Curve> curves, double lengthArr, double spacing)
        {
            var rebar = Rebar.CreateFromCurves(document, rebarStyle, rebarBarType, null, null, host, normal, curves, RebarHookOrientation.Left,
                RebarHookOrientation.Left, true, true);
            if (rebar == null || !(spacing > 0.0) || !(lengthArr > 0.0)) return rebar;

            // Phân bố theo bước tối đa: Revit tính số thanh = ceil(lengthArr / spacing) + 1
            rebar.GetShapeDrivenAccessor().SetLayoutAsMaximumSpacing(spacing, lengthArr, true, true, true);
            rebar.IncludeFirstBar = true;
            rebar.IncludeLastBar = true;

            return rebar;
        }

        /// <summary>
        /// Tạo dãy thép với số lượng và bước cách đều được xác định cụ thể.
        /// Khác với FixedNumber, phân bố này giữ nguyên bước <paramref name="spacing"/> giữa các thanh.
        /// </summary>
        /// <param name="quantity">Số thanh thép.</param>
        /// <param name="spacing">Bước cách đều giữa các thanh (feet).</param>
        public static Rebar CreateRebarNumberWithSpacing(this Document document, RebarStyle rebarStyle, RebarBarType rebarBarType, Element host,
            XYZ normal, List<Curve> curves, int quantity, double spacing)
        {
            var rebar = Rebar.CreateFromCurves(document, rebarStyle, rebarBarType, null, null, host, normal, curves, RebarHookOrientation.Left,
                RebarHookOrientation.Left, true, true);
            if (rebar == null || !(quantity > 0.0) || !(spacing > 0.0)) return rebar;

            // Phân bố theo số lượng + bước cách
            rebar.GetShapeDrivenAccessor().SetLayoutAsNumberWithSpacing(quantity, spacing, true, true, true);
            rebar.IncludeFirstBar = true;
            rebar.IncludeLastBar = true;

            return rebar;
        }

        /// <summary>
        /// Tạo dãy thép với khoảng hở tối thiểu <paramref name="spacing"/> giữa các thanh.
        /// Khác với MaximumSpacing, đây là khoảng thông thủy (clear spacing), không phải tim đến tim.
        /// </summary>
        /// <param name="lengthArr">Tổng chiều dài vùng phân bố (feet).</param>
        /// <param name="spacing">Khoảng hở tối thiểu giữa các thanh (feet).</param>
        public static Rebar CreateRebarMinimumClearSpacing(this Document document, RebarStyle rebarStyle, RebarBarType rebarBarType, Element host,
            XYZ normal, List<Curve> curves, double lengthArr, double spacing)
        {
            var rebar = Rebar.CreateFromCurves(document, rebarStyle, rebarBarType, null, null, host, normal, curves, RebarHookOrientation.Left,
                RebarHookOrientation.Left, true, true);
            if (rebar == null || !(lengthArr > 0.0) || !(spacing > 0.0)) return rebar;

            // Phân bố theo khoảng hở tối thiểu
            rebar.GetShapeDrivenAccessor().SetLayoutAsMinimumClearSpacing(spacing, lengthArr, true, true, true);
            rebar.IncludeFirstBar = true;
            rebar.IncludeLastBar = true;

            return rebar;
        }

        /// <summary>
        /// Tạo thép dạng tự do (FreeForm) từ danh sách CurveLoop.
        /// Dùng cho các hình dạng thép phức tạp không theo kiểu Shape-Driven thông thường.
        /// </summary>
        /// <param name="curves">Danh sách vòng đường cong khép kín định nghĩa hình dạng thép.</param>
        public static Rebar CreateRebarFreeForm(this Document document, RebarBarType rebarBarType, Element host, List<CurveLoop> curves)
        {
            return Rebar.CreateFreeForm(document, rebarBarType, host, curves, out RebarFreeFormValidationResult result);
        }
    }
}
