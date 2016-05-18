using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace ShaderEditorApp.View.DocumentEditor
{
	// A squiggly line error notification.
	class Squiggly : TextSegment
	{
		public Color Colour;
	}

	// Class that handles drawing squiggly lines underneath code errors.
	// This code is based on TextMarkerService from https://github.com/siegfriedpammer/AvalonEditSamples,
	// originally part of SharpDevelop.
	class SquigglyService : IBackgroundRenderer
	{
		private readonly TextView _textView;
		private readonly TextDocument _document;
		private TextSegmentCollection<Squiggly> _squigglies;

		public SquigglyService(TextView textView, TextDocument document, IObservable<IEnumerable<Squiggly>> squigglies)
		{
			_textView = textView;
			_document = document;
			squigglies.Subscribe(SetSquigglies);
		}

		public KnownLayer Layer => KnownLayer.Selection;

		public void Draw(TextView textView, DrawingContext drawingContext)
		{
			if (_squigglies == null || !textView.VisualLinesValid)
			{
				return;
			}

			var visualLines = textView.VisualLines;
			if (visualLines.Count == 0)
			{
				return;
			}

			int viewStart = visualLines.First().FirstDocumentLine.Offset;
			int viewEnd = visualLines.Last().LastDocumentLine.EndOffset;

			foreach (var squiggly in _squigglies.FindOverlappingSegments(viewStart, viewEnd - viewStart))
			{
				foreach (var r in BackgroundGeometryBuilder.GetRectsForSegment(textView, squiggly))
				{
					var startPoint = r.BottomLeft;
					var endPoint = r.BottomRight;

					var usedBrush = new SolidColorBrush(squiggly.Colour);
					usedBrush.Freeze();

					var geometry = new StreamGeometry();
					using (var ctx = geometry.Open())
					{
						double offset = 2.5;
						int count = Math.Max((int)((endPoint.X - startPoint.X) / offset) + 1, 4);

						ctx.BeginFigure(startPoint, false, false);
						ctx.PolyLineTo(CreateSquigglyPoints(startPoint, offset, count).ToArray(), true, false);
					}

					geometry.Freeze();

					var usedPen = new Pen(usedBrush, 1);
					usedPen.Freeze();
					drawingContext.DrawGeometry(Brushes.Transparent, usedPen, geometry);
				}
			}
		}

		private IEnumerable<Point> CreateSquigglyPoints(Point start, double offset, int count)
		{
			for (int i = 0; i < count; i++)
			{
				yield return new Point(start.X + i * offset, start.Y - ((i + 1) % 2 == 0 ? offset : 0));
			}
		}

		// Set the set of squigglies to draw.
		private void SetSquigglies(IEnumerable<Squiggly> squigglies)
		{
			// Just recreate a new segment collection from scratch each time.
			_squigglies = new TextSegmentCollection<Squiggly>(_document);
			foreach (var squiggly in squigglies)
			{
				_squigglies.Add(squiggly);
			}

			// Redraw the view with our new squigglies.
			_textView.Redraw();
		}
	}
}
