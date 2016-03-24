using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using SRPCommon.Interfaces;

namespace ShaderEditorApp.Model
{
	// Simple progress & status reporting mechanism.
	class Progress : IProgress
	{
		private readonly Subject<string> _subject = new Subject<string>();

		public IObservable<string> Status => _subject;

		public void Update(string status)
		{
			_subject.OnNext(status);
		}

		public void Complete()
		{
			_subject.OnNext("");
		}
	}
}
