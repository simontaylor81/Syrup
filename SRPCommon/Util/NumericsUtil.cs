using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.Util
{
	// Extension methods for System.Numerics.Vector*
	public static class VectorExtensions
	{
		public static float[] ToArray(this Vector2 vec)
		{
			var result = new float[2];
			vec.CopyTo(result);
			return result;
		}
		public static float[] ToArray(this Vector3 vec)
		{
			var result = new float[3];
			vec.CopyTo(result);
			return result;
		}
		public static float[] ToArray(this Vector4 vec)
		{
			var result = new float[4];
			vec.CopyTo(result);
			return result;
		}
	}

	// Matrix utility methods.
	public static class MatrixUtil
	{
		// Left-hand version of CreatePerspectiveFieldOfView, modifed version of the one found in
		//https://github.com/dotnet/corefx/blob/master/src/System.Numerics.Vectors/src/System/Numerics/Matrix4x4.cs (MIT license).
		// Matches D3DXMatrixPerspectiveFovLH (https://msdn.microsoft.com/en-us/library/windows/desktop/bb205350%28v=vs.85%29.aspx).
		/// <summary>
		/// Creates a left-hand perspective projection matrix based on a field of view, aspect ratio, and near and far view plane distances. 
		/// </summary>
		/// <param name="fieldOfView">Field of view in the y direction, in radians.</param>
		/// <param name="aspectRatio">Aspect ratio, defined as view space width divided by height.</param>
		/// <param name="nearPlaneDistance">Distance to the near view plane.</param>
		/// <param name="farPlaneDistance">Distance to the far view plane.</param>
		/// <returns>The perspective projection matrix.</returns>
		public static Matrix4x4 CreatePerspectiveFieldOfViewLH(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance)
		{
			if (fieldOfView <= 0.0f || fieldOfView >= Math.PI)
				throw new ArgumentOutOfRangeException(nameof(fieldOfView));

			if (nearPlaneDistance <= 0.0f)
				throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance));

			if (farPlaneDistance <= 0.0f)
				throw new ArgumentOutOfRangeException(nameof(farPlaneDistance));

			if (nearPlaneDistance >= farPlaneDistance)
				throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance));

			float yScale = 1.0f / (float)Math.Tan(fieldOfView * 0.5f);
			float xScale = yScale / aspectRatio;

			Matrix4x4 result;

			result.M11 = xScale;
			result.M12 = result.M13 = result.M14 = 0.0f;

			result.M22 = yScale;
			result.M21 = result.M23 = result.M24 = 0.0f;

			result.M31 = result.M32 = 0.0f;
			result.M33 = farPlaneDistance / (farPlaneDistance - nearPlaneDistance);
			result.M34 = 1.0f;

			result.M41 = result.M42 = result.M44 = 0.0f;
			result.M43 = -nearPlaneDistance * farPlaneDistance / (farPlaneDistance - nearPlaneDistance);

			return result;
		}

	}
}
