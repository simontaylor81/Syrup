using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX;
using SRPCommon.Util;
using SRPCommon.UserProperties;
using System.Reactive.Linq;
using System.Reactive;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Diagnostics.CodeAnalysis;

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
		[SuppressMessage("Language", "CSE0002:Use getter-only auto properties", Justification = "Needed for serialisation")]
		private string MaterialName { get; set; }

		protected List<IUserProperty> _userProperties = new List<IUserProperty>();
		public IEnumerable<IUserProperty> UserProperties => _userProperties;

		// Observable that fires when something important changes in the primitive.
		public IObservable<Unit> OnChanged { get; }

		protected Primitive()
		{
			Scale = new Vector3(1.0f, 1.0f, 1.0f);

			_userProperties.Add(new StructUserProperty("Position", () => Position, o => Position = (Vector3)o));
			_userProperties.Add(new StructUserProperty("Scale", () => Scale, o => Scale = (Vector3)o));
			_userProperties.Add(new StructUserProperty("Rotation", () => Rotation, o => Rotation = (Vector3)o));

			// We change whenever our properties change.
			OnChanged = Observable.Merge(UserProperties);
		}

		public Matrix LocalToWorld
			// TODO: rotation!
			=> Matrix.Scaling(Scale) * Matrix.Translation(Position);

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
	}
}
