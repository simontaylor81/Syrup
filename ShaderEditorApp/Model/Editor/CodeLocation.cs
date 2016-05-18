using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderEditorApp.Model.Editor
{
	// The location of something in the code.
	public class CodeLocation
	{
		public string Filename { get; set; }
		public int Offset { get; set; }
		public int Length { get; set; }
	}
}
