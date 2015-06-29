﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arch.CMessaging.Client.Transport.Command.Processor
{
    public interface ICommandProcessor
    {
        List<CommandType> commandTypes();
        void Process(CommandProcessorContext ctx);
    }
}
