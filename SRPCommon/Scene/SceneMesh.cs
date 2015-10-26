using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Assimp;
using SlimDX;
using SRPCommon.Util;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace SRPCommon.Scene
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public class SceneMesh
	{
		public string Name { get; internal set; }

		[JsonProperty]
		[SuppressMessage("Language", "CSE0002:Use getter-only auto properties", Justification = "Needed for serialisation")]
		public string Filename { get; private set; }

		private bool isValid = false;
		public bool IsValid => isValid;

		public DataStream Vertices { get; private set; }
		public DataStream Indices { get; private set; }

		// Load the mesh itself after serialisation.
		internal void PostLoad()
		{
			// Load the file if it exists.
			if (File.Exists(Filename))
			{
				Import();
			}
			else
			{
				OutputLogger.Instance.LogLine(LogCategory.Log, "Mesh not found: {0}", Filename);
			}
		}

		private void Import()
		{
			using (var importer = new AssimpContext())
			{
				// Set configuration. TODO: What does this stuff do?!
				importer.SetConfig(new Assimp.Configs.NormalSmoothingAngleConfig(66.0f));

				// Uncomment this line to echo assimp output to the log window.
				//importer.AttachLogStream(new Assimp.LogStream((msg, userData) => OutputLogger.Instance.Log(LogCategory.Log, msg)));

				// TODO: What do the post-process flags do?
				var model = importer.ImportFile(Filename, PostProcessPreset.TargetRealTimeMaximumQuality | PostProcessSteps.FlipUVs);

				// Create our mesh structure using the imported data.
				if (model.HasMeshes && model.MeshCount > 0)
				{
					// TODO: Handle multiple meshes per file?
					// TODO: Or maybe have an index in the scene file?
					var srcMesh = model.Meshes[0];

					if (!srcMesh.HasVertices || !srcMesh.HasFaces)
						return;

					// We have generate normals enabled, so all meshes should have them.
					Debug.Assert(srcMesh.HasNormals);

					// Create vertex stream.
					int vertexBufferSize = SceneVertex.GetStride() * srcMesh.VertexCount;
					Vertices = new DataStream(vertexBufferSize, true, true);

					for (int i = 0; i < srcMesh.VertexCount; i++)
					{
						var vertex = new SceneVertex(
							ToVector3(srcMesh.Vertices[i]),
							ToVector3(srcMesh.Normals[i]));

						for (int uvChannel = 0; uvChannel < srcMesh.TextureCoordinateChannelCount; uvChannel++)
						{
							var uv = srcMesh.TextureCoordinateChannels[uvChannel][i];
							vertex.SetUV(uvChannel, new Vector2(uv.X, uv.Y));
						}

						Vertices.Write(vertex);
					}

					int indexBufferSize = 2 * srcMesh.FaceCount * 3;
					Indices = new DataStream(indexBufferSize, true, true);

					foreach (var face in srcMesh.Faces)
					{
						// AssImp should triangulate the mesh for us (since it's enabled in the post-process options).
						Debug.Assert(face.IndexCount == 3);

						Indices.Write((Int16)face.Indices[0]);
						Indices.Write((Int16)face.Indices[1]);
						Indices.Write((Int16)face.Indices[2]);
					}

					isValid = true;
				}
			}
		}

		// Convert AssImp types to SlimDX ones.
		private static Vector3 ToVector3(Vector3D vec) => new Vector3(vec.X, vec.Y, vec.Z);
	}
}
