using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using SlimDX;

namespace ShaderEditorApp.Scene
{
	public abstract class Primitive
	{
		public Vector3 Position { get; set; }
		public Vector3 Scale { get; set; }
		public Vector3 Rotation { get; set; }
		public Material Material { get; private set; }

		public Primitive()
		{
			Scale = new Vector3(1.0f, 1.0f, 1.0f);
		}

		internal virtual void Load(XElement element, Scene scene)
		{
			SerialisationUtils.ParseAttribute(element, "position", str => { Position = SerialisationUtils.ParseVector3(str); });
			SerialisationUtils.ParseAttribute(element, "scale", str => { Scale = SerialisationUtils.ParseVector3(str); });
			SerialisationUtils.ParseAttribute(element, "rotation", str => { Rotation = SerialisationUtils.ParseVector3(str); });

			// Get material name.
			var matAttr = element.Attribute("material");
			if (matAttr != null)
			{
				// Look up mesh in the scene's collection.
				Material mat;
				if (scene.Materials.TryGetValue(matAttr.Value, out mat))
				{
					Material = mat;
				}
				else
				{
					OutputLogger.Instance.LogLine(LogCategory.Log, "Material not found: " + matAttr.Value);
				}
			}
		}

		public Matrix GetLocalToWorld()
		{
			// TODO: Scaling and rotation!
			return Matrix.Scaling(Scale) * Matrix.Translation(Position);
		}
	}
}
