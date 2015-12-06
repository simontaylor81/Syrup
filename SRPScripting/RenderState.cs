using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPScripting
{
	//----------------------------------------------------------------------------------------------
	// Rasterizer state types.

	// Fill mode to render with.
	public enum FillMode
	{
		Solid,
		Wireframe,
	}

	// Back-face culling mode.
	// Note: we don't expose CW vs CCW: all meshes should be CW.
	public enum CullMode
	{
		Back,
		Front,
		None,
	}

	// A comparison function. Mirrors D3D11_COMPARISON_FUNC.
	public enum ComparisonFunction
	{
		Never,
		Always,
		Equal,
		NotEqual,
		Less,
		LessEqual,
		Greater,
		GreaterEqual,
	}

	// Value to use as the input for blending. Mirrors D3D11_BLEND.
	public enum BlendInput
	{
		Zero,
		One,
		SourceColor,
		InvSourceColor,
		SourceAlpha,
		InvSourceAlpha,
		DestColor,
		InvDestColor,
		DestAlpha,
		InvDestAlpha,
		SourceAlphaSat,
		BlendFactor,
		InvBlendFactor,
		Source1Color,
		InvSource1Color,
		Source1Alpha,
		InvSource1Alpha,
	}

	// Blend operation. Mirrors D3D11_BLEND_OP.
	public enum	BlendOp
	{
		Add,
		Subtract,
		ReverseSubtract,
		Min,
		Max,
	}

	// Texture filtering modes.
	public enum TextureFilter
	{
		Point,
		Linear,
		Anisotropic,
	}

	// Texture addressing modes.
	public enum TextureAddressMode
	{
		Wrap,
		Clamp,
		Mirror,
	}

	// Class that collects the various rasterizer state settings.
	// Immutable to avoid confusion if you pass in a value then modify it before it's used internally.
	public class RastState
	{
		public RastState(FillMode fillMode = FillMode.Solid,
						 CullMode cullMode = CullMode.Back,
						 int depthBias = 0,
						 float slopeScaleDepthBias = 0.0f,
						 float depthBiasClamp = 0.0f,
						 bool enableScissor = false,
						 bool enableDepthClip = true)
		{
			this.fillMode = fillMode;
			this.cullMode = cullMode;
			this.depthBias = depthBias;
			this.slopeScaleDepthBias = slopeScaleDepthBias;
			this.depthBiasClamp = depthBiasClamp;
			this.enableScissor = enableScissor;
			this.enableDepthClip = enableDepthClip;
		}

		// Easy accessor for a default-valued object to avoid allocating a new one each time it's needed.
		private static RastState default_ = new RastState();
		public static RastState Default { get { return default_; } }

		public readonly FillMode fillMode;
		public readonly CullMode cullMode;
		public readonly int depthBias;
		public readonly float slopeScaleDepthBias;
		public readonly float depthBiasClamp;
		public readonly bool enableScissor;
		public readonly bool enableDepthClip;
	}

	// Class that collects the various depth & stencil state settings.
	// Immutable to avoid confusion if you pass in a value then modify it before it's used internally.
	public class DepthStencilState
	{
		public DepthStencilState(bool enableDepthTest = true,
								 bool enableDepthWrite = true,
								 ComparisonFunction depthFunc = ComparisonFunction.Less)
		{
			this.enableDepthTest = enableDepthTest;
			this.enableDepthWrite = enableDepthWrite;
			this.depthFunc = depthFunc;
		}

		// Easy accessors for some common use cases.
		public static DepthStencilState EnableDepth { get { return enableDepth_; } }
		public static DepthStencilState DisableDepth { get { return disableDepth_; } }
		public static DepthStencilState DisableDepthWrite { get { return disableDepthWrite_; } }
		public static DepthStencilState EqualDepth { get { return equalDepth_; } }

		private static DepthStencilState enableDepth_ = new DepthStencilState(true, true);
		private static DepthStencilState disableDepth_ = new DepthStencilState(false, false);
		private static DepthStencilState disableDepthWrite_ = new DepthStencilState(true, false);
		private static DepthStencilState equalDepth_ = new DepthStencilState(true, true, ComparisonFunction.Equal);

		public readonly bool enableDepthTest;
		public readonly bool enableDepthWrite;
		public readonly ComparisonFunction depthFunc;
	}

	// Class that collects the various blending state settings.
	// Currently don't support per-render target blend state.
	// Immutable to avoid confusion if you pass in a value then modify it before it's used internally.
	public class BlendState
	{
		public BlendState(bool enableBlending = false,
						  BlendInput sourceInput = BlendInput.One,
						  BlendInput destInput = BlendInput.Zero,
						  BlendInput sourceAlphaInput = BlendInput.One,
						  BlendInput destAlphaInput = BlendInput.Zero,
						  BlendOp colourOp = BlendOp.Add,
						  BlendOp alphaOp = BlendOp.Add)
		{
			this.enableBlending = enableBlending;
			this.sourceInput = sourceInput;
			this.destInput = destInput;
			this.sourceAlphaInput = sourceAlphaInput;
			this.destAlphaInput = destAlphaInput;
			this.colourOp = colourOp;
			this.alphaOp = alphaOp;
		}

		// Easy accessors for some common use cases.
		public static BlendState NoBlending { get { return noBlending_; } }
		public static BlendState AlphaBlending { get { return alphaBlending_; } }
		public static BlendState AdditiveBlending { get { return additiveBlending_; } }

		private static BlendState noBlending_ = new BlendState();
		private static BlendState alphaBlending_ = new BlendState(true, BlendInput.SourceAlpha, BlendInput.InvSourceAlpha, BlendInput.SourceAlpha, BlendInput.InvSourceAlpha, BlendOp.Add, BlendOp.Add);
		private static BlendState additiveBlending_ = new BlendState(true, BlendInput.One, BlendInput.One, BlendInput.One, BlendInput.One, BlendOp.Add, BlendOp.Add);

		public readonly bool enableBlending;
		public readonly BlendInput sourceInput;
		public readonly BlendInput destInput;
		public readonly BlendInput sourceAlphaInput;
		public readonly BlendInput destAlphaInput;
		public readonly BlendOp colourOp;
		public readonly BlendOp alphaOp;
	}

	// Class that collects the various sampler state settings.
	// Immutable to avoid confusion if you pass in a value then modify it before it's used internally.
	public class SamplerState
	{
		public SamplerState(TextureFilter filter = TextureFilter.Linear,
							TextureAddressMode addressMode = TextureAddressMode.Wrap)
		{
			this.filter = filter;
			this.addressMode = addressMode;
		}

		// Easy accessors for some common use cases.
		public static SamplerState LinearWrap => _linearWrap;
		public static SamplerState LinearClamp => _linearClamp;
		public static SamplerState PointWrap => _pointWrap;
		public static SamplerState PointClamp => _pointClamp;

		private static SamplerState _linearWrap = new SamplerState(TextureFilter.Linear, TextureAddressMode.Wrap);
		private static SamplerState _linearClamp = new SamplerState(TextureFilter.Linear, TextureAddressMode.Clamp);
		private static SamplerState _pointWrap = new SamplerState(TextureFilter.Point, TextureAddressMode.Wrap);
		private static SamplerState _pointClamp = new SamplerState(TextureFilter.Point, TextureAddressMode.Clamp);

		public readonly TextureFilter filter;
		public readonly TextureAddressMode addressMode;
		// TODO: Add the rest.
	}
}
