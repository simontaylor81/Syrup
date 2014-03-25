using Newtonsoft.Json.Linq;
using SlimDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderEditorApp.Scene
{
	/// <summary>
	/// A light in the scene. Point-lights only currently.
	/// </summary>
	public class SceneLight
	{
		public Vector3 Position { get; private set; }
		public Vector3 Colour { get; private set; }
		public float Radius { get; private set; }

		public SceneLight()
		{
			Colour = new Vector3(1.0f, 1.0f, 1.0f);
			Radius = 1.0f;
		}

		internal static SceneLight Load(JToken obj)
		{
			var result = new SceneLight();
			result.Position = SerialisationUtils.ParseVector3(obj["position"]);
			result.Colour = SerialisationUtils.ParseVector3(obj["colour"], new Vector3(1.0f, 1.0f, 1.0f));
			result.Radius = (float?)obj["radius"] ?? 1.0f;

			return result;
		}
	}
}
