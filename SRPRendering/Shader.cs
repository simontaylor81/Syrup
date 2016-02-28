using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;

using SRPCommon.Util;
using SRPCommon.Scripting;
using System.Text.RegularExpressions;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using SharpDX;
using SharpDX.Direct3D;

namespace SRPRendering
{
	public enum ShaderFrequency
	{
		Vertex,
		Pixel,
		Compute,
		MAX
	}

	public struct IncludedFile
	{
		public string SourceName;		// Original name from the #include line.
		public string ResolvedFile;		// File path that it was resolved to.
	}

	public interface IShader : IDisposable
	{
		// Bind the shader to the device.
		void Set(DeviceContext context);

		// Upload constants if required.
		void UpdateVariables(DeviceContext context, ViewInfo viewInfo, IPrimitive primitive, IDictionary<string, dynamic> overrides, IGlobalResources globalResources);

		// Reset to the same state as immediately after compile.
		void Reset();

		IEnumerable<IShaderVariable> Variables { get; }

		// Find a variable by name.
		IShaderVariable FindVariable(string name);

		// Find a resource variable by name.
		IShaderResourceVariable FindResourceVariable(string name);

		// Find a sample variable by name.
		IShaderSamplerVariable FindSamplerVariable(string name);

		// Find a UAV variable by name.
		IShaderUavVariable FindUavVariable(string name);

		// Input signature. Vertex shader only.
		ShaderSignature Signature { get; }

		// Frequency (i.e. type) of shader.
		ShaderFrequency Frequency { get; }

		// List of files that were included by this shader.
		IEnumerable<IncludedFile> IncludedFiles { get; }
	}

	class Shader : IShader
	{
		private Shader(Device device, string profile, IncludeHandler includeHandler, Func<ShaderBytecode> compiler)
		{
			// Compile the shader to bytecode.
			using (var bytecode = compiler())
			{
				IncludedFiles = includeHandler != null ? includeHandler.IncludedFiles : Enumerable.Empty<IncludedFile>();

				// Create the shader object of the appropriate type.
				switch (profile.Substring(0, 2))
				{
					case "vs":
						_vertexShader = new VertexShader(device, bytecode);
						Signature = ShaderSignature.GetInputSignature(bytecode);
						_frequency = ShaderFrequency.Vertex;
						break;

					case "ps":
						_pixelShader = new PixelShader(device, bytecode);
						_frequency = ShaderFrequency.Pixel;
						break;

					case "cs":
						_computeShader = new ComputeShader(device, bytecode);
						_frequency = ShaderFrequency.Compute;
						break;

					default:
						throw new Exception("Unsupported shader profile: " + profile);
				}

				// Get info about the shader's inputs.
				using (var reflection = new ShaderReflection(bytecode))
				{
					// Gether constant buffers.
					_cbuffers = reflection.GetConstantBuffers()
						.Where(cbuffer => cbuffer.Description.Type == ConstantBufferType.ConstantBuffer)
						.Select(cbuffer => new ConstantBuffer(device, cbuffer))
						.ToArray();
					_cbuffers_buffers = _cbuffers.Select(cbuffer => cbuffer.Buffer).ToArray();

					// Gather resource and sampler inputs.
					var boundResources = reflection.GetBoundResources();
					_resourceVariables = boundResources
						.Where(desc => IsShaderResource(desc.Type))
						.Select(desc => new ShaderResourceVariable(desc, Frequency))
						.ToArray();
					_samplerVariables = boundResources
						.Where(desc => desc.Type == ShaderInputType.Sampler)
						.Select(desc => new ShaderSamplerVariable(desc, Frequency))
						.ToArray();
					_uavVariables = boundResources
						.Where(desc => IsUav(desc.Type))
						.Select(desc => new ShaderUavVariable(desc, Frequency))
						.ToArray();
				}
			}
		}

		// Create a new shader by compiling a file.
		public static Shader CompileFromFile(Device device, string filename, string entryPoint, string profile,
			Func<string, string> includeLookup, ShaderMacro[] defines)
		{
			try
			{
				var includeHandler = includeLookup != null ? new IncludeHandler(includeLookup) : null;
				return new Shader(device, profile, includeHandler,
					() => ShaderBytecode.CompileFromFile(filename, entryPoint, profile, ShaderFlags.None, EffectFlags.None, defines, includeHandler));
			}
			catch (CompilationException ex)
			{
				throw TranslateException(ex, filename, includeLookup);
			}
		}

