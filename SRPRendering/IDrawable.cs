using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPRendering
{
	// Interface for something that can be drawn to a device context.
	// This is a silly name -- can we think of something better?
	public interface IDrawable
	{
		void Draw(SharpDX.Direct3D11.DeviceContext context);
	}
}
