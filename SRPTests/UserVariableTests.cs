using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRPRendering;
using SRPScripting;
using Xunit;
using SlimDX;
using SRPCommon.UserProperties;

namespace SRPTests
{
	public class UserVariableTests
	{
		[Theory]
		[InlineData(UserVariableType.Float, 3.142f, 2.718f)]
		[InlineData(UserVariableType.Bool, true, false)]
		public void ScalarVariable<T>(UserVariableType type, T defaultValue, T otherValue)
		{
			var uv = UserVariable.Create("myvar", type, defaultValue);
			Func<T> getValue = uv.GetFunction();

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
		[InlineData(UserVariableType.Float2, 0.0f, new[] { 1.0f, 2.0f }, new[] { 100.0f, 200.0f })]
		[InlineData(UserVariableType.Float3, 0.0f, new[] { 1.0f, 2.0f, 3.0f }, new[] { 100.0f, 200.0f, 300.0f })]
		[InlineData(UserVariableType.Float4, 0.0f, new[] { 1.0f, 2.0f, 3.0f, 4.0f }, new[] { 100.0f, 200.0f, 300.0f, 400.0f })]
		public void VectorVariable<T>(UserVariableType type, T dummy, T[] defaultValue, T[] otherValue)
		{
			var uv = UserVariable.Create("myvar", type, defaultValue);

			// Basics
			Assert.Equal("myvar", uv.Name);
			Assert.False(uv.IsReadOnly);

			// Must be a scalar property of the right type.
			Assert.IsAssignableFrom<IVectorProperty>(uv);
			var prop = (IVectorProperty)uv;

			Assert.Equal(defaultValue.Length, prop.NumComponents);

			// Check type of each component.
			for (int i = 0; i < prop.NumComponents; i++)
			{
				// All user variables are vectors of scalars (no vectors-of-vectors).
				Assert.IsAssignableFrom<IScalarProperty<T>>(prop.GetComponent(i));
			}

			// Check composite value is correct.
			AssertEqualDynamicEnumerable(defaultValue, uv.GetFunction()());

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
			AssertEqualDynamicEnumerable(otherValue, uv.GetFunction()());

			// Check each component was changed.
			for (int i = 0; i < prop.NumComponents; i++)
			{
				Assert.Equal(otherValue[i], ((IScalarProperty<T>)prop.GetComponent(i)).Value);
			}
		}

		private void AssertEqualDynamicEnumerable<T>(T[] expected, IEnumerable<dynamic> actual)
		{
			Assert.Equal(expected.Length, actual.Count());
			for (int i = 0; i < expected.Length; i++)
			{
				Assert.Equal<T>(expected[i], actual.ElementAt(i));
			}
		}
	}
}
