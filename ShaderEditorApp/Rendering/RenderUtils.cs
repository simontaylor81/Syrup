using System;
using System.Collections.Generic;
using System.IO;

namespace ShaderEditorApp.Rendering
{
	static class RenderUtils
	{
		public static void SafeDispose(IDisposable obj)
		{
			if (null != obj)
				obj.Dispose();
		}

		public static void DisposeList<T>(IList<T> resources) where T : IDisposable
		{
			foreach (var resource in resources)
				resource.Dispose();

			resources.Clear();
		}
		public static void DisposeArray<T>(ref T[] resources) where T : IDisposable
		{
			if (resources != null)
			{
				foreach (var resource in resources)
					resource.Dispose();

				resources = null;
			}
		}

		public static string GetShaderFilename(string file)
		{
			return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
				Path.Combine("..\\..\\..\\Shaders", file));
		}
	}
}
