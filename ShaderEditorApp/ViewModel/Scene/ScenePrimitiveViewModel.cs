using ReactiveUI;
using SlimDX;
using SRPCommon.Scene;
using SRPCommon.UserProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Windows.Input;

namespace ShaderEditorApp.ViewModel.Scene
{
	class ScenePrimitiveViewModel : ReactiveObject, IHierarchicalBrowserNodeViewModel
	{
		private readonly Primitive _primitive;

		#region IHierarchicalBrowserNodeViewModel interface

		public virtual string DisplayName => _primitive.Type.ToString();

		public IEnumerable<ICommand> Commands => Enumerable.Empty<ICommand>();

		public IEnumerable<IUserProperty> UserProperties => _primitive.UserProperties;

		// No children
		public IEnumerable<IHierarchicalBrowserNodeViewModel> Children => null;

		public ICommand DefaultCmd => null;

		public bool IsDefault => false;

		#endregion

		public static ScenePrimitiveViewModel Create(Primitive primitive)
		{
			var mesh = primitive as MeshInstancePrimitive;

			if (mesh != null)
			{
				return new MeshInstancePrimitiveViewModel(mesh);
			}
			else
			{
				return new ScenePrimitiveViewModel(primitive);
			}
		}

		protected ScenePrimitiveViewModel(Primitive primitive)
		{
			_primitive = primitive;
		}
	}
}
