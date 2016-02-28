using System;
using System.Collections.Generic;
using SRPCommon.Scene;
using System.Numerics;
using SharpDX.Direct3D11;
using DataStream = SharpDX.DataStream;

namespace SRPRendering
{
	// Methods for creation of basic meshes.
	class BasicMesh
	{
		// Create a cube with corners at 1 (side length 2).
		public static Mesh CreateCube(Device device)
		{
			// Generate vertices.
			int numVerts = 24;
			int vertexBufferSize = SceneVertex.GetStride() * numVerts;
			var vertices = new DataStream(vertexBufferSize, true, true);

			WriteCubeFaceVerts(vertices, Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY);
			WriteCubeFaceVerts(vertices, -Vector3.UnitX, -Vector3.UnitZ, Vector3.UnitY);

			WriteCubeFaceVerts(vertices, Vector3.UnitY, Vector3.UnitX, Vector3.UnitZ);
			WriteCubeFaceVerts(vertices, -Vector3.UnitY, Vector3.UnitX, -Vector3.UnitZ);

			WriteCubeFaceVerts(vertices, Vector3.UnitZ, -Vector3.UnitX, Vector3.UnitY);
			WriteCubeFaceVerts(vertices, -Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY);

			// Generate indices.
			int numIndices = 36;
			int indexBufferSize = numIndices * sizeof(Int16);
			var indices = new DataStream(indexBufferSize, true, true);

			for (int face = 0; face < 6; face++)
			{
				AddFace(indices, 4 * face + 0, 4 * face + 1, 4 * face + 3);
				AddFace(indices, 4 * face + 0, 4 * face + 3, 4 * face + 2);
			}

			return new Mesh(device, vertices, SceneVertex.GetStride(), indices, InputElements);
		}

		// Create single square face in the XZ plane with corners at 1 (edge length 2).
		public static Mesh CreatePlane(Device device)
		{
			// Generate vertices.
			int numVerts = 4;
			int vertexBufferSize = SceneVertex.GetStride() * numVerts;
			var vertices = new DataStream(vertexBufferSize, true, true);

			WriteSquareVerts(vertices, Vector3.Zero, Vector3.UnitY, Vector3.UnitX, Vector3.UnitZ);

			// Generate indices.
			int numIndices = 6;
			int indexBufferSize = numIndices * sizeof(Int16);
			var indices = new DataStream(indexBufferSize, true, true);

			AddFace(indices, 0, 1, 3);
			AddFace(indices, 0, 3, 2);

			return new Mesh(device, vertices, SceneVertex.GetStride(), indices, InputElements);
		}

		// Create a sphere with radius 1.
		public static Mesh CreateSphere(Device device, int slices, int stacks)
		{
			int i;

			// sin/cos caches.
			float[] sliceSin = new float[slices];
			float[] sliceCos = new float[slices];
			float[] stackSin = new float[stacks + 1];
			float[] stackCos = new float[stacks + 1];

			// Populate caches.
			for (i = 0; i < slices; i++)
			{
				double theta = 2.0 * Math.PI * (double)i / (double)slices;
				sliceSin[i] = (float)Math.Sin(theta);
				sliceCos[i] = (float)Math.Cos(theta);
			}
			for (i = 0; i <= stacks; i++)
			{
				double theta = Math.PI * (double)i / (double)stacks;
				stackSin[i] = (float)Math.Sin(theta);
				stackCos[i] = (float)Math.Cos(theta);
			}

			// Generate vertices.
			int numVerts = (stacks + 1) * (slices + 1);
			int vertexBufferSize = SceneVertex.GetStride() * numVerts;
			var vertices = new DataStream(vertexBufferSize, true, true);

			// Stacks
			for (int j = 0; j <= stacks; j++)
			{
				float v = (float)j / (float)stacks;

				for (i = 0; i <= slices; i++)
				{
					float u = (float)i / (float)slices;
					WriteSphereVert(vertices, new Vector3(
						sliceSin[i % slices] * stackSin[j],
						stackCos[j],
						sliceCos[i % slices] * stackSin[j]
						),
						new Vector2(u, v));
				}
			}

			// Generate indices.
			int numIndices = 2 * (stacks - 1) * slices * 3;
			int indexBufferSize = numIndices * sizeof(Int16);
			var indices = new DataStream(indexBufferSize, true, true);

			int rowA = 0;
			int rowB = rowA + slices + 1;

			for (i = 0; i < slices; i++)
			{
				AddFace(indices, rowA + i, rowB + i, rowB + i + 1);
			}

			// Interior stacks.
			for (int j = 1; j < stacks - 1; j++)
			{
				rowA = j * (slices + 1);
				rowB = rowA + slices + 1;

				for (i = 0; i < slices; i++)
				{
					AddFace(indices, rowA + i, rowB + i, rowA + i + 1);
					AddFace(indices, rowA + i + 1, rowB + i, rowB + i + 1);
				}
			}

			// -Z pole
			rowA = (stacks - 1) * (slices + 1);
			rowB = rowA + slices + 1;

			for (i = 0; i < slices; i++)
			{
				AddFace(indices, rowA + i, rowB + i, rowA + i + 1);
			}

			// Wrap up in a new mesh object.
			return new Mesh(device, vertices, SceneVertex.GetStride(), indices, InputElements);
		}

		public static InputElement[] InputElements => InputLayoutCache.SceneVertexInputElements;

		// Write a vertex for a sphere, which has a normal equal to its position.
		private static void WriteSphereVert(DataStream vertices, Vector3 position, Vector2 uv)
		{
			var tangent = Vector3.Cross(position, Vector3.UnitY);
			if (tangent.LengthSquared() < 0.01f)
			{
				tangent = Vector3.UnitX;
			}
			tangent = Vector3.Normalize(tangent);

			var biTangent = Vector3.Cross(position, tangent);
			biTangent = Vector3.Normalize(biTangent);

			vertices.Write(new SceneVertex(position, position, tangent, biTangent, uv));
		}

		// Write 4 verts forming a square quad.
		private static void WriteSquareVerts(DataStream vertices, Vector3 o, Vector3 n, Vector3 u, Vector3 v)
		{
			vertices.Write(new SceneVertex(o - u + v, n, u, v, new Vector2(0.0f, 0.0f)));
			vertices.Write(new SceneVertex(o + u + v, n, u, v, new Vector2(1.0f, 0.0f)));
			vertices.Write(new SceneVertex(o - u - v, n, u, v, new Vector2(0.0f, 1.0f)));
			vertices.Write(new SceneVertex(o + u - v, n, u, v, new Vector2(1.0f, 1.0f)));
		}

		// Write 4 verts forming the face of a cube.
		private static void WriteCubeFaceVerts(DataStream vertices, Vector3 n, Vector3 u, Vector3 v)
		{
			// Origin is the normal.
			WriteSquareVerts(vertices, n, n, u, v);
		}

		private static void AddFace(DataStream indices, int i0, int i1, int i2)
		{
			indices.Write((Int16)i0);
			indices.Write((Int16)i1);
			indices.Write((Int16)i2);
		}
	}
}
