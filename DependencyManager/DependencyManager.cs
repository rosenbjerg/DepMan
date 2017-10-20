using System;
using System.Collections.Concurrent;
using System.Linq;

namespace DepMan
{
    /// <summary>
    /// A lightweight dependency injection manager
    /// </summary>
    public static class DependencyManager
    {
        /// <summary>
        /// Initialize the DependecyManager. Must be called before using Get method.<br/>
        /// The Register method will automatically initialize, but without looking for Implements attribute.<br/>
        /// Either use the ImplementsAttribute->Init->Get pattern, or the Register->Get pattern.
        /// </summary>
        /// <param name="findUsingAttribute">Set to false if you do not want use reflection to look for classes marked with Implements attribute.</param>
        public static void Init(bool findUsingAttribute = true)
        {
            lock (_initLock)
            {
                if (_initialized)
                    throw new DependencyManagerException("DependencyManager is already initialized. Only call this method once");
                _initialized = true;
                _map = new ConcurrentDictionary<Type, IImplementor>();
            }
            if (findUsingAttribute) ReflectionLoad();
        }

        private static ConcurrentDictionary<Type, IImplementor> _map;
        private static bool _initialized;
        private static readonly object _initLock = new object();

        private static void ReflectionLoad()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var ass in assembly)
            {
                var implementors = ass.GetTypes().Where(type => type.IsDefined(typeof(ImplementsAttribute), false));
                foreach (var impl in implementors)
                {
                    var attr = impl.GetCustomAttributes(false).First() as ImplementsAttribute;
                    var type = attr.IntefaceType;
                    if (!type.IsAssignableFrom(impl)) throw new DependencyManagerException(
                        $"The class {impl.Name} does not implement {type.Name}.");
                    if (_map.ContainsKey(type)) throw new DependencyManagerException(
                        $"{type.Name} already registered!");
                    if (!attr.SingleInstance)
                    {
                        _map.TryAdd(type, new FactoryImpl(impl));
                    }
                    else if (attr.InitOnRegister)
                    {
                        _map.TryAdd(type, new EagerSingletonImpl(impl));
                    }
                    else
                    {
                        _map.TryAdd(type, new LazySingletonImpl(impl));
                    }
                }
            }
        }


        /// <summary>
        /// Method to register a platform specific class to interface type.<br/>
        /// Calling this method before initializing, will call Init(false) automatically<br/>
        /// Only use this method if the class does NOT have 'Implements' attribute 
        /// </summary>
        /// <typeparam name="TInterface">The interface that is implemented, and also the retrievement-key</typeparam>
        /// <typeparam name="TImplentor">The class that implements the interface</typeparam>
        public static bool Register<TInterface, TImplentor>(bool initOnRegister = true, bool singleInstance = true)
            where TImplentor : class, TInterface, new()
        {
            if (!_initialized) Init(false);
            if (_map.ContainsKey(typeof(TInterface)))
                throw new DependencyManagerException($"{typeof(TInterface).Name} already registered!");
            IImplementor impl;

            if (!singleInstance)
                impl = new FactoryImpl(typeof(TImplentor));
            else if (initOnRegister)
                impl = new EagerSingletonImpl(new TImplentor());
            else
                impl = new LazySingletonImpl(typeof(TImplentor));

            return _map.TryAdd(typeof(TInterface), impl);
        }

        /// <summary>
        /// Method to register a specific class to interface type. <br/>
        /// This overload does not require the implementing class to have an parameterless constructor,
        /// but takes an instance of the class as parameter.
        /// </summary>
        /// <typeparam name="TInterface">The interface that is implemented, and also the retrievement-key</typeparam>
        /// <typeparam name="TImplentor">The class that implements the interface</typeparam>
        /// <param name="instance">An instance of the implementing class, so you can use constructor with parameters</param>
        public static bool Register<TInterface, TImplentor>(TImplentor instance)
            where TImplentor : class, TInterface
        {
            if (!_initialized) Init(false);
            if (_map.ContainsKey(typeof(TInterface)))
                throw new DependencyManagerException($"{typeof(TInterface).Name} already registered!");
            return _map.TryAdd(typeof(TInterface), new EagerSingletonImpl(instance));
        }

        /// <summary>
        /// Check whether an instance is registered to specific interface.<br/>
        /// This might be needed in some very specific cases
        /// </summary>
        /// <typeparam name="TInterface">The interface to look for</typeparam>
        /// <returns></returns>
        public static bool IsRegistered<TInterface>()
        {
            return _map != null && _map.ContainsKey(typeof(TInterface));
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
            IImplementor obj;
            if (!_map.TryGetValue(typeof(TInterface), out obj)) throw new DependencyManagerException(
                $"{typeof(TInterface).Name} not registered!");
            return (TInterface)obj.Get();
        }

        internal interface IImplementor
        {
            object Get();
        }

        internal class EagerSingletonImpl : IImplementor
        {
            private readonly object _obj;

            public EagerSingletonImpl(object obj)
            {
                _obj = obj;
            }

            public EagerSingletonImpl(Type objType)
            {
                _obj = Activator.CreateInstance(objType);
            }

            public object Get()
            {
                return _obj;
            }
        }

        internal class LazySingletonImpl : IImplementor
        {
            private object _obj;
            private readonly Type _type;

            public LazySingletonImpl(Type objType)
            {
                _type = objType;
            }

            public object Get()
            {
                return _obj ?? (_obj = Activator.CreateInstance(_type));
            }
        }

        internal class FactoryImpl : IImplementor
        {
            private readonly Type _type;

            public FactoryImpl(Type objType)
            {
                _type = objType;
            }

            public object Get()
            {
                return Activator.CreateInstance(_type);
            }
        }
    }
}