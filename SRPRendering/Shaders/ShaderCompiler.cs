using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SRPCommon.Scripting;

namespace SRPRendering.Shaders
{
	static class ShaderCompiler
	{
		// Create a new shader by compiling a file.
		public static Shader CompileFromFile(Device device, string filename, string entryPoint, string profile,
			Func<string, string> includeLookup, ShaderMacro[] defines)
		{
			// SharpDX doesn't throw for *all* shader compile errors, so simplify things an throw for none.
			Configuration.ThrowOnShaderCompileError = false;

			var includeHandler = new IncludeHandler(includeLookup, filename);
			using (var compilationResult = ShaderBytecode.CompileFromFile(filename, entryPoint, profile, ShaderFlags.None, EffectFlags.None, defines, includeHandler))
			{
				// Don't check HasErrors or the status code, since apparent they sometimes
				// indicate succes even when the compile failed!
				if (compilationResult.Bytecode == null)
				{
					throw TranslateErrors(compilationResult, filename, includeHandler);
				}

				return new Shader(device, profile, includeHandler.InlcudedFilesList, compilationResult.Bytecode);
			}
		}

		// Create a new shader compiled from in-memory string.
		// Includes still come from the file system.
		public static Shader CompileFromString(Device device, string source, string entryPoint, string profile,
			Func<string, string> includeLookup, ShaderMacro[] defines)
		{
			// SharpDX doesn't throw for *all* shader compile errors, so simplify things an throw for none.
			Configuration.ThrowOnShaderCompileError = false;

			var includeHandler = new IncludeHandler(includeLookup, null);
			using (var compilationResult = ShaderBytecode.Compile(source, entryPoint, profile, ShaderFlags.None, EffectFlags.None, defines, includeHandler))
			{
				// Don't check HasErrors or the status code, since apparent they sometimes
				// indicate succes even when the compile failed!
				if (compilationResult.Bytecode == null)
				{
					throw TranslateErrors(compilationResult, "<string>", includeHandler);
				}

				return new Shader(device, profile, includeHandler.InlcudedFilesList, compilationResult.Bytecode);
			}
		}

		private static Exception TranslateErrors(CompilationResult result, string baseFilename, IncludeHandler includeHandler)
		{
			// The shader compiler error messages contain the name used to
			// include the file, rather than the full path, so we convert them back
			// with some regex fun.

			var filenameRegex = new Regex(@"^(.*)(\([0-9]+,[0-9\-]+\))", RegexOptions.Multiline);

			// SharpDX always passes a string to D3D, so errors in the original file (or string) are reported incorrectly.
			// For whatever reason, they show up as being in a file "unknown" in the current working directory.
			var unknownFile = Path.Combine(Environment.CurrentDirectory, "unknown");

			MatchEvaluator replacer = match =>
			{
				var matchedFile = match.Groups[1].Value;

				// If the filename is the original input filename, or the weird in-memory file, use the given name.
				string path;
				if (matchedFile == unknownFile)
				{
					path = baseFilename;
				}
				else
				{
					// Otherwise run it through the include lookup function again.
					if (!includeHandler.IncludedFiles.TryGetValue(matchedFile, out path))
					{
						// Error came from a file that was not included. Should never happen.
						throw new ScriptException("Internal error: shader reported error from file that was not included.");
					}
				}

				// Add back the line an column numbers.
				return path + match.Groups[2];
			};

			var message = filenameRegex.Replace(result.Message, replacer);

			return new ScriptException(message);
		}

		// Class for handling include file lookups.
		private class IncludeHandler : CallbackBase, Include
		{
			private readonly Func<string, string> _lookupFunc;
			private readonly string _baseDir;

			private readonly Dictionary<string, string> _includedFiles = new Dictionary<string, string>();
			public IReadOnlyDictionary<string, string> IncludedFiles => _includedFiles;

			public IEnumerable<IncludedFile> InlcudedFilesList => IncludedFiles.Select(pair => new IncludedFile { SourceName = pair.Key, ResolvedFile = pair.Value });

			public IncludeHandler(Func<string, string> includeLookup, string baseFilename)
			{
				_lookupFunc = includeLookup;
				_baseDir = Path.GetDirectoryName(baseFilename);
			}

			// Include interface.
			public Stream Open(IncludeType type, string filename, Stream parentStream)
			{
				// Check for null lookup function here rather than the constructor so it is possible
				// to compile #include-free shaders without bothering to provide a lookup.
				if (_lookupFunc == null)
				{
					throw new ScriptException("Attempting to compile a shader that includes files without a lookup function.");
				}

				// Check for relative include first
				string path = null;
				if (type == IncludeType.Local)
				{
					var parentFileStream = parentStream as FileStream;
					if (parentFileStream != null)
					{
						// Look relative to the parent file.
						path = Path.Combine(Path.GetDirectoryName(parentFileStream.Name), filename);
					}
					else if (_baseDir != null)
					{
						// No parent, so parent is the base file.
						path = Path.Combine(_baseDir, filename);
					}
				}

				if (!File.Exists(path))
				{
					// Path is not relative, or wasn't found in the relative location, so try global lookup.
					path = _lookupFunc(filename);
				}

				if (File.Exists(path))
				{
					// Remember that we included this file.
					_includedFiles.Add(filename, path);

					// Open file stream.
					return new FileStream(path, FileMode.Open, FileAccess.Read);
				}
				return null;
			}

			public void Close(Stream stream)
			{
				stream.Dispose();
			}
		}
	}
}
