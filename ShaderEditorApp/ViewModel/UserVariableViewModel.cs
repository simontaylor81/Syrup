using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShaderEditorApp.Rendering;

namespace ShaderEditorApp.ViewModel
{
	class UserVariableFloatVectorViewModel : VectorPropertyBase<float>
	{
		public UserVariableFloatVectorViewModel(UserVariableFloat userVar)
			: base(userVar.Name, userVar.NumComponents)
		{
			this.userVar = userVar;
		}

		public override float this[int index]
		{
			get { return userVar[index]; }
			set
			{
				if (value != userVar[index])
				{
					userVar[index] = value;
					OnPropertyChanged("Item[]");
				}
			}
		}

		private UserVariableFloat userVar;
	}

	class UserVariableBoolViewModel : ScalarPropertyBase<bool>
	{
		public UserVariableBoolViewModel(UserVariableBool userVar)
			: base(userVar.Name)
		{
			this.userVar = userVar;
		}

		public override bool Value
		{
			get { return userVar.Value; }
			set
			{
				if (value != userVar.Value)
				{
					userVar.Value = value;
					OnPropertyChanged();
				}
			}
		}

		private UserVariableBool userVar;
	}
}
