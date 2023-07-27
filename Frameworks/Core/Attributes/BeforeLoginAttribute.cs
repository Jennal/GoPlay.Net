using System;

namespace GoPlay.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class BeforeLoginAttribute : Attribute
    {
    }
}