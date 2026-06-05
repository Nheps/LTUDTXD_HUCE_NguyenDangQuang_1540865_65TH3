using LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Commands;
using Nice3point.Revit.Extensions.UI;
using Nice3point.Revit.Toolkit.External;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3
{
    /// <summary>
    /// Lớp khởi động chính của Add-in Revit.
    /// Được Revit gọi tự động khi phần mềm khởi chạy.
    /// Nhiệm vụ: đăng ký Ribbon (tab/panel/button) lên giao diện Revit.
    /// </summary>
    [UsedImplicitly]
    public class Application : ExternalApplication
    {
        /// <summary>
        /// Được gọi một lần khi Revit khởi động.
        /// Thực hiện tạo giao diện Ribbon cho add-in.
        /// </summary>
        public override void OnStartup()
        {
            CreateRibbon();
        }

        /// <summary>
        /// Tạo panel và nút bấm trên Ribbon của Revit.
        /// - Tab: "QUANG"
        /// - Panel: "Beam"
        /// - Nút bấm: "RebarBeam" — khi nhấn sẽ chạy <see cref="StartupCommand"/>
        /// </summary>
        private void CreateRibbon()
        {
            // Tạo panel "Beam" trong tab "QUANG"
            var panel = Application.CreatePanel("Beam", "QUANG");

            // Thêm nút bấm vào panel, gán icon 16x16 và 32x32
            panel.AddPushButton<StartupCommand>("RebarBeam")
                .SetImage("/LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3;component/Resources/Icons/RibbonIcon16.png")
                .SetLargeImage("/LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3;component/Resources/Icons/RibbonIcon32.png");
        }
    }
}