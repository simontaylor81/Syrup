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
	}
	public class DummyPropertyBool : DummyPropertyBase
	{
		public bool Value { get; set; }
	}
	public class DummyVectorProperty : DummyPropertyBase
	{
		public List<DummyPropertyBase> SubProperties { get; set; }
	}
	public class DummyMatrixProperty : DummyPropertyBase
	{
		public List<DummyPropertyList> Rows { get; set; }
	}
	public class DummyChoiceProperty : DummyPropertyBase
	{
		public string Value { get; set; }
		public List<string> Choices { get; set; }
	}

	public class DummyPropertyList : List<DummyPropertyBase>
	{ }
}
