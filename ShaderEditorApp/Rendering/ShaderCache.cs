using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using System.Security.Cryptography;

namespace ShaderEditorApp.Rendering
{
	// Simple cache to avoid recompiling shaders every execution if they haven't changed.
	class ShaderCache : IDisposable
	{
		public ShaderCache(Device device)
		{
			this.device = device;
		}

		public void Dispose()
		{
			// Dispose all shaders.
			foreach (var entry in cache.Values)
				entry.shader.Dispose();

			cache.Clear();
		}

		public Shader GetShader(string filename, string entryPoint, string profile)
		{
			// Read the file and compute its hash.
			var hash = ComputeHash(filename);

			// Look in the dictionary to see if we already have this file.
			var key = new ShaderCacheKey(filename, entryPoint, profile);
			ShaderCacheEntry existingEntry;
			if (cache.TryGetValue(key, out existingEntry))
			{
				// Compare the hashes.
				if (existingEntry.hash.SequenceEqual(hash))
				{
					// Cache hit -- return existing shader.
					existingEntry.shader.Reset();
					return existingEntry.shader;
				}
				else
				{
					// Hashes don't match, so must recompile.
					// Must dispose the existing shader.
					existingEntry.shader.Dispose();
				}
			}

			// If we go this far, either the shader is not present or had a hash mismatch,
			// so, we compile a new shader.
			var shader = new Shader(device, filename, entryPoint, profile);
			cache[key] = new ShaderCacheEntry(shader, hash);

			return shader;
		}

		private byte[] ComputeHash(string filename)
		{
			// Open the file to be hashed.
			using (var stream = File.OpenRead(filename))
			{
				// Hash it using the default has algorithm.
				var algorithm = HashAlgorithm.Create();
				return algorithm.ComputeHash(stream);
			}
		}

		private Device device;
		Dictionary<ShaderCacheKey, ShaderCacheEntry> cache = new Dictionary<ShaderCacheKey, ShaderCacheEntry>();
	}

	struct ShaderCacheKey
	{
		public ShaderCacheKey(string filename, string entryPoint, string profile)
		{
			this.filename = filename;
			this.entryPoint = entryPoint;
			this.profile = profile;
		}

		// Override equality and hash functions to semantic filename comparisons.
		public override bool Equals(object obj)
		{
			var other = (ShaderCacheKey)obj;

			return
				entryPoint == other.entryPoint &&
				profile == other.profile &&
				String.Equals(
					Path.GetFullPath(filename),
					Path.GetFullPath(other.filename),
					StringComparison.InvariantCultureIgnoreCase);
		}
		public override int GetHashCode()
		{
			return
				entryPoint.GetHashCode() ^
				profile.GetHashCode() ^
				Path.GetFullPath(filename).ToLowerInvariant().GetHashCode();
		}

		public string filename;
		public string entryPoint;
		public string profile;
	}

	struct ShaderCacheEntry
	{
		public ShaderCacheEntry(Shader shader, byte[] hash)
		{
			this.shader = shader;
			this.hash = hash;
		}

		public Shader shader;
		public byte[] hash;
	}
}
