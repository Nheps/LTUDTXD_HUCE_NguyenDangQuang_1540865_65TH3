using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Model
{
    /// <summary>
    /// Phân loại vị trí lớp thép trong tiết diện dầm.
    /// Dùng để xác định offset (khoảng cách từ mép dầm) khi tính tọa độ đặt thép.
    /// </summary>
    public enum RebarBeamType
    {
        /// <summary>Thép trên lớp 1 — cách mép trên 50mm (lớp bảo vệ).</summary>
        Top1,

        /// <summary>Thép trên lớp 2 — cách mép trên 130mm.</summary>
        Top2,

        /// <summary>Thép trên lớp 3 — cách mép trên 210mm.</summary>
        Top3,

        /// <summary>Thép dưới lớp 1 — cách mép dưới 50mm (lớp bảo vệ).</summary>
        Bottom1,

        /// <summary>Thép dưới lớp 2 — cách mép dưới 130mm.</summary>
        Bottom2,

        /// <summary>Thép dưới lớp 3 — cách mép dưới 210mm.</summary>
        Bottom3,

        /// <summary>Thép đai — bố trí dọc theo chiều dài dầm dưới dạng vòng khép kín.</summary>
        Stirrup
    }
}
