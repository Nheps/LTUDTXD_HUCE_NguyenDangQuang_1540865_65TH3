using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.ViewModel;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Commands
{
    /// <summary>
    /// Lệnh ngoài (External Command) — điểm vào khi người dùng nhấn nút "RebarBeam" trên Ribbon.
    /// Chế độ giao dịch: Manual — tự quản lý Transaction bên trong ViewModel.
    /// </summary>
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class StartupCommand : ExternalCommand
    {
        /// <summary>
        /// Revit gọi phương thức này khi người dùng nhấn nút trên Ribbon.
        /// Quy trình:
        ///   1. Tạo cửa sổ giao diện (View)
        ///   2. Khởi tạo ViewModel, truyền vào UIDocument và View
        ///   3. Gọi Run() — người dùng chọn dầm, cửa sổ hiện ra
        /// </summary>
        public override void Execute()
        {
            try
            {
                // Tạo cửa sổ WPF nhập thông số thép
                var view = new View.View();

                // Khởi tạo ViewModel, kết nối dữ liệu với View
                var viewModel = new RebarBeamViewModel(UiDocument, view);

                // Yêu cầu người dùng chọn dầm, sau đó hiển thị hộp thoại
                viewModel.Run();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}