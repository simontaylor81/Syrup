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
	class ChoicePropertyViewModel : ScalarPropertyViewModel<object>
	{
		private readonly IChoiceProperty _choiceProperty;

		public ChoicePropertyViewModel(IChoiceProperty property)
			: base(property)
		{
			_choiceProperty = property;
		}

		public IEnumerable<object> Choices => _choiceProperty.Choices;
	}
}
