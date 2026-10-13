using System;

namespace General.EventDispatcher
{
    public interface IEventDispatcherService : IService
    {
        void AddListener<T>(Action<T> listener) where T : ISignal;
        void RemoveListener<T>(Action<T> listener) where T : ISignal;
        void Dispatch(ISignal signal);
    }
}