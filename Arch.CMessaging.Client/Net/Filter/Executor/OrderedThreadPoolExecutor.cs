using System;
using System.Collections.Concurrent;
using System.Text;
using Arch.CMessaging.Client.Net.Core.Session;
using System.Diagnostics;
using Arch.CMessaging.Client.Core.Collections;

namespace Arch.CMessaging.Client.Net.Filter.Executor
{
    public class OrderedThreadPoolExecutor : ThreadPoolExecutor, IoEventExecutor
    {
        private readonly AttributeKey TASKS_QUEUE = new AttributeKey(typeof(OrderedThreadPoolExecutor), "tasksQueue");
        private readonly IoEventQueueHandler _queueHandler;

        public OrderedThreadPoolExecutor()
            : this(null)
        { }

        public OrderedThreadPoolExecutor(IoEventQueueHandler queueHandler)
        {
            _queueHandler = queueHandler == null ? NoopIoEventQueueHandler.Instance : queueHandler;
        }

        public IoEventQueueHandler QueueHandler
        {
            get { return _queueHandler; }
        }

        
        public void Execute(IoEvent ioe)
        {
            IoSession session = ioe.Session;
            SessionTasksQueue sessionTasksQueue = GetSessionTasksQueue(session);
            Boolean exec;

            // propose the new event to the event queue handler. If we
            // use a throttle queue handler, the message may be rejected
            // if the maximum size has been reached.
            Boolean offerEvent = _queueHandler.Accept(this, ioe);

            if (offerEvent)
            {
                lock (sessionTasksQueue.syncRoot)
                {
                    sessionTasksQueue.tasksQueue.Enqueue(ioe);

                    if (sessionTasksQueue.processingCompleted)
                    {
                        sessionTasksQueue.processingCompleted = false;
                        exec = true;
                    }
                    else
                    {
                        exec = false;
                    }

#if DEBUG
                    Print(sessionTasksQueue.tasksQueue, ioe);
#endif

                    
                }

                if (exec)
                {
                    Execute(() =>
                    {
                        RunTasks(sessionTasksQueue);
                    });
                }

                _queueHandler.Offered(this, ioe);
            }
        }

        private SessionTasksQueue GetSessionTasksQueue(IoSession session)
        {
            SessionTasksQueue queue = session.GetAttribute<SessionTasksQueue>(TASKS_QUEUE);

            if (queue == null)
            {
                queue = new SessionTasksQueue();
                SessionTasksQueue oldQueue = (SessionTasksQueue)session.SetAttributeIfAbsent(TASKS_QUEUE, queue);
                if (oldQueue != null)
                    queue = oldQueue;
            }

            return queue;
        }

        private void RunTasks(SessionTasksQueue sessionTasksQueue)
        {
            IoEvent ioe;
            while (true)
            {
                lock (sessionTasksQueue.syncRoot)
                {
                    if (!sessionTasksQueue.tasksQueue.TryDequeue(out ioe))
                    {
                        sessionTasksQueue.processingCompleted = true;
                        break;
                    }
                }

                _queueHandler.Polled(this, ioe);
                ioe.Fire();
            }
        }

        private void Print(ThreadSafeQueue<IoEvent> queue, IoEvent ioe)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Adding event ")
                .Append(ioe.EventType)
                .Append(" to session ")
                .Append(ioe.Session.Id);
            Boolean first = true;
            sb.Append("\nQueue : [");
            foreach (IoEvent elem in queue)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(", ");
                }

                sb.Append(((IoEvent)elem).EventType).Append(", ");
            }
            sb.Append("]\n");
            Debug.WriteLine(sb.ToString());
        }

        class SessionTasksQueue
        {
            public readonly Object syncRoot = new Byte[0];
            
            /// A queue of ordered event waiting to be processed

            public readonly ThreadSafeQueue<IoEvent> tasksQueue = new ThreadSafeQueue<IoEvent>();
            
            /// The current task state
            
            public Boolean processingCompleted = true;
        }
    }
}
