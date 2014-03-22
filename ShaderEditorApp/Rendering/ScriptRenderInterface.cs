﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRPScripting;

namespace ShaderEditorApp.Rendering
{
	// This is the class that we give to the script that implements SRPScripting.IRenderInterface.
	// It has to be kept separate from the main control logic, otherwise python can access the guts of the render system.
	// It basically just calls back to the ScriptRenderControl that created it.
	public class ScriptRenderInterface : IRenderInterface
	{
		public object LoadShader(string filename, string entryPoint, string profile)
		{
			return src.LoadShader(filename, entryPoint, profile);
		}

		public object CreateRenderTarget()
		{
			return src.CreateRenderTarget();
		}

		public void BindShaderVariable(dynamic shader, string var, ShaderVariableBindSource source)
		{
			src.BindShaderVariable(shader, var, source);
		}
		public void BindShaderVariableToMaterial(dynamic shader, string var, string param)
		{
			src.BindShaderVariableToMaterial(shader, var, param);
		}
		public void SetShaderVariable(dynamic shader, string var, dynamic value)
		{
			src.SetShaderVariable(shader, var, value);
		}
		public void ShaderVariableIsScriptOverride(dynamic shader, string var)
		{
			src.ShaderVariableIsScriptOverride(shader, var);
		}

		public void BindShaderResourceToMaterial(dynamic shader, string var, string param)
		{
			src.BindShaderResourceToMaterial(shader, var, param);
		}
		public void SetShaderResourceVariable(dynamic shader, string var, object value)
		{
			src.SetShaderResourceVariable(shader, var, value);
		}

		public dynamic AddUserVar(string name, UserVariableType type, dynamic defaultValue)
		{
			return src.AddUserVar(name, type, defaultValue);
		}

		public void SetFrameCallback(FrameCallback callback)
		{
			src.SetFrameCallback(callback);
		}

		// Handles to special resources.
		//public object BackBuffer { get { return RenderTargetHandle.BackBuffer; } }
		public object DepthBuffer { get { return DepthBufferHandle.Default; } }
		public object NoDepthBuffer { get { return DepthBufferHandle.NoDepthBuffer; } }

		// Internal constructor, so python can't create new instances.
		internal ScriptRenderInterface(ScriptRenderControl src)
		{
			this.src = src;
		}

		// Pointer back to the class that does the actual work.
		private ScriptRenderControl src;
	}
}
