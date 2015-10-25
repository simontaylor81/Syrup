using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.UserProperties
{
	public static class UserPropertyExtensions
	{
		public static void TryCopyFrom(this IUserProperty dest, IUserProperty source)
		{
			// Can't copy to read-only properties.
			if (dest.IsReadOnly)
			{
				return;
			}

			var destScalar = dest as IScalarProperty;
			var sourceScalar = source as IScalarProperty;
			var destVector = dest as IVectorProperty;
			var sourceVector = source as IVectorProperty;
			var destMatrix = dest as IMatrixProperty;
			var sourceMatrix = source as IMatrixProperty;

			if (destScalar != null && sourceScalar != null)
			{
				// Can only copy if they're the same type.
				if (destScalar.Type == sourceScalar.Type)
				{
					// Abuse dynamic dispatch to select the right generic type.
					dynamic destDynamic = destScalar;
					dynamic sourceDynamic = sourceScalar;
					CopyScalar(destDynamic, sourceDynamic);
				}
			}
			else if (destVector != null && sourceVector != null)
			{
				// Copy as many elements as possible.
				for (int i = 0; i < destVector.NumComponents && i < sourceVector.NumComponents; i++)
				{
					destVector.GetComponent(i).TryCopyFrom(sourceVector.GetComponent(i));
				}
			}
			else if (destMatrix != null && sourceMatrix != null)
			{
				// Copy as many elements as possible.
				for (int row = 0; row < destMatrix.NumRows && row < sourceMatrix.NumRows; row++)
				{
					for (int column = 0; column < destMatrix.NumColumns && column < sourceMatrix.NumColumns; column++)
					{
						destMatrix.GetComponent(row, column).TryCopyFrom(sourceMatrix.GetComponent(row, column));
					}
				}
			}
		}

		private static void CopyScalar<T>(IScalarProperty<T> dest, IScalarProperty<T> source)
		{
			dest.Value = source.Value;
		}
	}
}
