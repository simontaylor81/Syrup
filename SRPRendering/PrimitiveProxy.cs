﻿using System;
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
		System.Numerics.Matrix4x4 LocalToWorld { get; }
		Material Material { get; }
		IRenderScene Scene { get; }
		IDrawable Mesh { get; }
	}

	class PrimitiveProxy : IPrimitive
	{
		private readonly Primitive primitive;

		public IDrawable Mesh { get; }
		public IRenderScene Scene { get; }

		public System.Numerics.Matrix4x4 LocalToWorld => primitive.LocalToWorld;
		public Material Material => primitive.Material;

		public PrimitiveProxy(Primitive primitive, IDrawable mesh, IRenderScene scene)
		{
			this.primitive = primitive;
			this.Mesh = mesh;
			this.Scene = scene;
		}
	}

	// A primitive proxy that doesn't actually represent a primitive in the scene, just a simple shape drawn directly.
	class SimplePrimitiveProxy : IPrimitive
	{
		public System.Numerics.Matrix4x4 LocalToWorld { get; }
		public Material Material => null;
		public IRenderScene Scene => null;
		public IDrawable Mesh => null;

		public SimplePrimitiveProxy(System.Numerics.Matrix4x4 localToWorld)
		{
			LocalToWorld = localToWorld;
		}
	}
}
