using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SRPCommon.Scene;

namespace ShaderEditorApp.ViewModel.Scene
{
	class MeshInstancePrimitiveViewModel : ScenePrimitiveViewModel
	{
		private MeshInstancePrimitive _mesh;

		public override string DisplayName { get { return "Mesh"; } }

		public MeshInstancePrimitiveViewModel(MeshInstancePrimitive mesh)
			: base(mesh)
		{
			_mesh = mesh;
		}
	}
}
