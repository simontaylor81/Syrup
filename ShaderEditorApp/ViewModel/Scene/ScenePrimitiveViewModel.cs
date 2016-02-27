using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using ReactiveUI;
using SRPCommon.Scene;
using SRPCommon.UserProperties;

namespace ShaderEditorApp.ViewModel.Scene
{
	class ScenePrimitiveViewModel : ReactiveObject, IHierarchicalBrowserNodeViewModel
	{
		private readonly Primitive _primitive;

		#region IHierarchicalBrowserNodeViewModel interface

		public virtual string DisplayName => _primitive.Type.ToString();

		public IEnumerable<object> MenuItems { get; }

		public IEnumerable<IUserProperty> UserProperties => _primitive.UserProperties;

		// No children
		public IEnumerable<IHierarchicalBrowserNodeViewModel> Children => null;

		public ICommand DefaultCmd => null;

		public bool IsDefault => false;

		#endregion

		public static ScenePrimitiveViewModel Create(Primitive primitive, SRPCommon.Scene.Scene scene)
		{
			var mesh = primitive as MeshInstancePrimitive;

			if (mesh != null)
			{
				return new MeshInstancePrimitiveViewModel(mesh, scene);
			}
			else
			{
				return new ScenePrimitiveViewModel(primitive, scene);
			}
		}

		protected ScenePrimitiveViewModel(Primitive primitive, SRPCommon.Scene.Scene scene)
		{
			_primitive = primitive;

			// Create commands.
			MenuItems = new[]
			{
				new CommandMenuItem(new CommandViewModel("Remove", CommandUtil.Create(_ => scene.RemovePrimitive(_primitive))))
			};
		}
	}
}
