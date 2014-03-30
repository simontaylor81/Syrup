using System;
using System.Collections.Generic;
using System.IO;

namespace SRPRendering
{
	static class RenderUtils
	{
		public static string GetShaderFilename(string file)
		{
			return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
				Path.Combine("..\\..\\..\\Shaders", file));
		}
	}
}
