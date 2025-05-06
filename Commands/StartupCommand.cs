using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.View;

namespace LTUDTXD_HUCE_NguyenDangQuang_1540865_65TH3.Commands
{
	/// <summary>
	///     External command entry point invoked from the Revit interface
	/// </summary>
	[UsedImplicitly]
	[Transaction(TransactionMode.Manual)]
	public class StartupCommand : ExternalCommand
	{
		public override void Execute()
		{
			
			try
			{
				var view = new View.View();
				var viewModel = new ViewModel.RebarBeamViewModel(Document, view);
				viewModel.Run();
			}
			catch (Exception)
			{

				throw;
			}
		}
	}
}