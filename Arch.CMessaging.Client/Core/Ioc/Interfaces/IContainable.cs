﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arch.CMessaging.Client.Core.Ioc
{
    public interface IContainable
    {
        void SetContainer(IVenusContainer container);
    }
}
