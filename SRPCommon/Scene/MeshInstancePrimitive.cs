using SRPCommon.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SRPCommon.Scene
{
	public class MeshInstancePrimitive : Primitive
	{
		public override PrimitiveType Type => PrimitiveType.MeshInstance;

		public SceneMesh Mesh { get; private set; }

		[JsonProperty("mesh")]
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
