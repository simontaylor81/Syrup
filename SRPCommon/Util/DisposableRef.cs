using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.Util
{
	// Cheesy 'smart pointer' to an IDisposable that handles disposing the previous
	// reference when setting a new one. Still have to dispose manually when cleaning up.
	public struct DisposableRef<T> : IDisposable where T : IDisposable
	{
		private T _ref;

		public T Ref
		{
			get { return _ref; }
			set
			{
				_ref?.Dispose();
				_ref = value;
			}
		}

		public DisposableRef(T value)
		{
			_ref = value;
		}

		public void Dispose()
		{
			Ref = default(T);
		}
	}
}
