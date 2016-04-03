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
	// Base class for buffer handles.
	abstract class BufferHandle : IBuffer, IDeferredResource
	{
		protected UavHandle _uav;

		public ID3DShaderResource Resource { get; protected set; }

		public abstract void CreateResource(RenderDevice renderDevice, ILogger logger, MipGenerator mipGenerator);

		public IUav CreateUav()
		{
			_uav = _uav ?? new UavHandle();
			return _uav;
		}
	}

	// Handle to a deferred-creation script-generated buffer.
	class BufferHandleFormatted<T> : BufferHandle
	{
		private readonly int _numElements;
		private readonly Format _format;
		private readonly IEnumerable<T> _contents;

		public BufferHandleFormatted(int numElements, Format format, IEnumerable<T> contents)
		{
			_numElements = numElements;
			_format = format;
			_contents = contents;
		}

		public override void CreateResource(RenderDevice renderDevice, ILogger logger, MipGenerator mipGenerator)
		{
			using (var stream = _contents != null ? StreamUtil.CreateStream(_contents.Cast<object>(), _numElements, _format) : null)
			{
				Resource = new Buffer(renderDevice.Device, _numElements * _format.Size(), _format.Size(), _uav != null, stream);
				if (_uav != null)
				{
					_uav.UAV = Resource.UAV;
				}
			}
		}
	}

	// Handle to a deferred-creation structured buffer.
	class BufferHandleStructured<T> : BufferHandle where T : struct
	{
		private readonly IEnumerable<T> _contents;

		public BufferHandleStructured(IEnumerable<T> contents)
		{
			_contents = contents;
		}

		public override void CreateResource(RenderDevice renderDevice, ILogger logger, MipGenerator mipGenerator)
		{
			Resource = Buffer.CreateStructured(renderDevice.Device, _uav != null, _contents);
			if (_uav != null)
			{
				_uav.UAV = Resource.UAV;
			}
		}
	}

	// Handle to a deferred-creation unitialised buffer.
	class BufferHandleUnitialised : BufferHandle
	{
		private readonly int _sizeInBytes;
		private readonly int _stride;

		public BufferHandleUnitialised(int sizeInBytes, int stride)
		{
			_sizeInBytes = sizeInBytes;
			_stride = stride;
		}

		public override void CreateResource(RenderDevice renderDevice, ILogger logger, MipGenerator mipGenerator)
		{
			Resource = new Buffer(renderDevice.Device, _sizeInBytes, _stride, _uav != null, null);
			if (_uav != null)
			{
				_uav.UAV = Resource.UAV;
			}
		}
	}
}
