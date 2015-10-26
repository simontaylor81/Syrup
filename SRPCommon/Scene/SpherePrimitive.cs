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

		public override bool IsValid => Stacks > 0 && Slices > 0;

		public SpherePrimitive()
		{
			// Set safe defaults.
			Stacks = 6;
			Slices = 12;

			_userProperties.Add(new ObjectPropertyUserProperty<int>(GetType().GetProperty(nameof(Stacks)), this));
			_userProperties.Add(new ObjectPropertyUserProperty<int>(GetType().GetProperty(nameof(Slices)), this));
		}
	}
}
