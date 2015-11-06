using SRPCommon.UserProperties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace SRPRendering
{
	// User variable representing a multiple choice (i.e. drop down list).
	class UserVariableChoice : UserVariableScalar<object>, IChoiceProperty
	{
		public UserVariableChoice(string name, IEnumerable<object> choices, object defaultValue)
			: base(name, defaultValue)
		{
			Choices = choices;
		}

		public IEnumerable<object> Choices { get; }

		// Overridden so we can check that the value is one of the choices.
		public override object Value
		{
			get
			{
				return base.Value;
			}
			set
			{
				// This can be false when copying values from a previous script run if the choices have changed.
				if (Choices.Contains(value))
				{
					base.Value = value;
				}
			}
		}
	}
}
