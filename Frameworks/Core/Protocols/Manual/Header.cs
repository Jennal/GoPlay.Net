namespace GoPlay.Core.Protocols
{
    public partial class Header
    {
        public uint ClientId;
        
        public static Header Parse(byte[] bytes)
        {
            var header = Parser.ParseFrom(bytes);
            if (header.Status == null)
            {
                header.Status = new Status();
            }

            return header;
        }
    }
}