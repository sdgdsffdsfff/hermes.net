using System;
using System.Collections.Concurrent;

namespace Arch.CMessaging.Client.Impl.Consumer
{
    internal sealed class SingleTasks
    {
        private readonly ConcurrentDictionary<string, bool> _runDc = new ConcurrentDictionary<string, bool>();

        public SingleTask Run(string pullingRequestUri)
        {
            var isRun = !(_runDc.TryAdd(pullingRequestUri, true));
            return new SingleTask(pullingRequestUri, isRun, _runDc);
        }
    }

    internal sealed class SingleTask:IDisposable
    {
        private readonly string _pullingRequestUri;
        private readonly bool _isRun;
        private readonly ConcurrentDictionary<string, bool> _runDc;
        public SingleTask(string pullingRequestUri, bool isRun, ConcurrentDictionary<string, bool> runDc)
        {
            _pullingRequestUri = pullingRequestUri;
            _isRun = isRun;
            _runDc = runDc;
        }

        public bool IsRun
        {
            get { return _isRun; }
        }

        public void Dispose()
        {
            if (IsRun) return;
            bool isRun;
            _runDc.TryRemove(_pullingRequestUri, out isRun);
        }
    }
}
