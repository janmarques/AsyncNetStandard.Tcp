
// Type: AsyncNet.Core.Extensions.DnsExtensions
// Assembly: AsyncNet.Core, Version=1.2.4.0, Culture=neutral, PublicKeyToken=3f4900b9c5b8c297
// MVID: F4D263BE-F9A6-48FD-A5F5-96E8D9DF4B57
// Assembly location: C:\Users\pc2020\AppData\Local\Temp\Sibazez\907aa7090e\lib\netstandard2.0\AsyncNet.Core.dll
// XML documentation location: C:\Users\pc2020\AppData\Local\Temp\Sibazez\907aa7090e\lib\netstandard2.0\AsyncNet.Core.xml

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;


namespace AsyncNetStandard.Core.Extensions
{
  public static class DnsExtensions
  {
    public static async Task<IPAddress[]> GetHostAddressesWithCancellationTokenAsync(
      string hostNameOrAddress,
      CancellationToken cancellationToken)
    {
      TaskCompletionSource<IPAddress[]> taskCompletionSource = new TaskCompletionSource<IPAddress[]>();
      IPAddress[] cancellationTokenAsync;
      using (cancellationToken.Register((Action) (() => taskCompletionSource.TrySetCanceled()), false))
        cancellationTokenAsync = await (await Task.WhenAny<IPAddress[]>(Dns.GetHostAddressesAsync(hostNameOrAddress), taskCompletionSource.Task).ConfigureAwait(false)).ConfigureAwait(false);
      return cancellationTokenAsync;
    }
  }
}
