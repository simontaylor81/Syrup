using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Assimp;
using Newtonsoft.Json;
using SRPCommon.Util;

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

		public IEnumerable<SceneVertex> Vertices { get; private set; }
		public IEnumerable<short> Indices { get; private set; }

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

					// We have generate normals/tangent basis enabled, so all meshes should have them.
					Debug.Assert(srcMesh.HasNormals);
					Debug.Assert(srcMesh.HasTangentBasis);

					// Create vertex array.
					Vertices = Enumerable.Range(0, srcMesh.VertexCount)
						.Select(i =>
						{
							var vertex = new SceneVertex(
								ToVector3(srcMesh.Vertices[i]),
								ToVector3(srcMesh.Normals[i]),
								ToVector3(srcMesh.Tangents[i]),
								ToVector3(srcMesh.BiTangents[i]));

							for (int uvChannel = 0; uvChannel < srcMesh.TextureCoordinateChannelCount; uvChannel++)
							{
								var uv = srcMesh.TextureCoordinateChannels[uvChannel][i];
								vertex.SetUV(uvChannel, new System.Numerics.Vector2(uv.X, uv.Y));
							}

							return vertex;
						})
						.ToList();

					// AssImp should triangulate the mesh for us (since it's enabled in the post-process options).
					Debug.Assert(srcMesh.Faces.All(face => face.IndexCount == 3));

					Indices = srcMesh.Faces
						.SelectMany(face => face.Indices)
						.Select(i => (short)i)
						.ToList();

					isValid = true;
				}
			}
		}

		// Convert AssImp types to System.Numerics ones.
		private static System.Numerics.Vector3 ToVector3(Vector3D vec) => new System.Numerics.Vector3(vec.X, vec.Y, vec.Z);
	}
}
