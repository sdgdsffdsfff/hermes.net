using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Client.Net.Core.Buffer;

namespace Arch.CMessaging.Client.Transport.Command.Parser
{
    public class DefaultCommandParser : ICommandParser
    {
        #region ICommandParser Members

        public ICommand Parse(IoBuffer buf)
        {
            var header = new Header();
            header.Parse(buf);
            var command = header.CommandType.ToCommand(header.Version);
            if (command != null)
            {
                try
                {
                    command.Parse(buf, header);
                    return command;
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(
                        string.Format("Error to parse Command [{0}]", command.GetType().Name), ex);
                }
            }
            else throw new ArgumentException(string.Format("Error to create command from code [{0}]",
                header.CommandType == null ? -1 : header.CommandType.ToInt()));
        }

        #endregion
    }
}
