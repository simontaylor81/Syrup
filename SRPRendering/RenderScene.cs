﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRPCommon.Scene;
using SlimDX.Direct3D11;
using Buffer = SlimDX.Direct3D11.Buffer;

namespace SRPRendering
{
	public interface IRenderScene : IDisposable
	{
		IEnumerable<IPrimitive> Primitives { get; }

		// Return the texture for a given filename. Always returns a valid ref.
		Texture GetTexture(string filename);
	}

	class RenderScene : IRenderScene
	{
		public IEnumerable<IPrimitive> Primitives => primitiveProxies;

		// Return the texture for a given filename. Always returns a valid ref.
		public Texture GetTexture(string filename)
		{
			// Look in the dictionary for a texture for this filename.
			Texture texture;
			if (textures.TryGetValue(filename, out texture))
			{
				return texture;
			}
			else
			{
				// No texture found, so return error texture.
				System.Diagnostics.Debug.Assert(_globalResources.ErrorTexture != null);
				return _globalResources.ErrorTexture;
			}
		}

		public RenderScene(Scene scene, Device device, IGlobalResources globalResources)
		{
			_device = device;
			_globalResources = globalResources;

			var meshDict = new Dictionary<SceneMesh, Mesh>();

			// Any relative (texture) paths are relative to the scene file itself.
			var prevCurrentDir = Environment.CurrentDirectory;
			Environment.CurrentDirectory = Path.GetDirectoryName(scene.Filename);

			// Create render proxies for primitives in the scene.
			foreach (var primitive in scene.Primitives)
			{
			}

			Environment.CurrentDirectory = prevCurrentDir;
		}

		public void Dispose()
		{
			foreach (var mesh in meshes)
			{
				mesh.Dispose();
			}
			foreach (var texture in textures.Values)
			{
				texture.Dispose();
			}
		}

		private PrimitiveProxy CreateProxy(Primitive primitive)
		{
			PrimitiveProxy result = null;
			Mesh mesh = null;

			if (primitive is MeshInstancePrimitive)
			{
				var instance = (MeshInstancePrimitive)primitive;
				if (!meshDict.TryGetValue(instance.Mesh, out mesh))
				{
					// Create the mesh and add to the lookup map.
					mesh = new Mesh(_device, instance.Mesh.Vertices, SceneVertex.GetStride(),
						instance.Mesh.Indices, SceneVertex.InputElements);
					meshDict.Add(instance.Mesh, mesh);
				}
			}
			else if (primitive is SpherePrimitive)
			{
				// No instancing of spheres, just create a mesh for each one.
				var sphere = (SpherePrimitive)primitive;
				mesh = BasicMesh.CreateSphere(_device, sphere.Slices, sphere.Stacks);
			}

			if (mesh != null)
			{
				// Create proxy for the instance.
				result = new PrimitiveProxy(primitive, mesh, this);

				// Create texture resource for textures used by this primitive's material.
				if (primitive.Material != null)
				{
					foreach (var file in primitive.Material.Textures.Values)
					{
						if (!textures.ContainsKey(file))
						{
							try
							{
								textures.Add(file, Texture.LoadFromFile(_device, file));
							}
							catch (Exception)
							{
								// For now, just don't add the texture.
							}
						}
					}
				}
			}
		}

		private List<PrimitiveProxy> primitiveProxies = new List<PrimitiveProxy>();
		private Dictionary<SceneMesh, Mesh> _meshes = new Dictionary<SceneMesh, Mesh>();
		private Dictionary<string, Texture> textures = new Dictionary<string, Texture>();
		private readonly IGlobalResources _globalResources;
		private readonly Device _device;
	}
}
