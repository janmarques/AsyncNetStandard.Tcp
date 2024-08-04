
// Type: AsyncNetStandard.Tcp.Client.IAwaitaibleAsyncNetTcpClientusing AsyncNetStandard.Tcp.Remote;
using AsyncNetStandard.Tcp.Remote;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace AsyncNetStandard.Tcp.Client
{
  public interface IAwaitaibleAsyncNetTcpClient : IDisposable
  {
    AsyncNetTcpClient Client { get; }

    Task<IAwaitaibleRemoteTcpPeer> ConnectAsync();

    Task<IAwaitaibleRemoteTcpPeer> ConnectAsync(CancellationToken cancellationToken);
  }
}
