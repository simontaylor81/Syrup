using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.Interfaces
{
	// Simple progress update interface.
	public interface IProgress
	{
		void Update(string status);
		void Complete();
	}

	// Null implementation when you don't need progress reporting (e.g. in tests).
	public class NullProgress : IProgress
	{
		public void Complete() { }
		public void Update(string status) { }
	}
}
