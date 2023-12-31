using System;

namespace GoPlay.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RequestAttribute : NameAttribute
    {
        public RequestAttribute(string name = null) : base(name)
        {
        }
    }
}