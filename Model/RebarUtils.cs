using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB.Structure;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Model
{
    public static class RebarUtils
    {
        public static Rebar CreateRebarSingle(this Document document, RebarStyle rebarStyle, RebarBarType rebarBarType, Element host, XYZ normal,
        List<Curve> curves)
        {
            var rebar = Rebar.CreateFromCurves(document, rebarStyle, rebarBarType, null, null, host, normal, curves, RebarHookOrientation.Left,
                RebarHookOrientation.Left, true, true);
            return rebar;
        }

        public static Rebar CreateRebarFixedNumber(this Document document, RebarStyle rebarStyle, RebarBarType rebarBarType, Element host, XYZ normal,
            List<Curve> curves, double lengthArr, int quantity)
        {
            var rebar = Rebar.CreateFromCurves(document, rebarStyle, rebarBarType, null, null, host, normal, curves, RebarHookOrientation.Left,
                RebarHookOrientation.Left, true, true);
            if (rebar == null || quantity <= 0 || !(lengthArr > 0.0)) return rebar;
            rebar.GetShapeDrivenAccessor().SetLayoutAsFixedNumber(quantity, lengthArr, true, true, true);
            rebar.IncludeFirstBar = true;
            rebar.IncludeLastBar = true;

            return rebar;
        }

        public static Rebar CreateRebarMaximumSpacing(this Document document, RebarStyle rebarStyle, RebarBarType rebarBarType, Element host, XYZ normal,
            List<Curve> curves, double lengthArr, double spacing)
        {
            var rebar = Rebar.CreateFromCurves(document, rebarStyle, rebarBarType, null, null, host, normal, curves, RebarHookOrientation.Left,
                RebarHookOrientation.Left, true, true);
            if (rebar == null || !(spacing > 0.0) || !(lengthArr > 0.0)) return rebar;
            rebar.GetShapeDrivenAccessor().SetLayoutAsMaximumSpacing(spacing, lengthArr, true, true, true);
            rebar.IncludeFirstBar = true;
            rebar.IncludeLastBar = true;

            return rebar;
        }

        public static Rebar CreateRebarNumberWithSpacing(this Document document, RebarStyle rebarStyle, RebarBarType rebarBarType, Element host,
            XYZ normal, List<Curve> curves, int quantity, double spacing)
        {
            var rebar = Rebar.CreateFromCurves(document, rebarStyle, rebarBarType, null, null, host, normal, curves, RebarHookOrientation.Left,
                RebarHookOrientation.Left, true, true);
            if (rebar == null || !(quantity > 0.0) || !(spacing > 0.0)) return rebar;
            rebar.GetShapeDrivenAccessor().SetLayoutAsNumberWithSpacing(quantity, spacing, true, true, true);
            rebar.IncludeFirstBar = true;
            rebar.IncludeLastBar = true;

            return rebar;
        }

        public static Rebar CreateRebarMinimumClearSpacing(this Document document, RebarStyle rebarStyle, RebarBarType rebarBarType, Element host,
            XYZ normal, List<Curve> curves, double lengthArr, double spacing)
        {
            var rebar = Rebar.CreateFromCurves(document, rebarStyle, rebarBarType, null, null, host, normal, curves, RebarHookOrientation.Left,
                RebarHookOrientation.Left, true, true);
            if (rebar == null || !(lengthArr > 0.0) || !(spacing > 0.0)) return rebar;
            rebar.GetShapeDrivenAccessor().SetLayoutAsMinimumClearSpacing(spacing, lengthArr, true, true, true);
            rebar.IncludeFirstBar = true;
            rebar.IncludeLastBar = true;

            return rebar;
        }

        public static Rebar CreateRebarFreeForm(this Document document, RebarBarType rebarBarType, Element host, List<CurveLoop> curves)
        {
            return Rebar.CreateFreeForm(document, rebarBarType, host, curves, out RebarFreeFormValidationResult result);
        }
    }
}
