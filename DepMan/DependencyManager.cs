using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Rosenbjerg.DepMan
{
    public static class DependencyManager
    {
        /// <summary>
        /// Initialize the DependecyManager. Must be called before using Get method.
        /// The Register method will automatically initialize, but without looking for Implements attribute.
        /// Either use the ImplementsAttribute->Init->Get pattern, or the Register->Get pattern.
        /// </summary>
        /// <param name="findUsingAttribute">Set to false if you do not want use reflection to look for classes marked with Implements attribute.</param>
        public static void Init(bool findUsingAttribute = true)
        {
            lock (_lock)
            {
                if (_initialized)
                    throw new DependencyManagerException("DependencyManager is already initialized. Only call this method once");
                _initialized = true;
                _map = new ConcurrentDictionary<Type, Implementor>();
            }
            if (findUsingAttribute) ReflectionLoad();
        }

        private static ConcurrentDictionary<Type, Implementor> _map;
        private static bool _initialized;
        private static readonly object _lock = new object();

        private static void ReflectionLoad()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var ass in assembly)
            {
                var implementors = ass.GetTypes().Where(type => type.IsDefined(typeof(ImplementsAttribute), false));
                foreach (var impl in implementors)
                {
                    var attr = (impl.GetCustomAttributes(false).First() as ImplementsAttribute);
                    var type = attr.IntefaceType;
                    if (!type.IsAssignableFrom(impl)) throw new DependencyManagerException(
                        string.Format("The class {0} does not implement {1}.", impl.Name, type.Name));
                    if (_map.ContainsKey(type)) throw new DependencyManagerException(
                        string.Format("{0} already registered!", type.Name));
                    if (attr.InitOnRegister)
                    {
                        var obj = Activator.CreateInstance(impl);
                        _map.TryAdd(type, new Implementor(obj, attr.SingleInstance));
                    }
                    else
                    {
                        _map.TryAdd(type, new Implementor(impl, attr.SingleInstance));
                    }
                }
            }
        }


        /// <summary>
        /// Method to register a platform specific class to interface type.
        /// Calling this method before initializing, will call Init(false) automatically
        /// Only use this method if the class does NOT have 'Implements' attribute 
        /// </summary>
        /// <typeparam name="TInterface">The interface that is implemented, and also the retrievement-key</typeparam>
        /// <typeparam name="TImplentor">The class that implements the interface</typeparam>
        public static void Register<TInterface, TImplentor>(bool initOnRegister = true, bool singleInstance = true)
            where TImplentor : class, TInterface, new()
        {
            if (!_initialized) Init(false);
            if (_map.ContainsKey(typeof(TInterface)))
                throw new DependencyManagerException(string.Format("{0} already registered!", typeof(TInterface).Name));

            if (initOnRegister)
            {
                var impl = new Implementor(new TImplentor(), singleInstance);
                _map.TryAdd(typeof(TInterface), impl);
            }
            else
            {
                var impl = new Implementor(typeof(TImplentor), singleInstance);
                _map.TryAdd(typeof(TInterface), impl);
            }
        }

        /// <summary>
        /// Method to register a specific class to interface type.
        /// This overload does not require the implementing class to have an parameterless constructor,
        /// but takes an instance of the class as parameter.
        /// </summary>
        /// <typeparam name="TInterface">The interface that is implemented, and also the retrievement-key</typeparam>
        /// <typeparam name="TImplentor">The class that implements the interface</typeparam>
        /// <param name="instance">An instance of the implementing class, so you can use constructor with parameters</param>
        public static void Register<TInterface, TImplentor>(TImplentor instance, bool singleInstance = true)
            where TImplentor : class, TInterface
        {
            if (!_initialized) Init(false);
            if (_map.ContainsKey(typeof(TInterface)))
                throw new DependencyManagerException(string.Format("{0} already registered!", typeof(TInterface).Name));

            _map.TryAdd(typeof(TInterface), new Implementor(instance, singleInstance));
        }

        /// <summary>
        /// Retrieve registered object based on interface
        /// </summary>
        /// <typeparam name="TInterface">The interface of the class wanted</typeparam>
        /// <returns></returns>
        public static TInterface Get<TInterface>()
        {
            if (!_initialized)
                throw new DependencyManagerException("DependencyManager is not initialized. Call Init method");
            Implementor obj;
            if (!_map.TryGetValue(typeof(TInterface), out obj)) throw new DependencyManagerException(
                string.Format("{0} not registered!", typeof(TInterface).Name));
            return (TInterface)obj.Get();
        }

        internal class Implementor
        {
            private object _obj;
            private readonly Type _type;
            private readonly bool _singleInstance;

            public Implementor(object obj, bool singleInstance)
            {
                _obj = obj;
                _type = obj.GetType();
                _singleInstance = singleInstance;
            }

            public Implementor(Type timplementor, bool singleInstance)
            {
                _type = timplementor;
                _singleInstance = singleInstance;
            }

            public object Get()
            {
                if (!_singleInstance || _obj == null) _obj = Activator.CreateInstance(_type);
                return _obj;
            }
        }
    }
}