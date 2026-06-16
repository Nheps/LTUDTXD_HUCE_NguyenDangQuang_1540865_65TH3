using Autodesk.Revit.DB.Structure;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Model
{
	/// <summary>
	/// Quản lý việc tính toán và tạo thép đai (stirrup) cho MỘT nhịp dầm.
	///
	/// Sơ đồ phân vùng:
	/// ┌────────────┬──────────────────────┬────────────┐
	/// │  Đai 2 đầu │      Đai giữa        │  Đai 2 đầu │
	/// │    L/4     │        L/2           │    L/4     │
	/// └────────────┴──────────────────────┴────────────┘
	/// </summary>
	public class StirrupRebar
	{
		private BeamInfo BeamInfo { get; set; }
		private SpanInfo Span { get; set; }
		private RebarBarType EndBarType { get; set; }
		private RebarBarType CenterBarType { get; set; }
		private double EndSpacing { get; set; }
		private double CenterSpacing { get; set; }
		private double Cover { get; set; }
		private Document Document { get; set; }

		/// <param name="span">Nhịp cần tạo đai.</param>
		public StirrupRebar(RebarBarType centerBarType, RebarBarType endBarType,
							BeamInfo beamInfo, SpanInfo span,
							double cover, double centerSpacing, double endSpacing)
		{
			BeamInfo = beamInfo;
			Span = span;
			CenterBarType = centerBarType;
			EndBarType = endBarType;
			Cover = cover.MmToFeet();
			CenterSpacing = centerSpacing.MmToFeet();
			EndSpacing = endSpacing.MmToFeet();
			Document = beamInfo.Families[0].Document;
		}

		private List<Curve> GetStirrupCurves(XYZ position)
		{
			var halfWidth = Span.Width / 2 - Cover;
			var topOffset = Cover;
			var bottomOffset = Span.Height - Cover;

			var p1 = position.Add(halfWidth * (-BeamInfo.CrossDirection)).Add(topOffset * (-XYZ.BasisZ));
			var p2 = position.Add(halfWidth * BeamInfo.CrossDirection).Add(topOffset * (-XYZ.BasisZ));
			var p3 = position.Add(halfWidth * BeamInfo.CrossDirection).Add(bottomOffset * (-XYZ.BasisZ));
			var p4 = position.Add(halfWidth * (-BeamInfo.CrossDirection)).Add(bottomOffset * (-XYZ.BasisZ));

			return new List<Curve>
			{
				Line.CreateBound(p1, p2),
				Line.CreateBound(p2, p3),
				Line.CreateBound(p3, p4),
				Line.CreateBound(p4, p1)
			};
		}

		public void RebarCreation()
		{
			var host = DirectShape.CreateElement(Document, new ElementId(BuiltInCategory.OST_StructuralFraming));
			var spanLength = Span.StartPoint.DistanceTo(Span.EndPoint);
			var endZone = spanLength / 4;
			var centerZone = spanLength / 2;

			// Vùng 1 — đai đầu nhịp
			try
			{
				Document.CreateRebarMaximumSpacing(RebarStyle.StirrupTie, EndBarType, host,
					BeamInfo.Direction, GetStirrupCurves(Span.StartPoint), endZone, EndSpacing);
			}
			catch { }

			// Vùng 2 — đai giữa
			try
			{
				var zone2 = Span.StartPoint.Add(endZone * BeamInfo.Direction);
				Document.CreateRebarMaximumSpacing(RebarStyle.StirrupTie, CenterBarType, host,
					BeamInfo.Direction, GetStirrupCurves(zone2), centerZone, CenterSpacing);
			}
			catch { }

			// Vùng 3 — đai cuối nhịp
			try
			{
				var zone3 = Span.StartPoint.Add((endZone + centerZone) * BeamInfo.Direction);
				Document.CreateRebarMaximumSpacing(RebarStyle.StirrupTie, EndBarType, host,
					BeamInfo.Direction, GetStirrupCurves(zone3), endZone, EndSpacing);
			}
			catch { }
		}
	}
}
