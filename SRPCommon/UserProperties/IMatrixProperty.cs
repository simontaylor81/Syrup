using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.UserProperties
{
	public interface IMatrixProperty : IUserProperty
	{
		IUserProperty GetComponent(int x, int y);

		int NumComponentsX { get; }
		int NumComponentsY { get; }
	}
}
