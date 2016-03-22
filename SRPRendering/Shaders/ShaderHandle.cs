using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D;
using SRPCommon.Util;
using SRPScripting.Shader;

namespace SRPRendering.Shaders
{
	// Dummy handle object to give to script in lieu of an actual compiled shader object,
	// since shader compilation is deferred.
	class ShaderHandle : IShader
	{
		#region IShader implementation

		public IShaderConstantVariable FindConstantVariable(string name) =>
			_constantVariables.GetOrAdd(name, () => new ShaderConstantVariableHandle(name));

		public IShaderResourceVariable FindResourceVariable(string name) =>
			_resourceVariables.GetOrAdd(name, () => new ShaderResourceVariableHandle(name));

		public IShaderSamplerVariable FindSamplerVariable(string name) =>
			_samplerVariables.GetOrAdd(name, () => new ShaderSamplerVariableHandle(name));

		public IShaderUavVariable FindUavVariable(string name) =>
			_uavVariables.GetOrAdd(name, () => new ShaderUavVariableHandle(name));

		#endregion

		// Get access to the actual shader to render with.
		public Shader Shader
		{
			get
			{
				// Must have been compiled by now.
				Trace.Assert(_shader != null);
				return _shader;
			}
		}

		public ShaderHandle(string filename, string entryPoint, string profile, Func<string, string> includeLookup, ShaderMacro[] defines)
		{
			_filename = filename;
			_entryPoint = entryPoint;
			_profile = profile;
			_includeLookup = includeLookup;
			_defines = defines;
		}

		// Actually compile the shader code to produce a real Shader object.
		public Shader Compile(IShaderCache shaderCache)
		{
			_shader = shaderCache.GetShader(_filename, _entryPoint, _profile, _includeLookup, _defines);

			// Hook up variable bindings.
			// For constant variables, we must iterate over all those in the shader,
			// not just those that have been bound, as we need to set up auto-bindings too.
			foreach (var variable in _shader.ConstantVariables)
			{
				var varHandle = _constantVariables.GetOrDefault(variable.Name);
				if (varHandle != null)
				{
					variable.Binding = varHandle.Binding;
				}
				else
				{
					variable.AutoBind();
				}
			}

			// For all the other variable types, we don't have to worry about auto-binding so
			// we just iterate over all the bindings that the script set up and transfer them to the real variables.

			foreach (var kvp in _constantVariables)
			{
				var variable = _shader.FindConstantVariable(kvp.Key);
				if (variable != null)
				{
					variable.Binding = kvp.Value.Binding;
				}
			}

			foreach (var kvp in _resourceVariables)
			{
				var variable = _shader.FindResourceVariable(kvp.Key);
				if (variable != null)
				{
					variable.Binding = kvp.Value.Binding;
				}
			}

			foreach (var kvp in _samplerVariables)
			{
				var variable = _shader.FindSamplerVariable(kvp.Key);
				if (variable != null)
				{
					variable.State = kvp.Value.State;
				}
			}

			foreach (var kvp in _uavVariables)
			{
				var variable = _shader.FindUavVariable(kvp.Key);
				if (variable != null)
				{
					variable.UAV = kvp.Value.UAV;
				}
			}

			return _shader;
		}

		// Data we need to compile.
		private string _filename;
		private string _entryPoint;
		private string _profile;
		private Func<string, string> _includeLookup;
		private ShaderMacro[] _defines;

		// The actual compiled shader.
		private Shader _shader;

		// Variable handle caches (don't want to end up with multiple handles for the same variable).
		private Dictionary<string, ShaderConstantVariableHandle> _constantVariables = new Dictionary<string, ShaderConstantVariableHandle>();
		private Dictionary<string, ShaderResourceVariableHandle> _resourceVariables = new Dictionary<string, ShaderResourceVariableHandle>();
		private Dictionary<string, ShaderSamplerVariableHandle> _samplerVariables = new Dictionary<string, ShaderSamplerVariableHandle>();
		private Dictionary<string, ShaderUavVariableHandle> _uavVariables = new Dictionary<string, ShaderUavVariableHandle>();
	}
}
