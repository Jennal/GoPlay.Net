using System;

namespace GoPlay.Services.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class HideInHandShakeAttribute : Attribute
    {
    }
}