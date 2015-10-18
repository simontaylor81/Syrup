using Newtonsoft.Json.Linq;
using SRPCommon.UserProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SRPCommon.Scene
{
	public class SpherePrimitive : Primitive
	{
		public override PrimitiveType Type => PrimitiveType.Sphere;

		[JsonProperty]
		public int Stacks { get; set; }
		[JsonProperty]
		public int Slices { get; set; }

		public SpherePrimitive()
		{
			// Set safe defaults.
			Stacks = 6;
			Slices = 12;

			_userProperties.Add(new ObjectPropertyUserProperty<int>(GetType().GetProperty(nameof(Stacks)), this));
			_userProperties.Add(new ObjectPropertyUserProperty<int>(GetType().GetProperty(nameof(Slices)), this));
		}

		// Load the primitive from a JSON object.
		internal override void Load(JToken obj, Scene scene)
		{
			base.Load(obj, scene);

			Stacks = (int)obj["stacks"];
			Slices = (int)obj["slices"];
		}
	}
}