		// Create a new shader compiled from in-memory string.
		// Includes still come from the file system.
		public static Shader CompileFromString(Device device, string source, string entryPoint, string profile,
			Func<string, string> includeLookup, ShaderMacro[] defines)
		{
			try
			{
				var includeHandler = includeLookup != null ? new IncludeHandler(includeLookup) : null;
				return new Shader(device, profile, includeHandler,
					() => ShaderBytecode.Compile(source, entryPoint, profile, ShaderFlags.None, EffectFlags.None, defines, includeHandler));
			}
			catch (CompilationException ex)
			{
				throw TranslateException(ex, "<string>", includeLookup);
			}
		}

		private static Exception TranslateException(CompilationException ex, string baseFilename, Func<string, string> includeLookup)
		{
			// The shader compiler error messages contain the name used to
			// include the file, rather than the full path, so we convert them back
			// with some regex fun.

			var filenameRegex = new Regex(@"^(.*)(\([0-9]+,[0-9]+\))", RegexOptions.Multiline);

			// When compiling from string, the errors come from some weird non-existant path.
			var inMemoryFileRegex = new Regex(@"Shader@0x[0-9A-F]{8}$");

			MatchEvaluator replacer = match =>
			{
				var matchedFile = match.Groups[1].Value;
				
				// If the filename is the original input filename, or the weird in-memory file, use the given name.
				string path;
				if (matchedFile == baseFilename || inMemoryFileRegex.IsMatch(matchedFile))
				{
					path = baseFilename;
				}
				else
				{
					// Otherwise run it through the include lookup function again.
					path = includeLookup(matchedFile);
				}

				// Add back the line an column numbers.
				return path + match.Groups[2];
			};

			var message = filenameRegex.Replace(ex.Message, replacer);

			OutputLogger.Instance.Log(LogCategory.ShaderCompile, message);
			return new ScriptException("Shader compilation failed. See Shader Compilation log for details.", ex);
		}

		private static bool IsShaderResource(ShaderInputType type) =>
			type == ShaderInputType.Texture ||
			type == ShaderInputType.Structured ||
			type == ShaderInputType.ByteAddress;

		private bool IsUav(ShaderInputType type) =>
			type == ShaderInputType.UnorderedAccessViewRWTyped ||
			type == ShaderInputType.UnorderedAccessViewRWStructured ||
			type == ShaderInputType.UnorderedAccessViewRWStructuredWithCounter ||
			type == ShaderInputType.UnorderedAccessViewRWByteAddress;

		public void Dispose()
		{
			foreach (var cbuffer in _cbuffers)
				cbuffer.Dispose();

			DisposableUtil.SafeDispose(_vertexShader);
			DisposableUtil.SafeDispose(_pixelShader);
			DisposableUtil.SafeDispose(_computeShader);
			DisposableUtil.SafeDispose(Signature);
		}


		// Bind the shader to the device.
		public void Set(DeviceContext context)
		{
			if (_vertexShader != null)
			{
				context.VertexShader.Set(_vertexShader);
				context.VertexShader.SetConstantBuffers(0, _cbuffers_buffers);
			}
			else if (_pixelShader != null)
			{
				context.PixelShader.Set(_pixelShader);
				context.PixelShader.SetConstantBuffers(0, _cbuffers_buffers);
			}
			else if (_computeShader != null)
			{
				context.ComputeShader.Set(_computeShader);
				context.ComputeShader.SetConstantBuffers(0, _cbuffers_buffers);
			}
		}

