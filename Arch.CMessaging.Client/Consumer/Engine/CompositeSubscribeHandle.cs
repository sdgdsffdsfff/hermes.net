using System;
using System.Collections.Generic;

namespace Arch.CMessaging.Client.Consumer.Engine
{
    public class CompositeSubscribeHandle : ISubscribeHandle
    {
        public List<ISubscribeHandle> ChildHandles { get; private set; }

        public CompositeSubscribeHandle()
        {
            ChildHandles = new List<ISubscribeHandle>();
        }

        public void AddSubscribeHandle(ISubscribeHandle handle)
        {
            ChildHandles.Add(handle);
        }

        public void Close()
        {
            foreach (ISubscribeHandle child in ChildHandles)
            {
                child.Close();
            }
        }
    }
}

