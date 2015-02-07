using SRPCommon.UserProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderEditorApp.SampleData
{
	public class DummyWorkspace
	{
		public List<DummyPropertyBase> Properties { get; set; }
	}

	public class DummyPropertyBase
	{
		public string DisplayName { get; set; }
		public bool IsReadOnly { get; set; }
	}

	public class DummyPropertyFloat : DummyPropertyBase
	{
		public float Value { get; set; }
		//public bool IsComposite { get { return false; } }	// TEMP?
	}
	public class DummyPropertyBool : DummyPropertyBase
	{
		public bool Value { get; set; }
		//public bool IsComposite { get { return false; } }	// TEMP?
	}
	public class DummyVectorProperty : DummyPropertyBase
	{
		public List<DummyPropertyBase> SubProperties { get; set; }
		//public bool IsComposite { get { return true; } }	// TEMP?
	}

	public class DummyPropertyList : List<DummyPropertyBase>
	{ }

	public class DummyMatrixProperty : DummyPropertyBase
	{
		public List<DummyPropertyList> Rows { get; set; }
		//public bool IsComposite { get { return true; } }	// TEMP?
	}
}
