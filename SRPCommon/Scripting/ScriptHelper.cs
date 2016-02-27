using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using SRPCommon.Util;
using Microsoft.CSharp.RuntimeBinder;
using System.Collections;
using System.Numerics;

namespace SRPCommon.Scripting
{
	public static class ScriptHelper
	{
		// If x is a function, execute it and return the result. Otherwise just return x.
		public static object ResolveFunction(object x)
		{
			Func<object> func;
			if (x != null && TryConvert(x, out func))
			{
				// Catch exceptions caused by the function not taking zero arguments.
				try
				{
					return func();
				}
				catch (ArgumentTypeException ex)
				{
					throw new ScriptException("Functions pass to render	interface must take zero arguments.", ex);
				}
			}
			return x;
		}

		// Helper function for casting to a concrete type, wrapped in try/catch to convert the
		// weird script engine exceptions into a nicer ScriptException.
		public static T GuardedCast<T>(dynamic x) => RunGuarded(() => (T)x);

		// Helper functions that convert a dynamic object into various vector/colour types.
		public static Vector2 ConvertToVector2(dynamic x)
			// Scripts pass vectors as tuples, so we extract the values to form the vector.
			=> RunGuarded(() => new Vector2((float)x[0], (float)x[1]));

		public static Vector3 ConvertToVector3(dynamic x)
			// Scripts pass vectors as tuples, so we extract the values to form the vector.
			=> RunGuarded(() => new Vector3((float)x[0], (float)x[1], (float)x[2]));

		public static Vector4 ConvertToVector4(dynamic x)
			// Scripts pass vectors as tuples, so we extract the values to form the vector.
			=> RunGuarded(() => new Vector4((float) x[0], (float) x[1], (float) x[2], (float) x[3]));

		// Helper method for running potentially throwy code, throwing a ScriptException if something bad happen.
		private static T RunGuarded<T>(Func<T> func, [CallerMemberName] string context = null)
		{
			Debug.Assert(context != null);

			try
			{
				return func();
			}
			catch (Exception ex)
			{
				throw new ScriptException("Invalid parameters for " + context, ex);
			}
		}

		// Helper functions that determine if the given dynamic can be converted to a specific type.
		public static void CheckConvertibleFloat(object x, string description)
		{
			if (x == null || (!CanConvert<float>(x) && !CanConvert<Func<float>>(x)))
			{
				// Not convertible, so throw exception so the user will get an error message.
				throw new ScriptException(
					string.Format("{0} must be a float, or a zero-argument function returning float. Got '{1}'.",
					description, x != null ? x.ToString() : "null"));
			}
		}

		public static void CheckConvertibleFloatList(object x, int numComponents, string description)
		{
			if (numComponents == 1)
			{
				CheckConvertibleFloat(x, description);
			}
			else
			{
				// Strings can be converted to IEnumerables of chars, which can be
				// converted to floats, so will pass all these tests. However, it's
				// probably not what the user wanted, so handle it explicitly here.
				if (x != null && !(x is string))
				{
					// Is the value convertible to a list?
					// Use non-generic IEnumerable as it's oddly more forgiving.
					IEnumerable list;
					if (TryConvert(x, out list))
					{
						// Check it has at least enough components.
						var objList = list.Cast<object>();
						if (objList.Count() >= numComponents)
						{
							// Try to convert each element to float.
							if (objList.All(element => CanConvert<float>(element)))
							{
								return;
							}
						}
					}

					// Is it a function?
					if (CanConvert<Func<float>>(x))
					{
						return;
					}
				}

				// Not convertible, so report error to user.
				throw new ScriptException(
					String.Format("{0} must be a tuple of floats, or a zero-argument function returning a tuple of floats, of at least {1} elements. Got '{2}'.",
					description, numComponents, x != null ? x.ToString() : "null"));
			}
		}

		public static bool CanConvert<T>(object x)
		{
			T dummy;
			return TryConvert(x, out dummy);
		}

		public static bool TryConvert<T>(object x, out T result)
		{
			try
			{
				// Forcing to dynamic first lets the DLR kick in and do its thing,
				// which allows a wider range of conversions to be performed.
				result = (T)(dynamic)x;
				return true;
			}
			catch (Exception)
			{
				result = default(T);
				return false;
			}
		}
	}
}
