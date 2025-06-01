using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB.Structure;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Model
{
    public class LowerRebar
    {
        private RebarBeamType RebarBeamType { get; set; }
        private BeamInfo BeamInfo { get; set; }
        private XYZ Start { get; set; }
        private XYZ End { get; set; }
        private double Anchor { get; set; }
        private List<List<Curve>> Curves { get; set; } = new();
        private int Quantity { get; set; } = 0;
        private RebarBarType RebarBarType { get; set; }
        private Document Document { get; set; }

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
            RebarAnalys();
        }

        private void RebarAnalys()
        {
            if (Quantity is 0 or 1) return;
            Start = Start.Add(BeamInfo.Height * -XYZ.BasisZ);
            End = End.Add(BeamInfo.Height * -XYZ.BasisZ);
            switch (RebarBeamType)
            {
                case RebarBeamType.Top1:
                    Start = Start.Add(50.0.MmToFeet() * XYZ.BasisZ);
                    End = End.Add(50.0.MmToFeet() * XYZ.BasisZ);
                    break;
                case RebarBeamType.Top2:
                    Start = Start.Add(130.0.MmToFeet() * XYZ.BasisZ);
                    End = End.Add(130.0.MmToFeet() * XYZ.BasisZ);
                    break;
                case RebarBeamType.Top3:
                    Start = Start.Add(210.0.MmToFeet() * XYZ.BasisZ);
                    End = End.Add(210.0.MmToFeet() * XYZ.BasisZ);
                    break;
                default:
                    break;
            }
            var width = BeamInfo.Width - 100.0.MmToFeet();
            var distance = width / (Quantity - 1);
            Start = Start.Add(width / 2 * -BeamInfo.CrossDirection);
            End = End.Add(width / 2 * -BeamInfo.CrossDirection);
            if (Anchor == 0)
            {
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
                var start = Start.Add(Anchor * XYZ.BasisZ);
                var end = End.Add(Anchor * XYZ.BasisZ);
                for (int i = 0; i < Quantity; i++)
                {
                    var p1 = start.Add(i * distance * BeamInfo.CrossDirection);
                    var p2 = Start.Add(i * distance * BeamInfo.CrossDirection);
                    var p3 = End.Add(i * distance * BeamInfo.CrossDirection);
                    var p4 = end.Add(i * distance * BeamInfo.CrossDirection);
                    List<Curve> curves = new List<Curve>() { Line.CreateBound(p1, p2), Line.CreateBound(p2, p3), Line.CreateBound(p3, p4) };
                    Curves.Add(curves);
                }
            }
        }
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
                    // ignored
                }
            }
        }
    }
}
