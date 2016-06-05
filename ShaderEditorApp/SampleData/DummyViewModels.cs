using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using ShaderEditorApp.ViewModel.Properties;
using SRPCommon.UserProperties;

namespace ShaderEditorApp.SampleData
{
	public class DummyWorkspace
	{
		public List<object> Properties { get; set; } = new List<object>();

		public DummyWorkspace()
		{
			AddProperty(new MutableScalarProperty<float>("Property 1", 12.0f));
			AddProperty(new ReadOnlyScalarProperty<float>("Property 2", -7.0f));
			AddProperty(new MutableScalarProperty<bool>("Bool prop", true));

			AddProperty(new DesignTimeChoiceProperty("Choice prop", "Hello", new[] { "Hello", "World" }));

			var vec = new System.Numerics.Vector4(1.00001f, 2.0f, 3.0f, 4.0f);
			AddProperty(new StructUserProperty("Vec prop", () => vec, _ => { }));

			AddProperty(new DesignTimeMatrixProperty("Mat prop", new[] { 1.00001f, 2.0f, 3.0f, 4.0f }, 2));
		}

		// Save typing.
		private void AddProperty(IUserProperty property)
		{
			Properties.Add(PropertyViewModelFactory.CreateViewModel(property));
		}
	}

	internal class DesignTimeChoiceProperty : MutableScalarProperty<object>, IChoiceProperty
	{
		public IEnumerable<object> Choices { get; set; }

		public DesignTimeChoiceProperty(string name, object value, IEnumerable<object> choices)
			: base(name, value)
		{
			Choices = choices;
		}
	}

	internal class DesignTimeMatrixProperty : IMatrixProperty
	{
		private readonly MutableScalarProperty<float>[] _components;

		public bool IsReadOnly => true;
		public string Name { get; set; }
		public int NumColumns { get; }
		public int NumRows { get; }
		public bool RequiresReExecute => false;

		public DesignTimeMatrixProperty(string name, float[] elements, int numColumns)
		{
			Name = name;
			NumColumns = numColumns;
			NumRows = elements.Length / numColumns;
			_components = elements
				.Select((value, index) => new MutableScalarProperty<float>($"M{index}", value))
				.ToArray();
		}

		public IUserProperty GetComponent(int row, int col) => _components[row * NumColumns + col];

		public IDisposable Subscribe(IObserver<Unit> observer)
		{
			throw new NotImplementedException();
		}
	}
}
