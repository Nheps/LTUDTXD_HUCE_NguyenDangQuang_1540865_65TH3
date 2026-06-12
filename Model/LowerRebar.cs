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
    ///
    /// Với dầm khác tiết diện: thép dưới được tạo riêng theo từng nhịp vì cao độ đáy
    /// thay đổi theo chiều cao từng nhịp. Neo chỉ áp dụng ở 2 đầu ngoài cùng của hệ dầm.
    ///
    /// Lưu ý: yêu cầu quantity >= 2 mới tạo thép (1 thanh không đủ để phân bố).
    /// </summary>
    public class LowerRebar
    {
        /// <summary>Lớp thép dưới (dùng chung enum với Top, xem offset trong RebarAnalys).</summary>
        private RebarBeamType RebarBeamType { get; set; }

        /// <summary>Thông tin hình học của hệ dầm.</summary>
        private BeamInfo BeamInfo { get; set; }

        /// <summary>Chiều dài đoạn neo thép uốn lên tại 2 đầu ngoài cùng (feet). 0 = không có neo.</summary>
        private double Anchor { get; set; }

        /// <summary>
        /// Danh sách đường cong của từng thanh thép.
        /// Mỗi phần tử là danh sách Curve của một thanh (1 hoặc 2-3 đoạn nếu có neo).
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
        /// <param name="beamInfo">Thông tin hệ dầm đã chọn.</param>
        /// <param name="anchor">Chiều dài neo tại 2 đầu ngoài cùng (mm). 0 = không có neo.</param>
        /// <param name="quantity">Số lượng thanh thép cần đặt.</param>
        public LowerRebar(RebarBeamType rebarBeamType, RebarBarType rebarBarType, BeamInfo beamInfo, double anchor, int quantity)
        {
            RebarBeamType = rebarBeamType;
            RebarBarType = rebarBarType;
            BeamInfo = beamInfo;
            Document = beamInfo.Families[0].Document;
            Anchor = anchor.MmToFeet();
            Quantity = quantity;

            RebarAnalys();
        }

        /// <summary>
        /// Tính toán tọa độ từng thanh thép dưới theo từng nhịp.
        ///
        /// Mỗi nhịp sử dụng chiều cao và chiều rộng riêng của nhịp đó.
        /// Neo chỉ áp dụng tại đầu ngoài cùng bên trái (nhịp đầu) và bên phải (nhịp cuối).
        /// Các gối giữa không có neo để thép có thể tiếp giáp tự nhiên.
        /// </summary>
        private void RebarAnalys()
        {
            if (Quantity is 0 or 1) return;

            for (int spanIdx = 0; spanIdx < BeamInfo.Spans.Count; spanIdx++)
            {
                var span = BeamInfo.Spans[spanIdx];
                bool isFirst = spanIdx == 0;
                bool isLast = spanIdx == BeamInfo.Spans.Count - 1;
                var height = span.Height - 25.0.MmToFeet();

				// Dịch xuống đáy nhịp (StartPoint/EndPoint là mép trên)
				var spanStart = span.StartPoint.Add(height * -XYZ.BasisZ);
                var spanEnd = span.EndPoint.Add(height * -XYZ.BasisZ);

                // Offset lên từ mép dưới theo lớp thép
                switch (RebarBeamType)
                {
                    case RebarBeamType.Top1:
                        spanStart = spanStart.Add(50.0.MmToFeet() * XYZ.BasisZ);
                        spanEnd = spanEnd.Add(50.0.MmToFeet() * XYZ.BasisZ);
                        break;
                    case RebarBeamType.Top2:
                        spanStart = spanStart.Add(130.0.MmToFeet() * XYZ.BasisZ);
                        spanEnd = spanEnd.Add(130.0.MmToFeet() * XYZ.BasisZ);
                        break;
                    case RebarBeamType.Top3:
                        spanStart = spanStart.Add(210.0.MmToFeet() * XYZ.BasisZ);
                        spanEnd = spanEnd.Add(210.0.MmToFeet() * XYZ.BasisZ);
                        break;
                }

                // Phân bố ngang theo chiều rộng nhịp hiện tại
                var spanWidth = span.Width - 50.0.MmToFeet();
                var distance = spanWidth / (Quantity - 1);

                spanStart = spanStart.Add(spanWidth / 2 * -BeamInfo.CrossDirection);
                spanEnd = spanEnd.Add(spanWidth / 2 * -BeamInfo.CrossDirection);

                for (int i = 0; i < Quantity; i++)
                {
                    var p2 = spanStart.Add(i * distance * BeamInfo.CrossDirection); // góc trái nhịp
                    var p3 = spanEnd.Add(i * distance * BeamInfo.CrossDirection);   // góc phải nhịp

                    if (Anchor == 0)
                    {
                        // Không có neo: thanh thẳng cho nhịp này
                        Curves.Add(new List<Curve> { Line.CreateBound(p2, p3) });
                    }
                    else if (isFirst && isLast)
                    {
                        // Dầm 1 nhịp: neo ở cả 2 đầu
                        var p1 = p2.Add(Anchor * XYZ.BasisZ);
                        var p4 = p3.Add(Anchor * XYZ.BasisZ);
                        Curves.Add(new List<Curve>
                        {
                            Line.CreateBound(p1, p2),
                            Line.CreateBound(p2, p3),
                            Line.CreateBound(p3, p4)
                        });
                    }
                    else if (isFirst)
                    {
                        // Nhịp đầu tiên (nhiều nhịp): neo ở đầu trái, không neo ở đầu phải (gối giữa)
                        var p1 = p2.Add(Anchor * XYZ.BasisZ);
                        Curves.Add(new List<Curve>
                        {
                            Line.CreateBound(p1, p2),
                            Line.CreateBound(p2, p3)
                        });
                    }
                    else if (isLast)
                    {
                        // Nhịp cuối cùng (nhiều nhịp): không neo ở đầu trái (gối giữa), neo ở đầu phải
                        var p4 = p3.Add(Anchor * XYZ.BasisZ);
                        Curves.Add(new List<Curve>
                        {
                            Line.CreateBound(p2, p3),
                            Line.CreateBound(p3, p4)
                        });
                    }
                    else
                    {
                        // Nhịp giữa (nhiều nhịp): không neo ở cả 2 đầu (đều là gối giữa)
                        Curves.Add(new List<Curve> { Line.CreateBound(p2, p3) });
                    }
                }
            }
        }

        /// <summary>
        /// Tạo thực tế các thanh thép dưới trên Revit từ danh sách đường cong đã tính.
        /// Dùng DirectShape làm host. Bỏ qua lỗi từng thanh để không làm gián đoạn toàn bộ.
        /// </summary>
        public void RebarCreation()
        {
            var host = DirectShape.CreateElement(Document, new ElementId(BuiltInCategory.OST_StructuralFraming));

            foreach (var curves in Curves)
            {
                try
                {
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
