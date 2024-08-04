
// Type: AsyncNet.Core.Extensions.SslStreamExtensions
// Assembly: AsyncNet.Core, Version=1.2.4.0, Culture=neutral, PublicKeyToken=3f4900b9c5b8c297
// MVID: F4D263BE-F9A6-48FD-A5F5-96E8D9DF4B57
// Assembly location: C:\Users\pc2020\AppData\Local\Temp\Sibazez\907aa7090e\lib\netstandard2.0\AsyncNet.Core.dll
// XML documentation location: C:\Users\pc2020\AppData\Local\Temp\Sibazez\907aa7090e\lib\netstandard2.0\AsyncNet.Core.xml

using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;


namespace AsyncNetStandard.Core.Extensions
{
  public static class SslStreamExtensions
  {
    public static async Task AuthenticateAsServerWithCancellationAsync(
      this SslStream stream,
      X509Certificate serverCertificate,
      CancellationToken cancellationToken)
    {
      TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>();
      CancellationTokenRegistration tokenRegistration = cancellationToken.Register((Action) (() => taskCompletionSource.TrySetCanceled()), false);
      try
      {
        await (await Task.WhenAny(stream.AuthenticateAsServerAsync(serverCertificate), (Task) taskCompletionSource.Task).ConfigureAwait(false)).ConfigureAwait(false);
      }
      finally
      {
        tokenRegistration.Dispose();
      }
      tokenRegistration = new CancellationTokenRegistration();
    }

    public static async Task AuthenticateAsServerWithCancellationAsync(
      this SslStream stream,
      X509Certificate serverCertificate,
      bool clientCertificateRequired,
      SslProtocols enabledSslProtocols,
      bool checkCertificateRevocation,
      CancellationToken cancellationToken)
    {
      TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>();
      CancellationTokenRegistration tokenRegistration = cancellationToken.Register((Action) (() => taskCompletionSource.TrySetCanceled()), false);
      try
      {
        await (await Task.WhenAny(stream.AuthenticateAsServerAsync(serverCertificate, clientCertificateRequired, enabledSslProtocols, checkCertificateRevocation), (Task) taskCompletionSource.Task).ConfigureAwait(false)).ConfigureAwait(false);
      }
      finally
      {
        tokenRegistration.Dispose();
      }
      tokenRegistration = new CancellationTokenRegistration();
    }

    public static async Task AuthenticateAsClientWithCancellationAsync(
      this SslStream stream,
      string targetHost,
      CancellationToken cancellationToken)
    {
      TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>();
      CancellationTokenRegistration tokenRegistration = cancellationToken.Register((Action) (() => taskCompletionSource.TrySetCanceled()), false);
      try
      {
        await (await Task.WhenAny(stream.AuthenticateAsClientAsync(targetHost), (Task) taskCompletionSource.Task).ConfigureAwait(false)).ConfigureAwait(false);
      }
      finally
      {
        tokenRegistration.Dispose();
      }
      tokenRegistration = new CancellationTokenRegistration();
    }

    public static async Task AuthenticateAsClientWithCancellationAsync(
      this SslStream stream,
      string targetHost,
      X509CertificateCollection clientCertificates,
      SslProtocols enabledSslProtocols,
      bool checkCertificateRevocation,
      CancellationToken cancellationToken)
    {
      TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>();
      CancellationTokenRegistration tokenRegistration = cancellationToken.Register((Action) (() => taskCompletionSource.TrySetCanceled()), false);
      try
      {
        await (await Task.WhenAny(stream.AuthenticateAsClientAsync(targetHost, clientCertificates, enabledSslProtocols, checkCertificateRevocation), (Task) taskCompletionSource.Task).ConfigureAwait(false)).ConfigureAwait(false);
      }
      finally
      {
        tokenRegistration.Dispose();
      }
      tokenRegistration = new CancellationTokenRegistration();
    }
  }
}
