using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.Scene
{
	// Class representing primitives that just use one of the simple global mesh types.
	public class SimplePrimitive : Primitive
	{
		private readonly PrimitiveType _type;

		public override PrimitiveType Type => _type;

		public SimplePrimitive(PrimitiveType type)
		{
			Debug.Assert(type == PrimitiveType.Cube || type == PrimitiveType.Plane);
			_type = type;
		}
	}
}
