using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderEditorApp.Model.Editor
{
	// Result of a Signature Help request.
	public class SignatureHelp
	{
		public IEnumerable<SignatureHelpOverload> Overloads { get; set; }
		public int BestOverload { get; set; }
		public int ActiveParameter { get; set; }
	}

	// Information about a single overload for signature help.
	public class SignatureHelpOverload
	{
		public string Name { get; set; }
		public string Label { get; set; }
		public string Documentation { get; set; }
		public IEnumerable<SignatureHelpParameter> Parameters { get; set; }
	}

	// Information about a parameter to a function for signature help.
	public class SignatureHelpParameter
	{
		public string Name { get; set; }
		public string Label { get; set; }
		public string Documentation { get; set; }
	}
}
