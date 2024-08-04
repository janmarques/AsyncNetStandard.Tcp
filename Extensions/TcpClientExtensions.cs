
// Type: AsyncNetStandard.Tcp.Extensions.TcpClientExtensionsusing System;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


namespace AsyncNetStandard.Tcp.Extensions
{
  public static class TcpClientExtensions
  {
    public static async Task ConnectWithCancellationTokenAsync(
      this TcpClient tcpClient,
      IPAddress[] addresses,
      int port,
      CancellationToken cancellationToken)
    {
      TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
      CancellationTokenRegistration tokenRegistration = cancellationToken.Register((Action) (() => taskCompletionSource.TrySetCanceled()), false);
      try
      {
        await (await Task.WhenAny(tcpClient.ConnectAsync(addresses, port), (Task) taskCompletionSource.Task).ConfigureAwait(false)).ConfigureAwait(false);
      }
      finally
      {
        tokenRegistration.Dispose();
      }
      tokenRegistration = new CancellationTokenRegistration();
    }

    public static async Task ConnectWithCancellationTokenAsync(
      this TcpClient tcpClient,
      string hostname,
      int port,
      CancellationToken cancellationToken)
    {
      TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
      CancellationTokenRegistration tokenRegistration = cancellationToken.Register((Action) (() => taskCompletionSource.TrySetCanceled()), false);
      try
      {
        await (await Task.WhenAny(tcpClient.ConnectAsync(hostname, port), (Task) taskCompletionSource.Task).ConfigureAwait(false)).ConfigureAwait(false);
      }
      finally
      {
        tokenRegistration.Dispose();
      }
      tokenRegistration = new CancellationTokenRegistration();
    }
  }
}
