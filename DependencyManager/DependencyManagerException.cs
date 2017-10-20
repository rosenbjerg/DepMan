using System;

namespace DepMan
{
    public class DependencyManagerException : Exception
    {
        internal DependencyManagerException(string msg) : base(msg)
        {

        }
    }
}