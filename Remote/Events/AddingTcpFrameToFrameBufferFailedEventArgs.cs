
// Type: AsyncNet.Tcp.Remote.Events.AddingTcpFrameToFrameBufferFailedEventArgsusing System;


using System;

namespace AsyncNetStandard.Tcp.Remote.Events
{
  public class AddingTcpFrameToFrameBufferFailedEventArgs : EventArgs
  {
    public AddingTcpFrameToFrameBufferFailedEventArgs(
      IAwaitaibleRemoteTcpPeer awaitaibleRemoteTcpPeer,
      byte[] frameData)
    {
      this.AwaitaibleRemoteTcpPeer = awaitaibleRemoteTcpPeer;
      this.FrameData = frameData;
    }

    public IAwaitaibleRemoteTcpPeer AwaitaibleRemoteTcpPeer { get; set; }

    public byte[] FrameData { get; set; }
  }
}
