using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderEditorApp.Rendering
{
	// Opaque handles to the various resources to be given to script.
	// All internal so their workings are hidden from script.
	// They're also all classes so they're inherently nullable. IronPython boxes
	// value types anyway, so there's no real performance cost to this.

	// Base class for all handle types.
	internal abstract class HandleBase : IEquatable<HandleBase>
	{
		internal readonly int index;
		internal HandleBase(int index)
		{
			this.index = index;
		}

		public override bool Equals(object obj)
		{
			var other = obj as HandleBase;
			return other != null && Equals(other);
		}
		public bool Equals(HandleBase other)
		{
			return GetType() == other.GetType() && index == other.index;
		}
		public override int GetHashCode()
		{
			return index;
		}
	}

	// Handle to a shader.
	internal class ShaderHandle : HandleBase
	{
		internal ShaderHandle(int index)
			: base(index)
		{
		}
	}

	// Handle to a texture.
	internal class TextureHandle : HandleBase
	{
		internal TextureHandle(int index)
			: base(index)
		{
		}
	}

	// Handle to a render traget.
	internal class RenderTargetHandle : HandleBase
	{
		internal RenderTargetHandle(int index)
			: base(index)
		{
		}
	}

	// Handle to a depth buffer.
	internal class DepthBufferHandle : HandleBase
	{
		internal DepthBufferHandle(int index)
			: base(index)
		{
		}

		// Special handle to the default depth buffer.
		internal static DepthBufferHandle Default { get { return default_; } }
		private static DepthBufferHandle default_ = new DepthBufferHandle(-1);

		// Special handle to indicate that no depth buffer should be set.
		internal static DepthBufferHandle NoDepthBuffer { get { return noDepthBuffer_; } }
		private static DepthBufferHandle noDepthBuffer_ = new DepthBufferHandle(-2);
	}
}
