using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShaderEditorApp.Scene
{
	class MeshInstancePrimitive : Primitive
	{
		// Load the element from a JSON object.
		internal override void Load(JToken obj, Scene scene)
		{
			base.Load(obj, scene);

			// Get mesh name.
			var meshName = (string)obj["mesh"];
			if (meshName != null)
			{
				// Look up mesh in the scene's collection.
				SceneMesh mesh;
				if (scene.Meshes.TryGetValue(meshName, out mesh))
				{
					Mesh = mesh;
				}
				else
				{
					OutputLogger.Instance.LogLine(LogCategory.Log, "Mesh not found: " + meshName);
				}
			}
		}

		public SceneMesh Mesh { get; private set; }
	}
}
