using System;

namespace GoPlay.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class HideInHandShakeAttribute : Attribute
    {
    }
}