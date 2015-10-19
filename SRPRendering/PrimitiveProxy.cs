using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRPCommon.Scene;
using SlimDX;

namespace SRPRendering
{
	public interface IPrimitive
	{
		Matrix LocalToWorld { get; }
		Material Material { get; }
		IRenderScene Scene { get; }
		IDrawable Mesh { get; }
	}

	class PrimitiveProxy : IPrimitive
	{
		private Primitive primitive;
		public IDrawable Mesh { get; }
		public IRenderScene Scene { get; }

		public Matrix LocalToWorld => primitive.LocalToWorld;
		public Material Material => primitive.Material;

		public PrimitiveProxy(Primitive primitive, Mesh mesh, IRenderScene scene)
		{
			this.primitive = primitive;
			this.Mesh = mesh;
			this.Scene = scene;
		}
	}

	// A primitive proxy that doesn't actually represent a primitive in the scene, just a simple shape drawn directly.
	class SimplePrimitiveProxy : IPrimitive
	{
		public Matrix LocalToWorld => localToWorld;
		public Material Material => null;
		public IRenderScene Scene => null;
		public IDrawable Mesh => null;

		public SimplePrimitiveProxy(Matrix localToWorld)
		{
			this.localToWorld = localToWorld;
		}

		private Matrix localToWorld;
	}
}
