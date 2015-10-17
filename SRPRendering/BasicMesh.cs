using System;
using System.Collections.Generic;
using SRPCommon.Scene;
using SlimDX;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using Buffer = SlimDX.Direct3D11.Buffer;

namespace SRPRendering
{
	// Methods for creation of basic meshes.
	class BasicMesh
	{
		public static Mesh CreateSphere(SlimDX.Direct3D11.Device device, int slices, int stacks)
		{
			int i;

			// sin/cos caches.
			float[] sliceSin = new float[slices];
			float[] sliceCos = new float[slices];
			float[] stackSin = new float[stacks];
			float[] stackCos = new float[stacks];

			// Populate caches.
			for (i = 0; i < slices; i++)
			{
				double theta = 2.0 * Math.PI * (double)i / (double)slices;
				sliceSin[i] = (float)Math.Sin(theta);
				sliceCos[i] = (float)Math.Cos(theta);
			}
			for (i = 0; i < stacks; i++)
			{
				double theta = Math.PI * (double)i / (double)stacks;
				stackSin[i] = (float)Math.Sin(theta);
				stackCos[i] = (float)Math.Cos(theta);
			}

			// Generate vertices.
			int numVerts = (stacks - 1) * slices + 2;
			int vertexBufferSize = SceneVertex.GetStride() * numVerts;
			var vertices = new DataStream(vertexBufferSize, true, true);

			// +Y pole
			WriteSphereVert(vertices, new Vector3(0.0f, 1.0f, 0.0f));

			// Stacks
			for (int j = 1; j < stacks; j++)
			{
				for (i = 0; i < slices; i++)
				{
					WriteSphereVert(vertices, new Vector3(
						sliceSin[i] * stackSin[j],
						stackCos[j],
						sliceCos[i] * stackSin[j]
						));
				}
			}

			// -Y pole.
			WriteSphereVert(vertices, new Vector3(0.0f, -1.0f, 0.0f));

			// Generate indices.
			int numIndices = 2 * (stacks - 1) * slices * 3;
			int indexBufferSize = numIndices * sizeof(Int16);
			var indices = new DataStream(indexBufferSize, true, true);

			int rowA = 0;
			int rowB = 1;

			for (i = 0; i < slices - 1; i++)
				AddFace(indices, rowA, rowB + i, rowB + i + 1);

			AddFace(indices, rowA, rowB + i, rowB);

			// Interior stacks.
			for (int j = 1; j < stacks - 1; j++)
			{
				rowA = 1 + (j - 1) * slices;
				rowB = rowA + slices;

				for (i = 0; i < slices - 1; i++)
				{
					AddFace(indices, rowA + i, rowB + i, rowA + i + 1);
					AddFace(indices, rowA + i + 1, rowB + i, rowB + i + 1);
				}
				AddFace(indices, rowA, rowA + i, rowB + i);
				AddFace(indices, rowA, rowB + i, rowB);
			}

			// -Z pole
			rowA = 1 + (stacks - 2) * slices;
			rowB = rowA + slices;

			for (i = 0; i < slices - 1; i++)
				AddFace(indices, rowA + i, rowB, rowA + i + 1);

			AddFace(indices, rowA + i, rowB, rowA);

			// Wrap up in a new mesh object.
			return new Mesh(device, vertices, SceneVertex.GetStride(), indices, InputElements);
		}

		public static InputElement[] InputElements => SceneVertex.InputElements;

		// Write a vertex for a sphere, which has a normal equal to its position.
		private static void WriteSphereVert(DataStream stream, Vector3 position)
		{
			stream.Write(new SceneVertex(position, position));
		}

		private static void AddFace(DataStream stream, int i0, int i1, int i2)
		{
			stream.Write((Int16)i0);
			stream.Write((Int16)i1);
			stream.Write((Int16)i2);
		}
	}
}
