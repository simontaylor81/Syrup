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

namespace SRPRendering
{
	enum ShaderFrequency
	{
		Vertex,
		Pixel,
		Compute,
		MAX
	}

	class Shader : IDisposable
	{
		public Shader(Device device, string filename, string entryPoint, string profile, Func<string, string> includeLookup)
		{
			try
			{
				var includeHandler = includeLookup != null ? new IncludeLookup(includeLookup) : null;

				// Compile the shader to bytecode.
				using (var bytecode = ShaderBytecode.CompileFromFile(filename, entryPoint,
					profile, ShaderFlags.None, EffectFlags.None, null, includeHandler))
				{
					IncludedFiles = includeHandler != null ? includeHandler.IncludedFiles : Enumerable.Empty<string>();

					// Create the shader object of the appropriate type.
					switch (profile.Substring(0, 2))
					{
						case "vs":
							vertexShader = new VertexShader(device, bytecode);
							Signature = ShaderSignature.GetInputSignature(bytecode);
							frequency = ShaderFrequency.Vertex;
							break;

						case "ps":
							pixelShader = new PixelShader(device, bytecode);
							frequency = ShaderFrequency.Pixel;
							break;

						case "cs":
							computeShader = new ComputeShader(device, bytecode);
							frequency = ShaderFrequency.Compute;
							break;

						default:
							throw new Exception("Unsupported shader profile: " + profile);
					}

					// Get info about the shader's inputs.
					using (var reflection = new ShaderReflection(bytecode))
					{
						// Gether constant buffers.
						cbuffers = (from i in Enumerable.Range(0, reflection.Description.ConstantBuffers)
									select new ConstantBuffer(device, reflection.GetConstantBuffer(i))
								   ).ToArray();
						cbuffers_buffers = (from cbuffer in cbuffers select cbuffer.Buffer).ToArray();

						// Gather resource inputs.
						resourceVariables = (from i in Enumerable.Range(0, reflection.Description.BoundResources)
											 let desc = reflection.GetResourceBindingDescription(i)
											 where desc.Type == ShaderInputType.Texture		// TODO: Support more types.
											 select new ShaderResourceVariable(desc, Frequency)
											).ToArray();
					}
				}
			}
			catch (CompilationException ex)
			{
				OutputLogger.Instance.Log(LogCategory.ShaderCompile, ex.Message);
				throw new ScriptException("Shader compilation failed.", ex);
			}
		}

		public void Dispose()
		{
			foreach (var cbuffer in cbuffers)
				cbuffer.Dispose();

			DisposableUtil.SafeDispose(vertexShader);
			DisposableUtil.SafeDispose(pixelShader);
			DisposableUtil.SafeDispose(computeShader);
			DisposableUtil.SafeDispose(Signature);
		}


		// Bind the shader to the device.
		public void Set(DeviceContext context)
		{
			if (vertexShader != null)
			{
				context.VertexShader.Set(vertexShader);
				context.VertexShader.SetConstantBuffers(cbuffers_buffers, 0, cbuffers_buffers.Length);
			}
			else if (pixelShader != null)
			{
				context.PixelShader.Set(pixelShader);
				context.PixelShader.SetConstantBuffers(cbuffers_buffers, 0, cbuffers_buffers.Length);
			}
			else if (computeShader != null)
			{
				context.ComputeShader.Set(computeShader);
				context.ComputeShader.SetConstantBuffers(cbuffers_buffers, 0, cbuffers_buffers.Length);
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
			foreach (var cubffer in cbuffers)
				cubffer.Update(context);

			// Update resource variables too.
			foreach (var resourceVariable in resourceVariables)
			{
				if (resourceVariable.Bind != null)
				{
					resourceVariable.Bind.Set(primitive, viewInfo, resourceVariable, globalResources);
				}

				resourceVariable.SetToDevice(context);
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

			// Do the same for resource variables.
			foreach (var resourceVariable in resourceVariables)
			{
				if (resourceVariable.Bind != null)
				{
					resourceVariable.Resource = null;
					resourceVariable.Bind = null;
				}
			}
		}

		public IEnumerable<IShaderVariable> Variables
		{
			get
			{
				// Get all variables from all cbuffers.
				return from cbuffer in cbuffers from variable in cbuffer.Variables select variable;
			}
		}

		// Find a variable by name.
		public IShaderVariable FindVariable(string name)
		{
			return Variables.FirstOrDefault(v => v.Name == name);
		}

		// Find a resource variable by name.
		public IShaderResourceVariable FindResourceVariable(string name)
		{
			return resourceVariables.FirstOrDefault(v => v.Name == name);
		}

		// Actual shader. Only one of these is non-null.
		private VertexShader vertexShader;
		private PixelShader pixelShader;
		private ComputeShader computeShader;

		// Input signature. Vertex shader only.
		public ShaderSignature Signature { get; private set; }

		// Frequency (i.e. type) of shader.
		private readonly ShaderFrequency frequency;
		public ShaderFrequency Frequency { get { return frequency; } }

		// List of files that were included by this shader.
		public IEnumerable<string> IncludedFiles { get; private set; }

		// Constant buffer info.
		private ConstantBuffer[] cbuffers;
		private Buffer[] cbuffers_buffers;

		// Resource variable info.
		private ShaderResourceVariable[] resourceVariables;

		// Class for handling include file lookups.
		private class IncludeLookup : Include
		{
			private Func<string, string> includeLookup;

			private List<string> _includedFiles = new List<string>();
			public IEnumerable<string> IncludedFiles { get { return _includedFiles; } }

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

	class ConstantBuffer : IDisposable
	{
		public ConstantBuffer(Device device, SlimDX.D3DCompiler.ConstantBuffer bufferInfo)
		{
			// Gather info about the variables in this buffer.
			variables = (from i in Enumerable.Range(0, bufferInfo.Description.Variables)
						 select new ShaderVariable(bufferInfo.GetVariable(i))).ToArray();

			// Create a data stream containing the initial contents buffer.
			var stream = new DataStream(bufferInfo.Description.Size, true, true);
			contents = new DataBox(bufferInfo.Description.Size, bufferInfo.Description.Size, stream);

			// Write initial values to buffer.
			foreach (var variable in variables)
				variable.WriteToBuffer(stream);

			// Create the actual buffer.
			stream.Position = 0;
			Buffer = new Buffer(
				device,
				stream,
				bufferInfo.Description.Size,
				ResourceUsage.Default,
				BindFlags.ConstantBuffer,
				CpuAccessFlags.None,
				ResourceOptionFlags.None,
				0);

		}

		public void Dispose()
		{
			Buffer.Dispose();
		}

		// Upload the constants to the buffer if dirty.
		public void Update(DeviceContext context)
		{
			bool bDirty = false;
			foreach (var variable in variables)
				bDirty |= variable.WriteToBuffer(contents.Data);

			if (bDirty)
			{
				contents.Data.Position = 0;
				context.UpdateSubresource(contents, Buffer, 0);
			}
		}

		public Buffer Buffer { get; private set; }

		public IEnumerable<IShaderVariable> Variables
		{
			get { return variables; }
		}

		private ShaderVariable[] variables;
		private DataBox contents;
	}
}
