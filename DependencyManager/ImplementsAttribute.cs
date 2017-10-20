using System;

namespace DepMan
{
    /// <summary>
    /// Mark that this class implements a specific interface to automatically register it in DependencyManager with the interface type as key.
    /// </summary>  
    [AttributeUsage(AttributeTargets.Class)]
    public class ImplementsAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type">The type of the interface that is implemented by the class</param>
        /// <param name="initOnRegister">Whether it should be instantiated on registration, or on first use</param>
        /// <param name="singleInstance">Whether the created instance your be kept and reused</param>
        public ImplementsAttribute(Type type, bool initOnRegister = true, bool singleInstance = true)
        {
            IntefaceType = type;
            InitOnRegister = initOnRegister;
            SingleInstance = singleInstance;
        }

        /// <summary>
        /// Whether the created instance your be kept and reused
        /// </summary>
        public bool SingleInstance { get; private set; }

        /// <summary>
        /// Whether it should be instantiated on registration, or on first use
        /// </summary>
        public bool InitOnRegister { get; private set; }

        /// <summary>
        /// The type of the interface that is implemented by the class
        /// </summary>
        public Type IntefaceType { get; private set; }
    }
}
