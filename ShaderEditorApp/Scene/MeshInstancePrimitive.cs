using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ShaderEditorApp.Scene
{
	class MeshInstancePrimitive : Primitive
	{
		// Load the element from an xml element.
		internal override void Load(XElement element, Scene scene)
		{
			base.Load(element, scene);

			// Get mesh name.
			var meshAttr = element.Attribute("mesh");
			if (meshAttr != null)
			{
				// Look up mesh in the scene's collection.
				SceneMesh mesh;
				if (scene.Meshes.TryGetValue(meshAttr.Value, out mesh))
				{
					Mesh = mesh;
				}
				else
				{
					OutputLogger.Instance.LogLine(LogCategory.Log, "Mesh not found: " + meshAttr.Value);
				}
			}
		}

		public SceneMesh Mesh { get; private set; }
	}
}
