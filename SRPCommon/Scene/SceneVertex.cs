﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SlimDX;
using SlimDX.Direct3D11;

namespace SRPCommon.Scene
{
	public struct SceneVertex
	{
		public Vector3 Position;
		public Vector3 Normal;

		public Vector2 UV0;
		public Vector2 UV1;
		public Vector2 UV2;
		public Vector2 UV3;

		public SceneVertex(Vector3 position, Vector3 normal)
		{
			Position = position;
			Normal = normal;
			UV0 = UV1 = UV2 = UV3 = Vector2.Zero;
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
			throw new ArgumentOutOfRangeException("UV index must be between 0 and 3");
		}

		public void SetUV(int index, Vector2 value)
		{
			switch (index)
			{
				case 0: UV0 = value; break;
				case 1: UV1 = value; break;
				case 2: UV2 = value; break;
				case 3: UV3 = value; break;
				default: throw new ArgumentOutOfRangeException("UV index must be between 0 and 3");
			}
		}

		// Array of input element structures that describe the layout of vertices to D3D.
		public static InputElement[] InputElements
		{
			get
			{
				return new[] {
					new InputElement("POSITION", 0, SlimDX.DXGI.Format.R32G32B32_Float, 0),
					new InputElement("NORMAL", 0, SlimDX.DXGI.Format.R32G32B32_Float, 0),
					new InputElement("TEXCOORD", 0, SlimDX.DXGI.Format.R32G32_Float, 0),
					new InputElement("TEXCOORD", 1, SlimDX.DXGI.Format.R32G32_Float, 0),
					new InputElement("TEXCOORD", 2, SlimDX.DXGI.Format.R32G32_Float, 0),
					new InputElement("TEXCOORD", 3, SlimDX.DXGI.Format.R32G32_Float, 0)
				};
			}
		}

		// Get the size of a the vertex, to be used as the stride for vertex buffers.
		public static int GetStride()
		{
			return Marshal.SizeOf(typeof(SceneVertex));
		}
	}
}