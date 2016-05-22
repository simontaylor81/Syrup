using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderEditorApp.SampleData
{
	public class DummyWorkspace
	{
		public List<DummyPropertyBase> Properties { get; set; } = new List<DummyPropertyBase>();

		public DummyWorkspace()
		{
			Properties.Add(new DummyPropertyFloat { DisplayName = "Property 1", IsReadOnly = false, Value = 12.0f });
			Properties.Add(new DummyPropertyFloat { DisplayName = "Property 2", IsReadOnly = true, Value = -7.0f });
			Properties.Add(new DummyPropertyBool { DisplayName = "Bool prop", IsReadOnly = false, Value = true });
			Properties.Add(new DummyChoiceProperty
			{
				DisplayName = "Choice prop",
				IsReadOnly = false,
				Value = "Choice 1",
				Choices = { "Hello", "World", }
			});
			Properties.Add(new DummyVectorProperty
			{
				DisplayName = "Vec prop",
				IsReadOnly = true,
				SubProperties =
				{
					new DummyPropertyFloat { DisplayName="X", IsReadOnly=false, Value=1.00001f },
					new DummyPropertyFloat { DisplayName="Y", IsReadOnly=false, Value=2.0f },
					new DummyPropertyFloat { DisplayName="Z", IsReadOnly=false, Value=3.0f },
					new DummyPropertyFloat { DisplayName="W", IsReadOnly=false, Value=4.0f },
				}
			});
			Properties.Add(new DummyMatrixProperty
			{
				DisplayName = "Mat prop",
				IsReadOnly = true,
				Rows =
				{
					new DummyPropertyList
					{
						new DummyPropertyFloat { DisplayName="X", IsReadOnly=false, Value=1.00001f },
						new DummyPropertyFloat { DisplayName="Y", IsReadOnly=false, Value=2.0f },
					},
					new DummyPropertyList
					{
						new DummyPropertyFloat { DisplayName="Z", IsReadOnly=false, Value=3.0f },
						new DummyPropertyFloat { DisplayName="W", IsReadOnly=false, Value=4.0f },
					}
				}
			});
		}
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
		public List<DummyPropertyBase> SubProperties { get; set; } = new List<DummyPropertyBase>();
	}
	public class DummyMatrixProperty : DummyPropertyBase
	{
		public List<DummyPropertyList> Rows { get; set; } = new List<DummyPropertyList>();
	}
	public class DummyChoiceProperty : DummyPropertyBase
	{
		public string Value { get; set; }
		public List<string> Choices { get; set; } = new List<string>();
	}

	public class DummyPropertyList : List<DummyPropertyBase>
	{ }
}
