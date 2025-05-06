using LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Commands;
using Nice3point.Revit.Toolkit.External;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3
{
    /// <summary>
    ///     Application entry point
    /// </summary>
    [UsedImplicitly]
    public class Application : ExternalApplication
    {
        public override void OnStartup()
        {
            CreateRibbon();
        }

        private void CreateRibbon()
        {
            var panel = Application.CreatePanel("Beam", "QUANG");

            panel.AddPushButton<StartupCommand>("RebarBeam")
                .SetImage("/LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3;component/Resources/Icons/RibbonIcon16.png")
                .SetLargeImage("/LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3;component/Resources/Icons/RibbonIcon32.png");
        }
    }
}