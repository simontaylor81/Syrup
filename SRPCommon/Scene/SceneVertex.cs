using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Numerics;

namespace SRPCommon.Scene
{
	public struct SceneVertex
	{
		public Vector3 Position;
		public Vector3 Normal;
		public Vector3 Tangent;
		public Vector3 BiTangent;

		public Vector2 UV0;
		public Vector2 UV1;
		public Vector2 UV2;
		public Vector2 UV3;

		public SceneVertex(Vector3 position, Vector3 normal, Vector3 tangent, Vector3 biTangent)
			: this(position, normal, tangent, biTangent, Vector2.Zero)
		{
		}

		public SceneVertex(Vector3 position, Vector3 normal, Vector3 tangent, Vector3 biTangent, Vector2 uv)
		{
			Position = position;
			Normal = normal;
			Tangent = tangent;
			BiTangent = biTangent;
			UV0 = UV1 = UV2 = UV3 = uv;
		}

		public Vector2 GetUV(int index)
		{
			switch (index)
			{
				case 0: return UV0;
				case 1: return UV1;
				case 2: return UV2;
				case 3: return UV3;
			}
			throw new ArgumentOutOfRangeException(nameof(index), "UV index must be between 0 and 3");
		}

		public void SetUV(int index, Vector2 value)
		{
			switch (index)
			{
				case 0: UV0 = value; break;
				case 1: UV1 = value; break;
				case 2: UV2 = value; break;
				case 3: UV3 = value; break;
				default: throw new ArgumentOutOfRangeException(nameof(index), "UV index must be between 0 and 3");
			}
		}

		// Get the size of a the vertex, to be used as the stride for vertex buffers.
		public static int GetStride() => Marshal.SizeOf(typeof(SceneVertex));
	}
}
