namespace GoPlay.Core.Protocols
{
    public partial class Status
    {
        public static readonly Status Success = new Status
        {
            Code = StatusCode.Success,
            Message = ""
        };

        public static Status Fail(string msg)
        {
            return new Status
            {
                Code = StatusCode.Failed,
                Message = msg,
            };
        }

        public static Status Error(string msg)
        {
            return new Status
            {
                Code = StatusCode.Error,
                Message = msg,
            };
        }
    }
}