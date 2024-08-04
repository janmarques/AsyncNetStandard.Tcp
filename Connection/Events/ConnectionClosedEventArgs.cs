
// Type: AsyncNet.Tcp.Connection.Events.ConnectionClosedEventArgsusing AsyncNetStandard.Tcp.Remote;
using AsyncNetStandard.Tcp.Remote;
using System;


namespace AsyncNetStandard.Tcp.Connection.Events
{
  public class ConnectionClosedEventArgs : EventArgs
  {
    public ConnectionClosedEventArgs(IRemoteTcpPeer remoteTcpPeer)
    {
      this.RemoteTcpPeer = remoteTcpPeer;
    }

    public IRemoteTcpPeer RemoteTcpPeer { get; }

    public ConnectionCloseReason ConnectionCloseReason { get; set; }

    public Exception ConnectionCloseException { get; set; }
  }
}
