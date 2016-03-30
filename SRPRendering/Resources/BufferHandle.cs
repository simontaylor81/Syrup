﻿using System;
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
		private readonly bool _uav;
		private readonly Format _format;
		private readonly object _contents;

		public ID3DShaderResource Resource { get; private set; }

		public BufferHandleDynamic(int sizeInBytes, bool uav, Format format, object contents)
		{
			_sizeInBytes = sizeInBytes;
			_uav = uav;
			_format = format;
			_contents = contents;
		}

		public void CreateResource(RenderDevice renderDevice, ILogger logger, MipGenerator mipGenerator)
		{
			Resource = Buffer.CreateDynamic(renderDevice.Device, _sizeInBytes, _uav, _format, _contents);
		}
	}

	// Handle to a deferred-creation structured buffer.
	class BufferHandleStructured<T> : IBuffer, IDeferredResource where T : struct
	{
		private readonly bool _uav;
		private readonly IEnumerable<T> _contents;

		public ID3DShaderResource Resource { get; private set; }

		public BufferHandleStructured(bool uav, IEnumerable<T> contents)
		{
			_uav = uav;
			_contents = contents;
		}

		public void CreateResource(RenderDevice renderDevice, ILogger logger, MipGenerator mipGenerator)
		{
			Resource = Buffer.CreateStructured<T>(renderDevice.Device, _uav, _contents);
		}
	}
}
