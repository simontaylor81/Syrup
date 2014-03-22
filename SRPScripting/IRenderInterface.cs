﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPScripting
{
	public enum ShaderVariableBindSource
	{
		WorldToProjectionMatrix,	// Bind to the combined world to projection space matrix.
		ProjectionToWorldMatrix,	// Bind to the combined projection to world space matrix (i.e. the inverse of the WorldToProjectionMatrix).
		LocalToWorldMatrix,			// Bind to the object-local to world space matrix.
		WorldToLocalMatrix,			// Bind to the world to object-local space matrix (i.e. the inverse of the local to world matrix).
		CameraPosition,				// Bind to the position of the camera in world-space.
	}

	public enum UserVariableType
	{
		Float,		// Scalar floating point value.
		Float2,		// Floating point vector types.
		Float3,
		Float4,
		Bool,		// Boolean value.
		//TODO
	}

	// Delegate type for the per-frame callback. Cannot be inside the interface cos C# is silly.
	public delegate void FrameCallback(IRenderContext context);

	// Interface to the rendering system exposed to the scripting system.
	public interface IRenderInterface
	{
		object LoadShader(string filename, string entryPoint, string profile);

		// Create a render target of dimensions equal to the viewport.
		object CreateRenderTarget();

		void BindShaderVariable(dynamic shader, string var, ShaderVariableBindSource source);
		void BindShaderVariableToMaterial(dynamic shader, string var, string param);
		void SetShaderVariable(dynamic shader, string var, dynamic value);
		void ShaderVariableIsScriptOverride(dynamic shader, string var);

		void BindShaderResourceToMaterial(dynamic shader, string var, string param);
		void SetShaderResourceVariable(dynamic shader, string var, object value);

		dynamic AddUserVar(string name, UserVariableType type, dynamic defaultValue);

		// Still unsure if this is the best way to go.
		void SetFrameCallback(FrameCallback callback);

		// Handles to special resources.
		//object BackBuffer { get; }
		object DepthBuffer { get; }
		object NoDepthBuffer { get; }
	}
}
