namespace GoPlayProj.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class IgnorePermitAttribute : Attribute
{
}