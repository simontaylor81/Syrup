using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRPScripting.Shader;

namespace SRPScripting
{
	public interface IRenderContext
	{
		// Draw the full scene.
		void DrawScene(
			IShader vertexShaderIndex,
			IShader pixelShaderIndex,
			RastState rastState = null,
			DepthStencilState depthStencilState = null,
			BlendState blendState = null,
			IEnumerable<IRenderTarget> renderTargets = null,
			IDepthBuffer depthBuffer = null,
			IDictionary<string, object> shaderVariableOverrides = null);

		// Draw a shaded sphere.
		void DrawSphere(
			IShader vertexShaderIndex,
			IShader pixelShaderIndex,
			RastState rastState = null,
			DepthStencilState depthStencilState = null,
			BlendState blendState = null,
			IEnumerable<IRenderTarget> renderTargets = null,
			IDepthBuffer depthBuffer = null,
			IDictionary<string, object> shaderVariableOverrides = null);

		// Draw a fullscreen quad.
		void DrawFullscreenQuad(
			IShader vertexShaderIndex,
			IShader pixelShaderIndex,
			IEnumerable<IRenderTarget> renderTargets = null,
			IDictionary<string, object> shaderVariableOverrides = null);

		// Dispatch a compute shader.
		void Dispatch(IShader shader, int numGroupsX, int numGroupsY, int numGroupsZ,
			IDictionary<string, object> shaderVariableOverrides = null);

		// Indirect compute shader dispatch.
		void DispatchIndirect(IShader shader, IBuffer argBuffer, int argOffset,
			IDictionary<string, object> shaderVariableOverrides = null);

		// Clear render targets.
		void Clear(dynamic colour, IEnumerable<IRenderTarget> renderTargets = null);

		// Draw a wireframe sphere.
		void DrawWireSphere(
			dynamic position,
			float radius,
			dynamic colour,
			IRenderTarget renderTarget = null);
	}
}
