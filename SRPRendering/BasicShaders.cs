using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;
using SRPScripting;
using SRPCommon.Util;

namespace SRPRendering
{
	public interface IBasicShaders : IDisposable
	{
		/// <summary>
		/// Simple vertex shader for rendering the scene.
		/// </summary>
		IShader BasicSceneVS { get; }

		/// <summary>
		/// A simple pixel shader that simply outputs a constant colour.
		/// </summary>
		IShader SolidColourPS { get; }

		/// <summary>
		/// Shader variable for setting the solid colour to use when rendering with the solid colour pixel shader.
		/// </summary>
		IShaderVariable SolidColourShaderVar { get; }
	}

	// Class for managing basic application (i.e. not script) controlled shaders for basic rendering functionality.
	class BasicShaders : IBasicShaders
	{
		/// <summary>
		/// Simple vertex shader for rendering the scene.
		/// </summary>
		public IShader BasicSceneVS { get; }

		/// <summary>
		/// A simple pixel shader that simply outputs a constant colour.
		/// </summary>
		public IShader SolidColourPS { get; }

		/// <summary>
		/// Shader variable for setting the solid colour to use when rendering with the solid colour pixel shader.
		/// </summary>
		public IShaderVariable SolidColourShaderVar { get; }


		// List of shader that need to be disposed.
		private List<IDisposable> disposables = new List<IDisposable>();

		// Constructor.
		public BasicShaders(Device device)
		{
			var filename = RenderUtils.GetShaderFilename("BasicShaders.hlsl");

			// Compile basic scene vertex shader.
			BasicSceneVS = Shader.CompileFromFile(device, filename, "BasicSceneVS", "vs_4_0", null, null);
			disposables.Add(BasicSceneVS);

			// Bind the required shader variables.
			BindShaderVariable(BasicSceneVS, "LocalToWorldMatrix", ShaderVariableBindSource.LocalToWorldMatrix);
			BindShaderVariable(BasicSceneVS, "WorldToProjectionMatrix", ShaderVariableBindSource.WorldToProjectionMatrix);

			// Compile the solid colour pixel shader.
			SolidColourPS = Shader.CompileFromFile(device, filename, "SolidColourPS", "ps_4_0", null, null);
			disposables.Add(SolidColourPS);

			// Cache reference to the solid colour variable.
			SolidColourShaderVar = SolidColourPS.FindVariable("SolidColour");
			if (SolidColourShaderVar == null)
				throw new Exception("Could not find SolidColour variable for solid colour pixel shader.");
		}

		// IDisposable interface.
		public void Dispose()
		{
			DisposableUtil.DisposeList(disposables);
		}

		// Bind a shader variable unconditionally.
		private void BindShaderVariable(IShader shader, string variableName, ShaderVariableBindSource source)
		{
			var variable = shader.FindVariable(variableName);
			if (variable == null)
				throw new Exception("Failed to find shader variable for basic shader: " + variableName);

			variable.Bind = new SimpleShaderVariableBind(variable, source);
		}
	}
}
