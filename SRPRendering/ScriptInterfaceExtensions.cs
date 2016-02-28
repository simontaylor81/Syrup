using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;

namespace SRPRendering
{
	// Extension methods for the various script interface types.
	static class ScriptInterfaceExtensions
	{
		// Convert a script-interface rasterizer state to a D3D11 one.
		public static RasterizerStateDescription ToD3D11(this SRPScripting.RastState state)
		{
			// Allow transparent handling of null references.
			if (state == null)
				return SRPScripting.RastState.Default.ToD3D11();

			return new RasterizerStateDescription()
				{
					FillMode = state.fillMode.ToD3D11(),
					CullMode = state.cullMode.ToD3D11(),
					IsFrontCounterClockwise = false,
					DepthBias = state.depthBias,
					DepthBiasClamp = state.depthBiasClamp,
					SlopeScaledDepthBias = state.slopeScaleDepthBias,
					IsScissorEnabled = state.enableScissor,
					IsDepthClipEnabled = state.enableDepthClip,
					IsMultisampleEnabled = false,
					IsAntialiasedLineEnabled = false
				};
		}

		// Convert a script-interface depth-stencil state to a D3D11 one.
		public static DepthStencilStateDescription ToD3D11(this SRPScripting.DepthStencilState state)
		{
			// Allow transparent handling of null references.
			if (state == null)
				// Default to depth enabled.
				return SRPScripting.DepthStencilState.EnableDepth.ToD3D11();

			return new DepthStencilStateDescription()
				{
					IsDepthEnabled = state.enableDepthTest,
					DepthWriteMask = state.enableDepthWrite ? DepthWriteMask.All : DepthWriteMask.Zero,
					DepthComparison = state.depthFunc.ToD3D11()
				};
		}

		// Convert a script-interface blend state to a D3D11 one.
		public static BlendStateDescription ToD3D11(this SRPScripting.BlendState state)
		{
			// Allow transparent handling of null references.
			if (state == null)
				return SRPScripting.BlendState.NoBlending.ToD3D11();

			var rtState = new RenderTargetBlendDescription()
				{
					IsBlendEnabled = state.enableBlending,
					SourceBlend = state.sourceInput.ToD3D11(),
					DestinationBlend = state.destInput.ToD3D11(),
					SourceAlphaBlend = state.sourceAlphaInput.ToD3D11(),
					DestinationAlphaBlend = state.destAlphaInput.ToD3D11(),
					BlendOperation = state.colourOp.ToD3D11(),
					AlphaBlendOperation = state.alphaOp.ToD3D11(),
					RenderTargetWriteMask = ColorWriteMaskFlags.All,
				};

			var desc = new BlendStateDescription()
				{
					IndependentBlendEnable = false,
					AlphaToCoverageEnable = false,
				};
			desc.RenderTarget[0] = rtState;

			return desc;
		}

		// Convert a script-interface sampler state to a D3D11 one.
		public static SamplerStateDescription ToD3D11(this SRPScripting.SamplerState state)
		{
			// Allow transparent handling of null references.
			if (state == null)
				return SRPScripting.SamplerState.LinearWrap.ToD3D11();

			return new SamplerStateDescription()
			{
				Filter = state.filter.ToD3D11(),
				AddressU = state.addressMode.ToD3D11(),
				AddressV = state.addressMode.ToD3D11(),
				AddressW = state.addressMode.ToD3D11(),
				MipLodBias = 0,
				MaximumAnisotropy = 8,
				MinimumLod = 0,
				MaximumLod = float.MaxValue,
			};
		}

		public static SharpDX.Direct3D11.FillMode ToD3D11(this SRPScripting.FillMode fillMode)
		{
			switch (fillMode)
			{
				case SRPScripting.FillMode.Solid:
					return SharpDX.Direct3D11.FillMode.Solid;

				case SRPScripting.FillMode.Wireframe:
					return SharpDX.Direct3D11.FillMode.Wireframe;

				default:
					throw new ArgumentException("Invalid fill mode.");
			}
		}

		public static SharpDX.Direct3D11.CullMode ToD3D11(this SRPScripting.CullMode cullMode)
		{
			switch (cullMode)
			{
				case SRPScripting.CullMode.Back:
					return SharpDX.Direct3D11.CullMode.Back;

				case SRPScripting.CullMode.Front:
					return SharpDX.Direct3D11.CullMode.Front;

				case SRPScripting.CullMode.None:
					return SharpDX.Direct3D11.CullMode.None;

				default:
					throw new ArgumentException("Invalid cull mode.");
			}
		}

