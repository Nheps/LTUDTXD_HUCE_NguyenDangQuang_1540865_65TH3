using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Model;
using LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.ViewModel
{
    /// <summary>
    /// ViewModel chính của tool bố trí cốt thép dầm.
    /// Kế thừa ObservableObject (CommunityToolkit.Mvvm) để tự động thông báo thay đổi thuộc tính cho View.
    ///
    /// Luồng hoạt động:
    ///   1. Khởi tạo → nạp danh sách loại thép từ Revit (InitData)
    ///   2. Run() → người dùng chọn dầm → hiện hộp thoại
    ///   3. Người dùng nhập thông số → canvas tự cập nhật (OnPropertyChanged → UpdateCanvas)
    ///   4. Nhấn OK → tạo thép trong Transaction → đóng hộp thoại
    /// </summary>
    public partial class RebarBeamViewModel : ObservableObject
    {
        #region Thuộc tính và trường dữ liệu

        /// <summary>Tài liệu Revit hiện hành (để tạo phần tử, truy vấn dữ liệu).</summary>
        private Document Document { get; set; }

        /// <summary>UIDocument — cần để gọi lệnh chọn đối tượng (PickBeams).</summary>
        private UIDocument UiDocument { get; set; }

        /// <summary>Cửa sổ WPF chính — giữ tham chiếu để đóng sau khi hoàn thành.</summary>
        private LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.View.View MainView { get; set; }

        /// <summary>Thông tin hình học dầm đã chọn (được gán sau khi người dùng chọn dầm).</summary>
        private BeamInfo BeamInfo { get; set; }

        /// <summary>Danh sách tất cả loại thép (RebarBarType) có trong dự án Revit — nguồn cho ComboBox.</summary>
        public List<RebarBarType> TypeList { get; set; }

        // ── Loại thép từng lớp ──
        /// <summary>Loại thép trên lớp 1 (Top1).</summary>
        [ObservableProperty] private RebarBarType _top1;
        /// <summary>Loại thép trên lớp 2 (Top2).</summary>
        [ObservableProperty] private RebarBarType _top2;
        /// <summary>Loại thép trên lớp 3 (Top3).</summary>
        [ObservableProperty] private RebarBarType _top3;
        /// <summary>Loại thép dưới lớp 1 (Bot1).</summary>
        [ObservableProperty] private RebarBarType _bot1;
        /// <summary>Loại thép dưới lớp 2 (Bot2).</summary>
        [ObservableProperty] private RebarBarType _bot2;
        /// <summary>Loại thép dưới lớp 3 (Bot3).</summary>
        [ObservableProperty] private RebarBarType _bot3;
        /// <summary>Loại thép đai giữa dầm.</summary>
        [ObservableProperty] private RebarBarType _stirrupCenter;
        /// <summary>Loại thép đai 2 đầu dầm.</summary>
        [ObservableProperty] private RebarBarType _stirrup;

        // ── Số lượng thanh từng lớp (mặc định) ──
        /// <summary>Số thanh thép trên lớp 1. Mặc định 3.</summary>
        [ObservableProperty] private int _top1Count = 3;
        /// <summary>Số thanh thép trên lớp 2. Mặc định 3.</summary>
        [ObservableProperty] private int _top2Count = 3;
        /// <summary>Số thanh thép trên lớp 3. Mặc định 0 (không dùng).</summary>
        [ObservableProperty] private int _top3Count = 0;
        /// <summary>Số thanh thép dưới lớp 1. Mặc định 3.</summary>
        [ObservableProperty] private int _bot1Count = 3;
        /// <summary>Số thanh thép dưới lớp 2. Mặc định 3.</summary>
        [ObservableProperty] private int _bot2Count = 3;
        /// <summary>Số thanh thép dưới lớp 3. Mặc định 0 (không dùng).</summary>
        [ObservableProperty] private int _bot3Count = 0;

        // ── Thông số đai ──
        /// <summary>Bước đai giữa dầm (mm). Mặc định 200mm.</summary>
        [ObservableProperty] private int _stirrupCenterSpacing = 200;
        /// <summary>Bước đai 2 đầu dầm (mm). Mặc định 100mm.</summary>
        [ObservableProperty] private int _stirrupSpacing = 100;

        // ── Thông số neo và lớp bảo vệ ──
        /// <summary>Chiều dài đoạn neo thép trên tại 2 đầu dầm (mm). Mặc định 300mm.</summary>
        [ObservableProperty] private int _topAnchor = 300;
        /// <summary>Chiều dài đoạn neo thép dưới tại 2 đầu dầm (mm). Mặc định 300mm.</summary>
        [ObservableProperty] private int _botAnchor = 300;
        /// <summary>Chiều dày lớp bảo vệ đến tim đai (mm). Mặc định 25mm theo TCVN 5574.</summary>
        [ObservableProperty] private int _cover = 25;

        /// <summary>Canvas WPF dùng để vẽ preview mặt cắt ngang dầm.</summary>
        private Canvas _canvas;

        #endregion

        /// <summary>
        /// Xử lý khi người dùng nhấn nút OK.
        /// Mở Transaction, tạo toàn bộ cốt thép, commit, rồi đóng cửa sổ.
        /// DiscardWarningPreprocessor tự động bỏ qua các cảnh báo Revit
        /// (ví dụ: thép nằm ngoài phần tử host) để không làm gián đoạn.
        /// </summary>
        [RelayCommand]
        private void Ok()
        {
            var tran = new Transaction(Document);
            tran.Start("CreateRebar");

            // Tự động bỏ qua cảnh báo (Warning) trong quá trình tạo thép
            var failureOptions = tran.GetFailureHandlingOptions();
            failureOptions.SetFailuresPreprocessor(new DiscardWarningPreprocessor());
            tran.SetFailureHandlingOptions(failureOptions);

            CreateRebar();
            tran.Commit();
            MainView.Close();
        }

        /// <summary>
        /// Lớp xử lý lỗi Revit trong Transaction.
        /// Tự động xóa tất cả Warning (mức độ nhẹ) để Transaction không bị hủy.
        /// Error (mức độ nghiêm trọng) vẫn được giữ nguyên.
        /// </summary>
        private class DiscardWarningPreprocessor : IFailuresPreprocessor
        {
            public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
            {
                foreach (var failure in failuresAccessor.GetFailureMessages())
                {
                    // Chỉ xóa cảnh báo (Warning), không xóa lỗi (Error)
                    if (failure.GetSeverity() == FailureSeverity.Warning)
                        failuresAccessor.DeleteWarning(failure);
                }
                return FailureProcessingResult.Continue;
            }
        }

        /// <summary>
        /// Xử lý khi người dùng nhấn nút Cancel — đóng cửa sổ mà không tạo thép.
        /// </summary>
        [RelayCommand]
        private void Cancel()
        {
            MainView.Close();
        }

        /// <summary>
        /// Khởi tạo ViewModel.
        /// Gán DataContext cho View, đăng ký sự kiện Loaded để lấy Canvas,
        /// và nạp dữ liệu loại thép từ Revit.
        /// </summary>
        public RebarBeamViewModel(UIDocument uiDocument, LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.View.View view)
        {
            UiDocument = uiDocument;
            Document = uiDocument.Document;
            MainView = view;

            // Gán ViewModel làm DataContext để View binding dữ liệu
            MainView.DataContext = this;

            // Sau khi View load xong mới có thể tìm Canvas theo tên
            MainView.Loaded += MainView_Loaded;

            InitData();
        }

        /// <summary>
        /// Được gọi sau khi cửa sổ WPF đã load xong.
        /// Lấy tham chiếu Canvas "PreviewCanvas" và vẽ mặt cắt ban đầu.
        /// </summary>
        private void MainView_Loaded(object sender, RoutedEventArgs e)
        {
            _canvas = MainView.FindName("PreviewCanvas") as Canvas;
            UpdateCanvas();
        }

        /// <summary>
        /// Nạp danh sách loại thép (RebarBarType) từ dự án Revit hiện hành.
        /// Gán mặc định loại đầu tiên cho tất cả các lớp thép.
        /// </summary>
        private void InitData()
        {
            // Lấy tất cả RebarBarType trong dự án bằng FilteredElementCollector
            TypeList = new FilteredElementCollector(Document)
              .OfClass(typeof(RebarBarType))
              .Cast<RebarBarType>()
              .ToList();

            if (TypeList.Count == 0) return;

            // Gán loại thép mặc định (loại đầu tiên) cho tất cả ComboBox
            Top1 = TypeList.FirstOrDefault();
            Top2 = TypeList.FirstOrDefault();
            Top3 = TypeList.FirstOrDefault();
            Bot1 = TypeList.FirstOrDefault();
            Bot2 = TypeList.FirstOrDefault();
            Bot3 = TypeList.FirstOrDefault();
            StirrupCenter = TypeList.FirstOrDefault();
            Stirrup = TypeList.FirstOrDefault();
        }

        /// <summary>
        /// Điểm khởi chạy chính của tool.
        /// Yêu cầu người dùng chọn dầm, xây dựng BeamInfo, rồi hiện hộp thoại.
        /// </summary>
        public void Run()
        {
            if (MainView == null) return;

            // Yêu cầu người dùng chọn dầm từ mô hình
            var beams = UiDocument.PickBeams();
            if (beams.Count > 0)
            {
                // Trích xuất thông tin hình học từ danh sách dầm đã chọn
                BeamInfo = new BeamInfo(beams);

                // Hiện hộp thoại dưới dạng modal (chờ người dùng nhấn OK/Cancel)
                MainView.ShowDialog();
            }
            else
            {
                MessageBox.Show("Error");
            }
        }

        /// <summary>
        /// Được gọi mỗi khi bất kỳ thuộc tính nào thay đổi (do CommunityToolkit.Mvvm).
        /// Chỉ cập nhật canvas khi các thuộc tính ảnh hưởng đến hình dạng mặt cắt thay đổi.
        /// </summary>
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            // Cập nhật canvas khi thay đổi số lượng thép, bước đai, hoặc lớp bảo vệ
            if (e.PropertyName is nameof(Top1Count) or nameof(Bot1Count) or nameof(Top2Count) or nameof(Bot2Count) or nameof(Top3Count) or nameof(Bot3Count)
                or nameof(StirrupSpacing) or nameof(StirrupCenterSpacing) or nameof(Cover))
            {
                UpdateCanvas();
            }
        }

        /// <summary>
        /// Tạo toàn bộ cốt thép trong Revit theo thông số người dùng nhập.
        /// Thứ tự tạo: thép trên (3 lớp) → thép dưới (3 lớp) → thép đai.
        /// </summary>
        private void CreateRebar()
        {
            if (BeamInfo == null) return;

            // ── Thép chính phía trên ──
            var top1 = new UpperRebar(RebarBeamType.Top1, Top1, BeamInfo, TopAnchor, Top1Count);
            top1.RebarCreation();

            var top2 = new UpperRebar(RebarBeamType.Top2, Top2, BeamInfo, TopAnchor, Top2Count);
            top2.RebarCreation();

            var top3 = new UpperRebar(RebarBeamType.Top3, Top3, BeamInfo, TopAnchor, Top3Count);
            top3.RebarCreation();

            // ── Thép chính phía dưới ──
            var bot1 = new LowerRebar(RebarBeamType.Bottom1, Bot1, BeamInfo, BotAnchor, Bot1Count);
            bot1.RebarCreation();

            // Lưu ý: Bot2 dùng RebarBeamType.Top2 để lấy đúng offset 130mm từ mép dưới
            var bot2 = new LowerRebar(RebarBeamType.Top2, Bot2, BeamInfo, BotAnchor, Bot2Count);
            bot2.RebarCreation();

            var bot3 = new LowerRebar(RebarBeamType.Bottom3, Bot3, BeamInfo, BotAnchor, Bot3Count);
            bot3.RebarCreation();

            // ── Thép đai ──
            var stirrup = new StirrupRebar(StirrupCenter, Stirrup, BeamInfo, Cover, StirrupCenterSpacing, StirrupSpacing);
            stirrup.RebarCreation();
        }
        /// <summary>
        /// Vẽ preview mặt cắt ngang dầm lên Canvas WPF.
        /// Được gọi mỗi khi người dùng thay đổi số lượng thép, bước đai hoặc lớp bảo vệ.
        ///
        /// Quy trình vẽ:
        ///   1. Tính tỉ lệ scale để dầm vừa khung canvas (giữ tỉ lệ thực)
        ///   2. Căn giữa hình chữ nhật dầm trong canvas
        ///   3. Vẽ đường viền đai (hình chữ nhật cách mép cover)
        ///   4. Vẽ các chấm tròn thể hiện tiết diện từng thanh thép
        ///
        /// Tọa độ Y của từng lớp thép khớp chính xác với offset trong UpperRebar/LowerRebar:
        ///   Top: 50mm, 130mm, 210mm tính từ mép trên
        ///   Bot: 50mm, 130mm, 210mm tính từ mép dưới
        /// </summary>
        private void UpdateCanvas()
        {
            if (_canvas == null || BeamInfo == null) return;
            _canvas.Children.Clear();

            // Kích thước dầm thực tế chuyển sang mm để tính tỉ lệ
            var width = BeamInfo.Width.FeetToMm();
            var height = BeamInfo.Height.FeetToMm();
            double canvasWidth = _canvas.ActualWidth;
            double canvasHeight = _canvas.ActualHeight;
            if (canvasWidth == 0 || canvasHeight == 0) return;

            // Tính tỉ lệ scale: lấy min để dầm vừa khung canvas theo cả 2 chiều
            double scaleX = canvasWidth / width;
            double scaleY = canvasHeight / height;
            double scale = Math.Min(scaleX, scaleY);

            // Kích thước dầm tính bằng pixel
            double widthPx = width * scale;
            double heightPx = height * scale;

            // Tọa độ góc trên-trái của hình chữ nhật dầm (căn giữa canvas)
            double startX = (canvasWidth - widthPx) / 2;
            double startY = (canvasHeight - heightPx) / 2;

            // ── Vẽ hình chữ nhật biên dầm ──
            var beamRect = new System.Windows.Shapes.Rectangle
            {
                Width = widthPx,
                Height = heightPx,
                Stroke = System.Windows.Media.Brushes.Black,
                StrokeThickness = 1,
                Fill = System.Windows.Media.Brushes.White
            };
            Canvas.SetLeft(beamRect, startX);
            Canvas.SetTop(beamRect, startY);
            _canvas.Children.Add(beamRect);

            // Chiều dày lớp bảo vệ tính bằng pixel
            var coverpx = Cover * scale;

            // ── Vẽ đường viền đai (hình chữ nhật cách mép dầm = cover) ──
            var stirrupRect = new System.Windows.Shapes.Rectangle
            {
                Width = widthPx - 2 * coverpx,
                Height = heightPx - 2 * coverpx,
                Stroke = System.Windows.Media.Brushes.DarkGray,
                StrokeThickness = 1.5,
                Fill = System.Windows.Media.Brushes.Transparent
            };
            Canvas.SetLeft(stirrupRect, startX + coverpx);
            Canvas.SetTop(stirrupRect, startY + coverpx);
            _canvas.Children.Add(stirrupRect);

            // Bán kính chấm tròn thép = nửa đường kính thực × scale
            // (Top1.BarModelDiameter là đường kính danh nghĩa tính bằng feet)
            var rebarRadiusPx = (Top1.BarModelDiameter.FeetToMm() / 2) * scale;

            // Margin thép chủ: 50mm từ mép (nhất quán với code tạo thép thực tế)
            var rebarMarginPx = 50.0 * scale;

            // ── Vẽ thép trên: Y tính từ mép trên xuống theo offset thực tế (mm × scale) ──
            DrawRebarRow(Top1Count, startX, startY + 50.0 * scale,            widthPx, rebarMarginPx, rebarRadiusPx);
            DrawRebarRow(Top2Count, startX, startY + 130.0 * scale,           widthPx, rebarMarginPx, rebarRadiusPx);
            DrawRebarRow(Top3Count, startX, startY + 210.0 * scale,           widthPx, rebarMarginPx, rebarRadiusPx);

            // ── Vẽ thép dưới: Y tính từ mép dưới lên theo offset thực tế ──
            DrawRebarRow(Bot1Count, startX, startY + heightPx - 50.0 * scale,  widthPx, rebarMarginPx, rebarRadiusPx);
            DrawRebarRow(Bot2Count, startX, startY + heightPx - 130.0 * scale, widthPx, rebarMarginPx, rebarRadiusPx);
            DrawRebarRow(Bot3Count, startX, startY + heightPx - 210.0 * scale, widthPx, rebarMarginPx, rebarRadiusPx);
        }

        /// <summary>
        /// Vẽ một hàng ngang gồm <paramref name="count"/> chấm thép trên canvas.
        /// Các chấm được phân bố đều từ mép trái đến mép phải (cách mép cover).
        /// Trường hợp count = 1: đặt chấm tại tâm dầm.
        /// </summary>
        /// <param name="count">Số thanh thép trong hàng.</param>
        /// <param name="startX">Tọa độ X góc trái hình chữ nhật dầm (pixel).</param>
        /// <param name="y">Tọa độ Y tâm hàng thép (pixel).</param>
        /// <param name="widthPx">Chiều rộng dầm tính bằng pixel.</param>
        /// <param name="coverpx">Lớp bảo vệ tính bằng pixel.</param>
        /// <param name="rebarRadiusPx">Bán kính chấm tròn thép (pixel).</param>
        private void DrawRebarRow(int count, double startX, double y, double widthPx, double coverpx, double rebarRadiusPx)
        {
            if (count <= 0) return;

            // Chiều rộng vùng phân bố (từ tim đai trái đến tim đai phải)
            double innerWidth = widthPx - 2 * coverpx;

            for (int i = 0; i < count; i++)
            {
                // count = 1: đặt tại tâm; count > 1: phân bố đều
                double x = count == 1
                    ? startX + widthPx / 2
                    : startX + coverpx + i * (innerWidth / (count - 1));
                DrawRebar(rebarRadiusPx, x, y);
            }
        }

        /// <summary>
        /// Vẽ một chấm tròn (Ellipse) đại diện cho tiết diện thanh thép tại tọa độ (x, y).
        /// </summary>
        /// <param name="rebarRadiusPx">Bán kính chấm (pixel) — đã nhân scale, không cần nhân lại.</param>
        /// <param name="x">Tọa độ X tâm chấm (pixel).</param>
        /// <param name="y">Tọa độ Y tâm chấm (pixel).</param>
        private void DrawRebar(double rebarRadiusPx, double x, double y)
        {
            double diameter = rebarRadiusPx * 2;
            var rebar = new System.Windows.Shapes.Ellipse
            {
                Width = diameter,
                Height = diameter,
                Fill = System.Windows.Media.Brushes.DarkBlue,
            };
            // SetLeft/Top đặt góc trên-trái → trừ đi bán kính để tâm trùng với (x, y)
            Canvas.SetLeft(rebar, x - diameter / 2);
            Canvas.SetTop(rebar, y - diameter / 2);
            _canvas.Children.Add(rebar);
        }

    }
}