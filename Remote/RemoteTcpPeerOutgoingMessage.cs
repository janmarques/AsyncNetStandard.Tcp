
// Type: AsyncNet.Tcp.Remote.RemoteTcpPeerOutgoingMessageusing AsyncNetStandard.Core;
using AsyncNetStandard.Core;
using System.Threading;
using System.Threading.Tasks;


namespace AsyncNetStandard.Tcp.Remote
{
  public class RemoteTcpPeerOutgoingMessage
  {
    public RemoteTcpPeerOutgoingMessage(
      RemoteTcpPeer remoteTcpPeer,
      AsyncNetBuffer buffer,
      CancellationToken cancellationToken)
    {
      this.RemoteTcpPeer = remoteTcpPeer;
      this.Buffer = buffer;
      this.CancellationToken = cancellationToken;
      this.SendTaskCompletionSource = new TaskCompletionSource<bool>();
    }

    public RemoteTcpPeer RemoteTcpPeer { get; }

    public AsyncNetBuffer Buffer { get; }

    public CancellationToken CancellationToken { get; }

    public TaskCompletionSource<bool> SendTaskCompletionSource { get; }
  }
}
