
// Type: AsyncNet.Tcp.Server.Events.TcpServerStartedEventArgsusing System;
using System;
using System.Net;


namespace AsyncNetStandard.Tcp.Server.Events
{
  public class TcpServerStartedEventArgs : EventArgs
  {
    public IPAddress ServerAddress { get; set; }

    public int ServerPort { get; set; }
  }
}
