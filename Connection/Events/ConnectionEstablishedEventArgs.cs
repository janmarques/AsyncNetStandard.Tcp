
// Type: AsyncNetStandard.Tcp.Connection.Events.ConnectionEstablishedEventArgsusing AsyncNetStandard.Tcp.Remote;
using AsyncNetStandard.Tcp.Remote;
using System;


namespace AsyncNetStandard.Tcp.Connection.Events
{
  public class ConnectionEstablishedEventArgs : EventArgs
  {
    public ConnectionEstablishedEventArgs(IRemoteTcpPeer remoteTcpPeer)
    {
      this.RemoteTcpPeer = remoteTcpPeer;
    }

    public IRemoteTcpPeer RemoteTcpPeer { get; }
  }
}
