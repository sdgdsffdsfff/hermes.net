using Arch.CMessaging.Core.Content;
using Arch.CMessaging.Core.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arch.CMessaging.Client.Impl
{
    internal class Logg
    {
        const string key = "cooperation_id";
        const string value = "922105";
       public static void Write(
            Exception ex,
            LogLevel level,
            params KeyValue[] attrs)
        {
            Write(ex, level, null, attrs);
        }

        public static void Write(
            Exception ex, 
            LogLevel level, 
            string title = null,
            params KeyValue[] attrs)
        {
            KeyValue[] arr;
            if (attrs != null)
            {
                arr = new KeyValue[attrs.Length + 1];
                attrs.CopyTo(arr, 0);
                arr[arr.Length - 1] = new KeyValue { Key = key, Value = value };
            }
            else
            {
                arr = new[] { new KeyValue { Key = key, Value = value } };
            }
            Logger.Write(ex,level,title,arr);
        }

        public static void Write(
            string message,
            LogLevel level,
            params KeyValue[] attrs)
        {
            Write(message, level, null, attrs);
        }

        public static void Write(
            Func<string> msgBuilder,
            LogLevel level,
            params KeyValue[] attrs)
        {
            KeyValue[] arr;
            if (attrs != null)
            {
                arr = new KeyValue[attrs.Length + 1];
                attrs.CopyTo(arr, 0);
                arr[arr.Length - 1] = new KeyValue { Key = key, Value = value };
            }
            else
            {
                arr = new[] { new KeyValue { Key = key, Value = value } };
            }
            Logger.Write(msgBuilder, level, arr);
        }

        public static void Write(
            string message,
            LogLevel level,
            string title = null,
            params KeyValue[] attrs)
        {
            KeyValue[] arr;
            if (attrs != null)
            {
                arr = new KeyValue[attrs.Length + 1];
                attrs.CopyTo(arr, 0);
                arr[arr.Length - 1] = new KeyValue { Key = key, Value = value };
            }
            else
            {
                arr = new[] { new KeyValue { Key = key, Value = value } };
            }
            Logger.Write(message, level, title, arr);
        }
    }
}
