using SRPCommon.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace SRPCommon.Scene
{
	public class MeshInstancePrimitive : Primitive
	{
		public override PrimitiveType Type => PrimitiveType.MeshInstance;

		public SceneMesh Mesh { get; private set; }

		public override bool IsValid => Mesh != null && Mesh.IsValid;

		[JsonProperty("mesh")]
		[SuppressMessage("Language", "CSE0002:Use getter-only auto properties", Justification = "Needed for serialisation")]
		private string MeshName { get; set; }

		internal override void PostLoad(Scene scene)
		{
			base.PostLoad(scene);

			if (MeshName != null)
			{
				// Look up mesh in the scene's collection.
				SceneMesh mesh;
				if (scene.Meshes.TryGetValue(MeshName, out mesh))
				{
					Mesh = mesh;
				}
				else
				{
					OutputLogger.Instance.LogLine(LogCategory.Log, "Mesh not found: " + MeshName);
				}
			}
		}
	}
}
