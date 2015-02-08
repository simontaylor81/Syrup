using Newtonsoft.Json.Linq;
using SRPCommon.UserProperties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SRPCommon.Scene
{
	public class SpherePrimitive : Primitive
	{
		public int Stacks { get; set; }
		public int Slices { get; set; }

		public SpherePrimitive()
		{
			// Set safe defaults.
			Stacks = 12;
			Slices = 6;
		}

		// Load the primitive from a JSON object.
		internal override void Load(JToken obj, Scene scene)
		{
			base.Load(obj, scene);

			Stacks = (int)obj["stacks"];
			Slices = (int)obj["slices"];

			_userProperties.Add(new ObjectPropertyUserProperty<int>(GetType().GetProperty("Stacks"), this));
			_userProperties.Add(new ObjectPropertyUserProperty<int>(GetType().GetProperty("Slices"), this));
		}
	}
}
