using SRPCommon.UserProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Reactive;

namespace SRPTests.UnitTests
{
	public class UserPropertyCopyFromTests
	{
		[Fact]
		public void FloatScalarToFloatScalarCopiesCorrectly()
		{
			// Arrange.
			var source = new ReadOnlyScalarProperty<float>("source", 3.141f);
			var dest = new MutableScalarProperty<float>("dest", 0.0f);

			// Act.
			dest.TryCopyFrom(source);

			// Assert.
			Assert.Equal(source.Value, dest.Value);
		}

		[Fact]
		public void CopyToReadOnlyDoesNothing()
		{
			// Arrange.
			var source = new ReadOnlyScalarProperty<float>("source", 3.141f);
			var dest = new ReadOnlyScalarProperty<float>("dest", 0.0f);

			// Act.
			dest.TryCopyFrom(source);

			// Assert.
			Assert.Equal(0.0f, dest.Value);
		}

		[Fact]
		public void ScalarFloatToIntDoesNothing()
		{
			// Arrange.
			var source = new ReadOnlyScalarProperty<float>("source", 3.141f);
			var dest = new MutableScalarProperty<int>("dest", 12);

			// Act.
			dest.TryCopyFrom(source);

			// Assert.
			Assert.Equal(12, dest.Value);
		}

		[Fact]
		public void ScalarToVectorDoesNothing()
		{
			// Arrange.
			var source = new ReadOnlyScalarProperty<int>("source", 3);
			var initialValues = new[] { 1, 2 };
			var dest = new TestVectorProperty<int>(new[] { 1, 2 });

			// Act.
			dest.TryCopyFrom(source);

			// Assert.
			Assert.Equal(initialValues, dest.AllValues);
		}

		[Fact]
		public void FloatVectorToFloatVectorCopiesCorrectly()
		{
			// Arrange.
			var sourceValues = new[] { 3.141f, 2.718f };
			var source = new TestVectorProperty<float>(sourceValues);
			var dest = new TestVectorProperty<float>(new[] { 0.0f, 0.0f });

			// Act.
			dest.TryCopyFrom(source);

			// Assert.
			Assert.Equal(sourceValues, dest.AllValues);
		}

		[Fact]
		public void ComponentCountMismatchCopiesSome()
		{
			// Arrange.
			var sourceValues = new[] { 3.141f, 2.718f };
			var source = new TestVectorProperty<float>(sourceValues);
			var smallDest = new TestVectorProperty<float>(new[] { 0.0f });
			var largeDest = new TestVectorProperty<float>(new[] { 0.0f, 0.0f, 12.0f });

			// Act.
			smallDest.TryCopyFrom(source);
			largeDest.TryCopyFrom(source);

			// Assert.
			Assert.Equal(sourceValues.Take(1), smallDest.AllValues);
			Assert.Equal(sourceValues.Concat(new[] { 12.0f }), largeDest.AllValues);
		}

		[Fact]
		public void FloatMatrixToFloatMatrixCopiesCorrectly()
		{
			// Arrange.
			var sourceValues = new[,]
			{
				{ 3.141f, 2.718f },
				{ 1.234f, 6.02f },
			};
			var source = new TestMatrixProperty<float>(sourceValues);
			var dest = new TestMatrixProperty<float>(new[,] { { 0.0f, 0.0f }, { 0.0f, 0.0f } });

			// Act.
			dest.TryCopyFrom(source);

			// Assert.
			Assert.Equal(Enumerable.Cast<float>(sourceValues), dest.AllValues);
		}

		private class TestVectorProperty<T> : IVectorProperty
		{
			private readonly MutableScalarProperty<T>[] _values;

			public string Name => "testprop";
			public bool IsReadOnly => false;

			public int NumComponents => _values.Length;

			public TestVectorProperty(IEnumerable<T> values)
			{
				_values = values
					.Select(x => new MutableScalarProperty<T>("subprop", x))
					.ToArray();
			}

			public IUserProperty GetComponent(int index) => _values[index];

			public IDisposable Subscribe(IObserver<Unit> observer)
			{
				throw new NotImplementedException();
			}

			// Convenience for assertions.
			public IEnumerable<T> AllValues => _values.Select(x => x.Value);
		}

		private class TestMatrixProperty<T> : IMatrixProperty
		{
			private readonly MutableScalarProperty<T>[,] _values;

			public string Name => "testprop";
			public bool IsReadOnly => false;

			public int NumColumns => _values.GetLength(0);
			public int NumRows => _values.GetLength(1);

			public TestMatrixProperty(T[,] values)
			{
				_values = new MutableScalarProperty<T>[values.GetLength(0), values.GetLength(1)];
				for (int col = 0; col < values.GetLength(0); col++)
				{
					for (int row = 0; row < values.GetLength(1); row++)
					{
						_values[col, row] = new MutableScalarProperty<T>("subprop", values[col, row]);
					}
				}
			}

			public IUserProperty GetComponent(int row, int col) => _values[col, row];

			public IDisposable Subscribe(IObserver<Unit> observer)
			{
				throw new NotImplementedException();
			}

			// Convenience for assertions.
			public IEnumerable<T> AllValues => Enumerable.Cast<MutableScalarProperty<T>>(_values).Select(x => x.Value);
		}
	}
}
