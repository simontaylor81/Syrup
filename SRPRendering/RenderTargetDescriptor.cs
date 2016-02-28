using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX.DXGI;

namespace SRPRendering
{
	class RenderTargetDescriptor : IDisposable
	{
		public readonly Rational Width;
		public readonly Rational Height;
		public readonly bool bViewportRelative;

		// The render target resource itself.
		public RenderTarget renderTarget;

		public RenderTargetDescriptor(Rational width, Rational height, bool viewportRelative)
		{
			Width = width;
			Height = height;
			bViewportRelative = viewportRelative;
		}

		public void Dispose()
		{
			if (renderTarget != null)
			{
				renderTarget.Dispose();
				renderTarget = null;
			}
		}

		public int GetWidth(int viewportWidth)
		{
			if (bViewportRelative)
				return viewportWidth * Width.Numerator / Width.Denominator;
			else
				return Width.Numerator / Width.Denominator;
		}
		public int GetHeight(int viewportHeight)
		{
			if (bViewportRelative)
				return viewportHeight * Height.Numerator / Height.Denominator;
			else
				return Height.Numerator / Height.Denominator;
		}
	}
}
