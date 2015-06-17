﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace Arch.CMessaging.Client.Net.Core.Future
{
    
    public class CompositeIoFuture<TFuture> : DefaultIoFuture
        where TFuture : IoFuture
    {
        private Int32 _unnotified;
        private volatile Boolean _constructionFinished;
        public CompositeIoFuture(IEnumerable<TFuture> children)
            : base(null)
        {
            foreach (TFuture f in children)
            {
                f.Complete += OnComplete;
                Interlocked.Increment(ref _unnotified);
            }

            _constructionFinished = true;
            if (_unnotified == 0)
                Value = true;
        }

        private void OnComplete(Object sender, IoFutureEventArgs e)
        {
            if (Interlocked.Decrement(ref _unnotified) == 0 && _constructionFinished)
                Value = true;
        }
    }
}
