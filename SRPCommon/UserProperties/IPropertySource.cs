﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.UserProperties
{
	public interface IPropertySource
	{
		IEnumerable<IUserProperty> Properties { get; }
	}
}