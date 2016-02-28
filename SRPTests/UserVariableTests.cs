using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRPRendering;
using Xunit;
using SRPCommon.UserProperties;
using System.Numerics;
using SRPCommon.Util;

namespace SRPTests
{
	public class UserVariableTests
	{
		[Theory]
		[InlineData(3.142f, 2.718f)]
		[InlineData(true, false)]
		[InlineData(12, 42)]
		[InlineData("hi", "bye")]
		public void ScalarVariable<T>(T defaultValue, T otherValue)
		{
			var uv = UserVariable.CreateScalar<T>("myvar", defaultValue);
			var getValue = (Func<T>)uv.GetFunction();

			// Basics
			Assert.Equal("myvar", uv.Name);
			Assert.False(uv.IsReadOnly);

			// Must be a scalar property of the right type.
			Assert.IsAssignableFrom<IScalarProperty<T>>(uv);
			var prop = (IScalarProperty<T>)uv;

			// Should initially be the default value.
			Assert.Equal(defaultValue, prop.Value);
			Assert.Equal(defaultValue, getValue());

			// Should be notified when changing the value.
			bool receivedNotification = false;
			prop.Subscribe(_ => receivedNotification = true);
			prop.Value = otherValue;

			Assert.True(receivedNotification);

			Assert.Equal(otherValue, prop.Value);
			Assert.Equal(otherValue, getValue());
		}

		[Theory]
		[InlineData(0.0f, new[] { 1.0f, 2.0f }, new[] { 100.0f, 200.0f })]
		[InlineData(0.0f, new[] { 1.0f, 2.0f, 3.0f }, new[] { 100.0f, 200.0f, 300.0f })]
		[InlineData(0.0f, new[] { 1.0f, 2.0f, 3.0f, 4.0f }, new[] { 100.0f, 200.0f, 300.0f, 400.0f })]
		[InlineData(0, new[] { 1, 2 }, new[] { 100, 200 })]
		[InlineData(0, new[] { 1, 2, 3 }, new[] { 100, 200, 300 })]
		[InlineData(0, new[] { 1, 2, 3, 4 }, new[] { 100, 200, 300, 400 })]
		public void VectorVariable<T>(T dummy, T[] defaultValue, T[] otherValue)
		{
			var uv = UserVariable.CreateVector<T>(defaultValue.Length, "myvar", defaultValue);

			// Basics
			Assert.Equal("myvar", uv.Name);
			Assert.False(uv.IsReadOnly);

			// Must be a scalar property of the right type.
			var prop = Assert.IsAssignableFrom<IVectorProperty>(uv);

			Assert.Equal(defaultValue.Length, prop.NumComponents);

			// Check type of each component.
			for (int i = 0; i < prop.NumComponents; i++)
			{
				// All user variables are vectors of scalars (no vectors-of-vectors).
				Assert.IsAssignableFrom<IScalarProperty<T>>(prop.GetComponent(i));
			}

			var func = uv.GetFunction();

			// Check composite value is correct.
			Assert.Equal(defaultValue, func());

			// Check each component against default value.
			for (int i = 0; i < prop.NumComponents; i++)
			{
				Assert.Equal(defaultValue[i], ((IScalarProperty<T>)prop.GetComponent(i)).Value);
			}

			bool receivedNotification = false;
			prop.Subscribe(_ => receivedNotification = true);

			// Change each component to the other value.
			for (int i = 0; i < prop.NumComponents; i++)
			{
				((IScalarProperty<T>)prop.GetComponent(i)).Value = otherValue[i];
			}

			// Should be notified on the parent when changing the value of a component.
			Assert.True(receivedNotification);

			// Check composite value is correct.
			Assert.Equal(otherValue, func());

			// Check each component was changed.
			for (int i = 0; i < prop.NumComponents; i++)
			{
				Assert.Equal(otherValue[i], ((IScalarProperty<T>)prop.GetComponent(i)).Value);
			}
		}

		[Fact]
		public void FloatVectorVariable_Vector2Default()
		{
			var vector = new Vector2(1.0f, 2.0f);
			var uv = UserVariable.CreateVector<float>(2, "myvar", vector);
			Assert.Equal(vector.ToArray(), uv.GetFunction()());
		}

		[Fact]
		public void FloatVectorVariable_Vector3Default()
		{
			var vector = new Vector3(1.0f, 2.0f, 3.0f);
			var uv = UserVariable.CreateVector<float>(3, "myvar", vector);
			Assert.Equal(vector.ToArray(), uv.GetFunction()());
		}

		[Fact]
		public void FloatVectorVariable_Vector4Default()
		{
			var vector = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);
			var uv = UserVariable.CreateVector<float>(4, "myvar", vector);
			Assert.Equal(vector.ToArray(), uv.GetFunction()());
		}

		[Fact]
		public void DefaultValueIsCastCorrectly()
		{
			var defaultVal = new[] { 1, 2 };

			var variable = UserVariable.CreateVector<float>(2, "test", defaultVal);

			Assert.Equal(defaultVal.Select(x => (float)x), variable.GetFunction()());
		}
	}
}
