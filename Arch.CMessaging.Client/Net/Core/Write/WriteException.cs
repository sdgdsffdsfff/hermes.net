﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Arch.CMessaging.Client.Net.Core.Write
{
    [Serializable]
    public class WriteException : IOException
    {
        private readonly IList<IWriteRequest> _requests;

        public WriteException(IWriteRequest request)
        {
            _requests = AsRequestList(request);
        }

        public WriteException(IEnumerable<IWriteRequest> requests)
        {
            _requests = AsRequestList(requests);
        }

        protected WriteException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public IWriteRequest Request
        {
            get { return _requests[0]; }
        }

        public IEnumerable<IWriteRequest> Requests
        {
            get { return _requests; }
        }

        
        public override void GetObjectData(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        private static IList<IWriteRequest> AsRequestList(IWriteRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            List<IWriteRequest> requests = new List<IWriteRequest>(1);
            requests.Add(request);
            return requests.AsReadOnly();
        }

        private static IList<IWriteRequest> AsRequestList(IEnumerable<IWriteRequest> requests)
        {
            if (requests == null)
                throw new ArgumentNullException("requests");
            List<IWriteRequest> newRequests = new List<IWriteRequest>(requests);
            return newRequests.AsReadOnly();
        }
    }
}
