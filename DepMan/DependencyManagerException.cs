using System;

namespace Rosenbjerg.DepMan
{
    public class DependencyManagerException : Exception
    {
        internal DependencyManagerException(string msg) : base(msg)
        {

        }
    }
}