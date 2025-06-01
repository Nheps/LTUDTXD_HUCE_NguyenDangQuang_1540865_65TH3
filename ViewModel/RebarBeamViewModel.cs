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
    public partial class RebarBeamViewModel : ObservableObject
    {
        #region promp
        private Document Document { get; set; }
        private UIDocument UiDocument { get; set; }
        private LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.View.View MainView { get; set; }
        private BeamInfo BeamInfo { get; set; }
        public List<RebarBarType> TypeList { get; set; }
        [ObservableProperty] private RebarBarType _top1;
        [ObservableProperty] private RebarBarType _top2;
        [ObservableProperty] private RebarBarType _top3;
        [ObservableProperty] private RebarBarType _bot1;
        [ObservableProperty] private RebarBarType _bot2;
        [ObservableProperty] private RebarBarType _bot3;
        [ObservableProperty] private RebarBarType _stirrupCenter;
        [ObservableProperty] private RebarBarType _stirrup;
        [ObservableProperty] private int _top1Count = 3;
        [ObservableProperty] private int _top2Count = 3;
        [ObservableProperty] private int _top3Count = 0;
        [ObservableProperty] private int _bot1Count = 3;
        [ObservableProperty] private int _bot2Count = 3;
        [ObservableProperty] private int _bot3Count = 0;
        [ObservableProperty] private int _stirrupCenterSpacing = 200;
        [ObservableProperty] private int _stirrupSpacing = 100;
        [ObservableProperty] private int _topAnchor = 300;
        [ObservableProperty] private int _botAnchor = 300;
        [ObservableProperty] private int _cover = 50;
        private Canvas _canvas;
        #endregion

        [RelayCommand]
        private void Ok()
        {
            //MessageBox.Show("Developping......");
            var tran = new Transaction(Document);
            tran.Start("CreateRebar");
            CreateRebar();
            tran.Commit();
            MainView.Close();
        }

        [RelayCommand]
        private void Cancel()
        {
            MainView.Close();
        }

        public RebarBeamViewModel(UIDocument uiDocument, LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.View.View view)
        {
            UiDocument = uiDocument;
            Document = uiDocument.Document;
            MainView = view;
            MainView.DataContext = this;
            MainView.Loaded += MainView_Loaded;
            InitData();
        }

        private void MainView_Loaded(object sender, RoutedEventArgs e)
        {
            _canvas = MainView.FindName("PreviewCanvas") as Canvas;
            UpdateCanvas();
        }

        private void InitData()
        {

            TypeList = new FilteredElementCollector(Document)
              .OfClass(typeof(RebarBarType))
              .Cast<RebarBarType>()
              .ToList();
            if (TypeList.Count == 0)
                return;
            Top1 = TypeList.FirstOrDefault();
            Top2 = TypeList.FirstOrDefault();
            Top3 = TypeList.FirstOrDefault();
            Bot1 = TypeList.FirstOrDefault();
            Bot2 = TypeList.FirstOrDefault();
            Bot3 = TypeList.FirstOrDefault();
            StirrupCenter = TypeList.FirstOrDefault();
            Stirrup = TypeList.FirstOrDefault();
        }

        public void Run()
        {
            if (MainView == null)
                return;
            var beams = UiDocument.PickBeams();
            if (beams.Count > 0)
            {
                BeamInfo = new BeamInfo(beams);
                MainView.ShowDialog();
            }
            else
            {
                MessageBox.Show("Error");
            }
        }
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName is nameof(Top1Count) or nameof(Bot1Count) or nameof(Top2Count) or nameof(Bot2Count) or nameof(Top3Count) or nameof(Bot3Count))
            {
                UpdateCanvas();
            }
        }
        private void CreateRebar()
        {
            if (BeamInfo == null)
                return;
            var top1 = new UpperRebar(RebarBeamType.Top1, Top1, BeamInfo, TopAnchor, Top1Count);
            top1.RebarCreation();
            var top2 = new UpperRebar(RebarBeamType.Top2, Top2, BeamInfo, TopAnchor, Top2Count);
            top2.RebarCreation(); 
            var top3 = new UpperRebar(RebarBeamType.Top3, Top3, BeamInfo, TopAnchor, Top3Count);
            top3.RebarCreation();
            var bot1 = new LowerRebar(RebarBeamType.Bottom1, Bot1, BeamInfo, BotAnchor, Bot1Count);
            bot1.RebarCreation();
            var bot2 = new LowerRebar(RebarBeamType.Top2, Bot2, BeamInfo, BotAnchor, Bot2Count);
            bot2.RebarCreation();
            var bot3 = new LowerRebar(RebarBeamType.Bottom3, Bot3, BeamInfo, BotAnchor, Bot3Count);
            bot3.RebarCreation();
            //var stirrupCenter = new Stirrup(BeamInfo, BeamInfo.StartPoint, BeamInfo.EndPoint,
            //	StirrupCenterSpacing);
            //var stirrup = new Stirrup(Stirrup, BeamInfo.StartPoint, BeamInfo.EndPoint,
            //	StirrupSpacing);
        }
        private void UpdateCanvas()
        {
            if (_canvas == null) return;
            _canvas.Children.Clear();
            var width = BeamInfo.Width.FeetToMm();
            var height = BeamInfo.Height.FeetToMm();
            double canvasWidth = _canvas.ActualWidth;
            double canvasHeight = _canvas.ActualHeight;
            if (canvasWidth == 0 || canvasHeight == 0) return;

            double scaleX = canvasWidth / width;
            double scaleY = canvasHeight / height;
            double scale = Math.Min(scaleX, scaleY);

            double widthPx = width * scale;
            double heightPx = height * scale;

            double startX = (canvasWidth - widthPx) / 2;
            double startY = (canvasHeight - heightPx) / 2;

            System.Windows.Shapes.Rectangle columnRectangle = new System.Windows.Shapes.Rectangle
            {
                Width = widthPx,
                Height = heightPx,
                Stroke = System.Windows.Media.Brushes.Black,
                StrokeThickness = 1,
                Fill = System.Windows.Media.Brushes.White
            };

            Canvas.SetLeft(columnRectangle, startX);
            Canvas.SetTop(columnRectangle, startY);
            _canvas.Children.Add(columnRectangle);

            var coverpx = Cover * scale;
            var rebarDiameter = Top1.get_Parameter(BuiltInParameter.REBAR_BAR_DIAMETER)
                .AsDouble().FeetToMm();
            var rebarRadiusPx = (rebarDiameter / 2) * scale;
            var innerWidth = widthPx - 2 * coverpx;
            var innerHeight = heightPx - 2 * coverpx;
            double innerStartX = startX;
            double innerStartY = startY + coverpx;
            // Draw top rebar
            if (Top1Count > 0)
            {
                double topRebarY = innerStartY + coverpx / 2;
                for (int i = 0; i < Top1Count; i++)
                {
                    double x = innerStartX + coverpx + i * ((innerWidth - 0) / (Top1Count - 1));
                    DrawRebar(rebarRadiusPx, x, topRebarY);
                }
            }
            if (Top2Count > 0)
            {
                double topRebarY = innerStartY + coverpx * 2;
                for (int i = 0; i < Top2Count; i++)
                {
                    double x = innerStartX + coverpx + i * ((innerWidth - 0) / (Top2Count - 1));
                    DrawRebar(rebarRadiusPx, x, topRebarY);
                }
            }
            if (Top3Count > 0)
            {
                double topRebarY = innerStartY + coverpx * 4;
                for (int i = 0; i < Top3Count; i++)
                {
                    double x = innerStartX + coverpx + i * ((innerWidth - 0) / (Top3Count - 1));
                    DrawRebar(rebarRadiusPx, x, topRebarY);
                }
            }
            // Draw bottom rebar
            if (Bot1Count > 0)
            {
                double botRebarY = innerStartY + innerHeight - coverpx / 2;
                for (int i = 0; i < Bot1Count; i++)
                {
                    double x = innerStartX + coverpx + i * ((innerWidth - 0) / (Bot1Count - 1));
                    DrawRebar(rebarRadiusPx, x, botRebarY);
                }
            }
            if (Bot2Count > 0)
            {
                double botRebarY = innerStartY + innerHeight - coverpx * 2;
                for (int i = 0; i < Bot2Count; i++)
                {
                    double x = innerStartX + coverpx + i * ((innerWidth - 0) / (Bot2Count - 1));
                    DrawRebar(rebarRadiusPx, x, botRebarY);
                }
            }
            if (Bot3Count > 0)
            {
                double botRebarY = innerStartY + innerHeight - coverpx * 4;
                for (int i = 0; i < Bot3Count; i++)
                {
                    double x = innerStartX + coverpx + i * ((innerWidth - 0) / (Bot3Count - 1));
                    DrawRebar(rebarRadiusPx, x, botRebarY);
                }
            }
        }
        private void DrawRebar(double rebarRadiusPx, double x, double y)
        {
            double rebarDiameter = rebarRadiusPx * 2 * (_canvas.ActualWidth / BeamInfo.Width.FeetToMm());

            var rebar = new System.Windows.Shapes.Ellipse
            {
                Width = rebarDiameter,
                Height = rebarDiameter,
                Fill = System.Windows.Media.Brushes.DarkBlue,
            };
            Canvas.SetLeft(rebar, x - rebarDiameter / 2);
            Canvas.SetTop(rebar, y - rebarDiameter / 2);
            _canvas.Children.Add(rebar);
        }
    }
}