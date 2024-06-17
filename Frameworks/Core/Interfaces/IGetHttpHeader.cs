using System.Collections.Generic;

namespace GoPlay.Core.Interfaces
{
    public interface IGetHttpHeader
    {
        Dictionary<string, string> GetHttpHeaders(uint clientId);
        string GetHttpHeader(uint clientId, string header);
        bool HasHttpHeader(uint clientId, string header);
    }
}