		public static Comparison ToD3D11(this SRPScripting.ComparisonFunction func)
		{
			switch (func)
			{
				case SRPScripting.ComparisonFunction.Never: return Comparison.Never;
				case SRPScripting.ComparisonFunction.Always: return Comparison.Always;
				case SRPScripting.ComparisonFunction.Equal: return Comparison.Equal;
				case SRPScripting.ComparisonFunction.NotEqual: return Comparison.NotEqual;
				case SRPScripting.ComparisonFunction.Less: return Comparison.Less;
				case SRPScripting.ComparisonFunction.LessEqual: return Comparison.LessEqual;
				case SRPScripting.ComparisonFunction.Greater: return Comparison.Greater;
				case SRPScripting.ComparisonFunction.GreaterEqual: return Comparison.GreaterEqual;

				default:
					throw new ArgumentException("Invalid comparison function.");
			}
		}

		public static BlendOption ToD3D11(this SRPScripting.BlendInput blendInput)
		{
			switch (blendInput)
			{
				case SRPScripting.BlendInput.Zero: return BlendOption.Zero;
				case SRPScripting.BlendInput.One: return BlendOption.One;
				case SRPScripting.BlendInput.SourceColor: return BlendOption.SourceColor;
				case SRPScripting.BlendInput.InvSourceColor: return BlendOption.InverseSourceColor;
				case SRPScripting.BlendInput.SourceAlpha: return BlendOption.SourceAlpha;
				case SRPScripting.BlendInput.InvSourceAlpha: return BlendOption.InverseSourceAlpha;
				case SRPScripting.BlendInput.DestColor: return BlendOption.DestinationColor;
				case SRPScripting.BlendInput.InvDestColor: return BlendOption.InverseDestinationColor;
				case SRPScripting.BlendInput.DestAlpha: return BlendOption.DestinationAlpha;
				case SRPScripting.BlendInput.InvDestAlpha: return BlendOption.InverseDestinationAlpha;
				case SRPScripting.BlendInput.SourceAlphaSat: return BlendOption.SourceAlphaSaturate;
				case SRPScripting.BlendInput.BlendFactor: return BlendOption.BlendFactor;
				case SRPScripting.BlendInput.InvBlendFactor: return BlendOption.InverseBlendFactor;
				case SRPScripting.BlendInput.Source1Color: return BlendOption.SecondarySourceColor;
				case SRPScripting.BlendInput.InvSource1Color: return BlendOption.InverseSecondarySourceColor;
				case SRPScripting.BlendInput.Source1Alpha: return BlendOption.SecondarySourceAlpha;
				case SRPScripting.BlendInput.InvSource1Alpha: return BlendOption.InverseSecondarySourceAlpha;

				default:
					throw new ArgumentException("Invalid blend input.");
			}
		}

		public static BlendOperation ToD3D11(this SRPScripting.BlendOp blendOp)
		{
			switch (blendOp)
			{
				case SRPScripting.BlendOp.Add: return BlendOperation.Add;
				case SRPScripting.BlendOp.Subtract: return BlendOperation.Subtract;
				case SRPScripting.BlendOp.ReverseSubtract: return BlendOperation.ReverseSubtract;
				case SRPScripting.BlendOp.Min: return BlendOperation.Minimum;
				case SRPScripting.BlendOp.Max: return BlendOperation.Maximum;

				default:
					throw new ArgumentException("Invalid blend operation.");
			}
		}

		public static Filter ToD3D11(this SRPScripting.TextureFilter filter)
		{
			switch (filter)
			{
				case SRPScripting.TextureFilter.Point: return Filter.MinMagMipPoint;
				case SRPScripting.TextureFilter.Linear: return Filter.MinMagMipLinear;
				case SRPScripting.TextureFilter.Anisotropic: return Filter.Anisotropic;

				default:
					throw new ArgumentException("Invalid texture filter.");
			}
		}

		public static TextureAddressMode ToD3D11(this SRPScripting.TextureAddressMode mode)
		{
			switch (mode)
			{
				case SRPScripting.TextureAddressMode.Wrap: return TextureAddressMode.Wrap;
				case SRPScripting.TextureAddressMode.Clamp: return TextureAddressMode.Clamp;
				case SRPScripting.TextureAddressMode.Mirror: return TextureAddressMode.Mirror;

				default:
					throw new ArgumentException("Invalid texture addressing mode.");
			}
		}

		// Convert a script format to a DXGI one.
		public static SharpDX.DXGI.Format ToDXGI(this SRPScripting.Format format)
		{
			// This is rather dirty -- the formats are just copies of the SharpDX ones, currently.
			SharpDX.DXGI.Format result;
			if (Enum.TryParse(format.ToString(), out result))
			{
				return result;
			}

			throw new ArgumentException("Invalid DXGI format: " + format.ToString());
		}
	}
}
