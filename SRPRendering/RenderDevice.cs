using SlimDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace SRPRendering
{
	// Class responsible for initialisation and release of the central render
	// resources (i.e. the D3D device).
	public class RenderDevice : IDisposable
	{
		public Device Device { get; }
		public IGlobalResources GlobalResources { get; }

		private CompositeDisposable _disposables = new CompositeDisposable();

		public RenderDevice()
		{
			var deviceCreationFlags = DeviceCreationFlags.None;
#if DEBUG
			// Create debug device in debug mode.
			deviceCreationFlags |= DeviceCreationFlags.Debug;
#endif

			// If you get a debug-only crash here, make sure you have the debug D3D dlls installed
			// ("Graphics Tools" under Optional Features in Windows 10).
			Device = new Device(DriverType.Hardware, deviceCreationFlags);
			_disposables.Add(Device);

			// Initialise basic resources.
			GlobalResources = new GlobalResources(Device);
			_disposables.Add(GlobalResources);
		}

		public void Dispose()
		{
			_disposables.Dispose();
		}
	}
}
