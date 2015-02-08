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

namespace SRPCommon.Scene
{
	public abstract class Primitive
	{
		public Vector3 Position { get; set; }
		public Vector3 Scale { get; set; }
		public Vector3 Rotation { get; set; }
		public Material Material { get; private set; }

		protected List<IUserProperty> _userProperties = new List<IUserProperty>();
		public IEnumerable<IUserProperty> UserProperties { get { return _userProperties; } }

		// Observable that fires when something important changes in the primitive.
		public IObservable<Unit> OnChanged { get; private set; }

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
