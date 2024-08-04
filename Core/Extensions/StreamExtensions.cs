
// Type: AsyncNet.Core.Extensions.StreamExtensions
// Assembly: AsyncNet.Core, Version=1.2.4.0, Culture=neutral, PublicKeyToken=3f4900b9c5b8c297
// MVID: F4D263BE-F9A6-48FD-A5F5-96E8D9DF4B57
// Assembly location: C:\Users\pc2020\AppData\Local\Temp\Sibazez\907aa7090e\lib\netstandard2.0\AsyncNet.Core.dll
// XML documentation location: C:\Users\pc2020\AppData\Local\Temp\Sibazez\907aa7090e\lib\netstandard2.0\AsyncNet.Core.xml

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace AsyncNetStandard.Core.Extensions
{
  public static class StreamExtensions
  {
    public static async Task<int> ReadWithRealCancellationAsync(
      this Stream stream,
      byte[] buffer,
      int offset,
      int count,
      CancellationToken cancellationToken)
    {
      TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>();
      int num;
      using (cancellationToken.Register((Action) (() => taskCompletionSource.TrySetCanceled()), false))
        num = await (await Task.WhenAny<int>(stream.ReadAsync(buffer, offset, count), taskCompletionSource.Task).ConfigureAwait(false)).ConfigureAwait(false);
      return num;
    }

    public static async Task WriteWithRealCancellationAsync(
      this Stream stream,
      byte[] buffer,
      int offset,
      int count,
      CancellationToken cancellationToken)
    {
      TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>();
      CancellationTokenRegistration tokenRegistration = cancellationToken.Register((Action) (() => taskCompletionSource.TrySetCanceled()), false);
      try
      {
        await (await Task.WhenAny(stream.WriteAsync(buffer, offset, count), (Task) taskCompletionSource.Task).ConfigureAwait(false)).ConfigureAwait(false);
      }
      finally
      {
        tokenRegistration.Dispose();
      }
      tokenRegistration = new CancellationTokenRegistration();
    }

    public static async Task<bool> ReadUntilBufferIsFullAsync(
      this Stream stream,
      byte[] buffer,
      int offset,
      int count)
    {
      int num1 = 0;
      while (num1 < count)
      {
        int num = num1;
        num1 = num + await stream.ReadAsync(buffer, offset + num1, count - num1).ConfigureAwait(false);
        if (num1 < 1)
          return false;
      }
      return true;
    }

    public static async Task<bool> ReadUntilBufferIsFullAsync(
      this Stream stream,
      byte[] buffer,
      int offset,
      int count,
      CancellationToken cancellationToken)
    {
      int num1 = 0;
      while (num1 < count)
      {
        int num = num1;
        num1 = num + await stream.ReadWithRealCancellationAsync(buffer, offset + num1, count - num1, cancellationToken).ConfigureAwait(false);
        if (num1 < 1)
          return false;
      }
      return true;
    }
  }
}
