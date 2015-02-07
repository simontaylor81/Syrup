using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.UserProperties
{
	public interface IMatrixProperty : IUserProperty
	{
		IUserProperty GetComponent(int row, int col);

		int NumColumns { get; }
		int NumRows { get; }
	}
}
