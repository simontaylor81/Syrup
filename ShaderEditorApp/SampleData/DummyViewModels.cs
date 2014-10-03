using SRPCommon.UserProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderEditorApp.SampleData
{
	class DummyWorkspace
	{
		public List<DummyPropertyBase> Properties { get; set; }
	}

	class DummyPropertyBase
	{
		public string DisplayName { get; set; }
		public bool IsReadOnly { get; set; }
	}

	class DummyPropertyFloat : DummyPropertyBase
	{
		public float Value { get; set; }
		//public bool IsComposite { get { return false; } }	// TEMP?
	}
	class DummyCompositeProperty : DummyPropertyBase
	{
		public List<DummyPropertyBase> SubProperties { get; set; }
		//public bool IsComposite { get { return true; } }	// TEMP?
	}
}
