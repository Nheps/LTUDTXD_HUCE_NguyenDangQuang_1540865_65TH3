using Autodesk.Revit.DB.Structure;
using System.Collections.ObjectModel;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Model
{
    /// <summary>
    /// Toàn bộ cấu hình cốt thép cho MỘT nhịp dầm.
    /// Mỗi nhịp có danh sách lớp thép trên, thép dưới và thông số đai riêng.
    /// </summary>
    public partial class SpanRebarConfig : ObservableObject
    {
        // ── Thép chính ──────────────────────────────────────────────────
        public ObservableCollection<RebarLayerItem> TopLayers { get; } = new();
        public ObservableCollection<RebarLayerItem> BotLayers { get; } = new();

        [ObservableProperty] private int _topAnchor = 300;
        [ObservableProperty] private int _botAnchor = 300;

        // ── Thép đai ────────────────────────────────────────────────────
        [ObservableProperty] private RebarBarType _stirrup;
        [ObservableProperty] private RebarBarType _stirrupCenter;
        [ObservableProperty] private int _stirrupSpacing = 100;
        [ObservableProperty] private int _stirrupCenterSpacing = 200;

        // ── Lớp bảo vệ ──────────────────────────────────────────────────
        [ObservableProperty] private int _cover = 25;

        public SpanRebarConfig(RebarBarType defaultBarType)
        {
            Stirrup       = defaultBarType;
            StirrupCenter = defaultBarType;

            TopLayers.Add(new RebarLayerItem { BarType = defaultBarType, Count = 3 });
            TopLayers.Add(new RebarLayerItem { BarType = defaultBarType, Count = 2 });
            BotLayers.Add(new RebarLayerItem { BarType = defaultBarType, Count = 3 });
            BotLayers.Add(new RebarLayerItem { BarType = defaultBarType, Count = 2 });
        }
    }
}
