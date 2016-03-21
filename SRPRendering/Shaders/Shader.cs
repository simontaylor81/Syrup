using System;
using System.Collections.Generic;
using System.Linq;

using SRPCommon.Util;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using SRPScripting.Shader;
using System.Diagnostics;

namespace SRPRendering.Shaders
{
	public struct IncludedFile
	{
		public string SourceName;		// Original name from the #include line.
		public string ResolvedFile;		// File path that it was resolved to.
	}

	class Shader : IShader, IDisposable
	{
		public Shader(Device device, string profile, IEnumerable<IncludedFile> includedFiles, ShaderBytecode bytecode)
		{
			Trace.Assert(device != null);
			Trace.Assert(profile != null);
			Trace.Assert(bytecode != null);
			Trace.Assert(includedFiles != null);

			IncludedFiles = includedFiles;

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
		public void UpdateVariables(DeviceContext context, ViewInfo viewInfo, IPrimitive primitive, IDictionary<string, object> overrides, IGlobalResources globalResources)
		{
			// First, update the value of bound and overridden variables.
			foreach (var variable in ConstantVariables)
			{
				// Is the variable bound?
				if (variable.Binding != null)
				{
					variable.Binding.UpdateVariable(viewInfo, primitive, overrides);
				}

				// Warn if the user is attempting to override the value, but the variable has not been
				// set as overridable.
				if (overrides != null &&
					overrides.ContainsKey(variable.Name) &&
					(variable.Binding == null || !variable.Binding.AllowScriptOverride))
				{
					OutputLogger.Instance.LogLineOnce(LogCategory.Script,
						"Warning: attempt to override shader variable {0} which has not been marked as overridable. Call MarkAsScriptOverride to mark it thus.",
						variable.Name);
				}
			}

			// Next, do the actual upload the constant buffers.
			foreach (var cubffer in _cbuffers)
				cubffer.Update(context);

			// Update resource variables too.
			foreach (var resourceVariable in _resourceVariables)
			{
				resourceVariable.SetToDevice(context, primitive, viewInfo, globalResources);
			}

			// And samplers.
			foreach (var samplerVariable in _samplerVariables)
			{
				samplerVariable.SetToDevice(context, globalResources);
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
			foreach (var variable in ConstantVariables)
			{
				// Restore initial value if the variable was bound.
				// If it wasn't, we keep the original value so the user doesn't lose their settings.
				if (variable.Binding != null)
				{
					// Restore default value.
					variable.Reset();
				}
			}

			// Do the same for resource, sampler and UAV variables.
			foreach (var resourceVariable in _resourceVariables)
			{
				resourceVariable.Reset();
			}

			foreach (var samplerVariable in _samplerVariables)
			{
				samplerVariable.Reset();
			}

			foreach (var uavVariable in _uavVariables)
			{
				uavVariable.Reset();
			}
		}

		// Get all variables from all cbuffers.
		public IEnumerable<ShaderConstantVariable> ConstantVariables => _cbuffers.SelectMany(cbuffer => cbuffer.Variables);

		// Find a variable by name.
		public IShaderConstantVariable FindConstantVariable(string name)
			=> (IShaderConstantVariable)ConstantVariables.FirstOrDefault(v => v.Name == name) ?? new NullShaderConstantVariable(name);

		// Find a resource variable by name.
		public IShaderResourceVariable FindResourceVariable(string name)
			=> (IShaderResourceVariable)_resourceVariables.FirstOrDefault(v => v.Name == name) ?? new NullShaderResourceVariable(name);

		// Find a sampler variable by name.
		public IShaderSamplerVariable FindSamplerVariable(string name)
			=> (IShaderSamplerVariable)_samplerVariables.FirstOrDefault(v => v.Name == name) ?? new NullShaderSamplerVariable(name);

		// Find a UAV variable by name.
		public IShaderUavVariable FindUavVariable(string name)
			=> (IShaderUavVariable)_uavVariables.FirstOrDefault(v => v.Name == name) ?? new NullShaderUavVariable(name);

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
	}
}
