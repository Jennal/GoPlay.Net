// netstandard2.0 target 不含 IsExternalInit，但 C# 9 的 record 和 init-only setter 语法需要它。
// 生成器项目不对外暴露类型，polyfill 只存在于本程序集内。

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit { }
}
#endif
