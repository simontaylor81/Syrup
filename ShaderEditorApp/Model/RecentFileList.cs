using ReactiveUI;
using SRPCommon.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShaderEditorApp.Model
{
	// Class representing a list of recently open files.
	public class RecentFileList
	{
		// The actual list of files. We don't cap this by MaxSize so that changing the size down
		// then back up again doesn't lose everything.
		private ReactiveList<string> _files = new ReactiveList<string>();

		// The list of files to display. This is capped to MaxSize.
		public IReadOnlyReactiveList<string> Files => _files;

		// The maxium size the file list can grow to.
		private int _maxSize;
		public int MaxSize
		{
			get { return _maxSize; }
			set
			{
				_maxSize = value;

				// Trim file list if it's now too big.
				Trim();
			}
		}

		public RecentFileList(int maxSize)
		{
			MaxSize = maxSize;
		}

		// Add a newly accessed file.
		public void AddFile(string filename)
		{
			// Adding an already existing file moves it to the front of the list.
			// Easiest way to do this is just to remove it first.
			_files.RemoveByPredicate(f => PathUtils.PathsEqual(f, filename));

			// Add at front. This could be more efficient by conceptually reversing the
			// list, but this is such a rare operation that it's not worth the hassle.
			_files.Insert(0, filename);
			Trim();
		}

		// Trim the list to ensure it is less that the maximum size.
		private void Trim()
		{
			if (_files.Count > _maxSize)
			{
				_files.RemoveRange(_maxSize, _files.Count - _maxSize);
			}
		}
	}
}
