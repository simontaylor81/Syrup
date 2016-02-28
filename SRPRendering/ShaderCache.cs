using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;
using System.Security.Cryptography;
using SharpDX.Direct3D;

namespace SRPRendering
{
	public interface IShaderCache : IDisposable
	{
		IShader GetShader(string filename, string entryPoint, string profile,
			Func<string, string> includeLookup, ShaderMacro[] defines);
	}

	// Simple cache to avoid recompiling shaders every execution if they haven't changed.
	class ShaderCache : IShaderCache
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

		public IShader GetShader(string filename, string entryPoint, string profile,
			Func<string, string> includeLookup, ShaderMacro[] defines)
		{
			defines = defines ?? new ShaderMacro[0];

			// Look in the dictionary to see if we already have this file.
			var key = new ShaderCacheKey(filename, entryPoint, profile, defines);
			ShaderCacheEntry existingEntry;
			if (cache.TryGetValue(key, out existingEntry))
			{
				// We compute the hash of the include files from the previous compilation.
				// Whilst this isn't the same as the current set, at least one of them
				// must have changed in order for the set to change, so this is sufficient
				// to detect changes.
				var hash = ComputeHash(GetAllPaths(filename, existingEntry.shader));

				// Compare the hashes and includes.
				if (hash != null &&
					existingEntry.hash.SequenceEqual(hash) &&
					IncludesEqual(existingEntry.shader.IncludedFiles, includeLookup))
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

					// Explicitly remove the entry from the cache, as the re-compilation
					// might fail, and we don't want a stale reference in the cache.
					cache.Remove(key);
				}
			}

			// If we go this far, either the shader is not present or had a hash mismatch,
			// so, we compile a new shader.
			var shader = Shader.CompileFromFile(device, filename, entryPoint, profile, includeLookup, defines);
			cache[key] = new ShaderCacheEntry(shader, ComputeHash(GetAllPaths(filename, shader)));

			return shader;
		}

		private IEnumerable<string> GetAllPaths(string baseFile, IShader shader)
		{
			return shader.IncludedFiles.Select(f => f.ResolvedFile)
				.StartWith(baseFile);
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
				if (!File.Exists(filename))
				{
					// File that was previously included no longer exists.
					// Something must have changed!
					return null;
				}

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

		// Check that the set of included files still generates the same set.
		// Detects changes in the include lookup function (important for custom mip generation which
		// uses a custom include lookup which can change based on script input).
		private bool IncludesEqual(IEnumerable<IncludedFile> includedFiles, Func<string, string> includeLookup)
		{
			return includedFiles
				.All(f => string.Equals(f.ResolvedFile, includeLookup(f.SourceName), StringComparison.OrdinalIgnoreCase));
		}

		private Device device;
		Dictionary<ShaderCacheKey, ShaderCacheEntry> cache = new Dictionary<ShaderCacheKey, ShaderCacheEntry>();
	}

	struct ShaderCacheKey
	{
		public ShaderCacheKey(string filename, string entryPoint, string profile, ShaderMacro[] defines)
		{
			this.filename = filename;
			this.entryPoint = entryPoint;
			this.profile = profile;
			this.defines = defines;
		}

		// Override equality and hash functions to semantic filename comparisons.
		public override bool Equals(object obj)
		{
			var other = (ShaderCacheKey)obj;

			return
				entryPoint == other.entryPoint &&
				profile == other.profile &&
				string.Equals(
					Path.GetFullPath(filename),
					Path.GetFullPath(other.filename),
					StringComparison.InvariantCultureIgnoreCase) &&
				defines.SequenceEqual(other.defines);
		}

		public override int GetHashCode()
		{
			var definesHash = defines.Aggregate(0,
				(hash, define) => hash ^ define.GetHashCode());

			return entryPoint.GetHashCode() ^
				profile.GetHashCode() ^
				Path.GetFullPath(filename).ToLowerInvariant().GetHashCode() ^
				definesHash;
		}

		public readonly string filename;
		public readonly string entryPoint;
		public readonly string profile;
		public readonly ShaderMacro[] defines;
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
