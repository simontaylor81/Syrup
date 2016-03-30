using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SRPScripting;

namespace SRPRendering.Resources
{
	// Indirection layer to actual render target resource, which may be recreated when the screen is resized.
	class RenderTargetHandle : IRenderTarget, IViewDependentRenderTarget, IDisposable
	{
		public Rational Width { get; }
		public Rational Height { get; }
		public bool IsViewportRelative { get; }

		// The actual render target resource.
		// TODO: Multiple sub-resource to handle multiple viewports.
		private RenderTarget _renderTarget;

		// IViewDependentRenderTarget interface.
		public ID3DShaderResource GetShaderResource(ViewInfo viewInfo) => _renderTarget;
		public RenderTarget GetRenderTarget(ViewInfo viewInfo) => _renderTarget;

		public RenderTargetHandle(Rational width, Rational height, bool viewportRelative)
		{
			Width = width;
			Height = height;
			IsViewportRelative = viewportRelative;
		}

		public int GetWidth(int viewportWidth)
		{
			if (IsViewportRelative)
				return viewportWidth * Width.Numerator / Width.Denominator;
			else
				return Width.Numerator / Width.Denominator;
		}
		public int GetHeight(int viewportHeight)
		{
			if (IsViewportRelative)
				return viewportHeight * Height.Numerator / Height.Denominator;
			else
				return Height.Numerator / Height.Denominator;
		}

		public void Dispose()
		{
			_renderTarget?.Dispose();
			_renderTarget = null;
		}

		// (Re-)allocate the resource if its size does not match the given viewport dimensions.
		public void UpdateSize(SharpDX.Direct3D11.Device device, int viewportWidth, int viewportHeight)
		{
			int width = GetWidth(viewportWidth);
			int height = GetHeight(viewportHeight);

			// If there's no resource, or it's the wrong size, create a new one.
			if (_renderTarget == null || _renderTarget.Width != width || _renderTarget.Height != height)
			{
				// Don't forget to release the old one.
				_renderTarget?.Dispose();

				// TODO: Custom format
				_renderTarget = new RenderTarget(device, width, height, SharpDX.DXGI.Format.R8G8B8A8_UNorm);
			}
		}
	}
}
