using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB.Structure;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Model
{
    /// <summary>
    /// Quản lý việc tính toán tọa độ và tạo thép chính phía DƯỚI của dầm.
    /// Hỗ trợ tối đa 3 lớp thép dưới, offset lần lượt 50/130/210mm từ mép dưới.
    /// Logic tương tự UpperRebar nhưng tính từ mép dưới lên (chiều +Z).
    /// Lưu ý: yêu cầu quantity >= 2 mới tạo thép (1 thanh không đủ để phân bố).
    /// </summary>
    public class LowerRebar
    {
        /// <summary>Lớp thép dưới (dùng chung enum với Top, xem offset trong RebarAnalys).</summary>
        private RebarBeamType RebarBeamType { get; set; }

        /// <summary>Thông tin hình học của dầm.</summary>
        private BeamInfo BeamInfo { get; set; }

        /// <summary>Điểm đầu thanh thép (được tính toán lại trong RebarAnalys).</summary>
        private XYZ Start { get; set; }

        /// <summary>Điểm cuối thanh thép (được tính toán lại trong RebarAnalys).</summary>
        private XYZ End { get; set; }

        /// <summary>Chiều dài đoạn neo thép uốn lên tại 2 đầu dầm (feet). 0 = không có neo.</summary>
        private double Anchor { get; set; }

        /// <summary>
        /// Danh sách đường cong của từng thanh thép.
        /// Mỗi phần tử là danh sách Curve của một thanh (1 hoặc 3 đoạn nếu có neo).
        /// </summary>
        private List<List<Curve>> Curves { get; set; } = new();

        /// <summary>Số lượng thanh thép cần đặt trong lớp này (phải >= 2).</summary>
        private int Quantity { get; set; } = 0;

        /// <summary>Loại thép (đường kính, vật liệu) được chọn từ giao diện.</summary>
        private RebarBarType RebarBarType { get; set; }

        /// <summary>Tài liệu Revit để tạo phần tử.</summary>
        private Document Document { get; set; }

        /// <summary>
        /// Khởi tạo và tự động tính toán tọa độ các thanh thép dưới.
        /// </summary>
        /// <param name="rebarBeamType">Lớp thép (dùng Top1/Top2/Top3 để tra offset).</param>
        /// <param name="rebarBarType">Loại thép từ Revit.</param>
        /// <param name="beamInfo">Thông tin dầm đã chọn.</param>
        /// <param name="anchor">Chiều dài neo tại 2 đầu (mm). 0 = không có neo.</param>
        /// <param name="quantity">Số lượng thanh thép cần đặt.</param>
        public LowerRebar(RebarBeamType rebarBeamType, RebarBarType rebarBarType, BeamInfo beamInfo, double anchor, int quantity)
        {
            RebarBeamType = rebarBeamType;
            RebarBarType = rebarBarType;
            BeamInfo = beamInfo;
            Document = beamInfo.Families[0].Document;
            Start = BeamInfo.StartPoint;
            End = BeamInfo.EndPoint;
            Anchor = anchor.MmToFeet();
            Quantity = quantity;

            // Tính toán tọa độ thực tế của từng thanh thép
            RebarAnalys();
        }

        /// <summary>
        /// Tính toán tọa độ từng thanh thép dưới.
        ///
        /// Bước 1 — Dịch xuống đáy dầm: Start/End += Height * (-Z)
        ///
        /// Bước 2 — Offset lên từ mép dưới theo lớp:
        ///   Top1: +50mm,  Top2: +130mm,  Top3: +210mm  (chiều +Z = hướng lên)
        ///
        /// Bước 3 — Phân bố ngang: tương tự UpperRebar
        ///
        /// Bước 4 — Hình dạng thanh thép:
        ///   • Anchor = 0: thanh thẳng
        ///   • Anchor > 0: dạng chữ U gồm 3 đoạn:
        ///       đoạn neo đầu (↑) + đoạn ngang + đoạn neo cuối (↑)
        /// </summary>
        private void RebarAnalys()
        {
            // Cần ít nhất 2 thanh mới có ý nghĩa phân bố
            if (Quantity is 0 or 1) return;

            // Dịch chuyển về mép dưới dầm (StartPoint là mép trên)
            Start = Start.Add(BeamInfo.Height * -XYZ.BasisZ);
            End = End.Add(BeamInfo.Height * -XYZ.BasisZ);

            // Offset lên từ mép dưới theo lớp thép
            switch (RebarBeamType)
            {
                case RebarBeamType.Top1:
                    // Lớp 1 (Bot1): cách mép dưới 50mm
                    Start = Start.Add(50.0.MmToFeet() * XYZ.BasisZ);
                    End = End.Add(50.0.MmToFeet() * XYZ.BasisZ);
                    break;
                case RebarBeamType.Top2:
                    // Lớp 2 (Bot2): cách mép dưới 130mm
                    Start = Start.Add(130.0.MmToFeet() * XYZ.BasisZ);
                    End = End.Add(130.0.MmToFeet() * XYZ.BasisZ);
                    break;
                case RebarBeamType.Top3:
                    // Lớp 3 (Bot3): cách mép dưới 210mm
                    Start = Start.Add(210.0.MmToFeet() * XYZ.BasisZ);
                    End = End.Add(210.0.MmToFeet() * XYZ.BasisZ);
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
                // Có neo: mỗi thanh có 3 đoạn — neo đầu (↑) + ngang + neo cuối (↑)
                // Điểm đỉnh neo = lên thêm Anchor từ vị trí thép ngang
                var start = Start.Add(Anchor * XYZ.BasisZ); // đỉnh neo đầu
                var end = End.Add(Anchor * XYZ.BasisZ);     // đỉnh neo cuối

                for (int i = 0; i < Quantity; i++)
                {
                    var p1 = start.Add(i * distance * BeamInfo.CrossDirection); // đỉnh neo đầu
                    var p2 = Start.Add(i * distance * BeamInfo.CrossDirection); // góc quay đầu
                    var p3 = End.Add(i * distance * BeamInfo.CrossDirection);   // góc quay cuối
                    var p4 = end.Add(i * distance * BeamInfo.CrossDirection);   // đỉnh neo cuối

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
        /// Tạo thực tế các thanh thép dưới trên Revit từ danh sách đường cong đã tính.
        /// Dùng DirectShape làm host. Bỏ qua lỗi từng thanh để không làm gián đoạn toàn bộ.
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
                    // Bỏ qua lỗi cá biệt và tiếp tục
                }
            }
        }
    }
}
