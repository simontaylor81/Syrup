﻿using System;
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
		Cube,
		Plane,
	}

	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public abstract class Primitive
	{
		[JsonProperty]
		[JsonConverter(typeof(StringEnumConverter))]
		public abstract PrimitiveType Type { get; }

		[JsonProperty]
		public System.Numerics.Vector3 Position { get; set; }

		[JsonProperty]

		public System.Numerics.Vector3 Scale { get; set; }

		[JsonProperty]
		public System.Numerics.Vector3 Rotation { get; set; }

		public Material Material { get; private set; }

		[JsonProperty("material")]
		[SuppressMessage("Language", "CSE0002:Use getter-only auto properties", Justification = "Needed for serialisation")]
		private string MaterialName { get; set; }

		protected List<IUserProperty> _userProperties = new List<IUserProperty>();
		public IEnumerable<IUserProperty> UserProperties => _userProperties;

		public virtual bool IsValid => true;

		// Observable that fires when something important changes in the primitive.
		public IObservable<Unit> OnChanged { get; }

		protected Primitive()
		{
			Scale = new System.Numerics.Vector3(1.0f, 1.0f, 1.0f);

			_userProperties.Add(new StructUserProperty("Position", () => Position, o => Position = (System.Numerics.Vector3)o));
			_userProperties.Add(new StructUserProperty("Scale", () => Scale, o => Scale = (System.Numerics.Vector3)o));
			_userProperties.Add(new StructUserProperty("Rotation", () => Rotation, o => Rotation = (System.Numerics.Vector3)o));

			// We change whenever our properties change.
			OnChanged = Observable.Merge(UserProperties);
		}

		public System.Numerics.Matrix4x4 LocalToWorld
			=> System.Numerics.Matrix4x4.CreateScale(Scale)
				* System.Numerics.Matrix4x4.CreateFromYawPitchRoll(ToRadians(Rotation.Y), ToRadians(Rotation.X), ToRadians(Rotation.Z))
				* System.Numerics.Matrix4x4.CreateTranslation(Position);

		private float ToRadians(float degrees) => degrees * ((float)Math.PI / 180.0f);

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
