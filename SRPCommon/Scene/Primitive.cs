using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX;
using Newtonsoft.Json.Linq;
using SRPCommon.Util;
using SRPCommon.UserProperties;
using System.Reactive.Linq;
using System.Reactive;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SRPCommon.Scene
{
	public enum PrimitiveType
	{
		MeshInstance,
		Sphere,
	}

	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public abstract class Primitive
	{
		[JsonProperty]
		[JsonConverter(typeof(StringEnumConverter))]
		public abstract PrimitiveType Type { get; }

		[JsonProperty]
		public Vector3 Position { get; set; }

		[JsonProperty]

		public Vector3 Scale { get; set; }

		[JsonProperty]
		public Vector3 Rotation { get; set; }

		public Material Material { get; private set; }

		[JsonProperty("material")]
		private string MaterialName { get; set; }

		protected List<IUserProperty> _userProperties = new List<IUserProperty>();
		public IEnumerable<IUserProperty> UserProperties => _userProperties;

		// Observable that fires when something important changes in the primitive.
		public IObservable<Unit> OnChanged { get; }

		public Primitive()
		{
			Scale = new Vector3(1.0f, 1.0f, 1.0f);

			_userProperties.Add(new StructUserProperty("Position", () => Position, o => Position = (Vector3)o));
			_userProperties.Add(new StructUserProperty("Scale", () => Scale, o => Scale = (Vector3)o));
			_userProperties.Add(new StructUserProperty("Rotation", () => Rotation, o => Rotation = (Vector3)o));

			// We change whenever our properties change.
			OnChanged = Observable.Merge(UserProperties);
		}

		internal virtual void Load(JToken obj, Scene scene)
		{
			Position = SerialisationUtils.ParseVector3(obj["position"]);
			Scale = SerialisationUtils.ParseVector3(obj["scale"], new Vector3(1.0f, 1.0f, 1.0f));
			Rotation = SerialisationUtils.ParseVector3(obj["rotation"]);

			// Get material name.
			MaterialName = (string)obj["material"];
			//var matName = (string)obj["material"];
			//if (matName != null)
			//{
			//	// Look up mesh in the scene's collection.
			//	Material mat;
			//	if (scene.Materials.TryGetValue(matName, out mat))
			//	{
			//		Material = mat;
			//	}
			//	else
			//	{
			//		OutputLogger.Instance.LogLine(LogCategory.Log, "Material not found: " + matName);
			//	}
			//}
		}

		internal virtual void PostLoad(Scene scene)
		{
			if (MaterialName != null)
			{
				// Look up mesh in the scene's collection.
				Material mat;
				if (scene.Materials.TryGetValue(MaterialName, out mat))
				{
					Material = mat;
				}
				else
				{
					OutputLogger.Instance.LogLine(LogCategory.Log, "Material not found: " + MaterialName);
				}
			}
		}

		public Matrix GetLocalToWorld()
			// TODO: rotation!
			=> Matrix.Scaling(Scale) * Matrix.Translation(Position);
	}
}
