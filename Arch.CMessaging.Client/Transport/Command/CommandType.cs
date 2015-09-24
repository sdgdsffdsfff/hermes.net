using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arch.CMessaging.Client.Core.Utils;

namespace Arch.CMessaging.Client.Transport.Command
{
    public class CommandTypeToCommand
    {
        public static KeyValuePair<CommandType, Type> FindCommandInfoByCode(int code, int version)
        {
            KeyValuePair<CommandType, Type> result;
            bool found = commandTypes.TryGetValue(new Pair<int, int>(code, version), out result);

            if (!found)
            {
                throw new Exception(string.Format("Unknown command type {0}", code));
            }

            return result;
        }

        private static Dictionary<Pair<int, int>, KeyValuePair<CommandType, Type>> commandTypes = new Dictionary<Pair<int, int>, KeyValuePair<CommandType, Type>>()
        {
            { new Pair<int, int>(101, 1), new KeyValuePair<CommandType, Type>(CommandType.MessageSend, typeof(SendMessageCommand)) },
            { new Pair<int, int>(102, 2), new KeyValuePair<CommandType, Type>(CommandType.MessageAck, typeof(AckMessageCommandV2)) },
            { new Pair<int, int>(103, 2), new KeyValuePair<CommandType, Type>(CommandType.MessagePull, typeof(PullMessageCommandV2)) },
            { new Pair<int, int>(104, 1), new KeyValuePair<CommandType, Type>(CommandType.QueryOffset, typeof(QueryOffsetCommand)) },
            { new Pair<int, int>(201, 1), new KeyValuePair<CommandType, Type>(CommandType.AckMessageSend, typeof(SendMessageAckCommand)) },
            { new Pair<int, int>(301, 1), new KeyValuePair<CommandType, Type>(CommandType.ResultMessageSend, typeof(SendMessageResultCommand)) },
            { new Pair<int, int>(302, 2), new KeyValuePair<CommandType, Type>(CommandType.ResultMessagePull, typeof(PullMessageResultCommandV2)) },
            { new Pair<int, int>(303, 1), new KeyValuePair<CommandType, Type>(CommandType.ResultQueryOffset, typeof(QueryOffsetResultCommand)) }
        };
    }

    public enum CommandType
    {
        Dummy = 0,
        MessageSend = 101,
        MessageAck = 102,
        MessagePull = 103,
        QueryOffset = 104,
        AckMessageSend = 201,
        ResultMessageSend = 301,
        ResultMessagePull = 302,
        ResultQueryOffset = 303
    }
}
