﻿using System;

namespace GoPlay
{
    public interface IMainThreadActionRunner
    {
        void Invoke(Action action);
    }
    
    public class MainThreadActionRunner : IMainThreadActionRunner
    {
        public virtual void Invoke(Action action)
        {
            action?.Invoke();
        }
    }
}