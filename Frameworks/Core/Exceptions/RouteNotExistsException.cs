using System;

namespace GoPlay.Services.Exceptions
{
    public class RouteNotExistsException : Exception
    {
        private uint m_routeId;
        public uint RouteId => m_routeId;

        private string m_message;
        public override string Message => m_message;

        public RouteNotExistsException(uint routeId)
        {
            m_routeId = routeId;
            m_message = $"RouteNotExistsException: RouteId={routeId} not exists!";
        }
    }
}