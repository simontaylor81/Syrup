using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SRPCommon.Scene;

namespace ShaderEditorApp.ViewModel.Scene
{
	class MeshInstancePrimitiveViewModel : ScenePrimitiveViewModel
	{
		private readonly MeshInstancePrimitive _mesh;

		public override string DisplayName => "Mesh - " + _mesh.Mesh.Name;

		public MeshInstancePrimitiveViewModel(MeshInstancePrimitive mesh)
			: base(mesh)
		{
			_mesh = mesh;
		}
	}
}
