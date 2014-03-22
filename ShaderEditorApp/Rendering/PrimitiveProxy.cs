using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShaderEditorApp.Scene;
using SlimDX;

namespace ShaderEditorApp.Rendering
{
	interface IPrimitive
	{
		Matrix LocalToWorld { get; }
		Material Material { get; }
		RenderScene Scene { get; }
	}

	class PrimitiveProxy : IPrimitive
	{
		private Primitive primitive;
		public Mesh Mesh { get; private set; }
		public RenderScene Scene { get; private set; }

		public Matrix LocalToWorld { get { return primitive.GetLocalToWorld(); } }
		public Material Material { get { return primitive.Material; } }

		public PrimitiveProxy(Primitive primitive, Mesh mesh, RenderScene scene)
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
		public RenderScene Scene { get { return null; } }

		public SimplePrimitiveProxy(Matrix localToWorld)
		{
			this.localToWorld = localToWorld;
		}

		private Matrix localToWorld;
	}
}
