using System;

namespace GoPlay.Services.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class NameAttribute : Attribute
    {
        public string Name;

        public NameAttribute(string name = null)
        {
            Name = name;
        }
    }
}