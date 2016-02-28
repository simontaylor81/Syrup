using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRPCommon.Scene;
using SharpDX.Direct3D11;

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
		private readonly Dictionary<string, Texture> textures = new Dictionary<string, Texture>();
		private readonly RenderDevice _device;
		private readonly Scene _scene;
		private readonly IDisposable _subscription;
		private readonly SceneMeshCache _meshCache;

		public IEnumerable<IPrimitive> Primitives => primitiveProxies;

		public RenderScene(Scene scene, RenderDevice device)
		{
			_scene = scene;
			_device = device;

			_meshCache = new SceneMeshCache(device.Device);

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
				System.Diagnostics.Debug.Assert(_device.GlobalResources.ErrorTexture != null);
				return _device.GlobalResources.ErrorTexture;
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
				.Where(primitive => primitive.IsValid)
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
			IDrawable mesh = null;


			switch (primitive.Type)
			{
				case PrimitiveType.MeshInstance:
					var meshInstance = (MeshInstancePrimitive)primitive;
					mesh = _meshCache.GetForSceneMesh(meshInstance.Mesh);
					break;

				case PrimitiveType.Sphere:
					var sphere = (SpherePrimitive)primitive;
					mesh = _meshCache.GetForSphere(sphere.Slices, sphere.Stacks);
					break;

				case PrimitiveType.Cube:
					mesh = _device.GlobalResources.CubeMesh;
					break;

				case PrimitiveType.Plane:
					mesh = _device.GlobalResources.PlaneMesh;
					break;
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
								// Always generate mips for scene textures (for now, at least).
								textures.Add(file, Texture.LoadFromFile(_device.Device, file, MipGenerationMode.Full));
							}
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
							catch (Exception)
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
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
