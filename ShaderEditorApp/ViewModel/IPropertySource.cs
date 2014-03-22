using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderEditorApp.ViewModel
{
	public interface IPropertySource
	{
		IEnumerable<PropertyViewModel> Properties { get; }
	}
}
