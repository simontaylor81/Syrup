using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;

namespace SRPRendering
{
	public class ViewInfo
	{
		public ViewInfo(
			Matrix4x4 worldToViewMatrix,
			Matrix4x4 viewToProjMatrix,
			Vector3 eyePosition,
			float nearPlane,
			float farPlane,
			int viewportWidth,
			int viewportHeight,
			RenderTargetView backBuffer,
			DepthBuffer depthBuffer)
		{
			this.WorldToViewMatrix = worldToViewMatrix;
			this.ViewToProjMatrix = viewToProjMatrix;
			this.EyePosition = eyePosition;
			this.NearPlane = nearPlane;
			this.FarPlane = farPlane;
			this.ViewportWidth = viewportWidth;
			this.ViewportHeight = viewportHeight;
			this.BackBuffer = backBuffer;
			this.DepthBuffer = depthBuffer;
		}

		public Matrix4x4 WorldToViewMatrix;
		public Matrix4x4 ViewToProjMatrix;
		public Vector3 EyePosition;
		public float NearPlane;
		public float FarPlane;
		public int ViewportWidth;
		public int ViewportHeight;
		public RenderTargetView BackBuffer;
		public DepthBuffer DepthBuffer;
	}
}
