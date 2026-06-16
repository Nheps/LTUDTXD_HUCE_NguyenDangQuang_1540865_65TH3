using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Model;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Color = System.Windows.Media.Color;
using Ellipse = System.Windows.Shapes.Ellipse;
using Line = System.Windows.Shapes.Line;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.ViewModel
{
    public partial class RebarBeamViewModel : ObservableObject
    {
        private static readonly double[] LayerOffsets = { 50, 130, 210, 290, 370 };
        private const int MaxLayers = 5;

        #region Core Fields

        private Document Document { get; set; }
        private UIDocument UiDocument { get; set; }
        private LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.View.View MainView { get; set; }
        private BeamInfo BeamInfo { get; set; }

        public ObservableCollection<RebarBarType> TypeList { get; set; } = new();

        // ── Cấu hình theo từng nhịp ────────────────────────────────────────
        public ObservableCollection<SpanRebarConfig> SpanConfigs { get; } = new();

        /// <summary>Cấu hình của nhịp đang chọn.</summary>
        public SpanRebarConfig CurrentConfig
            => SpanConfigs.Count > SelectedSpanIndex ? SpanConfigs[SelectedSpanIndex] : null;

        // ── Nhịp đang chọn ────────────────────────────────────────────────
        [ObservableProperty] private int _selectedSpanIndex = 0;

        partial void OnSelectedSpanIndexChanged(int value)
        {
            SelectedTopLayer = null;
            SelectedBotLayer = null;
            RaiseAllProxies();
            UpdateCanvas();
            DrawSpanCanvas();
        }

        // ── UI selection (DataGrid rows) ──────────────────────────────────
        [ObservableProperty] private RebarLayerItem _selectedTopLayer;
        [ObservableProperty] private RebarLayerItem _selectedBotLayer;

        // ── Canvas references ─────────────────────────────────────────────
        private Canvas _topCanvas;
        private Canvas _botCanvas;
        private Canvas _stirrupCanvas;
        private Canvas _spanCanvas;

        #endregion

        #region Proxy Properties → CurrentConfig

        // Proxy: danh sách lớp thép (read-only — ItemsSource binding)
        public ObservableCollection<RebarLayerItem> TopLayers => CurrentConfig?.TopLayers;
        public ObservableCollection<RebarLayerItem> BotLayers => CurrentConfig?.BotLayers;

        public int TopAnchor
        {
            get => CurrentConfig?.TopAnchor ?? 300;
            set { if (CurrentConfig != null) { CurrentConfig.TopAnchor = value; OnPropertyChanged(); UpdateCanvas(); DrawSpanCanvas(); } }
        }

        public int BotAnchor
        {
            get => CurrentConfig?.BotAnchor ?? 300;
            set { if (CurrentConfig != null) { CurrentConfig.BotAnchor = value; OnPropertyChanged(); UpdateCanvas(); DrawSpanCanvas(); } }
        }

        public RebarBarType Stirrup
        {
            get => CurrentConfig?.Stirrup;
            set { if (CurrentConfig != null) { CurrentConfig.Stirrup = value; OnPropertyChanged(); UpdateCanvas(); } }
        }

        public RebarBarType StirrupCenter
        {
            get => CurrentConfig?.StirrupCenter;
            set { if (CurrentConfig != null) { CurrentConfig.StirrupCenter = value; OnPropertyChanged(); UpdateCanvas(); } }
        }

        public int StirrupSpacing
        {
            get => CurrentConfig?.StirrupSpacing ?? 100;
            set { if (CurrentConfig != null) { CurrentConfig.StirrupSpacing = value; OnPropertyChanged(); UpdateCanvas(); DrawSpanCanvas(); } }
        }

        public int StirrupCenterSpacing
        {
            get => CurrentConfig?.StirrupCenterSpacing ?? 200;
            set { if (CurrentConfig != null) { CurrentConfig.StirrupCenterSpacing = value; OnPropertyChanged(); UpdateCanvas(); DrawSpanCanvas(); } }
        }

        public int Cover
        {
            get => CurrentConfig?.Cover ?? 25;
            set { if (CurrentConfig != null) { CurrentConfig.Cover = value; OnPropertyChanged(); UpdateCanvas(); } }
        }

        /// <summary>Raise PropertyChanged cho tất cả proxy để WPF binding refresh khi đổi nhịp.</summary>
        private void RaiseAllProxies()
        {
            OnPropertyChanged(nameof(CurrentConfig));
            OnPropertyChanged(nameof(TopLayers));
            OnPropertyChanged(nameof(BotLayers));
            OnPropertyChanged(nameof(TopAnchor));
            OnPropertyChanged(nameof(BotAnchor));
            OnPropertyChanged(nameof(Stirrup));
            OnPropertyChanged(nameof(StirrupCenter));
            OnPropertyChanged(nameof(StirrupSpacing));
            OnPropertyChanged(nameof(StirrupCenterSpacing));
            OnPropertyChanged(nameof(Cover));
        }

        #endregion

        #region Constructor / Init

        public RebarBeamViewModel(UIDocument uiDocument,
                                   LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.View.View view)
        {
            UiDocument = uiDocument;
            Document   = uiDocument.Document;
            MainView   = view;
            MainView.DataContext = this;
            MainView.Loaded += MainView_Loaded;
            LoadBarTypes();
        }

        private void MainView_Loaded(object sender, RoutedEventArgs e)
        {
            _topCanvas     = MainView.FindName("TopPreviewCanvas")     as Canvas;
            _botCanvas     = MainView.FindName("BotPreviewCanvas")     as Canvas;
            _stirrupCanvas = MainView.FindName("StirrupPreviewCanvas") as Canvas;
            _spanCanvas    = MainView.FindName("SpanCanvas")           as Canvas;

            MainView.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Render,
                new Action(() =>
                {
                    UpdateCanvas();
                    DrawSpanCanvas();
                }));
        }

        /// <summary>Nạp danh sách loại thép từ Revit (gọi một lần khi khởi tạo).</summary>
        private void LoadBarTypes()
        {
            var types = new FilteredElementCollector(Document)
                .OfClass(typeof(RebarBarType))
                .Cast<RebarBarType>()
                .ToList();

            foreach (var t in types) TypeList.Add(t);
        }

        /// <summary>Tạo SpanRebarConfig cho mỗi nhịp sau khi có BeamInfo.</summary>
        private void InitSpanConfigs()
        {
            SpanConfigs.Clear();
            var first = TypeList.FirstOrDefault();

            for (int i = 0; i < BeamInfo.Spans.Count; i++)
            {
                var cfg = new SpanRebarConfig(first);
                HookConfig(cfg);
                SpanConfigs.Add(cfg);
            }

            SelectedSpanIndex = 0;
            RaiseAllProxies();
        }

        #endregion

        #region Config Hooks

        private void HookConfig(SpanRebarConfig cfg)
        {
            HookCollection(cfg.TopLayers);
            HookCollection(cfg.BotLayers);
        }

        private void HookCollection(ObservableCollection<RebarLayerItem> col)
        {
            col.CollectionChanged += OnLayerCollectionChanged;
            foreach (var item in col)
                item.PropertyChanged += OnLayerItemChanged;
        }

        private void OnLayerCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (RebarLayerItem item in e.NewItems)
                    item.PropertyChanged += OnLayerItemChanged;
            if (e.OldItems != null)
                foreach (RebarLayerItem item in e.OldItems)
                    item.PropertyChanged -= OnLayerItemChanged;
            UpdateCanvas();
            DrawSpanCanvas();
        }

        private void OnLayerItemChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateCanvas();
            DrawSpanCanvas();
        }

        private void AddLayerToCollection(ObservableCollection<RebarLayerItem> col,
                                          RebarBarType barType, int count)
        {
            var item = new RebarLayerItem { BarType = barType, Count = count };
            item.PropertyChanged += OnLayerItemChanged;
            col.Add(item);
        }

        #endregion

        #region Commands — Thêm / Xóa lớp thép

        [RelayCommand]
        private void AddTopLayer()
        {
            if (CurrentConfig == null || CurrentConfig.TopLayers.Count >= MaxLayers) return;
            AddLayerToCollection(CurrentConfig.TopLayers, TypeList.FirstOrDefault(), 2);
        }

        [RelayCommand]
        private void RemoveTopLayer()
        {
            if (CurrentConfig == null || SelectedTopLayer == null || CurrentConfig.TopLayers.Count <= 1) return;
            CurrentConfig.TopLayers.Remove(SelectedTopLayer);
            SelectedTopLayer = CurrentConfig.TopLayers.LastOrDefault();
        }

        [RelayCommand]
        private void AddBotLayer()
        {
            if (CurrentConfig == null || CurrentConfig.BotLayers.Count >= MaxLayers) return;
            AddLayerToCollection(CurrentConfig.BotLayers, TypeList.FirstOrDefault(), 2);
        }

        [RelayCommand]
        private void RemoveBotLayer()
        {
            if (CurrentConfig == null || SelectedBotLayer == null || CurrentConfig.BotLayers.Count <= 1) return;
            CurrentConfig.BotLayers.Remove(SelectedBotLayer);
            SelectedBotLayer = CurrentConfig.BotLayers.LastOrDefault();
        }

        #endregion

        #region OK / Cancel / Run

        [RelayCommand]
        private void Ok()
        {
            var tran = new Transaction(Document);
            tran.Start("CreateRebar");
            var opt = tran.GetFailureHandlingOptions();
            opt.SetFailuresPreprocessor(new DiscardWarningPreprocessor());
            tran.SetFailureHandlingOptions(opt);
            CreateRebar();
            tran.Commit();
            MainView.Close();
        }

        [RelayCommand]
        private void Cancel() => MainView.Close();

        private class DiscardWarningPreprocessor : IFailuresPreprocessor
        {
            public FailureProcessingResult PreprocessFailures(FailuresAccessor a)
            {
                foreach (var f in a.GetFailureMessages())
                    if (f.GetSeverity() == FailureSeverity.Warning) a.DeleteWarning(f);
                return FailureProcessingResult.Continue;
            }
        }

        public void Run()
        {
            if (MainView == null) return;
            var beams = UiDocument.PickBeams();
            if (beams == null || beams.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một dầm hợp lệ!", "Beam Rebar");
                return;
            }
            BeamInfo = new BeamInfo(beams);
            InitSpanConfigs();
            MainView.ShowDialog();
        }

        #endregion

        #region Rebar Creation

        private void CreateRebar()
        {
            if (BeamInfo == null) return;
            int n = BeamInfo.Spans.Count;

            // ── Thép trên: merge nhịp liền kề cùng config ────────────────
            for (int layerIdx = 0; layerIdx < LayerOffsets.Length; layerIdx++)
            {
                int s = 0;
                while (s < n)
                {
                    var sCfg = s < SpanConfigs.Count ? SpanConfigs[s] : null;
                    if (sCfg == null || sCfg.TopLayers.Count <= layerIdx) { s++; continue; }

                    var refLayer = sCfg.TopLayers[layerIdx];
                    int e = s;

                    // Kéo dài đoạn chạy khi nhịp tiếp theo có cùng cấu hình
                    while (e + 1 < n)
                    {
                        var nCfg = e + 1 < SpanConfigs.Count ? SpanConfigs[e + 1] : null;
                        if (nCfg == null || nCfg.TopLayers.Count <= layerIdx) break;
                        var nLayer = nCfg.TopLayers[layerIdx];
                        if (nLayer.Count != refLayer.Count ||
                            nLayer.BarType?.Id != refLayer.BarType?.Id) break;
                        e++;
                    }

                    int anchor = Math.Max(SpanConfigs[s].TopAnchor,
                                         e < SpanConfigs.Count ? SpanConfigs[e].TopAnchor : 0);
                    new UpperRebar(LayerOffsets[layerIdx], refLayer.BarType, BeamInfo,
                                   BeamInfo.Spans[s].StartPoint,
                                   BeamInfo.Spans[e].EndPoint,
                                   anchor, refLayer.Count).RebarCreation();
                    s = e + 1;
                }
            }

            // ── Thép dưới: merge nhịp liền kề cùng config + cùng chiều cao ─
            for (int layerIdx = 0; layerIdx < LayerOffsets.Length; layerIdx++)
            {
                int s = 0;
                while (s < n)
                {
                    var sCfg = s < SpanConfigs.Count ? SpanConfigs[s] : null;
                    if (sCfg == null || sCfg.BotLayers.Count <= layerIdx) { s++; continue; }

                    var refLayer = sCfg.BotLayers[layerIdx];
                    double refH  = BeamInfo.Spans[s].Height;
                    double refW  = BeamInfo.Spans[s].Width;
                    int e = s;

                    while (e + 1 < n)
                    {
                        var nCfg = e + 1 < SpanConfigs.Count ? SpanConfigs[e + 1] : null;
                        if (nCfg == null || nCfg.BotLayers.Count <= layerIdx) break;
                        var nLayer = nCfg.BotLayers[layerIdx];
                        if (nLayer.Count != refLayer.Count ||
                            nLayer.BarType?.Id != refLayer.BarType?.Id ||
                            Math.Abs(BeamInfo.Spans[e + 1].Height - refH) > 0.001) break;
                        e++;
                    }

                    // Tạo SpanInfo đại diện toàn đoạn liên tục
                    var mergedSpan = new SpanInfo
                    {
                        StartPoint = BeamInfo.Spans[s].StartPoint,
                        EndPoint   = BeamInfo.Spans[e].EndPoint,
                        Height     = refH,
                        Width      = refW,
                        Family     = BeamInfo.Spans[s].Family
                    };
                    int anchor = Math.Max(SpanConfigs[s].BotAnchor,
                                         e < SpanConfigs.Count ? SpanConfigs[e].BotAnchor : 0);
                    new LowerRebar(LayerOffsets[layerIdx], refLayer.BarType, BeamInfo,
                                   mergedSpan,
                                   isFirstSpan: s == 0,
                                   isLastSpan:  e == n - 1,
                                   anchor, refLayer.Count).RebarCreation();
                    s = e + 1;
                }
            }

            // ── Đai: luôn per span (bước đai khác nhau theo nhịp) ─────────
            for (int spanIdx = 0; spanIdx < n && spanIdx < SpanConfigs.Count; spanIdx++)
            {
                var cfg  = SpanConfigs[spanIdx];
                var span = BeamInfo.Spans[spanIdx];
                new StirrupRebar(cfg.StirrupCenter, cfg.Stirrup, BeamInfo,
                                 span, cfg.Cover, cfg.StirrupCenterSpacing, cfg.StirrupSpacing)
                    .RebarCreation();
            }
        }

        #endregion

        #region Canvas Drawing

        private void UpdateCanvas()
        {
            DrawSection(_topCanvas);
            DrawSection(_botCanvas);
            DrawSection(_stirrupCanvas);
        }

        private void DrawSection(Canvas canvas)
        {
            if (canvas == null || BeamInfo == null || CurrentConfig == null) return;
            canvas.Children.Clear();

            var span   = SelectedSpanIndex < BeamInfo.Spans.Count
                       ? BeamInfo.Spans[SelectedSpanIndex]
                       : BeamInfo.Spans[0];
            double beamW = span.Width.FeetToMm();
            double beamH = span.Height.FeetToMm();

            double cW = canvas.ActualWidth  > 0 ? canvas.ActualWidth  : canvas.Width;
            double cH = canvas.ActualHeight > 0 ? canvas.ActualHeight : canvas.Height;
            if (cW <= 0 || cH <= 0) return;

            double scale = Math.Min(cW / beamW, cH / beamH) * 0.82;
            double wPx   = beamW * scale;
            double hPx   = beamH * scale;
            double ox    = (cW - wPx) / 2;
            double oy    = (cH - hPx) / 2;

            AddRect(canvas, ox, oy, wPx, hPx, Brushes.Black, 1.5, Brushes.White);

            double cvPx = CurrentConfig.Cover * scale;
            AddRect(canvas, ox + cvPx, oy + cvPx, wPx - 2 * cvPx, hPx - 2 * cvPx,
                    Brushes.DimGray, 1.5, Brushes.Transparent);

            var firstType = CurrentConfig.TopLayers.FirstOrDefault()?.BarType
                         ?? CurrentConfig.BotLayers.FirstOrDefault()?.BarType;
            double rPx = firstType != null
                ? (firstType.BarModelDiameter.FeetToMm() / 2) * scale
                : 4;
            double mPx = 50.0 * scale;

            for (int i = 0; i < CurrentConfig.TopLayers.Count && i < LayerOffsets.Length; i++)
                DrawRebarRow(canvas, CurrentConfig.TopLayers[i].Count, ox,
                             oy + LayerOffsets[i] * scale, wPx, mPx, rPx, Brushes.DarkBlue);

            for (int i = 0; i < CurrentConfig.BotLayers.Count && i < LayerOffsets.Length; i++)
                DrawRebarRow(canvas, CurrentConfig.BotLayers[i].Count, ox,
                             oy + hPx - LayerOffsets[i] * scale, wPx, mPx, rPx, Brushes.DarkRed);
        }

        private static void AddRect(Canvas canvas, double x, double y, double w, double h,
                                    Brush stroke, double thickness, Brush fill)
        {
            var r = new Rectangle { Width = w, Height = h, Stroke = stroke, StrokeThickness = thickness, Fill = fill };
            Canvas.SetLeft(r, x); Canvas.SetTop(r, y);
            canvas.Children.Add(r);
        }

        private static void DrawRebarRow(Canvas canvas, int count,
                                         double ox, double y, double wPx, double mPx, double r, Brush fill)
        {
            if (count <= 0) return;
            double innerW = wPx - 2 * mPx;
            for (int i = 0; i < count; i++)
            {
                double x = count == 1 ? ox + wPx / 2 : ox + mPx + i * (innerW / (count - 1));
                var e = new Ellipse { Width = r * 2, Height = r * 2, Fill = fill };
                Canvas.SetLeft(e, x - r); Canvas.SetTop(e, y - r);
                canvas.Children.Add(e);
            }
        }

        private void DrawSpanCanvas()
        {
            if (_spanCanvas == null || BeamInfo == null || !BeamInfo.Spans.Any()) return;
            _spanCanvas.Children.Clear();

            double cW = _spanCanvas.ActualWidth;
            double cH = _spanCanvas.ActualHeight;
            if (cW <= 0 || cH <= 0) return;

            double totalMm = BeamInfo.Spans.Sum(s => s.StartPoint.DistanceTo(s.EndPoint).FeetToMm());
            double marginX = 52;
            double hScale  = (cW - 2 * marginX) / totalMm;
            double totalPx = totalMm * hScale;

            double beamHeightMm = BeamInfo.Height.FeetToMm();
            double beamH        = cH * 0.55;
            double beamTop      = cH * 0.20;
            double vScale       = beamH / beamHeightMm;

            DrawDimLine(_spanCanvas, marginX, marginX + totalPx, beamTop - 20,
                        $"Tổng: {(int)totalMm} mm");

            // ── Vẽ từng nhịp ──────────────────────────────────────────────
            double x = marginX;
            for (int i = 0; i < BeamInfo.Spans.Count; i++)
            {
                var span     = BeamInfo.Spans[i];
                var cfg      = i < SpanConfigs.Count ? SpanConfigs[i] : null;
                double mm    = span.StartPoint.DistanceTo(span.EndPoint).FeetToMm();
                double px    = mm * hScale;
                double spanH = span.Height.FeetToMm() * vScale;
                bool isSelected = i == SelectedSpanIndex;

                var fillColor = isSelected
                    ? Color.FromArgb(55, 21, 101, 192)
                    : Color.FromArgb(18, 100, 149, 237);
                AddRect(_spanCanvas, x, beamTop, px, spanH,
                        isSelected ? Brushes.DodgerBlue : Brushes.Black,
                        isSelected ? 2.0 : 1.5,
                        new SolidColorBrush(fillColor));

                PutText(_spanCanvas, $"Nhịp {i}", x + px / 2, beamTop - 16,
                        isSelected ? FontWeights.Bold : FontWeights.SemiBold, 11, center: true);

                PutText(_spanCanvas, $"{(int)mm} mm", x + px / 2, beamTop + spanH + 18,
                        FontWeights.Normal, 10, center: true);
                if (isSelected)
                    PutText(_spanCanvas,
                            $"h={(int)span.Height.FeetToMm()}  b={(int)span.Width.FeetToMm()}",
                            x + px / 2, beamTop + spanH + 30, FontWeights.Normal, 9, center: true);

                // Đai theo cấu hình nhịp này
                if (cfg != null)
                    DrawStirrupsInSpan(x, px, beamTop, spanH, hScale,
                                       cfg.StirrupSpacing, cfg.StirrupCenterSpacing);

                DrawSupport(_spanCanvas, x, beamTop + spanH);

                int capturedIdx = i;
                var overlay = new Rectangle
                {
                    Width  = px,
                    Height = spanH,
                    Fill   = Brushes.Transparent,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    ToolTip = $"Nhịp {i}  —  L={(int)mm} mm  h={(int)span.Height.FeetToMm()} mm  b={(int)span.Width.FeetToMm()} mm"
                };
                overlay.MouseLeftButtonDown += (_, e) =>
                {
                    SelectedSpanIndex = capturedIdx;
                    e.Handled = true;
                };
                Canvas.SetLeft(overlay, x);
                Canvas.SetTop(overlay, beamTop);
                _spanCanvas.Children.Add(overlay);

                x += px;
            }
            DrawSupport(_spanCanvas, x, BeamInfo.Spans.Last().Height.FeetToMm() * vScale + beamTop);

            // ── Precompute vị trí X từng nhịp ────────────────────────────
            int totalSpans = BeamInfo.Spans.Count;
            double[] spanX  = new double[totalSpans];
            double[] spanPx = new double[totalSpans];
            {
                double cx2 = marginX;
                for (int i = 0; i < totalSpans; i++)
                {
                    spanX[i]  = cx2;
                    spanPx[i] = BeamInfo.Spans[i].StartPoint.DistanceTo(BeamInfo.Spans[i].EndPoint).FeetToMm() * hScale;
                    cx2 += spanPx[i];
                }
            }

            // ── Thép trên: vẽ từng đoạn liên tục ─────────────────────────
            for (int layerIdx = 0; layerIdx < LayerOffsets.Length; layerIdx++)
            {
                int s = 0;
                while (s < totalSpans)
                {
                    var sCfg = s < SpanConfigs.Count ? SpanConfigs[s] : null;
                    if (sCfg == null || sCfg.TopLayers.Count <= layerIdx) { s++; continue; }

                    var refLayer = sCfg.TopLayers[layerIdx];
                    int e = s;
                    while (e + 1 < totalSpans)
                    {
                        var nCfg = e + 1 < SpanConfigs.Count ? SpanConfigs[e + 1] : null;
                        if (nCfg == null || nCfg.TopLayers.Count <= layerIdx) break;
                        var nL = nCfg.TopLayers[layerIdx];
                        if (nL.Count != refLayer.Count || nL.BarType?.Id != refLayer.BarType?.Id) break;
                        e++;
                    }

                    var eCfg = e < SpanConfigs.Count ? SpanConfigs[e] : sCfg;
                    double gx1 = spanX[s];
                    double gx2 = spanX[e] + spanPx[e];
                    double ry  = beamTop + LayerOffsets[layerIdx] * vScale;
                    double aStart = Math.Min(sCfg.TopAnchor * vScale, beamH * 0.38);
                    double aEnd   = Math.Min(eCfg.TopAnchor * vScale, beamH * 0.38);

                    var pts = new List<Point>();
                    if (s == 0 && sCfg.TopAnchor > 0)        pts.Add(new Point(gx1, ry + aStart));
                    pts.Add(new Point(gx1, ry));
                    pts.Add(new Point(gx2, ry));
                    if (e == totalSpans - 1 && eCfg.TopAnchor > 0) pts.Add(new Point(gx2, ry + aEnd));
                    DrawPolyline(_spanCanvas, pts, Brushes.DarkBlue, 1.6);

                    s = e + 1;
                }
            }

            // ── Thép dưới: merge khi cùng config + cùng chiều cao dầm ────
            for (int layerIdx = 0; layerIdx < LayerOffsets.Length; layerIdx++)
            {
                int s = 0;
                while (s < totalSpans)
                {
                    var sCfg = s < SpanConfigs.Count ? SpanConfigs[s] : null;
                    if (sCfg == null || sCfg.BotLayers.Count <= layerIdx) { s++; continue; }

                    var refLayer = sCfg.BotLayers[layerIdx];
                    double refH  = BeamInfo.Spans[s].Height;
                    int e = s;
                    while (e + 1 < totalSpans)
                    {
                        var nCfg = e + 1 < SpanConfigs.Count ? SpanConfigs[e + 1] : null;
                        if (nCfg == null || nCfg.BotLayers.Count <= layerIdx) break;
                        var nL = nCfg.BotLayers[layerIdx];
                        if (nL.Count != refLayer.Count || nL.BarType?.Id != refLayer.BarType?.Id ||
                            Math.Abs(BeamInfo.Spans[e + 1].Height - refH) > 0.001) break;
                        e++;
                    }

                    var eCfg     = e < SpanConfigs.Count ? SpanConfigs[e] : sCfg;
                    double spanH = refH.FeetToMm() * vScale;
                    double gx1   = spanX[s];
                    double gx2   = spanX[e] + spanPx[e];
                    double ry    = beamTop + spanH - LayerOffsets[layerIdx] * vScale;
                    double aStart = Math.Min(sCfg.BotAnchor * vScale, beamH * 0.38);
                    double aEnd   = Math.Min(eCfg.BotAnchor * vScale, beamH * 0.38);

                    var pts = new List<Point>();
                    if (s == 0 && sCfg.BotAnchor > 0)             pts.Add(new Point(gx1, ry - aStart));
                    pts.Add(new Point(gx1, ry));
                    pts.Add(new Point(gx2, ry));
                    if (e == totalSpans - 1 && eCfg.BotAnchor > 0) pts.Add(new Point(gx2, ry - aEnd));
                    DrawPolyline(_spanCanvas, pts, Brushes.DarkRed, 1.6);

                    s = e + 1;
                }
            }
        }

        private void DrawStirrupsInSpan(double spanX, double spanPx,
                                        double beamTop, double beamH, double hScale,
                                        int stirrupSpacing, int stirrupCenterSpacing)
        {
            double endPx      = spanPx / 4;
            double centerPx   = spanPx / 2;
            double endSpPx    = stirrupSpacing       * hScale;
            double centerSpPx = stirrupCenterSpacing * hScale;

            DrawStirrupZone(spanX,            endPx,    beamTop, beamH, endSpPx);
            DrawStirrupZone(spanX + endPx,    centerPx, beamTop, beamH, centerSpPx);
            DrawStirrupZone(spanX + 3*endPx,  endPx,    beamTop, beamH, endSpPx);
        }

        private void DrawStirrupZone(double zoneX, double zonePx,
                                     double beamTop, double beamH, double spacingPx)
        {
            if (spacingPx <= 1) return;
            double x = zoneX + spacingPx;
            int maxCount = 30;
            while (x < zoneX + zonePx - spacingPx * 0.5 && maxCount-- > 0)
            {
                _spanCanvas.Children.Add(new Line
                {
                    X1 = x, Y1 = beamTop + 1.5,
                    X2 = x, Y2 = beamTop + beamH - 1.5,
                    Stroke = new SolidColorBrush(Color.FromRgb(110, 110, 110)),
                    StrokeThickness = 0.9
                });
                x += spacingPx;
            }
        }

        private static void DrawPolyline(Canvas canvas, List<Point> pts, Brush stroke, double thickness)
        {
            for (int i = 0; i < pts.Count - 1; i++)
                canvas.Children.Add(new Line
                {
                    X1 = pts[i].X,     Y1 = pts[i].Y,
                    X2 = pts[i + 1].X, Y2 = pts[i + 1].Y,
                    Stroke = stroke, StrokeThickness = thickness
                });
        }

        private static void DrawSupport(Canvas canvas, double cx, double baseY)
        {
            canvas.Children.Add(new Polygon
            {
                Points = new PointCollection
                {
                    new Point(cx, baseY), new Point(cx - 9, baseY + 14), new Point(cx + 9, baseY + 14)
                },
                Fill = Brushes.DimGray, Stroke = Brushes.Black, StrokeThickness = 1
            });
            canvas.Children.Add(new Line
            {
                X1 = cx - 11, Y1 = baseY + 14, X2 = cx + 11, Y2 = baseY + 14,
                Stroke = Brushes.Black, StrokeThickness = 1.5
            });
        }

        private static void DrawDimLine(Canvas canvas, double x1, double x2, double y, string text)
        {
            canvas.Children.Add(new Line
            {
                X1 = x1, Y1 = y, X2 = x2, Y2 = y,
                Stroke = Brushes.DimGray, StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 3, 2 }
            });
            PutText(canvas, text, (x1 + x2) / 2, y - 14, FontWeights.Normal, 10, center: true);
        }

        private static void PutText(Canvas canvas, string text, double cx, double y,
                                    FontWeight weight, double size, bool center)
        {
            var tb = new System.Windows.Controls.TextBlock
            {
                Text = text, FontSize = size, FontWeight = weight, Foreground = Brushes.Black
            };
            tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double left = center ? cx - tb.DesiredSize.Width / 2 : cx;
            Canvas.SetLeft(tb, left); Canvas.SetTop(tb, y);
            canvas.Children.Add(tb);
        }

        #endregion
    }
}
