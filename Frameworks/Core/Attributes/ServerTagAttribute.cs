using System;
using GoPlay.Services.Core.Protocols;

namespace GoPlay.Services.Core.Attributes
{
    // [Flags]
    // public enum ServerTag
    // {
    //     FrontEnd = 0x01,
    //     BackEnd  = 0x10,
    //     All = FrontEnd | BackEnd,
    // }
    
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ServerTagAttribute : Attribute
    {
        public ServerTag Tag;
        
        public ServerTagAttribute(ServerTag tag = ServerTag.All)
        {
            Tag = tag;
        }
    }
}