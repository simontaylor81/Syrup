﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.UserProperties
{
	public interface IScalarProperty<T> : IUserProperty
	{
		// The value of the property.
		T Value { get; set; }
	}
}
