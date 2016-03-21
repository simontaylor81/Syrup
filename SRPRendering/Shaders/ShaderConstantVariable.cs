using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SRPCommon.UserProperties;
using SRPCommon.Scripting;
using SharpDX.D3DCompiler;
using SharpDX;
using SRPScripting.Shader;
using SRPScripting;

namespace SRPRendering.Shaders
{
	// Need out own shader variable type descriptor because the SlimDX one just references a D3D
	// object that needs to be cleaned up after compilation is complete.
	public struct ShaderVariableTypeDesc : IEquatable<ShaderVariableTypeDesc>
	{
		public ShaderVariableClass Class;
		public ShaderVariableType Type;
		public int Columns;
		public int Rows;

		public ShaderVariableTypeDesc(ShaderReflectionType type)
		{
			Class = type.Description.Class;
			Type = type.Description.Type;
			Columns = type.Description.ColumnCount;
			Rows = type.Description.RowCount;
		}

		public bool Equals(ShaderVariableTypeDesc other)
			=> Class == other.Class && Type == other.Type && Columns == other.Columns && Rows == other.Rows;
	}

	/*
	/// <summary>
	/// A single constant (non-resource) shader input.
	/// </summary>
	/// Observable fires when the value changes.
	public interface IShaderVariable : IObservable<Unit>
	{
		/// <summary>
		/// Name of the variable.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Descriptor storing information about the variable's type.
		/// </summary>
		ShaderVariableTypeDesc VariableType { get; }

		/// <summary>
		/// Optional bind to automatically set the variable to a certain value.
		/// </summary>
		IShaderVariableBind Bind { get; set; }

		/// <summary>
		/// If true, Bind was set automatically (and therefore can be overridden by the user manually).
		/// </summary>
		bool IsAutoBound { get; set; }

		T Get<T>() where T : struct;
		void Set<T>(T value) where T : struct;

		T GetComponent<T>(int index) where T : struct;
		void SetComponent<T>(int index, T value) where T : struct;

		void SetFromDynamic(dynamic value);

		/// <summary>
		/// Reset to initial value.
		/// </summary>
		void SetDefault();
	}
	*/

	/// <summary>
	/// Concrete implementation of IShaderVariable
	/// </summary>
	class ShaderConstantVariable : IShaderConstantVariable, IObservable<Unit>
	{
		// IShaderVariable interface.
		public string Name { get; }
		public bool IsNull => false;

		#region IShaderConstantVariable interface

		// Set directly to a given value.
		public void Set(dynamic value)
		{
			Binding = new ScriptShaderConstantVariableBinding(this, value);
			IsAutoBound = false;
		}

		// Bind to camera/scene property.
		public void Bind(ShaderConstantVariableBindSource bindSource)
		{
			Binding = new SimpleShaderConstantVariableBinding(this, bindSource);
			IsAutoBound = false;
		}

		// Bind to a material property.
		public void BindToMaterial(string param)
		{
			Binding = new MaterialShaderConstantVariableBinding(this, param);
			IsAutoBound = false;
		}

		// Mark the variable as script overridden, so it will not appear in the properties window.
		public void MarkAsScriptOverride()
		{
			Binding = new ScriptOverrideShaderConstantVariableBinding(this);
			IsAutoBound = false;
		}

		#endregion

		public ShaderVariableTypeDesc VariableType { get; }
		public bool IsAutoBound { get; private set; }

		private IShaderConstantVariableBinding _binding;
		public IShaderConstantVariableBinding Binding
		{
			get { return _binding; }
			private set
			{
				if (_binding != null && !IsAutoBound)
				{
					throw new ScriptException("Attempting to bind already bound shader variable: " + Name);
				}
				_binding = value;
			}
		}

		// Attempt to automatically bind this variable.
		public void AutoBind()
		{
			if (Binding == null)
			{
				// We auto bind variable with the same name as a bind source.
				ShaderConstantVariableBindSource source;
				if (Enum.TryParse(Name, out source))
				{
					Binding = new SimpleShaderConstantVariableBinding(this, source);
					IsAutoBound = true;
				}
			}
		}

