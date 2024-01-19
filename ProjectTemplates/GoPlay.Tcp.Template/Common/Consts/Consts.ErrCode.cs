namespace GoPlayProj;

public static partial class Consts
{
    public static class ErrCode
    {
        //Common
        public const string IDLE_TIMEOUT = "IDLE_TIMEOUT"; //长时间没有与服务器通讯
        public const string PARAMS_ERROR = "PARAMS_ERROR"; //参数错误
        public const string USER_NOT_FOUND = "USER_NOT_FOUND"; //用户不存在
        public const string DATA_NOT_FOUND = "DATA_NOT_FOUND"; //数据不存在

        //verify
        public const string TOKEN_EXPIRED = "TOKEN_EXPIRED"; //TOKEN过期 
        public const string LOGIN_REQUIRED = "LOGIN_REQUIRED"; //必须先登录才能进行该操作
    }
}