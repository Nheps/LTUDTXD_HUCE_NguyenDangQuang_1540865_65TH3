using Autodesk.Revit.DB.Structure;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Model
{
	/// <summary>
	/// Quản lý tính toán tọa độ và tạo thép chính phía DƯỚI cho MỘT nhịp.
	/// Neo chỉ áp dụng ở đầu ngoài cùng của nhịp; gối giữa không neo.
	/// </summary>
	public class LowerRebar
	{
		private BeamInfo BeamInfo { get; set; }
		private double Anchor { get; set; }
		private List<List<Curve>> Curves { get; set; } = new();
		private int Quantity { get; set; }
		private RebarBarType RebarBarType { get; set; }
		private Document Document { get; set; }
		private double OffsetMm { get; set; }

		/// <param name="offsetMm">Khoảng cách từ mép dưới nhịp lên đến tâm lớp thép (mm).</param>
		/// <param name="span">Thông tin nhịp cần vẽ thép.</param>
		/// <param name="isFirstSpan">True nếu đây là nhịp đầu tiên (neo đầu trái).</param>
		/// <param name="isLastSpan">True nếu đây là nhịp cuối cùng (neo đầu phải).</param>
		/// <param name="anchor">Chiều dài đoạn neo uốn lên tại đầu ngoài (mm). 0 = không neo.</param>
		public LowerRebar(double offsetMm, RebarBarType rebarBarType, BeamInfo beamInfo,
						  SpanInfo span, bool isFirstSpan, bool isLastSpan, double anchor, int quantity)
		{
			OffsetMm = offsetMm;
			RebarBarType = rebarBarType;
			BeamInfo = beamInfo;
			Document = beamInfo.Families[0].Document;
			Anchor = anchor.MmToFeet();
			Quantity = quantity;

			RebarAnalys(span, isFirstSpan, isLastSpan);
		}

		private void RebarAnalys(SpanInfo span, bool isFirst, bool isLast)
		{
			if (Quantity is 0 or 1) return;

			var height = span.Height;
			var spanStart = span.StartPoint.Add(height * -XYZ.BasisZ)
										   .Add(OffsetMm.MmToFeet() * XYZ.BasisZ);
			var spanEnd = span.EndPoint.Add(height * -XYZ.BasisZ)
										 .Add(OffsetMm.MmToFeet() * XYZ.BasisZ);

			var spanWidth = span.Width - 50.0.MmToFeet();
			var distance = spanWidth / (Quantity - 1);

			var startLeft = spanStart.Add(spanWidth / 2 * -BeamInfo.CrossDirection);
			var endLeft = spanEnd.Add(spanWidth / 2 * -BeamInfo.CrossDirection);

			for (int i = 0; i < Quantity; i++)
			{
				var p2 = startLeft.Add(i * distance * BeamInfo.CrossDirection);
				var p3 = endLeft.Add(i * distance * BeamInfo.CrossDirection);

				if (Anchor == 0)
				{
					Curves.Add(new List<Curve> { Line.CreateBound(p2, p3) });
				}
				else if (isFirst && isLast)
				{
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
					var p1 = p2.Add(Anchor * XYZ.BasisZ);
					Curves.Add(new List<Curve>
					{
						Line.CreateBound(p1, p2),
						Line.CreateBound(p2, p3)
					});
				}
				else if (isLast)
				{
					var p4 = p3.Add(Anchor * XYZ.BasisZ);
					Curves.Add(new List<Curve>
					{
						Line.CreateBound(p2, p3),
						Line.CreateBound(p3, p4)
					});
				}
				else
				{
					Curves.Add(new List<Curve> { Line.CreateBound(p2, p3) });
				}
			}
		}

		public void RebarCreation()
		{
			var host = DirectShape.CreateElement(Document, new ElementId(BuiltInCategory.OST_StructuralFraming));
			foreach (var curves in Curves)
			{
				try { Document.CreateRebarSingle(RebarStyle.Standard, RebarBarType, host, BeamInfo.CrossDirection, curves); }
				catch { }
			}
		}
	}
}