		// For debugger prettiness.
		public override string ToString() => Name;

		// Get the current value of the variable.
		public T GetValue<T>() where T : struct
		{
			if (Marshal.SizeOf<T>() != data.Length)
				throw new ArgumentException("Given size does not match shader variable size.");

			data.Position = 0;
			return data.Read<T>();
		}

		// Set the value of the variable.
		public void SetValue<T>(T value) where T : struct
		{
			if (Marshal.SizeOf<T>() < data.Length)
				throw new ArgumentException(String.Format("Cannot set shader variable '{0}': given value is the wrong size.", Name));

			data.Position = 0;
			data.Write(value);

			bDirty = true;
			_subject.OnNext(Unit.Default);
		}

		// Get the current value of an individual component of the array.
		public T GetComponent<T>(int index) where T : struct
		{
			int componentSize = Marshal.SizeOf<T>();
			if (componentSize * (index + 1) > data.Length)
				throw new IndexOutOfRangeException();

			data.Position = index * componentSize;
			return data.Read<T>();
		}

		// Get the current value of an individual component of the array.
		public void SetComponent<T>(int index, T value) where T : struct
		{
			int componentSize = Marshal.SizeOf<T>();
			if (componentSize * (index + 1) > data.Length)
				throw new IndexOutOfRangeException();

			data.Position = index * componentSize;
			data.Write(value);

			bDirty = true;
			_subject.OnNext(Unit.Default);
		}

		// Set the value of the variable from a dynamic object.
		public void SetFromDynamic(dynamic value)
		{
			int numComponents = VariableType.Columns * VariableType.Rows;
			if (numComponents == 1)
			{
				// Treat as scalar for single component.
				SetComponent<float>(0, ScriptHelper.GuardedCast<float>(value));
			}
			else
			{
				// Treat value as vector, setting each component.
				for (int i = 0; i < numComponents; i++)
				{
					SetComponent<float>(i, (float)value[i]);
				}
			}
		}

		// Set original default value.
		public void SetDefault()
		{
			data.Position = 0;
			data.Write(initialValue, 0, initialValue.Length);
			bDirty = true;
		}

		// Reset to initial state.
		public void Reset()
		{
			SetDefault();
			_binding = null;
		}

		// IObservable interface
		public IDisposable Subscribe(IObserver<Unit> observer)
		{
			return _subject.Subscribe(observer);
		}

		// Constructors.
		public ShaderConstantVariable(ShaderReflectionVariable shaderVariable)
		{
			Name = shaderVariable.Description.Name;
			VariableType = new ShaderVariableTypeDesc(shaderVariable.GetVariableType());
			IsAutoBound = false;

			offset = shaderVariable.Description.StartOffset;
			data = new DataStream(shaderVariable.Description.Size, true, true);

			if (shaderVariable.Description.DefaultValue != (IntPtr)0)
			{
				data.WriteRange(shaderVariable.Description.DefaultValue, shaderVariable.Description.Size);
			}
			else
			{
				// No initial contents: zero fill.
				// Not sure if this is necessary, but better safe than sorry.
				for (int i = 0; i < shaderVariable.Description.Size; i++)
					data.WriteByte(0);
			}

			// Save initial state so we can reset to it.
			initialValue = new byte[shaderVariable.Description.Size];
			data.Position = 0;
			data.Read(initialValue, 0, shaderVariable.Description.Size);
		}

		// Write the value into the destination stream at the correct offset.
		public bool WriteToBuffer(DataStream dest)
		{
			if (bDirty)
			{
				dest.Position = offset;
				data.Position = 0;
				data.CopyTo(dest);

				bDirty = false;
				return true;
			}
			return false;
		}

		private int offset;
		private DataStream data;
		private bool bDirty = true;
		private byte[] initialValue;
		private Subject<Unit> _subject = new Subject<Unit>();
	}
}
