using System;

namespace GoPlay.Services.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class BeforeLoginAttribute : Attribute
    {
    }
}