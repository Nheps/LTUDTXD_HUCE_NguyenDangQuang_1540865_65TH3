using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Autodesk.Revit.DB.Structure;
using LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.View;
using System.Windows;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.ViewModel
{
	public partial class RebarBeamViewModel : ObservableObject
	{
		public Document Document { get; set; }
		public View.View MainView { get; set; }
        public List<RebarBarType> TypeList { get; set; }

        [ObservableProperty]
        private RebarBarType _top1;
        [ObservableProperty]
        private RebarBarType _top2;
        [ObservableProperty]
        private RebarBarType _top3;
        [ObservableProperty]
        private RebarBarType _bot1;
        [ObservableProperty]
        private RebarBarType _bot2;
        [ObservableProperty]
        private RebarBarType _bot3;
        [ObservableProperty]
        private RebarBarType _stirrupCenter;
        [ObservableProperty]
        private RebarBarType _stirrup;
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
        [RelayCommand]
		public void OK()
		{
			MessageBox.Show("Developping......");
		}
		[RelayCommand]
		public void Cancel()
		{
			MainView.Close();
		}
		public RebarBeamViewModel(Document document, View.View view)
		{
			Document = document;
			MainView = view;
			MainView.DataContext = this;
            InitData();

        }
        public void InitData()
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
			MainView.ShowDialog();
		}
	}
}
