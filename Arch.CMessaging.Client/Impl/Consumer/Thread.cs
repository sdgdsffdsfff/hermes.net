using System;
using System.Threading.Tasks;

namespace Arch.CMessaging.Client.Impl.Consumer
{
    internal sealed class Thread:IDisposable
    {
        readonly ThreadPool _threadPool;
        public Thread(ThreadPool threadPool) {
            _threadPool = threadPool;
            //_isRun = false;
        }
        //private bool _isRun;
        public void Run(Action action)
        {
            if (action == null)
            {
                //_isRun = true;
                _threadPool.Release();
                return;
            }
            new Task(() =>
            {
                //_isRun = true;
                try
                {
                    action();
                }
                finally
                {
                    _threadPool.Release();
                }
            }).Start();
        }

        public void Dispose()
        {
            //if (!_isRun) _threadPool.Release();
        }
    }
}
