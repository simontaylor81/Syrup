using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SRPCommon.Util;
using Device = SharpDX.Direct3D11.Device;

namespace SRPRendering
{
	// Class responsible for initialisation and release of the central render
	// resources (i.e. the D3D device).
	public class RenderDevice : IDisposable
	{
		public Device Device { get; }
		public IGlobalResources GlobalResources { get; }

		public Adapter Adapter => _adapter.Value;
		private Lazy<Adapter> _adapter;

		private CompositeDisposable _disposables = new CompositeDisposable();

		public RenderDevice(bool useWarp = false)
		{
			var deviceCreationFlags = DeviceCreationFlags.None;
#if DEBUG
			// Create debug device in debug mode.
			deviceCreationFlags |= DeviceCreationFlags.Debug;
#endif

			// If you get a debug-only crash here, make sure you have the debug D3D dlls installed
			// ("Graphics Tools" under Optional Features in Windows 10).
			Device = new Device(useWarp ? DriverType.Warp : DriverType.Hardware, deviceCreationFlags);
			_disposables.Add(Device);

			// Lazily get adapter from DXGI device.
			_adapter = new Lazy<Adapter>(() =>
			{
				using (var dxgiDevice = Device.QueryInterface<SharpDX.DXGI.Device>())
				{
					return _disposables.AddAndReturn(dxgiDevice.Adapter);
				}
			});

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
