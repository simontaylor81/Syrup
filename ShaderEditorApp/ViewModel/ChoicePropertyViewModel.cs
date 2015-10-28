using SRPCommon.UserProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderEditorApp.ViewModel
{
	// View model for a property that lets the user select one of a number of strings.
	// Basically an enum, but string based as they need to be created at runtime (via script).
	class ChoicePropertyViewModel : ScalarPropertyViewModel<string>
	{
		public ChoicePropertyViewModel(IScalarProperty<string> property)
			: base(property)
		{
			// TODO
			Choices = new[] { "A", "B" };
		}

		public IEnumerable<string> Choices { get; }
	}
}
