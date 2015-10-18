using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRPCommon.Scene;
using SlimDX.Direct3D11;

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
		private List<PrimitiveProxy> primitiveProxies = new List<PrimitiveProxy>();
		private Dictionary<string, Texture> textures = new Dictionary<string, Texture>();
		private readonly IGlobalResources _globalResources;
		private readonly Device _device;
		private readonly Scene _scene;
		private readonly IDisposable _subscription;
		private readonly SceneMeshCache _meshCache;

		public IEnumerable<IPrimitive> Primitives => primitiveProxies;

		public RenderScene(Scene scene, Device device, IGlobalResources globalResources)
		{
			_scene = scene;
			_device = device;
			_globalResources = globalResources;

			_meshCache = new SceneMeshCache(device);

			// Create proxies now, and when the scene changes.
			CreateProxies();
			_subscription = scene.OnChanged.Subscribe(_ => CreateProxies());
		}

		public void Dispose()
		{
			_meshCache.Dispose();
			_subscription.Dispose();
			
			foreach (var texture in textures.Values)
			{
				texture.Dispose();
			}
		}

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

		// Create render proxies for primitives in the scene.
		private void CreateProxies()
		{
			// Any relative (texture) paths are relative to the scene file itself.
			var prevCurrentDir = Environment.CurrentDirectory;
			Environment.CurrentDirectory = Path.GetDirectoryName(_scene.Filename);

			// Create a proxy for each primitive.
			primitiveProxies = _scene.Primitives
				.Select(primitive => CreateProxy(primitive))
				.Where(proxy => proxy != null)
				.ToList();

			// Release unused meshes.
			var usedMeshes = primitiveProxies.Select(proxy => proxy.Mesh).Distinct().ToList();
			_meshCache.ReleaseUnusedMeshes(usedMeshes);

			Environment.CurrentDirectory = prevCurrentDir;
		}

		private PrimitiveProxy CreateProxy(Primitive primitive)
		{
			PrimitiveProxy result = null;
			Mesh mesh = null;

			if (primitive is MeshInstancePrimitive)
			{
				var instance = (MeshInstancePrimitive)primitive;
				mesh = _meshCache.GetForSceneMesh(instance.Mesh);
			}
			else if (primitive is SpherePrimitive)
			{
				var sphere = (SpherePrimitive)primitive;
				mesh = _meshCache.GetForSphere(sphere.Slices, sphere.Stacks);
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

			return result;
		}
	}
}
