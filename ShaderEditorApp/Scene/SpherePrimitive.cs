using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShaderEditorApp.Scene
{
	class SpherePrimitive : Primitive
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
		}
	}
}
