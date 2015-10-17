using NSubstitute;
using SRPCommon.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SRPTests.UnitTests
{
	public class LazyMappingTests
	{
		private readonly Func<string, DisposableString> _create;
		private readonly LazyMapping<string, DisposableString> _mapping;

		public LazyMappingTests()
		{
			_create = Substitute.For<Func<string, DisposableString>>();
			_create(null).ReturnsForAnyArgs(callInfo => new DisposableString(callInfo.Arg<string>()));

			_mapping = new LazyMapping<string, DisposableString>(_create);
		}

		protected IEnumerable<string> GetSourceArray(int count)
		{
			return Enumerable.Range(1, count)
				.Select(x => x.ToString());
		}

		protected void AssertResults(IEnumerable<string> expectedSource)
		{
			Assert.Equal(expectedSource.Count(), _mapping.Result.Count());
			for (int i = 0; i < expectedSource.Count(); i++)
			{
				Assert.Equal(expectedSource.ElementAt(i), _mapping.Result.ElementAt(i).Value);
			}
		}

		public class WithEmpty : LazyMappingTests
		{
			[Fact]
			public void NewItemsAreCreated()
			{
				// Act.
				_mapping.Update(GetSourceArray(2));

				// Assert.
				AssertResults(GetSourceArray(2));
				_create.ReceivedWithAnyArgs(2)(null);
			}
		}

		public class With2Items : LazyMappingTests
		{
			private readonly List<DisposableString> _prevResults;

			public With2Items()
			{
				_mapping.Update(GetSourceArray(2));

				Assert.NotEmpty(_mapping.Result);
				_prevResults = _mapping.Result.ToList();

				_create.ClearReceivedCalls();
			}

			[Fact]
			public void MissingItemsAreRemovedAndDisposed()
			{
				// Act.
				_mapping.Update(GetSourceArray(0));

				// Assert.
				Assert.Empty(_mapping.Result);
				_create.DidNotReceiveWithAnyArgs()(null);

				foreach (var prevResult in _prevResults)
				{
					Assert.True(prevResult.Disposed);
				}
			}

			[Fact]
			public void OnlyNewItemsAreCreated()
			{
				// Act.
				_mapping.Update(GetSourceArray(3));

				// Assert.
				AssertResults(GetSourceArray(3));
				_create.ReceivedWithAnyArgs(1)(null);
			}

			[Fact]
			public void OnlyMissingItemsAreRemoved()
			{
				// Act.
				_mapping.Update(GetSourceArray(1));

				// Assert.
				AssertResults(GetSourceArray(1));
				_create.DidNotReceiveWithAnyArgs()(null);

				Assert.False(_prevResults.ElementAt(0).Disposed);
				Assert.True(_prevResults.ElementAt(1).Disposed);
			}

			[Fact]
			public void MixedAddsAndRemoves()
			{
				// Arrange.
				var newSource = new[] { "2", "3" };

				// Act.
				_mapping.Update(newSource);

				// Assert.
				AssertResults(newSource);
			}
		}

		// Simple class for tracking disposal.
		class DisposableString : IDisposable
		{
			public string Value { get; private set; }
			public bool Disposed { get; private set; }

			public DisposableString(string value)
			{
				Value = value;
			}

			public void Dispose()
			{
				// Should only be disposed once.
				Assert.False(Disposed);
				Disposed = true;
			}
		}
	}
}
