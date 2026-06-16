using Autodesk.Revit.DB.Structure;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Model
{
	/// <summary>
	/// Quản lý tính toán tọa độ và tạo thép chính phía TRÊN cho MỘT nhịp.
	/// Offset từ mép trên được truyền trực tiếp qua <paramref name="offsetMm"/>.
	/// </summary>
	public class UpperRebar
	{
		private XYZ Start { get; set; }
		private XYZ End { get; set; }
		private double Anchor { get; set; }
		private List<List<Curve>> Curves { get; set; } = new();
		private int Quantity { get; set; }
		private RebarBarType RebarBarType { get; set; }
		private Document Document { get; set; }
		private BeamInfo BeamInfo { get; set; }

		private double OffsetMm { get; set; }

		/// <param name="offsetMm">Khoảng cách từ mép trên nhịp đến tâm lớp thép (mm).</param>
		/// <param name="spanStart">Điểm đầu nhịp (mép trên, tâm bề rộng).</param>
		/// <param name="spanEnd">Điểm cuối nhịp (mép trên, tâm bề rộng).</param>
		/// <param name="anchor">Chiều dài đoạn neo uốn xuống (mm). 0 = không neo.</param>
		public UpperRebar(double offsetMm, RebarBarType rebarBarType, BeamInfo beamInfo,
						  XYZ spanStart, XYZ spanEnd, double anchor, int quantity)
		{
			OffsetMm = offsetMm;
			BeamInfo = beamInfo;
			RebarBarType = rebarBarType;
			Quantity = quantity;
			Anchor = anchor.MmToFeet();
			Document = BeamInfo.Families.FirstOrDefault().Document;

			Start = spanStart.Add(offsetMm.MmToFeet() * -XYZ.BasisZ);
			End = spanEnd.Add(offsetMm.MmToFeet() * -XYZ.BasisZ);

			RebarAnalys();
		}

		private void RebarAnalys()
		{
			if (Quantity == 0) return;

			var width = BeamInfo.MinWidth - 50.0.MmToFeet();
			var distance = Quantity > 1 ? width / (Quantity - 1) : 0.0;

			var startLeft = Start.Add(width / 2 * -BeamInfo.CrossDirection);
			var endLeft = End.Add(width / 2 * -BeamInfo.CrossDirection);

			if (Anchor == 0)
			{
				for (int i = 0; i < Quantity; i++)
				{
					var p1 = startLeft.Add(i * distance * BeamInfo.CrossDirection);
					var p2 = endLeft.Add(i * distance * BeamInfo.CrossDirection);
					Curves.Add(new List<Curve> { Line.CreateBound(p1, p2) });
				}
			}
			else
			{
				var startAnchor = startLeft.Add(Anchor * -XYZ.BasisZ);
				var endAnchor = endLeft.Add(Anchor * -XYZ.BasisZ);

				for (int i = 0; i < Quantity; i++)
				{
					var p1 = startAnchor.Add(i * distance * BeamInfo.CrossDirection);
					var p2 = startLeft.Add(i * distance * BeamInfo.CrossDirection);
					var p3 = endLeft.Add(i * distance * BeamInfo.CrossDirection);
					var p4 = endAnchor.Add(i * distance * BeamInfo.CrossDirection);

					Curves.Add(new List<Curve>
					{
						Line.CreateBound(p1, p2),
						Line.CreateBound(p2, p3),
						Line.CreateBound(p3, p4)
					});
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
