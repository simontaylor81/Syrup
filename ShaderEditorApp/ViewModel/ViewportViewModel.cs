using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderEditorApp.ViewModel
{
	public class ViewportViewModel : ReactiveObject
	{
		public enum CameraMode
		{
			Orbit,
			Walk,
		}

		// Names of the available camera modes.
		public IEnumerable<CameraMode> CameraModes => Enum.GetValues(typeof(CameraMode)).OfType<CameraMode>();

		// The currently selected camera mode.
		private CameraMode _selectedCameraMode = CameraMode.Orbit;
		public CameraMode SelectedCameraMode
		{
			get { return _selectedCameraMode; }
			set { this.RaiseAndSetIfChanged(ref _selectedCameraMode, value); }
		}
	}
}
