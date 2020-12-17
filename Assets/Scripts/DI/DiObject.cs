﻿using Entities;

namespace DI
{
    public class DiObject : GameObservable
    {
        private bool m_WasUnregistered;
        
        protected DiObject()
        {
            ContainersManager.Instance.RegisterObject(this);
        }

        protected void Unregister()
        {
            if (m_WasUnregistered)
                return;
            ContainersManager.Instance.UnregisterObject(this);
            m_WasUnregistered = true;
        }
    }
}