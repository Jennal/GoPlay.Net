namespace GoPlay.Services.Core.Protocols
{
    public class IdLoopGenerator
    {
        public const uint INVALID = 0;
        
        protected uint m_current = 1;
        protected readonly uint m_max = byte.MaxValue;

        public IdLoopGenerator()
        {
        }

        public IdLoopGenerator(uint maxVal)
        {
            m_max = maxVal;
        }
        
        public uint Next()
        {
            var result = m_current;
            if (m_current >= m_max) m_current = 1;
            else m_current++;

            return result;
        }

        public void Reset()
        {
            m_current = 1;
        }
    }
}