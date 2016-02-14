using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;

using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using Buffer = SlimDX.Direct3D11.Buffer;
using SRPCommon.Util;
using SRPCommon.Scripting;
using System.Text.RegularExpressions;

namespace SRPRendering
{
	public enum ShaderFrequency
	{
		Vertex,
		Pixel,
		Compute,
		MAX
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

		// Input signature. Vertex shader only.
		ShaderSignature Signature { get; }

		// Frequency (i.e. type) of shader.
		ShaderFrequency Frequency { get; }

		// List of files that were included by this shader.
		IEnumerable<string> IncludedFiles { get; }
	}

	class Shader : IShader
	{
		public Shader(Device device, string filename, string entryPoint, string profile,
			Func<string, string> includeLookup, ShaderMacro[] defines)
		{
			try
			{
				var includeHandler = includeLookup != null ? new IncludeLookup(includeLookup) : null;

				// Compile the shader to bytecode.
				using (var bytecode = ShaderBytecode.CompileFromFile(filename, entryPoint,
					profile, ShaderFlags.None, EffectFlags.None, defines, includeHandler))
				{
					IncludedFiles = includeHandler != null ? includeHandler.IncludedFiles : Enumerable.Empty<string>();

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
						_cbuffers = (from i in Enumerable.Range(0, reflection.Description.ConstantBuffers)
									select new ConstantBuffer(device, reflection.GetConstantBuffer(i))
								   ).ToArray();
						_cbuffers_buffers = (from cbuffer in _cbuffers select cbuffer.Buffer).ToArray();

						// Gather resource and sampler inputs.
						var boundResources = Enumerable.Range(0, reflection.Description.BoundResources)
							.Select(i => reflection.GetResourceBindingDescription(i));
						_resourceVariables = boundResources
							.Where(desc => desc.Type == ShaderInputType.Texture)		// TODO: Support more types.
							.Select(desc => new ShaderResourceVariable(desc, Frequency))
							.ToArray();
						_samplerVariables = boundResources
							.Where(desc => desc.Type == ShaderInputType.Sampler)
							.Select(desc => new ShaderSamplerVariable(desc, Frequency))
							.ToArray();
					}
				}
			}
			catch (CompilationException ex)
			{
				// The shader compiler error messages contain the name used to
				// include the file, rather than the full path, so we convert them back
				// with some regex fun.

				var filenameRegex = new Regex(@"^(.*)(\([0-9]+,[0-9]+\))", RegexOptions.Multiline);

				MatchEvaluator replacer = match =>
				{
					// If the filename is the original input filename, use that,
					// otherwise run it through the include lookup function again.
					var file = match.Groups[1].Value;
					var path = file == filename ? file : includeLookup(file);

					// Add back the line an column numbers.
					return path + match.Groups[2];
				};

				var message = filenameRegex.Replace(ex.Message, replacer);

				OutputLogger.Instance.Log(LogCategory.ShaderCompile, message);
				throw new ScriptException("Shader compilation failed. See Shader Compilation log for details.", ex);
			}
		}

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
				context.VertexShader.SetConstantBuffers(_cbuffers_buffers, 0, _cbuffers_buffers.Length);
			}
			else if (_pixelShader != null)
			{
				context.PixelShader.Set(_pixelShader);
				context.PixelShader.SetConstantBuffers(_cbuffers_buffers, 0, _cbuffers_buffers.Length);
			}
			else if (_computeShader != null)
			{
				context.ComputeShader.Set(_computeShader);
				context.ComputeShader.SetConstantBuffers(_cbuffers_buffers, 0, _cbuffers_buffers.Length);
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

			// Do the same for resource and sampler variables.
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
		public IEnumerable<string> IncludedFiles { get; }

		// Constant buffer info.
		private ConstantBuffer[] _cbuffers;
		private Buffer[] _cbuffers_buffers;

		// Resource variable info.
		private ShaderResourceVariable[] _resourceVariables;

		// Sampler input info.
		private ShaderSamplerVariable[] _samplerVariables;

		// Class for handling include file lookups.
		private class IncludeLookup : Include
		{
			private Func<string, string> includeLookup;

			private List<string> _includedFiles = new List<string>();
			public IEnumerable<string> IncludedFiles => _includedFiles;

			public IncludeLookup(Func<string, string> includeLookup)
			{
				this.includeLookup = includeLookup;
			}

			// Include interface.
			public void Open(IncludeType type, string filename, Stream parentStream, out Stream stream)
			{
				// Find full path.
				var path = includeLookup(filename);

				// Open file stream.
				stream = new FileStream(path, FileMode.Open);

				// Remember that we included this file.
				_includedFiles.Add(path);
			}

			public void Close(Stream stream)
			{
				stream.Dispose();
			}
		}
	}
}
