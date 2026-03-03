using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Infrastructure
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, List<object>> _services = new(64);
        
        public static void Bind<T>(T instance) where T : class
        {
#if UNITY_EDITOR
            if (instance == null)
            {
                Debug.LogError($"ServiceLocator: Tried to register null for {typeof(T).Name}");
                return;
            }
#endif
            if (!_services.ContainsKey(typeof(T)))
                _services.Add(typeof(T), new List<object>());
            _services[typeof(T)].Add(instance);
        }

        public static bool TryResolve<T>(out T service) where T : class
        {
            service = null;
            var type = typeof(T);
            if (!_services.ContainsKey(type))
                return false;
            service = (T)(_services[type][0]);
            return true;
        }
        
        public static T Resolve<T>() where T : class
        {
#if UNITY_EDITOR
            if (!_services.ContainsKey(typeof(T)))
            {
                Debug.LogError($"ServiceLocator: Tried to get {typeof(T).Name}, but no service is registered");
                return null;
            }
#endif
            var services = _services[typeof(T)];
            return (T)services[0];
        }
        
        public static void ResolveList<T>(List<T> buffer, bool optional = false) where T : class
        {
            buffer.Clear();
            if (!_services.ContainsKey(typeof(T)))
            {
                if (optional == true)
                    return;
#if UNITY_EDITOR
                else
                    Debug.LogError($"ServiceLocator: Tried to get List of {typeof(T).Name}, but no service is registered");
#endif
            }
            var services = _services[typeof(T)];
            for (int i = 0; i < services.Count; i++)
            {
                buffer.Add((T)services[i]);
            }
        }
        
        public static void BindInterfaces(object instance, bool includeSelf = false)
        {
#if UNITY_EDITOR
            if (instance == null)
            {
                Debug.LogError("ServiceLocator: RegisterAllInterfaces got null");
                return;
            }
#endif
            var type = instance.GetType();
            var interfaces = type.GetInterfaces();

            for (int i = 0; i < interfaces.Length; i++)
            {
                var it = interfaces[i];
                
                //if (it == typeof(IDisposable))
                //    continue;
                
                if (!_services.ContainsKey(it))
                    _services.Add(it, new List<object>());
                _services[it].Add(instance);
            }
            
            if (includeSelf == true)
            {
                if (!_services.ContainsKey(type))
                    _services.Add(type, new List<object>());
                _services[type].Add(instance);
            }
        }
        
        public static void Bind<T1, T2>(object instance)
            where T1 : class where T2 : class
        {
            Bind((T1)instance);
            Bind((T2)instance);
        }

        public static void Bind<T1, T2, T3>(object instance)
            where T1 : class where T2 : class where T3 : class
        {
            Bind((T1)instance);
            Bind((T2)instance);
            Bind((T3)instance);
        }

        public static void Bind<T1, T2, T3, T4>(object instance)
            where T1 : class where T2 : class where T3 : class where T4 : class
        {
            Bind((T1)instance);
            Bind((T2)instance);
            Bind((T3)instance);
            Bind((T4)instance);
        }
    }
}