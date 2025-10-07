using System.Runtime.CompilerServices;
using System.Threading;

namespace GoPlay.Core.Protocols
{
    public class IdLoopGenerator
    {
        public const uint INVALID = 0;

        protected uint m_current = 1;
        protected readonly uint m_max = uint.MaxValue;

        public IdLoopGenerator()
        {
        }

        public IdLoopGenerator(uint maxVal)
        {
            m_max = maxVal;
        }

        public uint Next()
        {
            while (true)
            {
                var current = Volatile.Read(ref m_current);
                var next = current >= m_max ? 1u : current + 1u;

                var original = (uint)Interlocked.CompareExchange(
                    ref Unsafe.As<uint, int>(ref m_current),
                    (int)next,
                    (int)current);

                if (original == current)
                {
                    return current;
                }
            }
        }

        public void Reset()
        {
            m_current = 1;
        }
    }
}