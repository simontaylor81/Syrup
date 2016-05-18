using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderEditorApp.Model.Editor
{
	// An entry in a completion list.
	// This is deliberately quite rudimentary at the moment, as working
	// with the non-public completion APIs is so painful/dangerous.
	public struct CompletionItem
	{
		public string DisplayText;
		public string InsertionText;
		public int StartOffset;
	}
}
