using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Model
{
    public class BeamInfo
    {
        public List<FamilyInstance> Families { get; }
        public double Height { get; private set; }
        public double Width { get; private set; }
        public XYZ StartPoint { get; private set; }
        public XYZ EndPoint { get; private set; }
        public XYZ Direction { get; private set; }
        public XYZ CrossDirection { get; private set; }

        public BeamInfo(List<Element> elements)
        {
            if (elements == null) return;
            Families = elements.Select(x => x as FamilyInstance).ToList();
            GetHeightAndWidth();
            GetStartAndEndPoint();
        }

        private void GetHeightAndWidth()
        {
            var heights = new List<double>();
            var widths = new List<double>();
            foreach (var beam in Families)
            {
                var h = beam.GetParameter("h").AsDouble();
                var w = beam.GetParameter("b").AsDouble();
                heights.Add(h);
                widths.Add(w);
            }

            if (heights.Count != 1 || widths.Count != 1) return;
            Height = heights[0];
            Width = widths[0];
        }

        private void GetStartAndEndPoint()
        {
            Direction = ((Families[0].Location as LocationCurve)?.Curve as Line)?.Direction;
            CrossDirection = Direction!.CrossProduct(XYZ.BasisZ);
            var curves = new List<Curve>();
            foreach (var beam in Families)
            {
                var locationCurve = (beam.Location as LocationCurve)?.Curve;
                curves.Add(locationCurve);
            }
            var curs = curves.OrderBy(x => (x as Line)?.Direction.DotProduct(Direction) ?? 0);
            if (!curs.Any()) return;
            StartPoint = curs.First().GetEndPoint(0);
            EndPoint = curs.Last().GetEndPoint(1);
        }
    }
}
