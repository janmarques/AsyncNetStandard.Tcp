
// Type: AsyncNet.Tcp.Remote.Events.RemoteTcpPeerExceptionEventArgsusing AsyncNetStandard.Core.Events;
using AsyncNetStandard.Core.Events;
using System;


namespace AsyncNetStandard.Tcp.Remote.Events
{
  public class RemoteTcpPeerExceptionEventArgs : ExceptionEventArgs
  {
    public RemoteTcpPeerExceptionEventArgs(IRemoteTcpPeer remoteTcpPeer, Exception ex)
      : base(ex)
    {
      this.RemoteTcpPeer = remoteTcpPeer;
    }

    public IRemoteTcpPeer RemoteTcpPeer { get; }
  }
}
