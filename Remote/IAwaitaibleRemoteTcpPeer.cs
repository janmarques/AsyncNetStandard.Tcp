
// Type: AsyncNetStandard.Tcp.Remote.IAwaitaibleRemoteTcpPeerusing AsyncNetStandard.Tcp.Remote.Events;
using AsyncNetStandard.Tcp.Remote.Events;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace AsyncNetStandard.Tcp.Remote
{
  public interface IAwaitaibleRemoteTcpPeer : IDisposable
  {
    /// <summary>Fires when frame buffer is full</summary>
    event EventHandler<AddingTcpFrameToFrameBufferFailedEventArgs> AddingTcpFrameToFrameBufferFailed;

    /// <summary>Underlying remote peer. Use this for sending data</summary>
    IRemoteTcpPeer RemoteTcpPeer { get; }

    /// <summary>Reads tcp frame in an asynchronous manner</summary>
    /// <returns><see cref="T:System.Threading.Tasks.Task`1" /> which returns tcp frame bytes</returns>
    Task<byte[]> ReadFrameAsync();

    /// <summary>Reads tcp frame in an asynchronous manner</summary>
    /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
    /// <returns><see cref="T:System.Threading.Tasks.Task`1" /> which returns tcp frame bytes</returns>
    Task<byte[]> ReadFrameAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Reads all bytes from tcp stream until peer disconnects in an asynchronous manner
    /// </summary>
    /// <returns><see cref="T:System.Threading.Tasks.Task`1" /> which returns byte array</returns>
    Task<byte[]> ReadAllBytesAsync();

    /// <summary>
    /// Reads all bytes from tcp stream until peer disconnects in an asynchronous manner
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
    /// <returns><see cref="T:System.Threading.Tasks.Task`1" /> which returns byte array</returns>
    Task<byte[]> ReadAllBytesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Reads all frames until peer disconnects in an asynchronous manner
    /// </summary>
    /// <returns><see cref="T:System.Threading.Tasks.Task`1" /> which returns list of frames</returns>
    Task<IEnumerable<byte[]>> ReadAllFramesAsync();

    /// <summary>
    /// Reads all frames until peer disconnects in an asynchronous manner
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
    /// <returns><see cref="T:System.Threading.Tasks.Task`1" /> which returns list of frames</returns>
    Task<IEnumerable<byte[]>> ReadAllFramesAsync(CancellationToken cancellationToken);
  }
}
