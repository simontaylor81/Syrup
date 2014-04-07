using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SRPCommon.Scene;

namespace ShaderEditorApp.ViewModel.Scene
{
	class SpherePrimitiveViewModel : ScenePrimitiveViewModel
	{
		private SpherePrimitive _sphere;

		public override string DisplayName { get { return "Sphere"; } }

		public SpherePrimitiveViewModel(SRPCommon.Scene.SpherePrimitive sphere)
			: base(sphere)
		{
			_sphere = sphere;
		}
	}
}
