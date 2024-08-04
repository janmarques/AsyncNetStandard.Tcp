
// Type: AsyncNet.Tcp.Extensions.TcpListenerExtensionsusing System;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


namespace AsyncNetStandard.Tcp.Extensions
{
  public static class TcpListenerExtensions
  {
    public static async Task<TcpClient> AcceptTcpClientWithCancellationTokenAsync(
      this TcpListener tcpListener,
      CancellationToken cancellationToken)
    {
      TaskCompletionSource<TcpClient> taskCompletionSource = new TaskCompletionSource<TcpClient>();
      TcpClient tcpClient;
      using (cancellationToken.Register((Action) (() => taskCompletionSource.TrySetCanceled()), false))
        tcpClient = await (await Task.WhenAny<TcpClient>(tcpListener.AcceptTcpClientAsync(), taskCompletionSource.Task).ConfigureAwait(false)).ConfigureAwait(false);
      return tcpClient;
    }
  }
}
