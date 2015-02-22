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
		public IDrawable Mesh { get; private set; }
		public IRenderScene Scene { get; private set; }

		public Matrix LocalToWorld { get { return primitive.GetLocalToWorld(); } }
		public Material Material { get { return primitive.Material; } }

		public PrimitiveProxy(Primitive primitive, Mesh mesh, IRenderScene scene)
		{
			this.primitive = primitive;
			this.Mesh = mesh;
			this.Scene = scene;
		}
	}

	// A primitive proxy that doesn't actually represent a primitive in the scene, just a simple shape draw directly.
	class SimplePrimitiveProxy : IPrimitive
	{
		public Matrix LocalToWorld { get { return localToWorld; } }
		public Material Material { get { return null; } }
		public IRenderScene Scene { get { return null; } }
		public IDrawable Mesh { get { return null; } }

		public SimplePrimitiveProxy(Matrix localToWorld)
		{
			this.localToWorld = localToWorld;
		}

		private Matrix localToWorld;
	}
}
