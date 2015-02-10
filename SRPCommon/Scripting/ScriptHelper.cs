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
		public static ScriptHelper Instance { get { return instance; } }

		public ScriptEngine Engine { get; set; }
		public ObjectOperations Operations { get { return Engine.Operations; } }

		// If x is a function, execute it and return the result. Otherwise just return x.
		public dynamic ResolveFunction(dynamic x)
		{
			if (x != null && Operations.IsCallable(x))
			{
				// Catch exceptions caused by the function not taking zero arguments.
				try
				{
					return x();
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
		public static T GuardedCast<T>(dynamic x)
		{
			// Python floats are actually doubles, so we need a cast.
			return RunGuarded(() => (T)x);
		}

		// Helper functions that convert a dynamic object into various vector/colour types.
		public static Vector2 ConvertToVector2(dynamic x)
		{
			// Scripts pass vectors as tuples, so we extract the values to form the vector.
			return RunGuarded(() => new Vector2((float)x[0], (float)x[1]));
		}
		public static Vector3 ConvertToVector3(dynamic x)
		{
			// Scripts pass vectors as tuples, so we extract the values to form the vector.
			return RunGuarded(() => new Vector3((float)x[0], (float)x[1], (float)x[2]));
		}
		public static Vector4 ConvertToVector4(dynamic x)
		{
			// Scripts pass vectors as tuples, so we extract the values to form the vector.
			return RunGuarded(() => new Vector4((float) x[0], (float) x[1], (float) x[2], (float) x[3]));
		}
		public static Color3 ConvertToColor3(dynamic x)
		{
			return RunGuarded(() => new Color3((float)x[0], (float)x[1], (float)x[2]));
		}
		public static Color4 ConvertToColor4(dynamic x)
		{
			// Color4 constructor takes A, R, G, B.
			return RunGuarded(() => new Color4((float)x[3], (float)x[0], (float)x[1], (float)x[2]));
		}

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
		public void CheckConvertibleFloat(dynamic x, string description)
		{
			if (x != null)
			{
				// Is the value directly convertible to a float?
				float dummy;
				if (Operations.TryConvertTo<float>(x, out dummy))
					return;

				// If not a float, is it a function (can't tell more than this unfortunately).
				if (Operations.IsCallable(x))
					return;
			}

			// Not convertible, so throw exception so the user will get an error message.
			throw new ScriptException(String.Format("{0} must be a float, or a zero-argument function returning float. Got '{1}'.",
				description, x != null ? x.ToString() : "null"));
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
					IEnumerable<dynamic> list;
					if (Operations.TryConvertTo<IEnumerable<object>>(x, out list))
					{
						// Check it has at least enough components.
						if (list.Count() >= numComponents)
						{
							// Try to convert each element to float.
							bool allFloat = true;
							foreach (dynamic entry in list)
							{
								float dummyFloat;
								if (!Operations.TryConvertTo<float>(entry, out dummyFloat))
								{
									allFloat = false;
									break;
								}
							}

							if (allFloat)
								return;
						}
					}

					// Is it a function (can't tell more than this unfortunately).
					if (Operations.IsCallable(x))
						return;
				}

				// Not convertible, so report error to user.
				throw new ScriptException(
					String.Format("{0} must be a tuple of floats, or a zero-argument function returning a tuple of floats, of at least {1} elements. Got '{2}'.",
					description, numComponents, x != null ? x.ToString() : "null"));
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
