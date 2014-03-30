using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX;
using SlimDX.Direct3D11;
using SRPScripting;
using SRPCommon.Util;

namespace SRPRendering
{
	// Class for managing basic application (i.e. not script) controlled shaders for basic rendering functionality.
	class BasicShaders : IDisposable
	{
		/// <summary>
		/// Simple vertex shader for rendering the scene.
		/// </summary>
		public Shader BasicSceneVS { get; private set; }

		/// <summary>
		/// A simple pixel shader that simply outputs a constant colour.
		/// </summary>
		public Shader SolidColourPS { get; private set; }

		/// <summary>
		/// Solid colour to use when rendering with the solid colour pixel shader.
		/// </summary>
		public Color4 SolidColour
		{
			get { return solidColourVar.Get<Color4>(); }
			set { solidColourVar.Set(value); }
		}

		// Cached reference to the solid colour shader variable.
		private IShaderVariable solidColourVar;


		// List of shader that need to be disposed.
		private List<IDisposable> disposables = new List<IDisposable>();

		// Constructor.
		public BasicShaders(Device device)
		{
			var filename = RenderUtils.GetShaderFilename("BasicShaders.hlsl");

			// Compile basic scene vertex shader.
			BasicSceneVS = new Shader(device, filename, "BasicSceneVS", "vs_4_0", null); ;
			disposables.Add(BasicSceneVS);

			// Bind the required shader variables.
			BindShaderVariable(BasicSceneVS, "LocalToWorldMatrix", ShaderVariableBindSource.LocalToWorldMatrix);
			BindShaderVariable(BasicSceneVS, "WorldToProjectionMatrix", ShaderVariableBindSource.WorldToProjectionMatrix);

			// Compile the solid colour pixel shader.
			SolidColourPS = new Shader(device, filename, "SolidColourPS", "ps_4_0", null);
			disposables.Add(SolidColourPS);

			// Cache reference to the solid colour variable.
			solidColourVar = SolidColourPS.FindVariable("SolidColour");
			if (solidColourVar == null)
				throw new Exception("Could not find SolidColour variable for solid colour pixel shader.");
		}

		// IDisposable interface.
		public void Dispose()
		{
			DisposableUtil.DisposeList(disposables);
		}

		// Bind a shader variable unconditionally.
		private void BindShaderVariable(Shader shader, string variableName, ShaderVariableBindSource source)
		{
			var variable = shader.FindVariable(variableName);
			if (variable == null)
				throw new Exception("Failed to find shader variable for basic shader: " + variableName);

			variable.Bind = new SimpleShaderVariableBind(variable, source);
		}
	}
}
