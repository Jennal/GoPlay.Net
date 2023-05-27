using System;

namespace GoPlay.Services.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class NotifyAttribute : NameAttribute
    {
        public NotifyAttribute(string name = null) : base(name)
        {
        }
    }
}