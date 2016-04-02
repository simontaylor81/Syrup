using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;
using SRPCommon.Logging;
using SRPCommon.Scripting;
using SRPScripting;

namespace SRPRendering.Resources
{
	// Handle to a deferred-creation script-generated buffer.
	class BufferHandleDynamic : IBuffer, IDeferredResource
	{
		private readonly int _sizeInBytes;
		private readonly Format _format;
		private readonly object _contents;
		private UavHandle _uav;

		public ID3DShaderResource Resource { get; private set; }

		public BufferHandleDynamic(int sizeInBytes, Format format, object contents)
		{
			_sizeInBytes = sizeInBytes;
			_format = format;
			_contents = contents;
		}

		public IUav CreateUav()
		{
			if (_uav == null)
			{
				_uav = new UavHandle();
			}
			return _uav;
		}

		public void CreateResource(RenderDevice renderDevice, ILogger logger, MipGenerator mipGenerator)
		{
			Resource = Buffer.CreateDynamic(renderDevice.Device, _sizeInBytes, _uav != null, _format, _contents);
			if (_uav != null)
			{
				_uav.UAV = Resource.UAV;
			}
		}
	}

	// Handle to a deferred-creation structured buffer.
	class BufferHandleStructured<T> : IBuffer, IDeferredResource where T : struct
	{
		private readonly IEnumerable<T> _contents;
		private UavHandle _uav;

		public ID3DShaderResource Resource { get; private set; }

		public BufferHandleStructured(IEnumerable<T> contents)
		{
			_contents = contents;
		}

		public IUav CreateUav()
		{
			if (_uav == null)
			{
				_uav = new UavHandle();
			}
			return _uav;
		}

		public void CreateResource(RenderDevice renderDevice, ILogger logger, MipGenerator mipGenerator)
		{
			Resource = Buffer.CreateStructured(renderDevice.Device, _uav != null, _contents);
			if (_uav != null)
			{
				_uav.UAV = Resource.UAV;
			}
		}
	}
}
