using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using SlimDX;
using SRPCommon.Util;
using Microsoft.CSharp.RuntimeBinder;

namespace SRPCommon.Scripting
{
	public class ScriptHelper
	{
		public static ScriptHelper Instance => instance;

		public ScriptEngine Engine { get; set; }
		public ObjectOperations Operations => Engine.Operations;

		// If x is a function, execute it and return the result. Otherwise just return x.
		public object ResolveFunction(object x)
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

		public static Color3 ConvertToColor3(dynamic x)
			=> RunGuarded(() => new Color3((float)x[0], (float)x[1], (float)x[2]));

		public static Color4 ConvertToColor4(dynamic x)
			// Color4 constructor takes A, R, G, B.
			=> RunGuarded(() => new Color4((float)x[3], (float)x[0], (float)x[1], (float)x[2]));

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
		public void CheckConvertibleFloat(object x, string description)
		{
			if (x == null || (!CanConvert<float>(x) && !CanConvert<Func<float>>(x)))
			{
				// Not convertible, so throw exception so the user will get an error message.
				throw new ScriptException(
					string.Format("{0} must be a float, or a zero-argument function returning float. Got '{1}'.",
					description, x != null ? x.ToString() : "null"));
			}
		}

		public void CheckConvertibleFloatList(dynamic x, int numComponents, string description)
		{
			if (numComponents == 1)
			{
				CheckConvertibleFloat(x, description);
			}
			else
			{
				if (x != null)
				{
					// Is the value convertible to a list?
					IEnumerable<object> list;
					if (TryConvert<IEnumerable<object>>(x, out list))
					{
						// Check it has at least enough components.
						if (list.Count() >= numComponents)
						{
							// Try to convert each element to float.
							if (list.All(element => CanConvert<float>(element)))
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

		private bool CanConvert<T>(object x)
		{
			T dummy;
			return TryConvert(x, out dummy);
		}

		private bool TryConvert<T>(object x, out T result)
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

		public void LogScriptError(Exception ex)
		{
			OutputLogger.Instance.LogLine(LogCategory.Script, "Script execution failed.");

			var eo = Engine.GetService<ExceptionOperations>();

			if (ex.InnerException != null)
			{
				string error = eo.FormatException(ex.InnerException);
				OutputLogger.Instance.LogLine(LogCategory.Script, ex.Message);
				OutputLogger.Instance.LogLine(LogCategory.Script, error);
			}
			else
			{
				string error = eo.FormatException(ex);
				OutputLogger.Instance.LogLine(LogCategory.Script, error);
			}
		}

		private static ScriptHelper instance = new ScriptHelper();
	}
}
