using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arch.CMessaging.Client.Transport.Command
{
    public static class CommandTypeExtension
    {
        public static int ToInt(this CommandType? commandType)
        {
            if (commandType == null)
                return -1;
            return Convert.ToInt32(commandType);
        }

        public static CommandType? ToCommandType(this int type, int version)
        {
            CommandType commandType = CommandTypeToCommand.FindCommandInfoByCode(type, version).Key;
            return commandType;
        }

        public static ICommand ToCommand(this CommandType? commandType, int version)
        {
            ICommand command = null;
            var val = commandType.ToInt();
            Type type = CommandTypeToCommand.FindCommandInfoByCode(val, version).Value;
            command = Activator.CreateInstance(type) as ICommand;
            return command;
        }
    }
}