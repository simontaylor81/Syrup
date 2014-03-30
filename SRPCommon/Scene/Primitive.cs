using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX;
using Newtonsoft.Json.Linq;
using SRPCommon.Util;

namespace SRPCommon.Scene
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

		internal virtual void Load(JToken obj, Scene scene)
		{
			Position = SerialisationUtils.ParseVector3(obj["position"]);
			Scale = SerialisationUtils.ParseVector3(obj["scale"], new Vector3(1.0f, 1.0f, 1.0f));
			Rotation = SerialisationUtils.ParseVector3(obj["rotation"]);

			// Get material name.
			var matName = (string)obj["material"];
			if (matName != null)
			{
				// Look up mesh in the scene's collection.
				Material mat;
				if (scene.Materials.TryGetValue(matName, out mat))
				{
					Material = mat;
				}
				else
				{
					OutputLogger.Instance.LogLine(LogCategory.Log, "Material not found: " + matName);
				}
			}
		}

		public Matrix GetLocalToWorld()
		{
			// TODO: rotation!
			return Matrix.Scaling(Scale) * Matrix.Translation(Position);
		}
	}
}
