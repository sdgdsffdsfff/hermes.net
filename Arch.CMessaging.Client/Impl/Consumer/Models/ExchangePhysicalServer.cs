
namespace Arch.CMessaging.Client.Impl.Consumer.Models
{
    /// <summary>
    /// 继承于PhysicalServer,增加了Weight ExchangeName,
    /// </summary>
    public class ExchangePhysicalServer:PhysicalServer
    {
        public int Weight { get; set; }
        public string ExchangeName { get; set; }
    }
}
