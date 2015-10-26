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

		public override string DisplayName => "Mesh - " + (_mesh.Mesh != null ? _mesh.Mesh.Name : "<null>");

		public MeshInstancePrimitiveViewModel(MeshInstancePrimitive mesh, SRPCommon.Scene.Scene scene)
			: base(mesh, scene)
		{
			_mesh = mesh;
		}
	}
}
