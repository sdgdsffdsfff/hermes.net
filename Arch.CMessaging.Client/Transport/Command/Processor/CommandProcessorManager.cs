using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Client.Core.Config;
using Arch.CMessaging.Client.Core.Ioc;
using Arch.CMessaging.Client.Core.Collections;
using Freeway.Logging;
using System.Collections.Concurrent;
using Arch.CMessaging.Client.Core.Utils;

namespace Arch.CMessaging.Client.Transport.Command.Processor
{
    [Named(ServiceType = typeof(CommandProcessorManager))]
    public class CommandProcessorManager : IInitializable
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(CommandProcessorManager));

        [Inject]
        private CoreConfig config;
        [Inject]
        private ICommandProcessorRegistry registry;

        private ConcurrentDictionary<ICommandProcessor, ProducerConsumer<CommandProcessorContext>> executors = new ConcurrentDictionary<ICommandProcessor, ProducerConsumer<CommandProcessorContext>>();

        private ThreadSafe.Boolean stopped = new ThreadSafe.Boolean(false);

        private const int MAX_CAPACITY = int.MaxValue;

        public void Initialize()
        {
            HashSet<ICommandProcessor> cmdProcessors = registry.ListAllProcessors();

            foreach (ICommandProcessor cmdProcessor in cmdProcessors)
            {
                int threadCount = config.CommandProcessorThreadCount;

                System.Attribute[] attrs = System.Attribute.GetCustomAttributes(cmdProcessor.GetType());
                foreach (var attr in attrs)
                {
                    if (attr is SingleThreaded)
                    {
                        threadCount = 1;
                        break;
                    }
                }

                ProducerConsumer<CommandProcessorContext> executor = new ProducerConsumer<CommandProcessorContext>(MAX_CAPACITY, threadCount);
                executor.OnConsume += producer_OnConsume;
                executors.TryAdd(cmdProcessor, executor);
            }
        }

        void producer_OnConsume(object sender, ConsumeEventArgs e)
        {
            var ctx = (e.ConsumingItem as SingleConsumingItem<CommandProcessorContext>).Item;
            var command = ctx.Command;
            var commandType = command.Header.CommandType.Value;
            var processor = registry.FindProcessor(commandType);
            try
            {
                processor.Process(ctx);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        public void Offer(CommandProcessorContext ctx)
        {
            if (stopped.ReadFullFence())
            {
                return;
            }

            ICommand cmd = ctx.Command;
            CommandType type = cmd.Header.CommandType.Value;
            ICommandProcessor processor = registry.FindProcessor(type);
            if (processor == null)
            {
                log.Error(String.Format("Command processor not found for type {0}", type));
            }
            else
            {
                ProducerConsumer<CommandProcessorContext> executorService = null;
                executors.TryGetValue(processor, out executorService);

                if (executorService == null)
                {
                    throw new Exception(String.Format("No executor associated to processor {0}", processor.GetType()));
                }
                else
                {
                    executorService.Produce(ctx);
                }
            }
        }

        public void Stop()
        {
            if (stopped.CompareAndSet(false, true))
            {
                foreach (ProducerConsumer<CommandProcessorContext> executor in executors.Values)
                {
                    executor.Shutdown();
                }
            }
        }
    }
}
