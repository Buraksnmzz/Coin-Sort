using System;
using System.Collections.Generic;

namespace General
{
    public class ServiceLocator
    {
        private static Dictionary<Type, IService> _services = new Dictionary<Type, IService>();


        public static void Register<T>(T service) where T : IService
        {
            Type type = typeof(T);

            if (_services.ContainsKey(type))
            {
                throw new InvalidOperationException($"Service of type {type} is already registered.");
            }
            _services.Add(type, service);
        }

        public static T GetService<T>() where T : IService
        {
            Type type = typeof(T);

            if (_services.TryGetValue(type, out var service))
            {
                return (T)service;
            }
            throw new KeyNotFoundException($"Service of type {type} not found. Make sure to register it first.");
        }
    }
}