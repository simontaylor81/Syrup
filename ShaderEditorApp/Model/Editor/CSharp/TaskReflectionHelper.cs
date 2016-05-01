using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ShaderEditorApp.Model.Editor.CSharp
{
	// Simple helper for working with Task<> objects via reflection.
	class TaskReflectionHelper
	{
		private readonly PropertyInfo _resultProperty;

		public TaskReflectionHelper(Type type)
		{
			_resultProperty = typeof(Task<>).MakeGenericType(type).GetProperty("Result");
		}

		public object GetResult(Task t) => _resultProperty.GetValue(t);
	}

	// Simple helper for working with Task<T> objects via reflection.
	class TaskReflectionHelper<T> : TaskReflectionHelper
	{
		public TaskReflectionHelper()
			: base(typeof(T))
		{ }

		public new T GetResult(Task t) => (T)base.GetResult(t);
	}

}
