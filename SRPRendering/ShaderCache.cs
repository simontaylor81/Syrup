using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using System.Security.Cryptography;

namespace SRPRendering
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

		public Shader GetShader(string filename, string entryPoint, string profile, Func<string, string> includeLookup)
		{
			// Read the file and compute its hash.
			// TODO: Hash include files as well
			//var hash = ComputeHash(filename);

			// Look in the dictionary to see if we already have this file.
			var key = new ShaderCacheKey(filename, entryPoint, profile);
			ShaderCacheEntry existingEntry;
			if (cache.TryGetValue(key, out existingEntry))
			{
				// We compute the hash of the include files from the previous compilation.
				// Whilst this isn't the same as the current set, at least one of them
				// must have changed in order for the set to change, so this is sufficient
				// to detect changes.
				var hash = ComputeHash(existingEntry.shader.IncludedFiles.StartWith(filename));

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
			var shader = new Shader(device, filename, entryPoint, profile, includeLookup);
			cache[key] = new ShaderCacheEntry(shader, ComputeHash(shader.IncludedFiles.StartWith(filename)));

			return shader;
		}

		// Compute combined hash for a set of files.
		private byte[] ComputeHash(IEnumerable<string> filenames)
		{
			// Hash using MD5.
			var algorithm = MD5.Create();
			algorithm.Initialize();

			var buffer = new byte[64 * 1024];

			// Hash each file
			foreach (var filename in filenames)
			{
				using (var stream = File.OpenRead(filename))
				{
					// Stream through file building up hash.
					int bytesRead;
					while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
					{
						algorithm.TransformBlock(buffer, 0, bytesRead, null, 0);
					}
				}
			}

			// Finalise hash.
			algorithm.TransformFinalBlock(new byte[0], 0, 0);
			return algorithm.Hash;
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
