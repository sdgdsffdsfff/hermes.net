
namespace Arch.CMessaging.Client.Impl.Consumer.Models
{
    public class PhysicalServer
    {
        public PhysicalServer()
        {
            Mark = true;
        }

        public string ServerName { get; set; }
        public string ServerIP { get; set; }
        public string ServerDomainName { get; set; }
        /// <summary>
        /// 标记 true:同步时存在此服务器 false:未同步到此服务器
        /// </summary>
        public bool Mark { get; set; }

        public override bool Equals(object obj)
        {
            var server = obj as PhysicalServer;
            if (server == null) return false;
            return server.ServerDomainName == ServerDomainName
                && server.ServerIP == ServerIP
                && server.ServerName == ServerName;
        }

        public override int GetHashCode()
        {
            return string.IsNullOrEmpty(this.ServerName)
                ? base.GetHashCode()
                : this.ServerName.GetHashCode();
        }
    }
}
