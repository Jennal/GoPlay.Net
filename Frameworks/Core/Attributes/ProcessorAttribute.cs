using System;

namespace GoPlay.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ProcessorAttribute : NameAttribute
    {
        public ProcessorAttribute(string name = null) : base(name)
        {
        }
    }
}