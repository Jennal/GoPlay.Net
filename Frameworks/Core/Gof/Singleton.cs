using System;

namespace GoPlay.Core.Gof
{
    public class Singleton<T>
        where T : Singleton<T>, new()
    {
        protected static Lazy<T> s_instance = new Lazy<T>(() => new T());
        public static T Instance => s_instance.Value;
    }
}