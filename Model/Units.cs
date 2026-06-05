using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Model
{
    /// <summary>
    /// Các phương thức mở rộng hỗ trợ chuyển đổi đơn vị.
    /// Revit sử dụng đơn vị feet nội bộ; dữ liệu nhập từ người dùng thường là mm.
    /// Hệ số chuyển đổi: 1 foot = 304.8 mm.
    /// </summary>
    public static class Units
    {
        /// <summary>
        /// Chuyển đổi từ milimét (mm) sang feet.
        /// Dùng khi cần đưa giá trị do người dùng nhập (mm) vào API Revit (feet).
        /// </summary>
        /// <param name="mm">Giá trị milimét cần chuyển.</param>
        /// <returns>Giá trị tương đương tính bằng feet.</returns>
        public static double MmToFeet(this double mm)
        {
            return mm / 304.8;
        }

        /// <summary>
        /// Chuyển đổi từ feet sang milimét (mm).
        /// Dùng khi cần hiển thị hoặc vẽ canvas từ giá trị nội bộ Revit (feet).
        /// </summary>
        /// <param name="feet">Giá trị feet cần chuyển.</param>
        /// <returns>Giá trị tương đương tính bằng mm.</returns>
        public static double FeetToMm(this double feet)
        {
            return feet * 304.8;
        }
    }
}
