using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPScripting
{
	public interface IRenderContext
	{
		// Draw the full scene.
		void DrawScene(
			dynamic vertexShaderIndex,
			dynamic pixelShaderIndex,
			RastState rastState = null,
			DepthStencilState depthStencilState = null,
			BlendState blendState = null,
			IEnumerable<object> renderTargets = null,
			object depthBuffer = null,
			IDictionary<string, dynamic> shaderVariableOverrides = null);

		// Draw a shaded sphere.
		void DrawSphere(
			dynamic vertexShaderIndex,
			dynamic pixelShaderIndex,
			RastState rastState = null,
			DepthStencilState depthStencilState = null,
			BlendState blendState = null,
			IEnumerable<object> renderTargets = null,
			object depthBuffer = null,
			IDictionary<string, dynamic> shaderVariableOverrides = null);

		// Draw a fullscreen quad.
		void DrawFullscreenQuad(
			dynamic vertexShaderIndex,
			dynamic pixelShaderIndex,
			IEnumerable<object> renderTargets = null,
			IDictionary<string, dynamic> shaderVariableOverrides = null);

		// Dispatch a compute shader.
		void Dispatch(dynamic shader, int numGroupsX, int numGroupsY, int numGroupsZ,
			IDictionary<string, dynamic> shaderVariableOverrides = null);

		// Clear render targets.
		void Clear(dynamic colour, IEnumerable<object> renderTargets = null);

		// Draw a wireframe sphere.
		void DrawWireSphere(
			dynamic position,
			float radius,
			dynamic colour,
			object renderTarget = null);
	}
}