		// Upload constants if required.
		public void UpdateVariables(DeviceContext context, ViewInfo viewInfo, IPrimitive primitive, IDictionary<string, dynamic> overrides, IGlobalResources globalResources)
		{
			// First, update the value of bound and overridden variables.
			foreach (var variable in Variables)
			{
				// Is the variable bound?
				if (variable.Bind != null)
				{
					variable.Bind.UpdateVariable(viewInfo, primitive, overrides);
				}

				// Warn if the user is attempting to override the value, but the variable has not been
				// set as overridable.
				if (overrides != null &&
					overrides.ContainsKey(variable.Name) &&
					(variable.Bind == null || !variable.Bind.AllowScriptOverride))
				{
					OutputLogger.Instance.LogLineOnce(LogCategory.Script,
						"Warning: attempt to override shader variable {0} which has not been marked as overridable. Call ShaderVariableIsScriptOverride to mark it thus.",
						variable.Name);
				}
			}

			// Next, do the actual upload the constant buffers.
			foreach (var cubffer in _cbuffers)
				cubffer.Update(context);

			// Update resource variables too.
			foreach (var resourceVariable in _resourceVariables)
			{
				if (resourceVariable.Bind != null)
				{
					resourceVariable.Resource = resourceVariable.Bind.GetResource(primitive, viewInfo, globalResources);
				}

				resourceVariable.SetToDevice(context);
			}

			// And samplers.
			foreach (var samplerVariable in _samplerVariables)
			{
				if (samplerVariable.Bind != null)
				{
					samplerVariable.State = samplerVariable.Bind.GetState(primitive, viewInfo, globalResources);
				}
				samplerVariable.SetToDevice(context);
			}

			// And UAVs.
			foreach (var uavVariable in _uavVariables)
			{
				uavVariable.SetToDevice(context);
			}
		}

		// Reset to the same state as immediately after compile.
		public void Reset()
		{
			foreach (var variable in Variables)
			{
				// Restore initial value if the variable was bound.
				// If it wasn't, we keep the original value so the user doesn't lose their settings.
				if (variable.Bind != null)
				{
					// Restore default value.
					variable.SetDefault();

					// Clear bind.
					variable.Bind = null;
				}
			}

			// Do the same for resource, sampler and UAV variables.
			foreach (var resourceVariable in _resourceVariables)
			{
				resourceVariable.Resource = null;
				resourceVariable.Bind = null;
			}

			foreach (var samplerVariable in _samplerVariables)
			{
				samplerVariable.State = null;
				samplerVariable.Bind = null;
			}

			foreach (var uavVariable in _uavVariables)
			{
				uavVariable.UAV = null;
			}
		}

		// Get all variables from all cbuffers.
		public IEnumerable<IShaderVariable> Variables
			=> from cbuffer in _cbuffers from variable in cbuffer.Variables select variable;

		// Find a variable by name.
		public IShaderVariable FindVariable(string name)
			=> Variables.FirstOrDefault(v => v.Name == name);

		// Find a resource variable by name.
		public IShaderResourceVariable FindResourceVariable(string name)
			=> _resourceVariables.FirstOrDefault(v => v.Name == name);

		// Find a sampler variable by name.
		public IShaderSamplerVariable FindSamplerVariable(string name)
			=> _samplerVariables.FirstOrDefault(v => v.Name == name);

		// Find a UAV variable by name.
		public IShaderUavVariable FindUavVariable(string name)
			=> _uavVariables.FirstOrDefault(v => v.Name == name);

		// Actual shader. Only one of these is non-null.
		private VertexShader _vertexShader;
		private PixelShader _pixelShader;
		private ComputeShader _computeShader;

		// Input signature. Vertex shader only.
		public ShaderSignature Signature { get; }

		// Frequency (i.e. type) of shader.
		private readonly ShaderFrequency _frequency;
		public ShaderFrequency Frequency => _frequency;

		// List of files that were included by this shader.
		public IEnumerable<IncludedFile> IncludedFiles { get; }

		// Constant buffer info.
		private ConstantBuffer[] _cbuffers;
		private SharpDX.Direct3D11.Buffer[] _cbuffers_buffers;

		// Resource variable info.
		private ShaderResourceVariable[] _resourceVariables;

		// Sampler input info.
		private ShaderSamplerVariable[] _samplerVariables;

		// UAV variable info.
		private ShaderUavVariable[] _uavVariables;

		// Class for handling include file lookups.
		private class IncludeHandler : CallbackBase, Include
		{
			private Func<string, string> includeLookup;

			private List<IncludedFile> _includedFiles = new List<IncludedFile>();
			public IEnumerable<IncludedFile> IncludedFiles => _includedFiles;

			public IncludeHandler(Func<string, string> includeLookup)
			{
				this.includeLookup = includeLookup;
			}

			// Include interface.
			public Stream Open(IncludeType type, string filename, Stream parentStream)
			{
				// Find full path.
				var path = includeLookup(filename);

				// Remember that we included this file.
				_includedFiles.Add(new IncludedFile { SourceName = filename, ResolvedFile = path });

				// Open file stream.
				return new FileStream(path, FileMode.Open);
			}

			public void Close(Stream stream)
			{
				stream.Dispose();
			}
		}
	}
}
