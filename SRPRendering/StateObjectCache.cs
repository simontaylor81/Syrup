using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;

namespace SRPRendering
{
	public interface IStateObjectCache<StateType, StateDescriptorType> : IDisposable
		where StateDescriptorType : struct
		where StateType : IDisposable
	{
		StateType Get(StateDescriptorType desc);
	}

	static class StateObjectCache
	{
		// Convenience creation method.
		public static StateObjectCache<StateType, StateDescriptorType> Create<StateType, StateDescriptorType>(Func<StateDescriptorType, StateType> creationFunctor)
			where StateDescriptorType : struct
			where StateType : IDisposable
		{
			return new StateObjectCache<StateType, StateDescriptorType>(creationFunctor);
		}
	}

	// A cache for D3D state objects.
	class StateObjectCache<StateType, StateDescriptorType> : IStateObjectCache<StateType, StateDescriptorType>
		where StateDescriptorType : struct
		where StateType : IDisposable
	{
		public StateObjectCache(Func<StateDescriptorType, StateType> creationFunctor)
		{
			this.creationFunctor = creationFunctor;
		}

		// Get a state object for the given descriptor.
		public StateType Get(StateDescriptorType desc)
		{
			// Look for existing state object in the cache.
			StateType result;
			if (!cache.TryGetValue(desc, out result))
			{
				// Not found, so create a new one and add it.
				result = creationFunctor(desc);
				cache.Add(desc, result);
			}

			return result;
		}

		// Dispose of all state objects, and clear the cache.
		public void Dispose()
		{
			foreach (var state in cache.Values)
				state.Dispose();

			cache.Clear();
		}

		private Dictionary<StateDescriptorType, StateType> cache = new Dictionary<StateDescriptorType,StateType>();
		private Func<StateDescriptorType, StateType> creationFunctor;
	}
}
