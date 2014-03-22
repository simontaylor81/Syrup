using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

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

		// Load the element from an xml element.
		internal override void Load(XElement element, Scene scene)
		{
			base.Load(element, scene);

			SerialisationUtils.ParseAttribute(element, "stacks", str => { Stacks = int.Parse(str); });
			SerialisationUtils.ParseAttribute(element, "slices", str => { Slices = int.Parse(str); });
		}
	}
}
