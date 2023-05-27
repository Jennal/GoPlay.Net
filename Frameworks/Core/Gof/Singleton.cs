using System;

namespace GoPlay.Services.Core.Gof
{
    public class Singleton<T>
        where T : Singleton<T>, new()
    {
        protected static T s_instance;
        public static T Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = new T();
                }

                return s_instance;
            }
        }

        protected Singleton()
        {
            if (s_instance != null) throw new Exception($"Type {typeof(T).Name} is singleton, can't instantiate twice!");
            s_instance = (T)this;
        }
    }
}