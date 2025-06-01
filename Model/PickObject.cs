using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.UI;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Model
{
    public static class PickObject
    {
        public static List<Element> PickBeams(this UIDocument uiDocument)
        {
            var eles = uiDocument.Selection.PickObjects(ObjectType.Element, new BeamFilter())
              .Select(x => uiDocument.Document.GetElement(x) as Element).ToList();
            if (!eles.Any()) return null;
            return eles.CheckBeams() ? eles : null;
        }
        private static bool CheckBeams(this List<Element> eles)
        {
            var heights = new List<double>();
            var widths = new List<double>();
            foreach (var beam in eles)
            {
                var h = beam.GetParameter("h").AsDouble();
                var w = beam.GetParameter("b").AsDouble();
                heights.Add(h);
                widths.Add(w);
            }

            var enumerable = heights.Distinct().ToList();
            var distinct = widths.Distinct().ToList();
            return enumerable.Count == 1 && distinct.Count == 1;
        }
    }
    public class BeamFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is not FamilyInstance familyInstance) return false;

            if ((familyInstance.Location as LocationCurve)?.Curve == null) return false;
            return familyInstance.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            throw new NotImplementedException();
        }
    }
}
