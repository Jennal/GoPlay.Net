// netstandard2.0 target 不含 IsExternalInit，polyfill 一份以启用 C# 9 record / init-only setter。
// analyzer 程序集内部可见，不对外暴露。

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit { }
}
#endif
